using System.IO.Compression;

namespace DriveMediaDedup.Services;

public class ZipService
{
    private readonly string _outputFolder;

    public ZipService(string outputFolder)
    {
        _outputFolder = outputFolder;
        EnsureFolderExists(_outputFolder);
    }

    // Zipa apenas organizadas e videos (SEM repetidas) para subir ao S3
    public string ZipForS3(string baseDownloadsPath)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var zipFileName = $"DriveMediaDedup_{timestamp}.zip";
        var zipPath = Path.Combine(_outputFolder, zipFileName);

        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);

        // Apenas organizadas e videos — repetidas ficam de fora
        AddFolderToZip(archive, baseDownloadsPath, "organizadas");
        AddFolderToZip(archive, baseDownloadsPath, "videos");

        return zipPath;  // ← FALTAVA ESTA LINHA
    }

    private static void AddFolderToZip(ZipArchive archive, string basePath, string folderName)
    {
        var folderPath = Path.Combine(basePath, folderName);

        if (!Directory.Exists(folderPath))
            return;

        var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var entryName = $"{folderName}/{Path.GetFileName(file)}";
            archive.CreateEntryFromFile(file, entryName);
        }
    }

    private static void EnsureFolderExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}