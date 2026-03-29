namespace Bookify.API.Services
{
    public interface IBlobStorageService
    {
        Task<string> UploadAudioAsync(string fileName, Stream content, string contentType);
        Task<string> UploadCoverAsync(string fileName, Stream content, string contentType);
        string GetAudioSasUrl(string blobName, TimeSpan expiresIn);
        string GetCoverUrl(string blobName);
    }
}
