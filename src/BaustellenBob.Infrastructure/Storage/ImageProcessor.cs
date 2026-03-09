using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace BaustellenBob.Infrastructure.Storage;

/// <summary>
/// Resizes and re-encodes uploaded images to save storage space.
/// </summary>
public static class ImageProcessor
{
    private static void ResizeIfNeeded(Image image, int maxWidth, int maxHeight)
    {
        if (image.Width > maxWidth || image.Height > maxHeight)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(maxWidth, maxHeight)
            }));
        }
    }

    private static void StripMetadata(Image image)
    {
        image.Metadata.ExifProfile = null;
        image.Metadata.IccProfile = null;
        image.Metadata.XmpProfile = null;
    }

    /// <summary>
    /// Resizes an image to fit within maxWidth×maxHeight (preserving aspect ratio),
    /// saves as JPEG at the given quality. Returns the file extension to use (.jpg).
    /// </summary>
    public static async Task SaveAsJpegAsync(Stream input, string outputPath, int maxWidth, int maxHeight, int quality = 82)
    {
        using var image = await Image.LoadAsync(input);
        ResizeIfNeeded(image, maxWidth, maxHeight);

        var encoder = new JpegEncoder { Quality = quality };
        await image.SaveAsync(outputPath, encoder);
    }

    /// <summary>
    /// Resizes to fit within maxWidth×maxHeight and saves as PNG (for logos with transparency).
    /// </summary>
    public static async Task SaveAsPngAsync(Stream input, string outputPath, int maxWidth, int maxHeight)
    {
        using var image = await Image.LoadAsync(input);
        ResizeIfNeeded(image, maxWidth, maxHeight);

        await image.SaveAsync(outputPath, new PngEncoder());
    }

    public static async Task<byte[]> ToJpegBytesAsync(Stream input, int maxWidth, int maxHeight, int quality = 82)
    {
        using var image = await Image.LoadAsync(input);
        ResizeIfNeeded(image, maxWidth, maxHeight);
        StripMetadata(image);

        using var output = new MemoryStream();
        await image.SaveAsync(output, new JpegEncoder { Quality = quality });
        return output.ToArray();
    }

    public static async Task<byte[]> ToPngBytesAsync(Stream input, int maxWidth, int maxHeight)
    {
        using var image = await Image.LoadAsync(input);
        ResizeIfNeeded(image, maxWidth, maxHeight);

        using var output = new MemoryStream();
        await image.SaveAsync(output, new PngEncoder());
        return output.ToArray();
    }
}
