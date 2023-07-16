namespace GameBoySharp.Emu.Utils
{
    /// <summary>
    /// The pixel data format of the image.
    /// </summary>
    public enum PixelFormat
    {
        Bgra32,
        Argb32,
        Rgb24
    }

    /// <summary>
    /// The color space for the image
    /// </summary>
    public enum ColorSpace
    {
        Linear,
        sRGB
    }

    /// <summary>
    /// The sRGB color space intent
    /// </summary>
    public enum SrgbIntent: byte
    {
        Perceptual = 0,
        Relative = 1,
        Saturation = 2,
        Absolute = 3
    }

    /// <summary>
    /// Minimal implementation of Bitmap with support for 32 and 24 bit colors.
    /// </summary>
    public class Bitmap
    {
        /// <summary>
        /// The size of an Inch in Meters.
        /// </summary>
        public const double InchInMeters = 0.0254;

        /// <summary>
        /// The raw pixel information
        /// </summary>
        public byte[] RawData { get; private set; }

        /// <summary>
        /// The pixel width of the image
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// The pixel height of the image
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// The pixel format (RGB24 or BGRA32)
        /// </summary>
        public PixelFormat PixelFormat { get; private set; }

        /// <summary>
        /// The number of bytes per pixel (3 or 4)
        /// </summary>
        public int BytesPerPixel { get; private set; }

        /// <summary>
        /// The length of a pixel row, including padding to align with 4-byte chunks in 24bit mode
        /// </summary>
        public int Stride { get; private set; }

        /// <summary>
        /// The intended color space, sRGB is the default.
        /// </summary>
        public ColorSpace ColorSpace { get; set; } = ColorSpace.sRGB;

        /// <summary>
        /// The sRGB intent mode
        /// </summary>
        public SrgbIntent SrgbIntent { get; set; } = SrgbIntent.Perceptual;

        /// <summary>
        /// The Gamma correction mode stored as (Gamma * 100,000) - 2.2 is the default for sRGB, stored here as 45455.
        /// </summary>
        public uint Gamma { get; set; } = 45455;

        /// <summary>
        /// True if the SizeX and SizeY values represent the physical size of the image in meters. False if the size is unknown.
        /// </summary>
        public bool SizeInMeters { get; set; } = false; // phys unit = 1

        /// <summary>
        /// The physical width of the image in meters, or the width part of a pixel aspect ratio.
        /// </summary>
        public uint SizeX { get; set; } = 1;

        /// <summary>
        /// The physical height of the image in meters, or the height part of a pixel aspect ratio.
        /// </summary>
        public uint SizeY { get; set; } = 1;

        /// <summary>
        /// Create a new instance of the Bitmap class.
        /// </summary>
        /// <param name="width">The pixel height of the bitmap.</param>
        /// <param name="height">The pixel width of the bitmap.</param>
        /// <param name="pixelFormat">The pixel format.</param>
        public Bitmap(int width, int height, PixelFormat pixelFormat)
        {
            this.Width = width;
            this.Height = height;
            this.PixelFormat = pixelFormat;
            this.BytesPerPixel = pixelFormat switch
            {
                PixelFormat.Bgra32 => 4,
                PixelFormat.Argb32 => 4,
                PixelFormat.Rgb24 => 3,
                _ => throw new NotImplementedException($"Pixel Forma {pixelFormat} is not supported yet.")
            };
            this.Stride = 4 * ((Width * BytesPerPixel + 3) / 4);
            this.RawData = new byte[Height * Stride];
        }

        /// <summary>
        /// Calculates the stride value for a bitmap.
        /// </summary>
        /// <param name="width">The pixel width of the bitmap</param>
        /// <param name="bytesPerPixel">The number of bytes per pixel</param>
        /// <returns></returns>
        public static int CalcStride(int width, int bytesPerPixel)
        {
            return 4 * ((width * bytesPerPixel + 3) / 4);
        }

        /// <summary>
        /// Gets the pixel color at a specific x,y coordinate in the image.
        /// </summary>
        /// <returns></returns>
        public Color GetPixel(int x, int y)
        {
            var row = y * Stride;
            var col = x * BytesPerPixel;
            var idx = row + col;
            if (PixelFormat == PixelFormat.Bgra32)
            {
                var b = RawData[idx];
                var g = RawData[idx + 1];
                var r = RawData[idx + 2];
                var a = RawData[idx + 3];    
                return new Color(a, r, g, b);
            }
            else if (PixelFormat == PixelFormat.Argb32)
            {
                var a = RawData[idx];
                var r = RawData[idx + 1];
                var g = RawData[idx + 2];
                var b = RawData[idx + 3];
                return new Color(a, r, g, b);
            }
            else
            {
                return new Color(255, RawData[idx], RawData[idx + 1], RawData[idx + 2]);
            }
        }

        /// <summary>
        /// Sets the color at a specific x,y coordinate in the image.
        /// </summary>
        /// <returns></returns>
        public void SetPixel(int x, int y, Color pixel)
        {
            var idx = (y * Stride) + (x * BytesPerPixel);

            switch (PixelFormat)
            {
                case PixelFormat.Bgra32:
                    RawData[idx + 3] = pixel.A;
                    RawData[idx + 2] = pixel.R;
                    RawData[idx + 1] = pixel.G;
                    RawData[idx] = pixel.B;
                    break;
                case PixelFormat.Argb32:
                    RawData[idx] = pixel.A;
                    RawData[idx + 1] = pixel.R;
                    RawData[idx + 2] = pixel.G;
                    RawData[idx + 3] = pixel.B;
                    break;
                case PixelFormat.Rgb24:
                    RawData[idx] = pixel.R;
                    RawData[idx + 1] = pixel.G;
                    RawData[idx + 2] = pixel.B;
                    break;
            }
        }

        /// <summary>
        /// Sets all the pixel in the bitmap to the specified color.
        /// </summary>
        /// <param name="color"></param>
        public void Clear(Color color)
        {
            for(int y = 0; y < Height; y++)
            {
                for(int x = 0; x < Width; x++)
                {
                    SetPixel(x, y, color);
                }
            }
        }
    }
}
