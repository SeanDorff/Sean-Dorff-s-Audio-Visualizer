using Common;

using OpenTK.Mathematics;

using System;
using System.Reflection;

namespace Sean_Dorff_s_Audio_Visualizer
{
    public class Stars
    {
        private readonly int starCount;
        private readonly float alphaDimm;
        private readonly int spectrumBarGenerations;
        private SStar[] stars;

        private readonly int starVertexesCount;
        private readonly int starIndexesCount;

        private readonly Random random = new();

        public int StarVertexesCount { get => starVertexesCount; }
        public int StarIndexesCount { get => starIndexesCount; }

        public Stars(int starCount, float alphaDimm, int spectrumBarGenerations)
        {
            this.starCount = starCount;
            this.alphaDimm = alphaDimm;
            this.spectrumBarGenerations = spectrumBarGenerations;
            starVertexesCount = starCount * 8;
            starIndexesCount = starCount;
            InitStars();
        }

        public void UpdateStars(GenericShader genericShader)
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                genericShader.VertexesCount = starVertexesCount;
                genericShader.IndexesCount = starIndexesCount;

                int remainingGenerator = starCount / spectrumBarGenerations;
                SStar star;
                for (int i = 0; i < starCount; i++)
                {
                    star = stars[i];
                    star.Generation += 1;
                    star.Color.W *= alphaDimm;
                    if ((star.Generation <= 0) || (star.Generation > 150))
                    {
                        if (remainingGenerator-- > 0)
                        {
                            star.Generation = 0;
                            star.Position = new Vector3(NextRendomFloat() * 4 - 2, NextRendomFloat() * 4 - 2, 0.0f);
                            star.Color = Vector4.One;
                        }
                        else
                        {
                            star.Generation = float.MinValue;
                            star.Color = Vector4.Zero;
                        }
                    }
                    stars[i] = star;
                }
            }

            TransformToVertexes();

            void TransformToVertexes()
            {
                float[] starVertexes = new float[starCount * 8];
                uint[] starVertexIndexes = new uint[starCount];

                for (int i = 0; i < starCount; i++)
                {
                    SStar star = stars[i];
                    starVertexes[i * 8] = star.Position.X;
                    starVertexes[i * 8 + 1] = star.Position.Y;
                    starVertexes[i * 8 + 2] = star.Position.Z;
                    starVertexes[i * 8 + 3] = star.Generation;
                    starVertexes[i * 8 + 4] = star.Color.X;
                    starVertexes[i * 8 + 5] = star.Color.Y;
                    starVertexes[i * 8 + 6] = star.Color.Z;
                    starVertexes[i * 8 + 7] = star.Color.W;
                    starVertexIndexes[i] = (uint)i;
                }

                Array.Copy(starVertexes, 0, genericShader.Vertexes, 0, starVertexes.Length);
                Array.Copy(starVertexIndexes, 0, genericShader.Indexes, 0, starVertexIndexes.Length);
            }

            float NextRendomFloat() => (float)random.NextDouble();
        }

        private void InitStars()
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                stars = new SStar[starCount];
                for (int i = 0; i < starCount; i++)
                    stars[i] = new SStar
                    {
                        Position = Vector3.Zero,
                        Generation = float.MinValue,
                        Color = Vector4.Zero
                    };
            }
        }
    }
}
