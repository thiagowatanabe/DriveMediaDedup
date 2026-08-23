using DriveMediaDedup.Models;

namespace DriveMediaDedup.Services;

public class DownloadService
{
    private readonly GoogleDriveService _driveService;
    private readonly string _tempFolder;

    public DownloadService(GoogleDriveService driveService, string tempFolder)
    {
        _driveService = driveService;
        _tempFolder = tempFolder;

        // Cria a pasta temporária se não existir
        if (!Directory.Exists(_tempFolder))
        {
            Directory.CreateDirectory(_tempFolder);
        }
    }

    // Baixa um arquivo do Drive para a pasta temporária
    public async Task<string> DownloadAsync(MediaFile mediaFile)
    {
        var localPath = Path.Combine(_tempFolder, mediaFile.Name);

        // Se já existe um arquivo com o mesmo nome, adiciona um número
        if (File.Exists(localPath))
        {
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(mediaFile.Name);
            var extension = Path.GetExtension(mediaFile.Name);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            localPath = Path.Combine(_tempFolder, $"{fileNameWithoutExt}_{timestamp}{extension}");
        }

        await _driveService.DownloadFileAsync(mediaFile.Id, localPath);

        return localPath;
    }
}