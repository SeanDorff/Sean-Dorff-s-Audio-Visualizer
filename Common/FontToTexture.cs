using OpenTK.Graphics.OpenGL4;

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Reflection;

using GLPixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;
using SDIPixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Common
{
    public class FontToTexture : IDisposable
    {
        private const string GLYPHS = " !\"#$%&'()*+,-./0123456789:;<=>?@AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz[\\]^_";
        private const int GLYPH_HEIGHT = 20;
        private const int GLYPH_WIDTH = 20;
        private int textureWidth;
        private int textureHeight;
        private int textureHandle;
        private bool disposedValue;

        public int TextureHeight { get => textureHeight; }
        public int TextureWidth { get => textureWidth; }
        public int TextureHandle { get => textureHandle; }
        public string Glyphs { get => GLYPHS; }
        public int GlyphHeight { get => GLYPH_HEIGHT; }
        public int GlyphWidth { get => GLYPH_WIDTH; }

        public FontToTexture()
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                int width = NextPow2(GLYPHS.Length * GLYPH_WIDTH);
                int height = NextPow2(GLYPH_HEIGHT);
                using Bitmap bitmap = new(width, height, SDIPixelFormat.Format32bppArgb);
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    using Font font = new(new FontFamily("Consolas"), 20);
                    for (int i = 0; i < GLYPHS.Length; i++)
                        graphics.DrawString(GLYPHS[i].ToString(), font, Brushes.White, new Point(i * GLYPH_WIDTH, 0));
                }
                int textureHandle = LoadTexture(bitmap);
            }
        }

        private int LoadTexture(Bitmap bitmap)
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                int textureHandle = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, textureHandle);
                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmap.Width, bitmap.Height, 0, GLPixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                bitmap.UnlockBits(data);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                textureWidth = bitmap.Width;
                textureHeight = bitmap.Height;
                return textureHandle;
            }
        }

        private static int NextPow2(int target)
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                int number = 1;
                do
                {
                    number *= 2;
                }
                while (number < target);
                return number;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        GL.DeleteTexture(TextureHandle);
                    }

                    disposedValue = true;
                }
            }
        }

        public void Dispose()
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }
    }
}
