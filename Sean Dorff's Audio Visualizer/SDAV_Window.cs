using Common;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using WasAPI;

namespace Sean_Dorff_s_Audio_Visualizer
{
    public class SDAV_Window : GameWindow
    {
        private Camera camera;
        private bool isFullScreen = false;
        private readonly Vector2i originalSize;

        private WasAPIAudio wasAPIAudio;
        private float[] spectrumData;
        private readonly uint spectrumBarCount = 1024;
        private readonly int minFrequency = 20;
        private readonly int maxFrequency = 20000;
        private readonly Vector2[] barBorders;

        private readonly uint spectrumBarGenerations = 150;
        private SSpectrumBar[,] spectrumBars;

        private readonly uint starCount = 15000;
        private SStar[] stars;

        private GenericShader genericShader;
        private readonly int spectrumBarVertexesCount;
        private readonly int starVertexesCount;
        private readonly int spectrumBarIndexesCount;
        private readonly int starIndexesCount;

        private double time;
        private readonly Random random = new();

        public SDAV_Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            originalSize = nativeWindowSettings.Size;
            barBorders = SplitRange(spectrumBarCount, -1.0f, 1.0f);
            spectrumBarVertexesCount = (int)(spectrumBarCount * spectrumBarGenerations * 32);
            starVertexesCount = (int)(starCount * 8);
            spectrumBarIndexesCount = (int)(spectrumBarCount * spectrumBarGenerations * 6);
            starIndexesCount = (int)starCount;
        }

        protected override void OnLoad()
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
            {
                CursorGrabbed = true;

                InitGL();
                InitCamera();
                InitWasAPIAudio();

                InitSpectrumBars();
                InitStars();
                BuildShaders();

                base.OnLoad();
            }
        }

        private void InitSpectrumBars()
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
            {
                spectrumBars = new SSpectrumBar[spectrumBarGenerations, spectrumBarCount * 2 * 3 * 2];
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

        private void InitStars()
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
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

        private void UpdateSpectrumBars()
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
            {
                MoveBarGenerations();
                AddCurrentSpectrum();
                TransformToVertexes();
                SortVerticesByCameraDistance();
            }

            void MoveBarGenerations()
            {
                using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
                {
                    const float alphaDimm = 0.97f;
                    for (int generation = (int)spectrumBarGenerations - 1; generation > 0; generation--)
                        for (int bar = 0; bar < spectrumBarCount; bar++)
                        {
                            SSpectrumBar spectrumBar = spectrumBars[generation - 1, bar];
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
                using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
                {
                    Task[] taskArray = new Task[spectrumBarGenerations];

                    foreach (int generation in Enumerable.Range(0, (int)spectrumBarGenerations).ToArray())
                        taskArray[generation] = Task.Factory.StartNew(() =>
                        TransformSpectrumToVertices(generation));

                    Task.WaitAll(taskArray);
                }
            }

            void TransformSpectrumToVertices(int generation)
            {
                const int stride = 4 * 4 + 4 * 4;
                SSpectrumBar spectrumBar = new();
                int generationOffset = 0;
                int barByStride = 0;
                int offsetPlusBarByStride = 0;
                float ColorX = 0;
                float ColorY = 0;
                float ColorZ = 0;
                float ColorW = 0;
                for (int bar = 0; bar < spectrumBarCount; bar++)
                {
                    spectrumBar = spectrumBars[generation, bar];
                    generationOffset = generation * (int)spectrumBarCount * stride;
                    barByStride = bar * stride;
                    offsetPlusBarByStride = generationOffset + barByStride;
                    ColorX = spectrumBar.Color.X;
                    ColorY = spectrumBar.Color.Y;
                    ColorZ = spectrumBar.Color.Z;
                    ColorW = spectrumBar.Color.W;
                    genericShader.Vertexes[offsetPlusBarByStride] = spectrumBar.LowerLeft.X;
                    genericShader.Vertexes[offsetPlusBarByStride + 1] = spectrumBar.LowerLeft.Y;
                    genericShader.Vertexes[offsetPlusBarByStride + 2] = spectrumBar.LowerLeft.Z;
                    genericShader.Vertexes[offsetPlusBarByStride + 3] = spectrumBar.LowerLeft.W;
                    genericShader.Vertexes[offsetPlusBarByStride + 4] = ColorX;
                    genericShader.Vertexes[offsetPlusBarByStride + 5] = ColorY;
                    genericShader.Vertexes[offsetPlusBarByStride + 6] = ColorZ;
                    genericShader.Vertexes[offsetPlusBarByStride + 7] = ColorW;
                    genericShader.Vertexes[offsetPlusBarByStride + 8] = spectrumBar.LowerRight.X;
                    genericShader.Vertexes[offsetPlusBarByStride + 9] = spectrumBar.LowerRight.Y;
                    genericShader.Vertexes[offsetPlusBarByStride + 10] = spectrumBar.LowerRight.Z;
                    genericShader.Vertexes[offsetPlusBarByStride + 11] = spectrumBar.LowerRight.W;
                    genericShader.Vertexes[offsetPlusBarByStride + 12] = ColorX;
                    genericShader.Vertexes[offsetPlusBarByStride + 13] = ColorY;
                    genericShader.Vertexes[offsetPlusBarByStride + 14] = ColorZ;
                    genericShader.Vertexes[offsetPlusBarByStride + 15] = ColorW;
                    genericShader.Vertexes[offsetPlusBarByStride + 16] = spectrumBar.UpperLeft.X;
                    genericShader.Vertexes[offsetPlusBarByStride + 17] = spectrumBar.UpperLeft.Y;
                    genericShader.Vertexes[offsetPlusBarByStride + 18] = spectrumBar.UpperLeft.Z;
                    genericShader.Vertexes[offsetPlusBarByStride + 19] = spectrumBar.UpperLeft.W;
                    genericShader.Vertexes[offsetPlusBarByStride + 20] = ColorX;
                    genericShader.Vertexes[offsetPlusBarByStride + 21] = ColorY;
                    genericShader.Vertexes[offsetPlusBarByStride + 22] = ColorZ;
                    genericShader.Vertexes[offsetPlusBarByStride + 23] = ColorW;
                    genericShader.Vertexes[offsetPlusBarByStride + 24] = spectrumBar.UpperRight.X;
                    genericShader.Vertexes[offsetPlusBarByStride + 25] = spectrumBar.UpperRight.Y;
                    genericShader.Vertexes[offsetPlusBarByStride + 26] = spectrumBar.UpperRight.Z;
                    genericShader.Vertexes[offsetPlusBarByStride + 27] = spectrumBar.UpperRight.W;
                    genericShader.Vertexes[offsetPlusBarByStride + 28] = ColorX;
                    genericShader.Vertexes[offsetPlusBarByStride + 29] = ColorY;
                    genericShader.Vertexes[offsetPlusBarByStride + 30] = ColorZ;
                    genericShader.Vertexes[offsetPlusBarByStride + 31] = ColorW;
                    generationOffset = generation * (int)spectrumBarCount * 6;
                    int offsetPlusBarBy6 = generationOffset + bar * 6;
                    uint barPlusBarCount = 4 * ((uint)bar + spectrumBarCount * (uint)generation);
                    genericShader.Indexes[offsetPlusBarBy6] = barPlusBarCount;
                    genericShader.Indexes[offsetPlusBarBy6 + 1] = barPlusBarCount + 1;
                    genericShader.Indexes[offsetPlusBarBy6 + 2] = barPlusBarCount + 2;
                    genericShader.Indexes[offsetPlusBarBy6 + 3] = barPlusBarCount + 1;
                    genericShader.Indexes[offsetPlusBarBy6 + 4] = barPlusBarCount + 2;
                    genericShader.Indexes[offsetPlusBarBy6 + 5] = barPlusBarCount + 3;
                }
            }

            void SortVerticesByCameraDistance()
            {
                const int cTenPowSeven = 10000000;
                using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
                {
                    SIndexDistance[] distList = new SIndexDistance[spectrumBarGenerations * spectrumBarCount];
                    int distListIndex = 0;
                    float[] spectrumBarVertexes = genericShader.Vertexes;
                    uint[] spectrumBarVertexIndexes = genericShader.Indexes;
                    int generationOffset = 0;
                    uint index = 0;

                    for (int generation = 0; generation < spectrumBarGenerations; generation++)
                        for (int bar = 0; bar < spectrumBarCount; bar++)
                        {
                            generationOffset = generation * (int)spectrumBarCount * 6;
                            index = spectrumBarVertexIndexes[generationOffset + bar * 6];
                            distList[distListIndex++] = new SIndexDistance
                            {
                                Index = index,
                                IntegerDistance = (int)((camera.Position.Z - spectrumBarVertexes[index + 3]) * cTenPowSeven)
                            };
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
        }

        private void UpdateStars()
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
            {
                const float alphaDimm = 0.97f;
                int remainingGenerator = (int)(starCount / spectrumBarGenerations);
                for (int i = 0; i < starCount; i++)
                {
                    SStar star = stars[i];
                    star.Generation += 1;
                    star.Color.W *= alphaDimm;
                    if ((star.Generation <= 0) || (star.Generation > 150))
                    {
                        if (remainingGenerator-- > 0)
                        {
                            star.Generation = 0;
                            star.Position = new Vector3((float)random.NextDouble() * 4 - 2, (float)random.NextDouble() * 4 - 2, 0.0f);
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
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
            {
                time += e.Time;
                if (time > MathHelper.TwoPi)
                    time -= MathHelper.TwoPi;

                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                genericShader.VertexesCount = spectrumBarVertexesCount;
                genericShader.IndexesCount = spectrumBarIndexesCount;
                UpdateSpectrumBars();
                genericShader.Use();
                genericShader.SendData();
                genericShader.SetModelViewProjection(camera);
                genericShader.SetVertexAttribPointerAndArrays();
                genericShader.SetFloat("drift", 0.1f);
                genericShader.DrawTriangleElements();

                genericShader.VertexesCount = starVertexesCount;
                genericShader.IndexesCount = starIndexesCount;
                UpdateStars();
                genericShader.Use();
                genericShader.SendData();
                genericShader.SetModelViewProjection(camera);
                genericShader.SetVertexAttribPointerAndArrays();
                genericShader.SetFloat("drift", 0.1f);
                genericShader.DrawPointElements();

                SwapBuffers();

                base.OnRenderFrame(e);
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
            {
                if (IsFocused)
                {
                    KeyboardState keyInput = KeyboardState;

                    if (keyInput.IsKeyDown(Keys.Escape))
                        Close();

                    if (keyInput.IsKeyReleased(Keys.F))
                        ToggleFullscreen();

                    const float cameraSpeed = 1.0f;
                    const float mouseSensitivity = 0.2f;

                    if (keyInput.IsKeyDown(Keys.W))
                    {
                        camera.Position += camera.Front * cameraSpeed * (float)e.Time; // Forward
                    }

                    if (keyInput.IsKeyDown(Keys.S))
                    {
                        camera.Position -= camera.Front * cameraSpeed * (float)e.Time; // Backwards
                    }
                    if (keyInput.IsKeyDown(Keys.A))
                    {
                        camera.Position -= camera.Right * cameraSpeed * (float)e.Time; // Left
                    }
                    if (keyInput.IsKeyDown(Keys.D))
                    {
                        camera.Position += camera.Right * cameraSpeed * (float)e.Time; // Right
                    }
                    if (keyInput.IsKeyDown(Keys.Space))
                    {
                        camera.Position += camera.Up * cameraSpeed * (float)e.Time; // Up
                    }
                    if (keyInput.IsKeyDown(Keys.LeftShift))
                    {
                        camera.Position -= camera.Up * cameraSpeed * (float)e.Time; // Down
                    }
                    // Apply the camera pitch and yaw (we clamp the pitch in the camera class)
                    camera.Yaw += (MouseState.X - MouseState.PreviousX) * mouseSensitivity;
                    camera.Pitch -= (MouseState.Y - MouseState.PreviousY) * mouseSensitivity; // reversed since y-coordinates range from bottom to top
                }
                base.OnUpdateFrame(e);
            }
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
            {
                GL.Viewport(0, 0, e.Width, e.Height);
                camera.AspectRatio = Size.X / (float)Size.Y;
                base.OnResize(e);
            }
        }

        protected override void OnClosed()
        {
            base.OnClosed();
            OnUnload();
        }

        protected override void OnUnload()
        {
            wasAPIAudio.StopListen();
            genericShader.Unload();
            base.OnUnload();
        }

        private static Vector2[] SplitRange(uint chunks, float min, float max)
        {
            Vector2[] result = new Vector2[chunks];
            float size = (max - min) / chunks;
            for (int chunkNo = 0; chunkNo < chunks; chunkNo++)
            {
                result[chunkNo] = new Vector2(min + size * chunkNo, min + size * (chunkNo + 1));
            }
            return result;
        }

        private void ToggleFullscreen()
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
            {
                if (isFullScreen)
                {
                    WindowBorder = WindowBorder.Resizable;
                    WindowState = WindowState.Normal;
                    Size = originalSize;
                }
                else
                {
                    WindowBorder = WindowBorder.Hidden;
                    WindowState = WindowState.Fullscreen;
                }

                isFullScreen = !isFullScreen;
            }
        }

        private static void InitGL()
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
            {
                GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
                GL.Enable(EnableCap.DepthTest);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc((BlendingFactor)BlendingFactorSrc.SrcAlpha, (BlendingFactor)BlendingFactorDest.OneMinusSrcAlpha);
            }
        }

        private void InitCamera()
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
            {
                camera = new Camera(new Vector3(0, (Vector3.UnitY.Y / 2), Vector3.UnitZ.Z), Size.X / Size.Y);
            }
        }

        private void BuildShaders()
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
            {
                genericShader = new((uint)Math.Max(spectrumBarVertexesCount, starVertexesCount), (uint)Math.Max(spectrumBarIndexesCount, starIndexesCount));
            }
        }

        private void InitWasAPIAudio()
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
            {
                wasAPIAudio = new WasAPIAudio((int)spectrumBarCount, minFrequency, maxFrequency, spectrumData => { this.spectrumData = spectrumData; });
                wasAPIAudio.StartListen();
            }
        }

        private float GetCurrentLoudness()
        {
            const int iFraction = 15;
            float loudness = 0.0f;
            for (int i = 0; i < spectrumBarCount / iFraction; i++)
                loudness += DeNullifiedSpectrumData(i);
            return Math.Clamp(loudness / iFraction, 0.0f, 1.0f);
        }

        private float DeNullifiedSpectrumData(int i) => (spectrumData != null) ? spectrumData[i] : 0.0f;
    }
}