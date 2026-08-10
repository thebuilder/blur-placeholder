using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace TheBuilder.BlurPlaceholder.Generation;

internal static class ImageRasterizer
{
    public static async Task<Image<Rgba32>> LoadAsync(
        Stream source,
        int maximumDimension,
        CancellationToken cancellationToken)
    {
        Size? targetSize = null;
        if (source.CanSeek)
        {
            var position = source.Position;
            try
            {
                var imageInfo = await Image.IdentifyAsync(
                    new DecoderOptions { MaxFrames = 1 },
                    source,
                    cancellationToken);
                if (imageInfo.Width > maximumDimension || imageInfo.Height > maximumDimension)
                    targetSize = new Size(maximumDimension, maximumDimension);
            }
            finally
            {
                source.Position = position;
            }
        }

        var decoderOptions = new DecoderOptions
        {
            MaxFrames = 1,
            TargetSize = targetSize,
        };

        var image = await Image.LoadAsync<Rgba32>(decoderOptions, source, cancellationToken);
        image.Mutate(context => context.AutoOrient());

        if (image.Width > maximumDimension || image.Height > maximumDimension)
        {
            image.Mutate(context => context.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(maximumDimension, maximumDimension),
            }));
        }

        return image;
    }
}
