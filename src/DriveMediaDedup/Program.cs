using Microsoft.Extensions.Configuration;
using DriveMediaDedup.Services;
using DriveMediaDedup.Models;  

// Carrega as configurações
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var driveConfig = config.GetSection("GoogleDrive");
var foldersConfig = config.GetSection("Folders");
var gitHubConfig = config.GetSection("GitHub");
var s3Config = config.GetSection("S3");

// Caminho base dos downloads — SEMPRE na pasta de output (bin/Debug/net10.0/)
var downloadsPath = Path.Combine(AppContext.BaseDirectory, "downloads");

// Lê as configurações de pastas — caminhos absolutos baseados no downloadsPath
var tempFolder = Path.Combine(downloadsPath, "temp");
var organizedFolder = Path.Combine(downloadsPath, "organizadas");
var repeatedFolder = Path.Combine(downloadsPath, "repetidas");
var videosFolder = Path.Combine(downloadsPath, "videos");

Console.WriteLine("=== DriveMediaDedup ===");
Console.WriteLine();

// Cria os serviços
var credentialsPath = Path.Combine(AppContext.BaseDirectory, driveConfig["CredentialsPath"]!);

if (!File.Exists(credentialsPath))
{
    Console.WriteLine($"ERRO: credentials.json não encontrado em {credentialsPath}");
    return;
}

var driveService = new GoogleDriveService(credentialsPath, driveConfig["ApplicationName"]!);
var downloadService = new DownloadService(driveService, tempFolder);
var dedupService = new DedupService(organizedFolder, repeatedFolder, videosFolder);

// === ETAPA 1: Buscar arquivos no Drive ===
Console.WriteLine("1. Buscando arquivos de imagem e vídeo no Google Drive...");
var mediaFiles = await driveService.SearchMediaFilesAsync();
Console.WriteLine($"   Encontrados: {mediaFiles.Count} arquivos");
Console.WriteLine($"   Imagens: {mediaFiles.Count(f => f.IsImage)}");
Console.WriteLine($"   Vídeos: {mediaFiles.Count(f => f.IsVideo)}");
Console.WriteLine();

if (mediaFiles.Count == 0)
{
    Console.WriteLine("Nenhum arquivo encontrado. Encerrando...");
    return;
}

// === ETAPA 2: Download e deduplicação ===
Console.WriteLine("2. Baixando e organizando arquivos...");
var totalUnique = 0;
var totalDuplicates = 0;
var totalVideos = 0;
var processedFiles = new List<(MediaFile File, bool Success)>();

foreach (var file in mediaFiles)
{
    Console.Write($"   Baixando: {file.Name}... ");

    try
    {
        var localPath = await downloadService.DownloadAsync(file);
        var result = dedupService.ProcessFile(localPath, file.IsImage);

        if (file.IsVideo)
        {
            totalVideos++;
            Console.WriteLine("Vídeo -> videos/");
        }
        else if (result.IsDuplicate)
        {
            totalDuplicates++;
            Console.WriteLine("REPETIDA -> repetidas/");
        }
        else
        {
            totalUnique++;
            Console.WriteLine("ÚNICA -> organizadas/");
        }

        processedFiles.Add((file, true));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERRO: {ex.Message}");
        processedFiles.Add((file, false));
    }
}

// === ETAPA 3: Zipar os arquivos (apenas únicas e vídeos — sem repetidas) ===
Console.WriteLine();
Console.WriteLine("3. Compactando arquivos (organizadas + vídeos)...");

var zipOutputFolder = Path.Combine(downloadsPath, "zips");
var zipService = new ZipService(zipOutputFolder);
var zipPath = zipService.ZipForS3(downloadsPath);

Console.WriteLine($"   Arquivo zip criado: {Path.GetFileName(zipPath)}");
Console.WriteLine($"   Tamanho: {new FileInfo(zipPath).Length / 1024 / 1024} MB");
Console.WriteLine($"   (Imagens duplicadas NÃO incluídas no zip)");
Console.WriteLine();

// === ETAPA 4: Upload para S3 ===
Console.WriteLine("4. Enviando para Amazon S3...");
var s3Service = new S3Service(
    s3Config["AccessKey"]!,
    s3Config["SecretKey"]!,
    s3Config["BucketName"]!,
    s3Config["Region"]!);

var s3UploadSuccess = false;

try
{
    var s3Url = await s3Service.UploadFileAsync(zipPath);
    Console.WriteLine($"   Upload concluído: {s3Url}");
    s3UploadSuccess = true;
}
catch (Exception ex)
{
    Console.WriteLine($"   ERRO no upload S3: {ex.Message}");
}
Console.WriteLine();

// === ETAPA 5: Deletar arquivos do Drive ===
Console.WriteLine("5. Deletando arquivos do Google Drive...");
var deletedCount = 0;
var skippedCount = 0;

foreach (var (file, success) in processedFiles)
{
    if (!success)
        continue;

    try
    {
        await driveService.DeleteFileAsync(file.Id);
        deletedCount++;
    }
    catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.Forbidden)
    {
        // Arquivo compartilhado — não temos permissão para deletar
        skippedCount++;
        Console.WriteLine($"   ⏭️  Pulado (sem permissão): {file.Name}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"   Erro ao deletar {file.Name}: {ex.Message}");
    }
}

Console.WriteLine($"   Deletados: {deletedCount}");
Console.WriteLine($"   Pulados (sem permissão): {skippedCount}");
Console.WriteLine();

// === ETAPA 6: Limpeza dos arquivos locais ===
Console.WriteLine("6. Limpando arquivos locais...");

if (s3UploadSuccess)
{
    try
    {
        // Deleta todas as pastas de downloads
        if (Directory.Exists(Path.Combine(downloadsPath, "temp")))
        {
            Directory.Delete(Path.Combine(downloadsPath, "temp"), recursive: true);
            Console.WriteLine("   ✓ Pasta temp/ excluída");
        }

        if (Directory.Exists(Path.Combine(downloadsPath, "organizadas")))
        {
            Directory.Delete(Path.Combine(downloadsPath, "organizadas"), recursive: true);
            Console.WriteLine("   ✓ Pasta organizadas/ excluída");
        }

        if (Directory.Exists(Path.Combine(downloadsPath, "repetidas")))
        {
            Directory.Delete(Path.Combine(downloadsPath, "repetidas"), recursive: true);
            Console.WriteLine("   ✓ Pasta repetidas/ excluída");
        }

        if (Directory.Exists(Path.Combine(downloadsPath, "videos")))
        {
            Directory.Delete(Path.Combine(downloadsPath, "videos"), recursive: true);
            Console.WriteLine("   ✓ Pasta videos/ excluída");
        }

        // Deleta o arquivo zip
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
            Console.WriteLine("   ✓ Arquivo zip excluído");
        }

        // Deleta a pasta zips se estiver vazia
        if (Directory.Exists(zipOutputFolder))
        {
            Directory.Delete(zipOutputFolder, recursive: true);
        }

        Console.WriteLine("   Limpeza concluída!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"   Erro na limpeza: {ex.Message}");
    }
}
else
{
    Console.WriteLine("   ⚠️ Upload S3 falhou — mantendo arquivos locais como backup");
}

Console.WriteLine();

// === RELATÓRIO FINAL ===
Console.WriteLine("=== RELATÓRIO FINAL ===");
Console.WriteLine($"Total baixado:       {mediaFiles.Count}");
Console.WriteLine($"Imagens únicas:      {totalUnique}");
Console.WriteLine($"Imagens repetidas:    {totalDuplicates} (mantidas localmente, não foram para o S3)");
Console.WriteLine($"Vídeos:              {totalVideos}");
Console.WriteLine($"Arquivo zip:         {Path.GetFileName(zipPath)} (organizadas + vídeos)");
Console.WriteLine($"Upload S3:           {(s3UploadSuccess ? "Concluído" : "Falhou")}");
Console.WriteLine($"Deletados do Drive:   {deletedCount}");
Console.WriteLine($"Arquivos locais:      {(s3UploadSuccess ? "Excluídos" : "Mantidos (backup)")}");
Console.WriteLine();

// === ETAPA 7: Criar repositório no GitHub ===
Console.WriteLine("7. Criando repositório no GitHub...");
var gitHubService = new GitHubService(
    gitHubConfig["Token"]!,
    gitHubConfig["Owner"]!,
    gitHubConfig["RepoName"]!,
    gitHubConfig["Description"]!);

var repoExists = await gitHubService.RepositoryExistsAsync();

if (!repoExists)
{
    var cloneUrl = await gitHubService.CreateRepositoryAsync();
    Console.WriteLine($"   Repositório criado: {cloneUrl}");
}
else
{
    Console.WriteLine("   Repositório já existe — pulando criação.");
}

Console.WriteLine();
Console.WriteLine("Concluído!");

// === FUNÇÃO AUXILIAR: Deletar pasta recursivamente ===
static void DeleteFolderIfExists(string path)
{
    if (Directory.Exists(path))
    {
        Directory.Delete(path, recursive: true);
    }
}