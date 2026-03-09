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
    /// <summary>
    /// Resizes an image to fit within maxWidth×maxHeight (preserving aspect ratio),
    /// saves as JPEG at the given quality. Returns the file extension to use (.jpg).
    /// </summary>
    public static async Task SaveAsJpegAsync(Stream input, string outputPath, int maxWidth, int maxHeight, int quality = 82)
    {
        using var image = await Image.LoadAsync(input);

        if (image.Width > maxWidth || image.Height > maxHeight)
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(maxWidth, maxHeight)
            }));

        var encoder = new JpegEncoder { Quality = quality };
        await image.SaveAsync(outputPath, encoder);
    }

    /// <summary>
    /// Resizes to fit within maxWidth×maxHeight and saves as PNG (for logos with transparency).
    /// </summary>
    public static async Task SaveAsPngAsync(Stream input, string outputPath, int maxWidth, int maxHeight)
    {
        using var image = await Image.LoadAsync(input);

        if (image.Width > maxWidth || image.Height > maxHeight)
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(maxWidth, maxHeight)
            }));

        await image.SaveAsync(outputPath, new PngEncoder());
    }
}
