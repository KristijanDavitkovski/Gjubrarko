namespace Gjubrarko.Application.Abstractions;

public sealed record ProcessedImage(byte[] Full, byte[] Thumbnail, int Width, int Height, double? Latitude, double? Longitude);

public interface IImageProcessor
{
    /// <summary>Re-encodes to WebP with all metadata removed. GPS from EXIF is returned, never stored in the file.</summary>
    Task<ProcessedImage> ProcessAsync(Stream input, CancellationToken ct);
}
