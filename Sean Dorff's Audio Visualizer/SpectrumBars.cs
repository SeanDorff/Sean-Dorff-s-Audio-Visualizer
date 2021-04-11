using Common;

using OpenTK.Mathematics;

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Sean_Dorff_s_Audio_Visualizer
{
    public class SpectrumBars
    {
        private readonly SSpectrumBar[,] spectrumBars;
        private readonly int spectrumBarGenerations;
        private readonly int spectrumBarCount;

        private readonly int spectrumBarVertexesCount;
        private readonly int spectrumBarIndexesCount;

        private readonly Vector2[] barBorders;

        private readonly float[] vertexes;
        private readonly uint[] indexes;

        public int SpectrumBarVertexesCount { get => spectrumBarVertexesCount; }
        public int SpectrumBarIndexesCount { get => spectrumBarIndexesCount; }

        public SpectrumBars(int spectrumBarGenerations, int spectrumBarCount)
        {
            this.spectrumBarGenerations = spectrumBarGenerations;
            this.spectrumBarCount = spectrumBarCount;

            spectrumBarVertexesCount = spectrumBarCount * spectrumBarGenerations * 32;
            spectrumBarIndexesCount = spectrumBarCount * spectrumBarGenerations * 6;

            vertexes = new float[spectrumBarVertexesCount];
            indexes = new uint[spectrumBarIndexesCount];

            barBorders = SplitInterval(spectrumBarCount, -1.0f, 1.0f);

            spectrumBars = new SSpectrumBar[spectrumBarGenerations, spectrumBarCount * 2 * 3 * 2];

            InitSpectrumBars();
        }

        /// <summary>
        /// Initialises <see cref="spectrumBars"/> for a given number of <see cref="spectrumBarGenerations"/> with <see cref="spectrumBarCount"/> spectrum bars each.
        /// </summary>
        private void InitSpectrumBars()
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                for (int i = 0; i < spectrumBarGenerations; i++)
                    for (int j = 0; j < spectrumBarCount; j++)
                        spectrumBars[i, j] = new SSpectrumBar()
                        {
                            LowerLeft = Vector4.Zero,
                            LowerRight = Vector4.Zero,
                            UpperLeft = Vector4.Zero,
                            UpperRight = Vector4.Zero,
                            Color = Vector4.Zero
                        };
            }
        }

        public void UpdateSpectrumBars(ref GenericShader genericShader, float cameraPositionZ)
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                genericShader.VertexesCount = spectrumBarVertexesCount;
                genericShader.IndexesCount = spectrumBarIndexesCount;

                MoveBarGenerations();
                AddCurrentSpectrum();
                TransformToVertexes();
                SortVerticesByCameraDistance(cameraPositionZ);

                Array.Copy(vertexes, 0, genericShader.Vertexes, 0, vertexes.Length);
                Array.Copy(indexes, 0, genericShader.Indexes, 0, indexes.Length);
            }

        }

        private void MoveBarGenerations()
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                SSpectrumBar spectrumBar;
                for (int generation = spectrumBarGenerations - 1; generation > 0; generation--)
                    for (int bar = 0; bar < spectrumBarCount; bar++)
                    {
                        spectrumBar = spectrumBars[generation - 1, bar];
                        spectrumBar.LowerLeft.W += 1;
                        spectrumBar.LowerRight.W += 1;
                        spectrumBar.UpperLeft.W += 1;
                        spectrumBar.UpperRight.W += 1;
                        spectrumBars[generation, bar] = spectrumBar;
                    }
            }
        }

        private void AddCurrentSpectrum()
        {
            float loudness = SpectrumDataHelper.GetCurrentLoudness();
            for (int bar = 0; bar < spectrumBarCount; bar++)
                spectrumBars[0, bar] = new SSpectrumBar()
                {
                    LowerLeft = new Vector4(barBorders[bar].X, 0f, 0f, 0f),
                    LowerRight = new Vector4(barBorders[bar].Y, 0f, 0f, 0f),
                    UpperLeft = new Vector4(barBorders[bar].X, SpectrumDataHelper.DeNullifiedSpectrumData(bar), 0f, 0f),
                    UpperRight = new Vector4(barBorders[bar].Y, SpectrumDataHelper.DeNullifiedSpectrumData(bar), 0f, 0f),
                    Color = new Vector4(loudness, 1 - barOfBarCount(bar), barOfBarCount(bar), 1f)
                };

            float barOfBarCount(int bar) => bar / (float)spectrumBarCount;
        }

        private void TransformToVertexes()
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                Task[] taskArray = new Task[spectrumBarGenerations];

                foreach (int generation in Enumerable.Range(0, spectrumBarGenerations).ToArray())
                    taskArray[generation] = Task.Factory.StartNew(() =>
                    TransformSpectrumToVertices(generation));

                Task.WaitAll(taskArray);
            }
        }

        private void TransformSpectrumToVertices(int generation)
        {
            const int STRIDE = 4 * 4 + 4 * 4; // 4 * Vector4 vertex + 4 * Vector4 color
            SSpectrumBar spectrumBar;
            int generationOffsetForVertex = generation * spectrumBarCount * STRIDE;
            int generationOffsetForIndex = generation * spectrumBarCount * 6;
            int barByStride;
            int offsetPlusBarByStride;
            float ColorX;
            float ColorY;
            float ColorZ;
            float ColorW;
            for (int bar = 0; bar < spectrumBarCount; bar++)
            {
                spectrumBar = spectrumBars[generation, bar];
                barByStride = bar * STRIDE;
                offsetPlusBarByStride = generationOffsetForVertex + barByStride;
                ColorX = spectrumBar.Color.X;
                ColorY = spectrumBar.Color.Y;
                ColorZ = spectrumBar.Color.Z;
                ColorW = spectrumBar.Color.W;
                vertexes[offsetPlusBarByStride++] = spectrumBar.LowerLeft.X;
                vertexes[offsetPlusBarByStride++] = spectrumBar.LowerLeft.Y;
                vertexes[offsetPlusBarByStride++] = spectrumBar.LowerLeft.Z;
                vertexes[offsetPlusBarByStride++] = spectrumBar.LowerLeft.W;
                vertexes[offsetPlusBarByStride++] = ColorX;
                vertexes[offsetPlusBarByStride++] = ColorY;
                vertexes[offsetPlusBarByStride++] = ColorZ;
                vertexes[offsetPlusBarByStride++] = ColorW;
                vertexes[offsetPlusBarByStride++] = spectrumBar.LowerRight.X;
                vertexes[offsetPlusBarByStride++] = spectrumBar.LowerRight.Y;
                vertexes[offsetPlusBarByStride++] = spectrumBar.LowerRight.Z;
                vertexes[offsetPlusBarByStride++] = spectrumBar.LowerRight.W;
                vertexes[offsetPlusBarByStride++] = ColorX;
                vertexes[offsetPlusBarByStride++] = ColorY;
                vertexes[offsetPlusBarByStride++] = ColorZ;
                vertexes[offsetPlusBarByStride++] = ColorW;
                vertexes[offsetPlusBarByStride++] = spectrumBar.UpperLeft.X;
                vertexes[offsetPlusBarByStride++] = spectrumBar.UpperLeft.Y;
                vertexes[offsetPlusBarByStride++] = spectrumBar.UpperLeft.Z;
                vertexes[offsetPlusBarByStride++] = spectrumBar.UpperLeft.W;
                vertexes[offsetPlusBarByStride++] = ColorX;
                vertexes[offsetPlusBarByStride++] = ColorY;
                vertexes[offsetPlusBarByStride++] = ColorZ;
                vertexes[offsetPlusBarByStride++] = ColorW;
                vertexes[offsetPlusBarByStride++] = spectrumBar.UpperRight.X;
                vertexes[offsetPlusBarByStride++] = spectrumBar.UpperRight.Y;
                vertexes[offsetPlusBarByStride++] = spectrumBar.UpperRight.Z;
                vertexes[offsetPlusBarByStride++] = spectrumBar.UpperRight.W;
                vertexes[offsetPlusBarByStride++] = ColorX;
                vertexes[offsetPlusBarByStride++] = ColorY;
                vertexes[offsetPlusBarByStride++] = ColorZ;
                vertexes[offsetPlusBarByStride++] = ColorW;

                int offsetPlusBarBy6 = generationOffsetForIndex + bar * 6;
                uint barPlusBarCount = (uint)(4 * (bar + spectrumBarCount * generation));
                indexes[offsetPlusBarBy6++] = barPlusBarCount;
                indexes[offsetPlusBarBy6++] = barPlusBarCount + 1;
                indexes[offsetPlusBarBy6++] = barPlusBarCount + 2;
                indexes[offsetPlusBarBy6++] = barPlusBarCount + 1;
                indexes[offsetPlusBarBy6++] = barPlusBarCount + 2;
                indexes[offsetPlusBarBy6++] = barPlusBarCount + 3;
            }
        }

        /// <summary>
        /// Sorts the indexes pointing to the vertexes by the distance to the camera's Z position of the vertexes.
        /// </summary>
        /// <param name="cameraPositionZ"></param>
        private void SortVerticesByCameraDistance(float cameraPositionZ)
        {
            const int TEN_POW_SEVEN = 10000000;
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                SIndexDistance[] distList = new SIndexDistance[spectrumBarGenerations * spectrumBarCount];
                int distListIndex = 0;
                int generationOffset;
                uint index;

                for (int generation = 0; generation < spectrumBarGenerations; generation++)
                {
                    generationOffset = generation * spectrumBarCount * 6;
                    for (int bar = 0; bar < spectrumBarCount; bar++)
                    {
                        index = indexes[generationOffset + bar * 6];
                        distList[distListIndex++] = new SIndexDistance
                        {
                            Index = index,
                            IntegerDistance = (int)((cameraPositionZ - vertexes[index + 3]) * TEN_POW_SEVEN)
                        };
                    }
                }

                uint[] newIndexes = new uint[indexes.Length];
                uint newIndex = 0;
                ParallelMergeSort(ref distList, 3);
                for (int i = 0; i < distList.Length; i++)
                {
                    newIndexes[newIndex++] = distList[i].Index;
                    newIndexes[newIndex++] = distList[i].Index + 1;
                    newIndexes[newIndex++] = distList[i].Index + 2;
                    newIndexes[newIndex++] = distList[i].Index + 1;
                    newIndexes[newIndex++] = distList[i].Index + 2;
                    newIndexes[newIndex++] = distList[i].Index + 3;
                }

                Array.Copy(newIndexes, 0, indexes, 0, newIndexes.Length);
            }

            void ParallelMergeSort(ref SIndexDistance[] distances, int depth)
            {
                SIndexDistance[] distances1 = new SIndexDistance[distances.Length / 2];
                SIndexDistance[] distances2 = new SIndexDistance[distances.Length - distances1.Length];
                Array.Copy(distances, distances1, distances1.Length);
                Array.Copy(distances, distances1.Length, distances2, 0, distances2.Length);
                Task[] taskArray = new Task[2];
                if (depth == 0)
                {
                    taskArray[0] = Task.Factory.StartNew(() => MergeSort(ref distances1));
                    taskArray[1] = Task.Factory.StartNew(() => MergeSort(ref distances2));
                }
                else
                {
                    taskArray[0] = Task.Factory.StartNew(() => ParallelMergeSort(ref distances1, depth - 1));
                    taskArray[1] = Task.Factory.StartNew(() => ParallelMergeSort(ref distances2, depth - 1));
                }
                Task.WaitAll(taskArray);
                int distancesIndex = 0;
                int distances1Index = 0;
                int distances2Index = 0;
                bool d1Finished = false;
                bool d2Finished = false;
                while (!d1Finished || !d2Finished)
                {
                    if (!d2Finished)
                    {
                        if (!d1Finished && (distances1[distances1Index].IntegerDistance >= distances2[distances2Index].IntegerDistance))
                        {
                            distances[distancesIndex++] = distances1[distances1Index++];
                            d1Finished = distances1Index == distances1.Length;
                        }
                        else
                        {
                            distances[distancesIndex++] = distances2[distances2Index++];
                            d2Finished = distances2Index == distances2.Length;
                        }
                    }
                    else
                    {
                        distances[distancesIndex++] = distances1[distances1Index++];
                        d1Finished = distances1Index == distances1.Length;
                    }
                }
            }

            void MergeSort(ref SIndexDistance[] distances)
            {
                int left = 0;
                int right = distances.Length - 1;
                InternalMergeSort(ref distances, left, right);

                void InternalMergeSort(ref SIndexDistance[] distances, int left, int right)
                {
                    if (left < right)
                    {
                        int mid = (left + right) / 2;
                        InternalMergeSort(ref distances, left, mid);
                        InternalMergeSort(ref distances, (mid + 1), right);
                        MergeSortedArray(ref distances, left, mid, right);
                    }

                    void MergeSortedArray(ref SIndexDistance[] distances, int left, int mid, int right)
                    {
                        int index = 0;
                        int totalElements = right - left + 1; //BODMAS rule
                        int rightStart = mid + 1;
                        int tempLocation = left;

                        SIndexDistance[] temp = new SIndexDistance[totalElements];

                        while ((left <= mid) && (rightStart <= right))
                            if (distances[left].IntegerDistance <= distances[rightStart].IntegerDistance)
                                temp[index++] = distances[left++];
                            else
                                temp[index++] = distances[rightStart++];

                        if (left > mid)
                            for (int j = rightStart; j <= right; j++)
                                temp[index++] = distances[rightStart++];
                        else
                            for (int j = left; j <= mid; j++)
                                temp[index++] = distances[left++];

                        Array.Copy(temp, 0, distances, tempLocation, totalElements);
                    }
                }
            }
        }

        /// <summary>
        /// Splits an interval into chunks.
        /// </summary>
        /// <param name="chunks">The desired number of chunks.</param>
        /// <param name="min">The lover boundary.</param>
        /// <param name="max">The upper boundary.</param>
        /// <returns>A <see cref="Vector2"/> array containing the boundaries for each chunk.</returns>
        private static Vector2[] SplitInterval(int chunks, float min, float max)
        {
            Vector2[] result = new Vector2[chunks];
            float size = (max - min) / chunks;
            for (int chunkNo = 0; chunkNo < chunks; chunkNo++)
            {
                result[chunkNo] = new Vector2(min + size * chunkNo, min + size * (chunkNo + 1));
            }
            return result;
        }
    }
}
