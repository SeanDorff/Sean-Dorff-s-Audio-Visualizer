using Common;

using OpenTK.Mathematics;

using System;
using System.Reflection;

namespace Sean_Dorff_s_Audio_Visualizer
{
    public class Stars
    {
        private readonly int spectrumBarGenerations;
        private readonly int starsPerGeneration;
        private readonly int spectrumBarGenerationMultiplier;
        private SStar[] stars;

        private readonly int starVertexesCount;
        private readonly int starIndexesCount;

        private readonly Vector4 starColor = new(Vector3.One, 0.9f);

        private readonly Random random = new();

        public int StarVertexesCount { get => starVertexesCount; }
        public int StarIndexesCount { get => starIndexesCount; }

        public Stars(int spectrumBarGenerations, int starsPerGeneration, int spectrumBarGenerationMultiplier)
        {
            this.spectrumBarGenerations = spectrumBarGenerations;
            this.starsPerGeneration = starsPerGeneration;
            this.spectrumBarGenerationMultiplier = spectrumBarGenerationMultiplier;
            InitStars();
            starVertexesCount = stars.Length * 8;
            starIndexesCount = stars.Length;
        }

        public void UpdateStars(ref GenericShader genericShader)
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                genericShader.VertexesCount = starVertexesCount;
                genericShader.IndexesCount = starIndexesCount;

                int remainingGenerator = starsPerGeneration;
                SStar star;
                for (int i = 0; i < stars.Length; i++)
                {
                    star = stars[i];
                    star.Generation += 1;
                    if (star.Generation > spectrumBarGenerations * 2)
                    {
                        if (remainingGenerator-- > 0)
                        {
                            star.Generation = 0;
                            star.Position = new Vector3(RandomPosition(), RandomPosition(), 15.0f);
                            star.Color = starColor;
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

            TransformToVertexes(ref genericShader);
        }

        private void TransformToVertexes(ref GenericShader genericShader)
        {
            float[] starVertexes = new float[stars.Length * 8];
            uint[] starVertexIndexes = new uint[stars.Length];

            for (int i = 0; i < stars.Length; i++)
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

        private void InitStars()
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                stars = new SStar[StarCount()];

                for (int generation = 0; generation < spectrumBarGenerations * spectrumBarGenerationMultiplier; generation++)
                    for (int star = 0; star < starsPerGeneration; star++)
                        stars[generation * starsPerGeneration + star] = new SStar
                        {
                            Position = new Vector3(RandomPosition(), RandomPosition(), 15.0f - generation * 0.1f),
                            Generation = generation,
                            Color = starColor
                        };
            }

            int StarCount() => starsPerGeneration * spectrumBarGenerations * spectrumBarGenerationMultiplier;
        }

        private float RandomPosition() => (float)random.NextDouble() * 8 - 4;
    }
}
