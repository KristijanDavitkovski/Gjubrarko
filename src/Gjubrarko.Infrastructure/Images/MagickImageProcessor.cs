using Gjubrarko.Application.Abstractions;
using Gjubrarko.Application.Common;
using ImageMagick;

namespace Gjubrarko.Infrastructure.Images;

public class MagickImageProcessor : IImageProcessor
{
    private const uint FullSize = 1600;
    private const uint ThumbSize = 400;
    private static readonly MagickFormat[] Allowed = [MagickFormat.Jpeg, MagickFormat.Png, MagickFormat.WebP, MagickFormat.Heic, MagickFormat.Heif];

    static MagickImageProcessor()
    {
        // Guards against decompression bombs: a 10 MB upload must not expand into gigabytes of pixels.
        ResourceLimits.Width = 12_000;
        ResourceLimits.Height = 12_000;
        ResourceLimits.Area = 80_000_000;
    }

    public async Task<ProcessedImage> ProcessAsync(Stream input, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, ct);
        var bytes = buffer.ToArray();

        MagickImage image;
        try
        {
            // The format comes from the file signature, not the extension.
            var info = new MagickImageInfo(bytes);
            if (!Allowed.Contains(info.Format))
                throw AppException.BadRequest("Only JPEG, PNG, WebP and HEIC photos are supported.");
            image = new MagickImage(bytes);
        }
        catch (MagickException)
        {
            throw AppException.BadRequest("The file is not a supported image.");
        }

        using (image)
        {
            var (lat, lng) = ReadGps(image.GetExifProfile());
            image.AutoOrient();
            image.Strip(); // drops EXIF, XMP, IPTC and colour profiles

            image.Resize(new MagickGeometry(FullSize, FullSize) { Greater = true });
            var full = Encode(image);
            var (width, height) = ((int)image.Width, (int)image.Height);

            image.Resize(new MagickGeometry(ThumbSize, ThumbSize) { Greater = true });
            var thumb = Encode(image);

            return new ProcessedImage(full, thumb, width, height, lat, lng);
        }
    }

    private static byte[] Encode(MagickImage image)
    {
        image.Quality = 80;
        return image.ToByteArray(MagickFormat.WebP);
    }

    private static (double?, double?) ReadGps(IExifProfile? exif)
    {
        var lat = exif?.GetValue(ExifTag.GPSLatitude)?.Value;
        var lng = exif?.GetValue(ExifTag.GPSLongitude)?.Value;
        if (lat is not { Length: 3 } || lng is not { Length: 3 }) return (null, null);

        static double ToDegrees(Rational[] p) => p[0].ToDouble() + p[1].ToDouble() / 60 + p[2].ToDouble() / 3600;
        var latitude = ToDegrees(lat);
        var longitude = ToDegrees(lng);
        if (exif!.GetValue(ExifTag.GPSLatitudeRef)?.Value == "S") latitude = -latitude;
        if (exif.GetValue(ExifTag.GPSLongitudeRef)?.Value == "W") longitude = -longitude;
        return (latitude, longitude);
    }
}
