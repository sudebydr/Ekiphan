using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Ekiphan.Application.Media;
using Microsoft.Extensions.Configuration;

namespace Ekiphan.Infrastructure.Media;

public sealed class S3MediaStorageOptions
{
    public const string SectionName = "MediaStorage:S3";

    public required string Bucket { get; init; }
    public required string Region { get; init; }
    public string? Prefix { get; init; }

    public static S3MediaStorageOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var bucket = configuration[$"{SectionName}:Bucket"]?.Trim();
        var region = configuration[$"{SectionName}:Region"]?.Trim();
        var prefix = NormalizePrefix(configuration[$"{SectionName}:Prefix"]);

        if (string.IsNullOrWhiteSpace(bucket))
        {
            throw new InvalidOperationException(
                "MediaStorage:S3:Bucket is required when MediaStorage:Provider is 'S3'.");
        }

        if (string.IsNullOrWhiteSpace(region))
        {
            throw new InvalidOperationException(
                "MediaStorage:S3:Region is required when MediaStorage:Provider is 'S3'.");
        }

        return new S3MediaStorageOptions
        {
            Bucket = bucket,
            Region = region,
            Prefix = prefix,
        };
    }

    internal static string? NormalizePrefix(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return null;
        }

        var normalized = prefix.Trim().Trim('/');
        if (normalized.Length > 400 ||
            normalized.Contains('\\') ||
            normalized.Contains("..", StringComparison.Ordinal) ||
            normalized.Split('/').Any(string.IsNullOrWhiteSpace) ||
            normalized.Any(character =>
                !char.IsLetterOrDigit(character) &&
                character is not '/' and not '-' and not '_' and not '.'))
        {
            throw new InvalidOperationException(
                "MediaStorage:S3:Prefix must be a safe relative object-key prefix.");
        }

        return normalized;
    }
}

public sealed class S3MediaFileStorage : IMediaFileStorage, IDisposable
{
    private readonly IAmazonS3 client;
    private readonly string bucket;
    private readonly string? prefix;
    private readonly string? publicBaseUrl;

    public S3MediaFileStorage(S3MediaStorageOptions options, string? configuredPublicBaseUrl = null)
        : this(
            new AmazonS3Client(
                new AmazonS3Config
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region),
                }),
            options,
            configuredPublicBaseUrl)
    {
    }

    internal S3MediaFileStorage(
        IAmazonS3 client,
        S3MediaStorageOptions options,
        string? configuredPublicBaseUrl = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.Bucket) || string.IsNullOrWhiteSpace(options.Region))
        {
            throw new InvalidOperationException(
                "S3 media storage requires both bucket and region configuration.");
        }

        this.client = client;
        bucket = options.Bucket;
        prefix = S3MediaStorageOptions.NormalizePrefix(options.Prefix);
        publicBaseUrl = configuredPublicBaseUrl?.Trim().TrimEnd('/');
    }

    public bool IsConfigured => true;

    public string ProviderName => "S3";

    public async Task SaveAsync(
        string storageKey,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        await client.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = bucket,
                Key = ResolveObjectKey(storageKey),
                InputStream = content,
                AutoCloseStream = false,
            },
            cancellationToken);
    }

    public async Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        await client.DeleteObjectAsync(
            new DeleteObjectRequest
            {
                BucketName = bucket,
                Key = ResolveObjectKey(storageKey),
            },
            cancellationToken);
    }

    public async Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.GetObjectAsync(
                new GetObjectRequest
                {
                    BucketName = bucket,
                    Key = ResolveObjectKey(storageKey),
                },
                cancellationToken);
            return new S3ResponseStream(response);
        }
        catch (AmazonS3Exception exception) when (
            exception.StatusCode == System.Net.HttpStatusCode.NotFound ||
            string.Equals(exception.ErrorCode, "NoSuchKey", StringComparison.Ordinal))
        {
            return null;
        }
    }

    public async Task MoveAsync(
        string sourceKey,
        string destinationKey,
        CancellationToken cancellationToken = default)
    {
        var sourceObjectKey = ResolveObjectKey(sourceKey);
        var destinationObjectKey = ResolveObjectKey(destinationKey);
        if (string.Equals(sourceObjectKey, destinationObjectKey, StringComparison.Ordinal))
        {
            return;
        }

        await client.CopyObjectAsync(
            new CopyObjectRequest
            {
                SourceBucket = bucket,
                SourceKey = sourceObjectKey,
                DestinationBucket = bucket,
                DestinationKey = destinationObjectKey,
            },
            cancellationToken);
        await client.DeleteObjectAsync(
            new DeleteObjectRequest
            {
                BucketName = bucket,
                Key = sourceObjectKey,
            },
            cancellationToken);
    }

    public string GetPublicUrl(string storageKey) => publicBaseUrl is null
        ? "/" + ValidateStorageKey(storageKey)
        : $"{publicBaseUrl}/{ValidateStorageKey(storageKey)}";

    public void Dispose() => client.Dispose();

    private string ResolveObjectKey(string storageKey) => prefix is null
        ? ValidateStorageKey(storageKey)
        : $"{prefix}/{ValidateStorageKey(storageKey)}";

    private static string ValidateStorageKey(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) ||
            storageKey.Length > 500 ||
            storageKey.StartsWith('/') ||
            storageKey.Contains('\\') ||
            storageKey.Contains("..", StringComparison.Ordinal) ||
            storageKey.Any(character =>
                !char.IsLetterOrDigit(character) &&
                character is not '/' and not '-' and not '_' and not '.'))
        {
            throw new UnsafeMediaFileException("The media storage key is unsafe.");
        }

        return storageKey;
    }

    private sealed class S3ResponseStream(GetObjectResponse response) : Stream
    {
        private readonly GetObjectResponse response = response;
        private Stream Content => response.ResponseStream;

        public override bool CanRead => Content.CanRead;
        public override bool CanSeek => Content.CanSeek;
        public override bool CanWrite => false;
        public override long Length => Content.Length;
        public override long Position { get => Content.Position; set => Content.Position = value; }
        public override void Flush() => Content.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => Content.FlushAsync(cancellationToken);
        public override int Read(byte[] buffer, int offset, int count) => Content.Read(buffer, offset, count);
        public override int Read(Span<byte> buffer) => Content.Read(buffer);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => Content.ReadAsync(buffer, offset, count, cancellationToken);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => Content.ReadAsync(buffer, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => Content.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                response.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
