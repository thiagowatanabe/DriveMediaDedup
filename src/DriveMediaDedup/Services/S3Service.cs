using Amazon.S3;
using Amazon.S3.Transfer;

namespace DriveMediaDedup.Services;

public class S3Service
{
    private readonly AmazonS3Client _client;
    private readonly string _bucketName;

    public S3Service(string accessKey, string secretKey, string bucketName, string region)
    {
        _client = new AmazonS3Client(accessKey, secretKey, Amazon.RegionEndpoint.GetBySystemName(region));
        _bucketName = bucketName;
    }

    public async Task<string> UploadFileAsync(string localFilePath)
    {
        var fileName = Path.GetFileName(localFilePath);

        using var transferUtility = new TransferUtility(_client);

        var uploadRequest = new TransferUtilityUploadRequest
        {
            FilePath = localFilePath,
            BucketName = _bucketName,
            Key = $"DriveMediaDedup/{fileName}"
        };

        await transferUtility.UploadAsync(uploadRequest);

        return $"https://{_bucketName}.s3.amazonaws.com/DriveMediaDedup/{fileName}";
    }
}