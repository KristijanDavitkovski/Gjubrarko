using Amazon.S3;
using Amazon.S3.Model;
using Gjubrarko.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Gjubrarko.Infrastructure.Storage;

public class StorageOptions
{
    public const string Section = "Storage";
    public required string ServiceUrl { get; set; }
    public required string Bucket { get; set; }
    public required string AccessKey { get; set; }
    public required string SecretKey { get; set; }
    public int ReadUrlMinutes { get; set; } = 60;
}

public class S3PhotoStorage(IAmazonS3 s3, IOptions<StorageOptions> options) : IPhotoStorage
{
    private readonly StorageOptions _options = options.Value;

    public Task UploadAsync(string key, Stream content, string contentType, CancellationToken ct) =>
        s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
        }, ct);

    public string GetReadUrl(string key) =>
        s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(_options.ReadUrlMinutes),
        });
}
