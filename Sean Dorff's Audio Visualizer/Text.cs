using Common;

using OpenTK.Mathematics;

using System;

namespace Sean_Dorff_s_Audio_Visualizer
{
    public class Text
    {
        FontToTexture fontToTexture = new();
        private float[] texCoords;
        private float[] pointCoords;

        public Text()
        {

        }

        public void UpdateText(ref TextureShader textureShader, string text, Vector2 screenSize)
        {
            float xPos = 0;
            float yPos = 0;
            float xPosStep = screenSize.X / fontToTexture.GlyphWidth;
            float yPosStep = screenSize.Y / fontToTexture.GlyphHeight;
            float xTexPosStep = 1 / fontToTexture.Glyphs.Length;
            float yTexPosStep = 1;

            texCoords = new float[text.Length * 12];
            pointCoords = new float[text.Length * 12];
            for (int stringPos = 0; stringPos < text.Length; stringPos++)
            {
                char currentChar = text[stringPos];
                int glyphPos = FindGlyph(currentChar);
                pointCoords[stringPos * 12] = xPos; // upper left
                pointCoords[stringPos * 12 + 1] = yPos; // upper left
                pointCoords[stringPos * 12 + 2] = xPos + xPosStep; // upper right
                pointCoords[stringPos * 12 + 3] = yPos; // upper right
                pointCoords[stringPos * 12 + 4] = xPos; // lower left
                pointCoords[stringPos * 12 + 5] = yPos + yPosStep; // lower right

                pointCoords[stringPos * 12 + 6] = xPos; // lower left
                pointCoords[stringPos * 12 + 7] = yPos + yPosStep; // lower left
                pointCoords[stringPos * 12 + 8] = xPos + xPosStep; // upper right
                pointCoords[stringPos * 12 + 9] = yPos; // upper right
                pointCoords[stringPos * 12 + 10] = xPos + xPosStep; // lower right
                pointCoords[stringPos * 12 + 11] = yPos + yPosStep; // lower right

                xPos += xPosStep;

                float xTexPos = glyphPos * (1 / fontToTexture.Glyphs.Length);
                float yTexPos = 0;

                texCoords[stringPos * 12] = xTexPos; // upper left
                texCoords[stringPos * 12 + 1] = yTexPos; // upper left
                texCoords[stringPos * 12 + 2] = xTexPos + xTexPosStep; // upper right
                texCoords[stringPos * 12 + 3] = yTexPos; // upper right
                texCoords[stringPos * 12 + 4] = xTexPos; // lower left
                texCoords[stringPos * 12 + 5] = yTexPos + yTexPosStep; // lower right

                texCoords[stringPos * 12 + 6] = xTexPos; // lower left
                texCoords[stringPos * 12 + 7] = yTexPos + yTexPosStep; // lower left
                texCoords[stringPos * 12 + 8] = xTexPos + xTexPosStep; // upper right
                texCoords[stringPos * 12 + 9] = yTexPos; // upper right
                texCoords[stringPos * 12 + 10] = xTexPos + xTexPosStep; // lower right
                texCoords[stringPos * 12 + 11] = yTexPos + yTexPosStep; // lower right
            }

            textureShader.CurrentBuffer = 0;
            Array.Copy(pointCoords, 0, textureShader.ArrayBuffer, 0, pointCoords.Length);
            textureShader.CurrentBuffer = 1;
            Array.Copy(texCoords, 0, textureShader.ArrayBuffer, 0, pointCoords.Length);
        }

        private int FindGlyph(char c) => fontToTexture.Glyphs.IndexOf(c);
    }
}
