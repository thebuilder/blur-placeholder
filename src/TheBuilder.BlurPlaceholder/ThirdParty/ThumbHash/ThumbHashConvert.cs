// C# port authored by Glitched Polygons GmbH and adapted from
// https://github.com/evanw/thumbhash/pull/50 at commit
// https://github.com/evanw/thumbhash/pull/50/commits/e9a71498144c06be02e6afcedd268d2e9740b3ce (MIT).
// The PNG/data-URL helper methods were intentionally omitted; the package emits WebP data URLs.
namespace TheBuilder.BlurPlaceholder.ThirdParty.ThumbHash;

using System.Buffers;

/// <summary>
/// ThumbHash .NET implementation based on https://github.com/evanw/thumbhash
/// </summary>
internal static class ThumbHashConvert
{
    /// <summary>
    /// Encodes an RGBA image to a ThumbHash.
    /// </summary>
    /// <param name="w">The width of the input image. Must be ≤100px.</param>
    /// <param name="h">The height of the input image. Must be ≤100px.</param>
    /// <param name="rgba">The pixels in the input image, row-by-row. Must have w*h*4 elements.</param>
    /// <remarks>RGB should not be premultiplied by A.</remarks>
    /// <returns>ThumbHash bytes.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="w"/> or <paramref name="h"/> is greater than <c>100</c>.</exception>
    public static Span<byte> FromRgba(int w, int h, ReadOnlySpan<byte> rgba)
    {
        byte[] thumbHashBuffer = new byte[32];

        int n = FromRgba(w, h, rgba, thumbHashBuffer);

        return thumbHashBuffer.AsSpan(0, n);
    }

    /// <summary>
    /// Encodes an RGBA image to a ThumbHash.
    /// </summary>
    /// <param name="w">The width of the input image. Must be ≤100px.</param>
    /// <param name="h">The height of the input image. Must be ≤100px.</param>
    /// <param name="rgba">The pixels in the input image, row-by-row. Must have <c>w*h*4</c> elements.</param>
    /// <param name="output">Output byte buffer of at least 32B in size into which the ThumbHash should be written.</param>
    /// <remarks>RGB should not be premultiplied by A.</remarks>
    /// <returns>Number of ThumbHash bytes written into <paramref name="output"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="w"/> or <paramref name="h"/> is greater than <c>100</c> or the <paramref name="output"/> buffer size is insufficient (must be at least 32 bytes).</exception>
    public static int FromRgba(int w, int h, ReadOnlySpan<byte> rgba, Span<byte> output)
    {
        if (output.Length < 32)
        {
            throw new ArgumentException("Output buffer is too small. Please allocate at least 32 bytes of storage!", nameof(output));
        }

        // Encoding an image larger than 100x100 is slow with no benefit.
        if (w > 100 || h > 100)
        {
            throw new ArgumentException($"{w}x{h} is larger than the maximum allowed input resolution of 100x100.");
        }

        double averageR = 0;
        double averageG = 0;
        double averageB = 0;
        double averageA = 0;

        int resolution = w * h;

        for (int i = 0, j = 0; i < resolution; ++i, j += 4)
        {
            double alpha = (rgba[j + 3] & 255) / 255.0;

            averageR += alpha / 255.0f * (rgba[j] & 255);
            averageG += alpha / 255.0f * (rgba[j + 1] & 255);
            averageB += alpha / 255.0f * (rgba[j + 2] & 255);
            averageA += alpha;
        }

        if (averageA > 0)
        {
            averageR /= averageA;
            averageG /= averageA;
            averageB /= averageA;
        }

        bool hasAlpha = averageA < resolution;

        int luminanceLimit = hasAlpha ? 5 : 7; // Use fewer luminance bits if there's an alpha.

        int lx = (int)Math.Max(1, Round(luminanceLimit * w / (double)Math.Max(w, h)));
        int ly = (int)Math.Max(1, Round(luminanceLimit * h / (double)Math.Max(w, h)));

        double[] lpqaBuffer = ArrayPool<double>.Shared.Rent(resolution * 4);

        try
        {
            Span<double> l = lpqaBuffer.AsSpan(resolution * 0, resolution); // luminance
            Span<double> p = lpqaBuffer.AsSpan(resolution * 1, resolution); // yellow - blue
            Span<double> q = lpqaBuffer.AsSpan(resolution * 2, resolution); // red - green
            Span<double> a = lpqaBuffer.AsSpan(resolution * 3, resolution); // alpha

            // Convert the image from RGBA to LPQA (composite atop the average color).
            for (int i = 0, j = 0; i < resolution; ++i, j += 4)
            {
                double alpha = (rgba[j + 3] & 255) / 255.0;
                double r = averageR * (1.0 - alpha) + alpha / 255.0 * (rgba[j] & 255);
                double g = averageG * (1.0 - alpha) + alpha / 255.0 * (rgba[j + 1] & 255);
                double b = averageB * (1.0 - alpha) + alpha / 255.0 * (rgba[j + 2] & 255);
                l[i] = (r + g + b) / 3.0;
                p[i] = (r + g) / 2.0 - b;
                q[i] = r - g;
                a[i] = alpha;
            }

            // Encode using the DCT into DC (constant) and normalized AC (varying) terms.
            Channel channelL = new Channel(Math.Max(3, lx), Math.Max(3, ly)).Encode(w, h, l);
            Channel channelP = new Channel(3, 3).Encode(w, h, p);
            Channel channelQ = new Channel(3, 3).Encode(w, h, q);
            Channel? channelA = hasAlpha ? new Channel(5, 5).Encode(w, h, a) : null;

            bool isLandscape = w > h;

            int header24 =
                Round(63.0 * channelL.DC)
                |
                (Round(31.5 + 31.5 * channelP.DC) << 6)
                |
                (Round(31.5 + 31.5 * channelQ.DC) << 12)
                |
                (Round(31.0 * channelL.Scale) << 18)
                |
                (hasAlpha ? 1 << 23 : 0);

            int header16 =
                (isLandscape ? ly : lx)
                |
                (Round(63.0 * channelP.Scale) << 3)
                |
                (Round(63.0 * channelQ.Scale) << 9)
                |
                (isLandscape ? 1 << 15 : 0);

            int acStart = hasAlpha ? 6 : 5;

            int acCount = channelL.AC.Length + channelP.AC.Length + channelQ.AC.Length + (hasAlpha ? channelA!.AC.Length : 0);

            int outputSize = acStart + (acCount + 1) / 2;

            output[0] = (byte)header24;
            output[1] = (byte)(header24 >> 8);
            output[2] = (byte)(header24 >> 16);
            output[3] = (byte)header16;
            output[4] = (byte)(header16 >> 8);

            if (hasAlpha)
            {
                output[5] = (byte)(Round(15.0 * channelA!.DC) | (Round(15.0 * channelA.Scale) << 4));
            }

            int acIndex = 0;

            acIndex = channelL.WriteTo(output, acStart, acIndex);
            acIndex = channelP.WriteTo(output, acStart, acIndex);
            acIndex = channelQ.WriteTo(output, acStart, acIndex);

            if (hasAlpha)
            {
                channelA!.WriteTo(output, acStart, acIndex);
            }

            return outputSize;
        }
        finally
        {
            ArrayPool<double>.Shared.Return(lpqaBuffer);
        }
    }

    private static int Round(double value) => (int)Math.Floor(value + 0.5);


    /// <summary>
    /// Decodes a ThumbHash to an RGBA image. RGB is not to be premultiplied by A.
    /// </summary>
    /// <param name="hash">The bytes of the ThumbHash.</param>
    /// <returns>The width, height, and pixels (rgba) of the rendered placeholder image.</returns>
    /// <exception cref="ArgumentException">Thrown if the passed <paramref name="hash"/> is empty.</exception>
    public static(int, int, byte[]) ToRgba(ReadOnlySpan<byte> hash)
    {
        if (hash.IsEmpty)
        {
            throw new ArgumentException("The passed ThumbHash is empty.", nameof(hash));
        }

        // Read the constants.
        int header24 = (hash[0] & 255) | ((hash[1] & 255) << 8) | ((hash[2] & 255) << 16);
        int header16 = (hash[3] & 255) | ((hash[4] & 255) << 8);
        double l_dc = (header24 & 63) / 63.0;
        double p_dc = ((header24 >> 6) & 63) / 31.5 - 1.0;
        double q_dc = ((header24 >> 12) & 63) / 31.5 - 1.0;
        double l_scale = ((header24 >> 18) & 31) / 31.0;
        bool hasAlpha = (header24 >> 23) != 0;
        double p_scale = ((header16 >> 3) & 63) / 63.0;
        double q_scale = ((header16 >> 9) & 63) / 63.0;
        bool isLandscape = (header16 >> 15) != 0;
        int lx = Math.Max(3, isLandscape ? hasAlpha ? 5 : 7 : header16 & 7);
        int ly = Math.Max(3, isLandscape ? header16 & 7 : hasAlpha ? 5 : 7);
        double a_dc = hasAlpha ? (hash[5] & 15) / 15.0 : 1.0;
        double a_scale = ((hash[5] >> 4) & 15) / 15.0;

        // Read the varying factors (boost saturation by 1.25x to compensate for quantization).
        int ac_start = hasAlpha ? 6 : 5;
        int ac_index = 0;

        Channel l_channel = new Channel(lx, ly);
        Channel p_channel = new Channel(3, 3);
        Channel q_channel = new Channel(3, 3);
        Channel? a_channel = null;

        ac_index = l_channel.Decode(hash, ac_start, ac_index, l_scale);
        ac_index = p_channel.Decode(hash, ac_start, ac_index, p_scale * 1.25f);
        ac_index = q_channel.Decode(hash, ac_start, ac_index, q_scale * 1.25f);

        if (hasAlpha)
        {
            a_channel = new Channel(5, 5);
            a_channel.Decode(hash, ac_start, ac_index, a_scale);
        }

        double[] l_ac = l_channel.AC;
        double[] p_ac = p_channel.AC;
        double[] q_ac = q_channel.AC;
        double[]? a_ac = hasAlpha ? a_channel!.AC : null;

        // Decode using the DCT into RGB.
        double ratio = ToApproximateAspectRatio(hash);

        int w = (int)Round(ratio > 1.0 ? 32.0 : 32.0 * ratio);
        int h = (int)Round(ratio > 1.0 ? 32.0 / ratio : 32.0);

        byte[] rgba = new byte[w * h * 4];

        int cx_stop = Math.Max(lx, hasAlpha ? 5 : 3);
        int cy_stop = Math.Max(ly, hasAlpha ? 5 : 3);

        Span<double> fx = stackalloc double[cx_stop];
        Span<double> fy = stackalloc double[cy_stop];

        for (int y = 0, i = 0; y < h; ++y)
        {
            for (int x = 0; x < w; ++x, i += 4)
            {
                double l = l_dc, p = p_dc, q = q_dc, a = a_dc;

                // Precompute the coefficients
                for (int cx = 0; cx < cx_stop; ++cx)
                {
                    fx[cx] = Math.Cos(Math.PI / w * (x + 0.5) * cx);
                }

                for (int cy = 0; cy < cy_stop; ++cy)
                {
                    fy[cy] = Math.Cos(Math.PI / h * (y + 0.5) * cy);
                }

                // Decode L
                for (int cy = 0, j = 0; cy < ly; ++cy)
                {
                    double fy2 = fy[cy] * 2.0;
                    for (int cx = cy > 0 ? 0 : 1; cx * ly < lx * (ly - cy); ++cx, ++j)
                    {
                        l += l_ac[j] * fx[cx] * fy2;
                    }
                }

                // Decode P and Q.
                for (int cy = 0, j = 0; cy < 3; ++cy)
                {
                    double fy2 = fy[cy] * 2.0;

                    for (int cx = cy > 0 ? 0 : 1; cx < 3 - cy; ++cx, ++j)
                    {
                        double f = fx[cx] * fy2;
                        p += p_ac[j] * f;
                        q += q_ac[j] * f;
                    }
                }

                // Decode A.
                if (hasAlpha)
                {
                    for (int cy = 0, j = 0; cy < 5; ++cy)
                    {
                        double fy2 = fy[cy] * 2.0;
                        for (int cx = cy > 0 ? 0 : 1; cx < 5 - cy; ++cx, ++j)
                        {
                            a += a_ac![j] * fx[cx] * fy2;
                        }
                    }
                }

                // Convert to RGB.
                double b = l - 2.0 / 3.0 * p;
                double r = (3.0 * l - b + q) / 2.0;
                double g = r - q;

                rgba[i] = (byte)Math.Max(0, Math.Round(255.0 * Math.Min(1, r), MidpointRounding.AwayFromZero));
                rgba[i + 1] = (byte)Math.Max(0, Math.Round(255.0 * Math.Min(1, g), MidpointRounding.AwayFromZero));
                rgba[i + 2] = (byte)Math.Max(0, Math.Round(255.0 * Math.Min(1, b), MidpointRounding.AwayFromZero));
                rgba[i + 3] = (byte)Math.Max(0, Math.Round(255.0 * Math.Min(1, a), MidpointRounding.AwayFromZero));
            }
        }

        return (w, h, rgba);
    }

    /// <summary>
    /// Extracts the average color from a ThumbHash. RGB is not to be premultiplied by A.
    /// </summary>
    /// <param name="hash">ThumbHash bytes.</param>
    /// <returns>A tuple containing the RGBA values for the average color. Each value ranges from 0 to 1.</returns>
    public static(float, float, float, float) ToAverageRgba(ReadOnlySpan<byte> hash)
    {
        int header = (hash[0] & 255) | ((hash[1] & 255) << 8) | ((hash[2] & 255) << 16);
        float l = (header & 63) / 63.0f;
        float p = ((header >> 6) & 63) / 31.5f - 1.0f;
        float q = ((header >> 12) & 63) / 31.5f - 1.0f;
        bool hasAlpha = header >> 23 != 0;
        float a = hasAlpha ? (hash[5] & 15) / 15.0f : 1.0f;
        float b = l - 2.0f / 3.0f * p;
        float r = (3.0f * l - b + q) / 2.0f;
        float g = r - q;

        return
        (
            Math.Max(0.0f, Math.Min(1.0f, r)),
            Math.Max(0.0f, Math.Min(1.0f, g)),
            Math.Max(0.0f, Math.Min(1.0f, b)),
            a
        );
    }

    /// <summary>
    /// Extracts the approximate aspect ratio of the original image.
    /// </summary>
    /// <param name="hash">The bytes of the ThumbHash.</param>
    /// <returns>The approximate aspect ratio (width / height).</returns>
    public static float ToApproximateAspectRatio(ReadOnlySpan<byte> hash)
    {
        byte header = hash[3];
        bool hasAlpha = (hash[2] & 0x80) != 0;
        bool isLandscape = (hash[4] & 0x80) != 0;

        int lx = isLandscape ? hasAlpha ? 5 : 7 : header & 7;
        int ly = isLandscape ? header & 7 : hasAlpha ? 5 : 7;

        return lx / (float)ly;
    }



    private class Channel
    {
        private readonly int nx;
        private readonly int ny;

        public double Scale { get; private set; }
        public double DC { get; private set; }
        public double[] AC { get; }

        public Channel(int nx, int ny)
        {
            this.nx = nx;
            this.ny = ny;

            int n = 0;

            for (int cy = 0; cy < ny; ++cy)
            {
                for (int cx = cy > 0 ? 0 : 1; cx * ny < nx * (ny - cy); ++cx)
                {
                    ++n;
                }
            }

            AC = new double[n];
        }

        public Channel Encode(int w, int h, ReadOnlySpan<double> channel)
        {
            int n = 0;
            double[] fx = new double[w];

            for (int cy = 0; cy < ny; ++cy)
            {
                for (int cx = 0; cx * ny < nx * (ny - cy); ++cx)
                {
                    double f = 0;

                    for (int x = 0; x < w; ++x)
                    {
                        fx[x] = Math.Cos(Math.PI / w * cx * (x + 0.5));
                    }

                    for (int y = 0; y < h; ++y)
                    {
                        double fy = Math.Cos(Math.PI / h * cy * (y + 0.5));

                        for (int x = 0; x < w; ++x)
                        {
                            f += channel[x + y * w] * fx[x] * fy;
                        }
                    }

                    f /= w * h;

                    if (cx > 0 || cy > 0)
                    {
                        AC[n++] = f;
                        Scale = Math.Max(Scale, Math.Abs(f));
                    }
                    else
                    {
                        DC = f;
                    }
                }
            }

            if (Scale > 0)
            {
                for (int i = 0; i < AC.Length; ++i)
                {
                        AC[i] = 0.5 + 0.5 / Scale * AC[i];
                }
            }

            return this;
        }

        public int Decode(ReadOnlySpan<byte> hash, int start, int index, double scale)
        {
            for (int i = 0; i < AC.Length; ++i)
            {
                int data = hash[start + (index >> 1)] >> ((index & 1) << 2);
                AC[i] = ((data & 15) / 7.5f - 1.0f) * scale;
                ++index;
            }

            return index;
        }

        public int WriteTo(Span<byte> hash, int start, int index)
        {
            foreach (double v in AC)
            {
                hash[start + (index >> 1)] |= (byte)(Round(15.0 * v) << ((index & 1) << 2));
                ++index;
            }

            return index;
        }
    }

}
