using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace Bookify.API.Services
{
    public class BlobStorageService(
        BlobServiceClient blobServiceClient,
        IConfiguration configuration
    ) : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient = blobServiceClient;
        private readonly string _audioContainerName =
            configuration.GetValue<string>("Storage:AudioContainer") ?? "audiobooks";
        private readonly string _coverContainerName =
            configuration.GetValue<string>("Storage:CoverContainer") ?? "covers";

        private async Task EnsureContainersCreatedAsync()
        {
            var audioContainer = _blobServiceClient.GetBlobContainerClient(_audioContainerName);
            await audioContainer.CreateIfNotExistsAsync(PublicAccessType.None);

            var coverContainer = _blobServiceClient.GetBlobContainerClient(_coverContainerName);
            await coverContainer.CreateIfNotExistsAsync(PublicAccessType.Blob); // Covers can be publicly readable
        }

        public async Task<string> UploadAudioAsync(
            string fileName,
            Stream content,
            string contentType
        )
        {
            await EnsureContainersCreatedAsync();
            var containerClient = _blobServiceClient.GetBlobContainerClient(_audioContainerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            await blobClient.UploadAsync(
                content,
                new BlobHttpHeaders { ContentType = contentType }
            );

            return fileName;
        }

        public async Task<string> UploadCoverAsync(
            string fileName,
            Stream content,
            string contentType
        )
        {
            await EnsureContainersCreatedAsync();
            var containerClient = _blobServiceClient.GetBlobContainerClient(_coverContainerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            await blobClient.UploadAsync(
                content,
                new BlobHttpHeaders { ContentType = contentType }
            );

            return blobClient.Uri?.ToString() ?? string.Empty;
        }

        public string GetAudioSasUrl(string blobName, TimeSpan expiresIn)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_audioContainerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!blobClient.CanGenerateSasUri)
            {
                throw new InvalidOperationException(
                    "BlobClient cannot generate SAS Uri. Ensure connection string has rights."
                );
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _audioContainerName,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.Add(expiresIn),
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);
            var sasUri = blobClient.GenerateSasUri(sasBuilder);

            return sasUri?.ToString() ?? string.Empty;
        }

        public string GetCoverUrl(string blobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_coverContainerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            return blobClient.Uri?.ToString() ?? string.Empty;
        }
    }
}
