using Common;

using OpenTK.Mathematics;

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

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

        private readonly float[] rotationHistory;
        private float currentRotation = 0.05f;

        private readonly Vector4 starColor = new(Vector3.One, 0.9f);

        private readonly Random random = new();

        public int StarVertexesCount { get => starVertexesCount; }
        public int StarIndexesCount { get => starIndexesCount; }
        public float[] RotationHistory { get => rotationHistory; }

        private const float ROTATION_DIFF = 0.01f;

        public Stars(int spectrumBarGenerations, int starsPerGeneration, int spectrumBarGenerationMultiplier)
        {
            this.spectrumBarGenerations = spectrumBarGenerations;
            this.starsPerGeneration = starsPerGeneration;
            this.spectrumBarGenerationMultiplier = spectrumBarGenerationMultiplier;
            InitStars();
            starVertexesCount = stars.Length * 8;
            starIndexesCount = stars.Length;

            rotationHistory = new float[spectrumBarGenerations];
            for (int i = 0; i < spectrumBarGenerations; i++)
                rotationHistory[i] = currentRotation;
        }

        public void UpdateStars(ref TriangleAndPointShader triangleAndPointShader)
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                int maxGeneration = spectrumBarGenerations * spectrumBarGenerationMultiplier;

                for (int i = 0; i < stars.Length; i++)
                {
                    stars[i].Generation++;
                    if (stars[i].Generation > maxGeneration)
                        stars[i] = new SStar
                        {
                            Generation = 0,
                            Position = new Vector3(RandomPosition(), RandomPosition(), 15.0f),
                            Color = starColor
                        };
                }
            }

            TransformToVertexes(ref triangleAndPointShader);
        }

        public void ChangeRotationSpeed(int direction)
        {
            if (Math.Abs(direction) != 1)
                throw new ArgumentOutOfRangeException(paramName: nameof(direction));

            currentRotation += direction * ROTATION_DIFF;
        }

        public void UpdateRotationHistory()
        {
            for (int i = rotationHistory.Length - 1; i > 0; i--)
                rotationHistory[i] = rotationHistory[i - 1];
            rotationHistory[0] = currentRotation;
        }

        private void TransformToVertexes(ref TriangleAndPointShader triangleAndPointShader)
        {
            float[] starVertexes = new float[stars.Length * 8];
            uint[] starVertexIndexes = new uint[stars.Length];
            int vertexIndex = 0;

            for (int i = 0; i < stars.Length; i++)
            {
                SStar star = stars[i];
                starVertexes[vertexIndex++] = star.Position.X;
                starVertexes[vertexIndex++] = star.Position.Y;
                starVertexes[vertexIndex++] = star.Position.Z;
                starVertexes[vertexIndex++] = star.Generation;
                starVertexes[vertexIndex++] = star.Color.X;
                starVertexes[vertexIndex++] = star.Color.Y;
                starVertexes[vertexIndex++] = star.Color.Z;
                starVertexes[vertexIndex++] = star.Color.W;
                starVertexIndexes[i] = (uint)i;
            }

            Array.Copy(starVertexes, 0, triangleAndPointShader.Vertexes, 0, starVertexes.Length);
            Array.Copy(starVertexIndexes, 0, triangleAndPointShader.Indexes, 0, starVertexIndexes.Length);
        }

        private void InitStars()
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                stars = new SStar[StarCount()];

                Parallel.ForEach(Enumerable.Range(0, spectrumBarGenerations * spectrumBarGenerationMultiplier).ToList<int>(), generation =>
                {
                    for (int star = 0; star < starsPerGeneration; star++)
                        stars[generation * starsPerGeneration + star] = new SStar
                        {
                            Position = new Vector3(RandomPosition(), RandomPosition(), 15.0f - generation * 0.1f),
                            Generation = generation,
                            Color = starColor
                        };
                });
            };

            int StarCount() => starsPerGeneration * spectrumBarGenerations * spectrumBarGenerationMultiplier;
        }

        private float RandomPosition() => (float)random.NextDouble() * 8 - 4;
    }
}
