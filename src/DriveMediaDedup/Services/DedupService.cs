using System.Security.Cryptography;

namespace DriveMediaDedup.Services;

public class DedupService
{
    private readonly HashSet<string> _knownHashes = new();
    private readonly string _organizedFolder;
    private readonly string _repeatedFolder;
    private readonly string _videosFolder;

    public DedupService(string organizedFolder, string repeatedFolder, string videosFolder)
    {
        _organizedFolder = organizedFolder;
        _repeatedFolder = repeatedFolder;
        _videosFolder = videosFolder;

        // Cria as pastas se não existirem
        EnsureFolderExists(_organizedFolder);
        EnsureFolderExists(_repeatedFolder);
        EnsureFolderExists(_videosFolder);
    }

    // Processa cada arquivo baixado
    public (bool IsDuplicate, string DestinationPath) ProcessFile(string localPath, bool isImage)
    {
        // Se for vídeo, move direto para a pasta de vídeos (sem dedup)
        if (!isImage)
        {
            var videoPath = Path.Combine(_videosFolder, Path.GetFileName(localPath));
            File.Move(localPath, videoPath, overwrite: true);
            return (false, videoPath);
        }

        // Calcula o hash da imagem
        var hash = CalculateFileHash(localPath);

        // Verifica se o hash já existe
        if (_knownHashes.Contains(hash))
        {
            // É repetida → move para a pasta "repetidas"
            var repeatedPath = Path.Combine(_repeatedFolder, Path.GetFileName(localPath));
            File.Move(localPath, repeatedPath, overwrite: true);
            return (true, repeatedPath);
        }
        else
        {
            // É única → registra o hash e move para "organizadas"
            _knownHashes.Add(hash);
            var organizedPath = Path.Combine(_organizedFolder, Path.GetFileName(localPath));
            File.Move(localPath, organizedPath, overwrite: true);
            return (false, organizedPath);
        }
    }

    // Calcula o hash SHA256 do arquivo
    private static string CalculateFileHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hashBytes = SHA256.HashData(stream);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    private static void EnsureFolderExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}