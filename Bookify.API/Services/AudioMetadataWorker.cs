using System.IO;
using Azure.Storage.Blobs;
using Bookify.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookify.API.Services
{
    public class AudioMetadataWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<AudioMetadataWorker> _logger;
        private readonly string _audioContainerName;

        public AudioMetadataWorker(
            IServiceProvider serviceProvider,
            BlobServiceClient blobServiceClient,
            ILogger<AudioMetadataWorker> logger,
            IConfiguration config
        )
        {
            _serviceProvider = serviceProvider;
            _blobServiceClient = blobServiceClient;
            _logger = logger;
            _audioContainerName = config.GetValue<string>("Storage:AudioContainer") ?? "audiobooks";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var chaptersToProcess = await dbContext
                        .Chapters.Where(c => c.DurationSeconds == 0)
                        .Take(10)
                        .ToListAsync(stoppingToken);

                    foreach (var chapter in chaptersToProcess)
                    {
                        var containerClient = _blobServiceClient.GetBlobContainerClient(
                            _audioContainerName
                        );
                        var blobClient = containerClient.GetBlobClient(chapter.AudioBlobName);

                        if (await blobClient.ExistsAsync(stoppingToken))
                        {
                            var tempFile = Path.GetTempFileName() + ".mp3";
                            try
                            {
                                await blobClient.DownloadToAsync(tempFile, stoppingToken);

                                using (var tfile = TagLib.File.Create(tempFile))
                                {
                                    chapter.DurationSeconds =
                                        tfile.Properties?.Duration.TotalSeconds ?? 0;
                                }

                                _logger.LogInformation(
                                    $"Processed duration for chapter {chapter.Id}: {chapter.DurationSeconds}s"
                                );
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(
                                    ex,
                                    $"Error processing metadata for chapter {chapter.Id}"
                                );
                                // Set to -1 to avoid infinite retries on corrupted files
                                chapter.DurationSeconds = -1;
                            }
                            finally
                            {
                                if (File.Exists(tempFile))
                                    File.Delete(tempFile);
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"Blob {chapter.AudioBlobName} not found.");
                            chapter.DurationSeconds = -1;
                        }
                    }

                    if (chaptersToProcess.Any())
                    {
                        await dbContext.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in AudioMetadataWorker.");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
