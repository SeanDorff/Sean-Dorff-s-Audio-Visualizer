using Common;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using System;
using System.Drawing;
using System.Reflection;

namespace Sean_Dorff_s_Audio_Visualizer
{
    public class Text
    {
        FontToTexture fontToTexture = new();
        private float[] pointAndTextureVertexes;
        private uint[] pointAndTextureIndexes;
        private int textureHandle;

        public Text()
        {
            textureHandle = UploadTexture(fontToTexture.Bitmap);
        }

        public void UpdateText(ref TextureShader textureShader, string text, Vector2 screenSize)
        {
            float xPos = 0;
            float yPos = 0;
            float xPosStep = fontToTexture.GlyphWidth * 10 / screenSize.X;
            float yPosStep = fontToTexture.GlyphHeight * 10 / screenSize.Y;
            float xTexPosStep = fontToTexture.GlyphWidth / (float)fontToTexture.Bitmap.Width / fontToTexture.Glyphs.Length;
            float yTexPosStep = 1;

            pointAndTextureVertexes = new float[text.Length * 24];
            pointAndTextureIndexes = new uint[text.Length];
            for (int stringPos = 0; stringPos < text.Length; stringPos++)
            {
                char currentChar = text[stringPos];
                int glyphPos = FindGlyph(currentChar);
                // point coordinates
                pointAndTextureVertexes[stringPos * 24] = xPos; // lower left
                pointAndTextureVertexes[stringPos * 24 + 1] = yPos; //lower left
                pointAndTextureVertexes[stringPos * 24 + 2] = xPos + xPosStep; // lower right
                pointAndTextureVertexes[stringPos * 24 + 3] = yPos; // lower right
                pointAndTextureVertexes[stringPos * 24 + 4] = xPos; // upper left
                pointAndTextureVertexes[stringPos * 24 + 5] = yPos + yPosStep; // upper left

                pointAndTextureVertexes[stringPos * 24 + 6] = xPos; // upper left
                pointAndTextureVertexes[stringPos * 24 + 7] = yPos + yPosStep; // upper left
                pointAndTextureVertexes[stringPos * 24 + 8] = xPos + xPosStep; // lower right
                pointAndTextureVertexes[stringPos * 24 + 9] = yPos; // lower right
                pointAndTextureVertexes[stringPos * 24 + 10] = xPos + xPosStep; // upper right
                pointAndTextureVertexes[stringPos * 24 + 11] = yPos + yPosStep; // upper right

                xPos += xPosStep;

                float xTexPos = glyphPos * (1 / fontToTexture.Glyphs.Length);
                float yTexPos = 0;

                // texture coordinates
                pointAndTextureVertexes[stringPos * 24 + 12] = xTexPos; // lower left
                pointAndTextureVertexes[stringPos * 24 + 13] = yTexPos; // lower left
                pointAndTextureVertexes[stringPos * 24 + 14] = xTexPos + xTexPosStep; // upper right
                pointAndTextureVertexes[stringPos * 24 + 15] = yTexPos; // upper right
                pointAndTextureVertexes[stringPos * 24 + 16] = xTexPos; // upper left
                pointAndTextureVertexes[stringPos * 24 + 17] = yTexPos + yTexPosStep; // upper left

                pointAndTextureVertexes[stringPos * 24 + 18] = xTexPos; // upper left
                pointAndTextureVertexes[stringPos * 24 + 19] = yTexPos + yTexPosStep; // upper left
                pointAndTextureVertexes[stringPos * 24 + 20] = xTexPos + xTexPosStep; // lower right
                pointAndTextureVertexes[stringPos * 24 + 21] = yTexPos; // lower right
                pointAndTextureVertexes[stringPos * 24 + 22] = xTexPos + xTexPosStep; // upper right
                pointAndTextureVertexes[stringPos * 24 + 23] = yTexPos + yTexPosStep; // upper right

                pointAndTextureIndexes[stringPos] = (uint)stringPos;
            }

            textureShader.CurrentBuffer = 0;
            //textureShader.ArrayBuffer = pointAndTextureVertexes;
            textureShader.Vertexes = new float[pointAndTextureVertexes.Length];
            textureShader.Indexes = new uint[pointAndTextureIndexes.Length];

            Array.Copy(pointAndTextureVertexes, 0, textureShader.Vertexes, 0, pointAndTextureVertexes.Length);
            Array.Copy(pointAndTextureIndexes, 0, textureShader.Indexes, 0, pointAndTextureIndexes.Length);
        }

        private int UploadTexture(Bitmap bitmap)
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                int textureHandle = GL.GenTexture();
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, textureHandle);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmap.Width, bitmap.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, GetPixelArray(ref bitmap));
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                return textureHandle;
            }
        }

        private byte[] GetPixelArray(ref Bitmap bitmap)
        {
            Color color;
            byte[] pixelArray = new byte[bitmap.Width * bitmap.Height * 4];
            int arrayPointer = 0;
            for (int y = 0; y < bitmap.Height; y++)
                for (int x = 0; x < bitmap.Width; x++)
                {
                    color = bitmap.GetPixel(x, y);
                    pixelArray[arrayPointer++] = color.R;
                    pixelArray[arrayPointer++] = color.G;
                    pixelArray[arrayPointer++] = color.B;
                    pixelArray[arrayPointer++] = color.A;
                }
            return pixelArray;
        }

        public void SetActiveAndBindTexture()
        {
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureHandle);
        }

        private int FindGlyph(char c) => fontToTexture.Glyphs.IndexOf(c);

        ~Text()
        {
            GL.DeleteTexture(textureHandle);
        }
    }
}
