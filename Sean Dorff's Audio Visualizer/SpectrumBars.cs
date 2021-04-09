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
        private readonly float alphaDimm;

        private readonly Vector2[] barBorders;

        private float[] spectrumData;

        private GenericShader genericShader;
        private readonly Camera camera;

        public SpectrumBars(int spectrumBarGenerations, int spectrumBarCount, float alphaDimm, ref Camera camera)
        {
            this.spectrumBarGenerations = spectrumBarGenerations;
            this.spectrumBarCount = spectrumBarCount;
            this.alphaDimm = alphaDimm;

            barBorders = SplitRange(spectrumBarCount, -1.0f, 1.0f);

            this.camera = camera;

            spectrumBars = new SSpectrumBar[spectrumBarGenerations, spectrumBarCount * 2 * 3 * 2];

            InitSpectrumBars();
        }

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

        public void UpdateSpectrumBars(ref GenericShader genericShader, float[] spectrumData)
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                this.genericShader = genericShader;
                this.spectrumData = spectrumData;
                MoveBarGenerations();
                AddCurrentSpectrum();
                TransformToVertexes();
                SortVerticesByCameraDistance();
            }

            void MoveBarGenerations()
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
                            spectrumBar.Color.W *= alphaDimm;
                            spectrumBars[generation, bar] = spectrumBar;
                        }
                }
            }

            void AddCurrentSpectrum()
            {
                float loudness = GetCurrentLoudness();
                for (int bar = 0; bar < spectrumBarCount; bar++)
                    spectrumBars[0, bar] = new SSpectrumBar()
                    {
                        LowerLeft = new Vector4(barBorders[bar].X, 0, 0, 0),
                        LowerRight = new Vector4(barBorders[bar].Y, 0, 0, 0),
                        UpperLeft = new Vector4(barBorders[bar].X, DeNullifiedSpectrumData(bar), 0, 0),
                        UpperRight = new Vector4(barBorders[bar].Y, DeNullifiedSpectrumData(bar), 0, 0),
                        Color = new Vector4(loudness, 1 - barOfBarCount(bar), barOfBarCount(bar), 0.75f)
                    };

                float barOfBarCount(int bar) => bar / (float)spectrumBarCount;
            }

            void TransformToVertexes()
            {
#if (DEBUG)
                using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
                {
                    Task[] taskArray = new Task[spectrumBarGenerations];

                    foreach (int generation in Enumerable.Range(0, (int)spectrumBarGenerations).ToArray())
                        taskArray[generation] = Task.Factory.StartNew(() =>
                        TransformSpectrumToVertices(generation));

                    Task.WaitAll(taskArray);
                }
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
                genericShader.Vertexes[offsetPlusBarByStride++] = spectrumBar.LowerLeft.X;
                genericShader.Vertexes[offsetPlusBarByStride++] = spectrumBar.LowerLeft.Y;
                genericShader.Vertexes[offsetPlusBarByStride++] = spectrumBar.LowerLeft.Z;
                genericShader.Vertexes[offsetPlusBarByStride++] = spectrumBar.LowerLeft.W;
                genericShader.Vertexes[offsetPlusBarByStride++] = ColorX;
                genericShader.Vertexes[offsetPlusBarByStride++] = ColorY;
                genericShader.Vertexes[offsetPlusBarByStride++] = ColorZ;
                genericShader.Vertexes[offsetPlusBarByStride++] = ColorW;
                genericShader.Vertexes[offsetPlusBarByStride++] = spectrumBar.LowerRight.X;
                genericShader.Vertexes[offsetPlusBarByStride++] = spectrumBar.LowerRight.Y;
                genericShader.Vertexes[offsetPlusBarByStride++] = spectrumBar.LowerRight.Z;
                genericShader.Vertexes[offsetPlusBarByStride++] = spectrumBar.LowerRight.W;
                genericShader.Vertexes[offsetPlusBarByStride++] = ColorX;
                genericShader.Vertexes[offsetPlusBarByStride++] = ColorY;
                genericShader.Vertexes[offsetPlusBarByStride++] = ColorZ;
                genericShader.Vertexes[offsetPlusBarByStride++] = ColorW;
                genericShader.Vertexes[offsetPlusBarByStride++] = spectrumBar.UpperLeft.X;
                genericShader.Vertexes[offsetPlusBarByStride++] = spectrumBar.UpperLeft.Y;
                genericShader.Vertexes[offsetPlusBarByStride++] = spectrumBar.UpperLeft.Z;
                genericShader.Vertexes[offsetPlusBarByStride++] = spectrumBar.UpperLeft.W;
                genericShader.Vertexes[offsetPlusBarByStride++] = ColorX;
                genericShader.Vertexes[offsetPlusBarByStride++] = ColorY;
                genericShader.Vertexes[offsetPlusBarByStride++] = ColorZ;
                genericShader.Vertexes[offsetPlusBarByStride++] = ColorW;
                genericShader.Vertexes[offsetPlusBarByStride++] = spectrumBar.UpperRight.X;
                genericShader.Vertexes[offsetPlusBarByStride++] = spectrumBar.UpperRight.Y;
                genericShader.Vertexes[offsetPlusBarByStride++] = spectrumBar.UpperRight.Z;
                genericShader.Vertexes[offsetPlusBarByStride++] = spectrumBar.UpperRight.W;
                genericShader.Vertexes[offsetPlusBarByStride++] = ColorX;
                genericShader.Vertexes[offsetPlusBarByStride++] = ColorY;
                genericShader.Vertexes[offsetPlusBarByStride++] = ColorZ;
                genericShader.Vertexes[offsetPlusBarByStride++] = ColorW;

                int offsetPlusBarBy6 = generationOffsetForIndex + bar * 6;
                uint barPlusBarCount = (uint)(4 * (bar + spectrumBarCount * generation));
                genericShader.Indexes[offsetPlusBarBy6++] = barPlusBarCount;
                genericShader.Indexes[offsetPlusBarBy6++] = barPlusBarCount + 1;
                genericShader.Indexes[offsetPlusBarBy6++] = barPlusBarCount + 2;
                genericShader.Indexes[offsetPlusBarBy6++] = barPlusBarCount + 1;
                genericShader.Indexes[offsetPlusBarBy6++] = barPlusBarCount + 2;
                genericShader.Indexes[offsetPlusBarBy6++] = barPlusBarCount + 3;
            }
        }

        private void SortVerticesByCameraDistance()
        {
            const int TEN_POW_SEVEN = 10000000;
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                SIndexDistance[] distList = new SIndexDistance[spectrumBarGenerations * spectrumBarCount];
                int distListIndex = 0;
                float[] spectrumBarVertexes = genericShader.Vertexes;
                uint[] spectrumBarVertexIndexes = genericShader.Indexes;
                int generationOffset;
                uint index;

                for (int generation = 0; generation < spectrumBarGenerations; generation++)
                {
                    generationOffset = generation * spectrumBarCount * 6;
                    for (int bar = 0; bar < spectrumBarCount; bar++)
                    {
                        index = spectrumBarVertexIndexes[generationOffset + bar * 6];
                        distList[distListIndex++] = new SIndexDistance
                        {
                            Index = index,
                            IntegerDistance = (int)((camera.Position.Z - spectrumBarVertexes[index + 3]) * TEN_POW_SEVEN)
                        };
                    }
                }

                uint[] newIndexes = new uint[spectrumBarVertexIndexes.Length];
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

                Array.Copy(newIndexes, 0, genericShader.Indexes, 0, newIndexes.Length);
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

        private static Vector2[] SplitRange(int chunks, float min, float max)
        {
            Vector2[] result = new Vector2[chunks];
            float size = (max - min) / chunks;
            for (int chunkNo = 0; chunkNo < chunks; chunkNo++)
            {
                result[chunkNo] = new Vector2(min + size * chunkNo, min + size * (chunkNo + 1));
            }
            return result;
        }

        private float GetCurrentLoudness()
        {
            const float FRACTION = 15;
            int scanLimit = (int)(spectrumBarCount / FRACTION);
            float loudness = 0.0f;
            for (int i = 0; i < scanLimit; i++)
                loudness += DeNullifiedSpectrumData(i);
            return Math.Clamp(loudness / FRACTION, 0.0f, 1.0f);
        }

        private float DeNullifiedSpectrumData(int i) => (spectrumData != null) ? spectrumData[i] : 0.0f;
    }
}
