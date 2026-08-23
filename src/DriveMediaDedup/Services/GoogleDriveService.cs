using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using DriveMediaDedup.Models;

namespace DriveMediaDedup.Services;

public class GoogleDriveService
{
    private readonly DriveService _driveService;

    public GoogleDriveService(string credentialsPath, string applicationName)
    {
        // ⚠️ MUDOU: agora usa Drive (não DriveReadonly) para permitir exclusão
        string[] scopes = { DriveService.Scope.Drive };

        var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.FromFile(credentialsPath).Secrets,
            scopes,
            "user",
            CancellationToken.None,
            new FileDataStore(Path.Combine(AppContext.BaseDirectory, "token.json"), true)).Result;

        _driveService = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = applicationName
        });
    }

    // Busca todos os arquivos de imagem e vídeo no Drive
    public async Task<List<MediaFile>> SearchMediaFilesAsync()
    {
        var files = new List<MediaFile>();

        var request = _driveService.Files.List();
        request.PageSize = 100;
        request.Fields = "nextPageToken, files(id, name, mimeType, size)";
        request.Q = "mimeType contains 'image/' or mimeType contains 'video/'";

        do
        {
            var result = await request.ExecuteAsync();

            foreach (var file in result.Files)
            {
                files.Add(new MediaFile
                {
                    Id = file.Id,
                    Name = file.Name,
                    MimeType = file.MimeType,
                    Size = file.Size ?? 0
                });
            }

            request.PageToken = result.NextPageToken;
        } while (!string.IsNullOrEmpty(request.PageToken));

        return files;
    }

    // Faz o download de um arquivo específico
    public async Task DownloadFileAsync(string fileId, string localPath)
    {
        var request = _driveService.Files.Get(fileId);

        using var stream = new FileStream(localPath, FileMode.Create, FileAccess.Write);
        await request.DownloadAsync(stream);
    }

    // 🆕 Deleta um arquivo do Drive
    public async Task DeleteFileAsync(string fileId)
    {
        await _driveService.Files.Delete(fileId).ExecuteAsync();
    }
}