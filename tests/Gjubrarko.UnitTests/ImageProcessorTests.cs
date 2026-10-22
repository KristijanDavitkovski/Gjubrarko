using Gjubrarko.Application.Common;
using Gjubrarko.Infrastructure.Images;
using ImageMagick;

namespace Gjubrarko.UnitTests;

public class ImageProcessorTests
{
    private static MemoryStream JpegWithGps(uint width, uint height)
    {
        using var image = new MagickImage(MagickColors.SeaGreen, width, height);
        var exif = new ExifProfile();
        exif.SetValue(ExifTag.Model, "Test phone");
        exif.SetValue(ExifTag.GPSLatitude, [new Rational(41, 1), new Rational(59, 1), new Rational(0, 1)]);
        exif.SetValue(ExifTag.GPSLatitudeRef, "N");
        exif.SetValue(ExifTag.GPSLongitude, [new Rational(21, 1), new Rational(26, 1), new Rational(0, 1)]);
        exif.SetValue(ExifTag.GPSLongitudeRef, "E");
        image.SetProfile(exif);
        return new MemoryStream(image.ToByteArray(MagickFormat.Jpeg));
    }

    [Fact]
    public async Task Strips_metadata_resizes_and_returns_gps()
    {
        var result = await new MagickImageProcessor().ProcessAsync(JpegWithGps(3000, 2000), CancellationToken.None);

        Assert.Equal(1600, result.Width);
        Assert.Equal(41.983, result.Latitude!.Value, 3);
        Assert.Equal(21.433, result.Longitude!.Value, 3);

        using var full = new MagickImage(result.Full);
        Assert.Equal(MagickFormat.WebP, full.Format);
        Assert.Null(full.GetExifProfile());
        using var thumb = new MagickImage(result.Thumbnail);
        Assert.True(thumb.Width <= 400 && thumb.Height <= 400);
    }

    [Fact]
    public async Task Rejects_files_that_are_not_images()
    {
        var notAnImage = new MemoryStream("%PDF-1.7 not a photo"u8.ToArray());
        await Assert.ThrowsAsync<AppException>(() => new MagickImageProcessor().ProcessAsync(notAnImage, CancellationToken.None));
    }
}
