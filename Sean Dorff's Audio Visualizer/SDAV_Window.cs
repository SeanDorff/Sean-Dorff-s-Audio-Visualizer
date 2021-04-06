using Common;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private SpectrumBar[,] spectrumBars;
        private readonly uint generationsPerShader = 150;

        private SpectrumBarShader[] spectrumBarShaders;

        private double time;
        private bool mouseFirstMove = true;
        private Vector2 lastMousePos;

        public SDAV_Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            originalSize = nativeWindowSettings.Size;
            barBorders = SplitRange(spectrumBarCount, -1.0f, 1.0f);
        }

        protected override void OnLoad()
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
            {
                InitGL();
                InitCamera();
                InitWasAPIAudio();

                CursorGrabbed = true;

                InitSpectrumBars();
                BuildShaders();

                base.OnLoad();
            }
        }

        private void InitSpectrumBars()
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
            {
                spectrumBars = new SpectrumBar[spectrumBarGenerations, spectrumBarCount * 2 * 3 * 2];
                for (int i = 0; i < spectrumBarGenerations; i++)
                    for (int j = 0; j < spectrumBarCount; j++)
                        spectrumBars[i, j] = new SpectrumBar()
                        {
                            LowerLeft = Vector3.Zero,
                            LowerRight = Vector3.Zero,
                            UpperLeft = Vector3.Zero,
                            UpperRight = Vector3.Zero,
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
                const float movement = 0.1f;
                const float alphaDimm = 0.97f;
                for (int generation = (int)spectrumBarGenerations - 1; generation > 0; generation--)
                    for (int bar = 0; bar < spectrumBarCount; bar++)
                    {
                        spectrumBars[generation, bar] = spectrumBars[generation - 1, bar];
                        spectrumBars[generation, bar].LowerLeft.Z -= movement;
                        spectrumBars[generation, bar].LowerRight.Z -= movement;
                        spectrumBars[generation, bar].UpperLeft.Z -= movement;
                        spectrumBars[generation, bar].UpperRight.Z -= movement;
                        spectrumBars[generation, bar].Color.W *= alphaDimm;
                    }
            }

            void AddCurrentSpectrum()
            {
                for (int bar = 0; bar < spectrumBarCount; bar++)
                    spectrumBars[0, bar] = new SpectrumBar()
                    {
                        LowerLeft = new Vector3(barBorders[bar].X, 0, 0),
                        LowerRight = new Vector3(barBorders[bar].Y, 0, 0),
                        UpperLeft = new Vector3(barBorders[bar].X, deNullifiedSpectrumData(bar), 0),
                        UpperRight = new Vector3(barBorders[bar].Y, deNullifiedSpectrumData(bar), 0),
                        Color = new Vector4(barOfBarCount(bar), 1 - barOfBarCount(bar), 1, 0.75f)
                    };

                float barOfBarCount(int bar) => bar / (float)spectrumBarCount;
                float deNullifiedSpectrumData(int i) => (spectrumData != null) ? spectrumData[i] : 0.0f;
            }

            void TransformToVertexes()
            {
                using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
                {
                    Task[] taskArray = new Task[spectrumBarGenerations];
                    {
                        List<Tuple<int, int>> startParameters = new();

                        for (int shaderNo = 0; shaderNo < (spectrumBarGenerations / generationsPerShader); shaderNo++)
                            for (int generation = 0; generation < generationsPerShader; generation++)
                                startParameters.Add(new Tuple<int, int>(shaderNo, generation));

                        foreach (Tuple<int, int> startParameter in startParameters)
                            taskArray[(startParameter.Item1 * generationsPerShader) + startParameter.Item2] = Task.Factory.StartNew(() =>
                            TransformSpectrumToVertices(startParameter.Item1, startParameter.Item2));

                        startParameters.Clear();
                    }

                    Task.WaitAll(taskArray);
                }
            }

            void TransformSpectrumToVertices(int shaderNo, int generation)
            {
                const int stride = 4 * 3 + 4 * 4;
                for (int bar = 0; bar < spectrumBarCount; bar++)
                {
                    SpectrumBar spectrumBar = new();
                    try
                    {
                        spectrumBar = spectrumBars[shaderNo * generationsPerShader + generation, bar];
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        Debug.WriteLine(e);
                    }
                    int generationOffset = generation * (int)spectrumBarCount * stride;
                    int barByStride = bar * stride;
                    int offsetPlusBarByStride = generationOffset + barByStride;
                    float ColorX = spectrumBar.Color.X;
                    float ColorY = spectrumBar.Color.Y;
                    float ColorZ = spectrumBar.Color.Z;
                    float ColorW = spectrumBar.Color.W;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride] = spectrumBar.LowerLeft.X;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 1] = spectrumBar.LowerLeft.Y;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 2] = spectrumBar.LowerLeft.Z;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 3] = ColorX;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 4] = ColorY;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 5] = ColorZ;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 6] = ColorW;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 7] = spectrumBar.LowerRight.X;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 8] = spectrumBar.LowerRight.Y;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 9] = spectrumBar.LowerRight.Z;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 10] = ColorX;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 11] = ColorY;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 12] = ColorZ;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 13] = ColorW;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 14] = spectrumBar.UpperLeft.X;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 15] = spectrumBar.UpperLeft.Y;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 16] = spectrumBar.UpperLeft.Z;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 17] = ColorX;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 18] = ColorY;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 19] = ColorZ;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 20] = ColorW;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 21] = spectrumBar.UpperRight.X;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 22] = spectrumBar.UpperRight.Y;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 23] = spectrumBar.UpperRight.Z;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 24] = ColorX;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 25] = ColorY;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 26] = ColorZ;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexes[offsetPlusBarByStride + 27] = ColorW;
                    generationOffset = generation * (int)spectrumBarCount * 6;
                    int offsetPlusBarBy6 = generationOffset + bar * 6;
                    // original calculation: (uint)bar * 4 + spectrumBarCount * 4 * (uint)generation
                    // simplified calculation: 4 * ((uint)bar + spectrumBarCount * (uint)generation)
                    uint barPlusBarCount = 4 * ((uint)bar + spectrumBarCount * (uint)generation);
                    spectrumBarShaders[shaderNo].SpectrumBarVertexIndexes[offsetPlusBarBy6] = barPlusBarCount;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexIndexes[offsetPlusBarBy6 + 1] = barPlusBarCount + 1;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexIndexes[offsetPlusBarBy6 + 2] = barPlusBarCount + 2;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexIndexes[offsetPlusBarBy6 + 3] = barPlusBarCount + 1;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexIndexes[offsetPlusBarBy6 + 4] = barPlusBarCount + 2;
                    spectrumBarShaders[shaderNo].SpectrumBarVertexIndexes[offsetPlusBarBy6 + 5] = barPlusBarCount + 3;

                }
            }

            void SortVerticesByCameraDistance()
            {
                using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
                {
                    SIndexDistance[] distList = new SIndexDistance[spectrumBarGenerations * spectrumBarCount];
                    int distListIndex = 0;
                    for (int shaderNo = 0; shaderNo < (spectrumBarGenerations / generationsPerShader); shaderNo++)
                    {
                        for (int generation = 0; generation < generationsPerShader; generation++)
                            for (int bar = 0; bar < spectrumBarCount; bar++)
                            {
                                int generationOffset = generation * (int)spectrumBarCount * 6;
                                uint index = spectrumBarShaders[shaderNo].SpectrumBarVertexIndexes[generationOffset + bar * 6];
                                distList[distListIndex++] = new SIndexDistance { Index = index, Distance = camera.Position.Z - spectrumBarShaders[shaderNo].SpectrumBarVertexes[index + 3] };
                            }
                        uint[] newIndexes = new uint[spectrumBarShaders[shaderNo].SpectrumBarVertexIndexes.Length];
                        uint newIndex = 0;
                        MergeSort(ref distList);
                        foreach (SIndexDistance kvp in distList)
                        {
                            newIndexes[newIndex++] = kvp.Index;
                            newIndexes[newIndex++] = kvp.Index + 1;
                            newIndexes[newIndex++] = kvp.Index + 2;
                            newIndexes[newIndex++] = kvp.Index + 1;
                            newIndexes[newIndex++] = kvp.Index + 2;
                            newIndexes[newIndex++] = kvp.Index + 3;
                        }
                        spectrumBarShaders[shaderNo].SpectrumBarVertexIndexes = newIndexes;
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
                            {
                                if (distances[left].Distance <= distances[rightStart].Distance)
                                    temp[index++] = distances[left++];
                                else
                                    temp[index++] = distances[rightStart++];
                            }
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

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
            {
                time += e.Time;
                if (time > MathHelper.TwoPi)
                    time -= MathHelper.TwoPi;

                Matrix4 model = Matrix4.Identity;
                Matrix4 view = camera.GetViewMatrix();
                Matrix4 projection = camera.GetProjectionMatrix();

                UpdateSpectrumBars();
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                for (int shaderNo = 0; shaderNo < (spectrumBarGenerations / generationsPerShader); shaderNo++)
                {
                    SendSpectrumBarData(shaderNo);

                    spectrumBarShaders[shaderNo].Shader.Use();
                    GL.BindVertexArray(spectrumBarShaders[shaderNo].VertexArrayHandle);

                    spectrumBarShaders[shaderNo].Shader.SetMatrix4("model", model);
                    spectrumBarShaders[shaderNo].Shader.SetMatrix4("view", view);
                    spectrumBarShaders[shaderNo].Shader.SetMatrix4("projection", projection);

                    GL.DrawElements(PrimitiveType.Triangles, spectrumBarShaders[shaderNo].SpectrumBarVertexIndexes.Length, DrawElementsType.UnsignedInt, 0);
                }

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

                    MouseState mouseInput = MouseState;

                    if (mouseFirstMove) // this bool variable is initially set to true
                    {
                        lastMousePos = new Vector2(mouseInput.X, mouseInput.Y);
                        mouseFirstMove = false;
                    }
                    else
                    {
                        // Calculate the offset of the mouse position
                        var deltaX = mouseInput.X - lastMousePos.X;
                        var deltaY = mouseInput.Y - lastMousePos.Y;
                        lastMousePos = new Vector2(mouseInput.X, mouseInput.Y);

                        // Apply the camera pitch and yaw (we clamp the pitch in the camera class)
                        camera.Yaw += deltaX * mouseSensitivity;
                        camera.Pitch -= deltaY * mouseSensitivity; // reversed since y-coordinates range from bottom to top
                    }
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
            for (int shaderNo = 0; shaderNo < (spectrumBarGenerations / generationsPerShader); shaderNo++)
                spectrumBarShaders[shaderNo].Unload();
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
                spectrumBarShaders = new SpectrumBarShader[spectrumBarGenerations / generationsPerShader];
                for (int shaderNo = 0; shaderNo < (spectrumBarGenerations / generationsPerShader); shaderNo++)
                    BuildShader(shaderNo);
            }
        }

        private void BuildShader(int shaderNo)
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name + " (shaderNo: " + shaderNo + ")", true))
            {
                spectrumBarShaders[shaderNo] = new();

                spectrumBarShaders[shaderNo].SpectrumBarVertexes = new float[spectrumBarCount * ((4 * 3) + (4 * 4)) * generationsPerShader];
                spectrumBarShaders[shaderNo].SpectrumBarVertexIndexes = new uint[2 * 3 * spectrumBarCount * generationsPerShader];

                spectrumBarShaders[shaderNo].VertexArrayHandle = GL.GenVertexArray();
                GL.BindVertexArray(spectrumBarShaders[shaderNo].VertexArrayHandle);

                spectrumBarShaders[shaderNo].VertexBufferHandle = GL.GenBuffer();
                spectrumBarShaders[shaderNo].ElementBufferHandle = GL.GenBuffer();

                SendSpectrumBarData(shaderNo);

                spectrumBarShaders[shaderNo].Shader = new Shader("Shaders/spectrumBar.vert", "Shaders/spectrumBar.frag");
                spectrumBarShaders[shaderNo].Shader.Use();

                SetVertexAttribPointerAndArray("aPosition", 3, 7 * sizeof(float), 0);
                SetVertexAttribPointerAndArray("aColor", 4, 7 * sizeof(float), 3 * sizeof(float));
            }

            void SetVertexAttribPointerAndArray(string attribute, int size, int stride, int offset)
            {
                int location = spectrumBarShaders[shaderNo].Shader.GetAttribLocation(attribute);
                GL.VertexAttribPointer(location, size, VertexAttribPointerType.Float, false, stride, offset);
                GL.EnableVertexAttribArray(location);
            }
        }

        private void SendSpectrumBarData(int shaderNo)
        {
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, spectrumBarShaders[shaderNo].VertexBufferHandle);
                GL.BufferData(BufferTarget.ArrayBuffer, spectrumBarShaders[shaderNo].SpectrumBarVertexes.Length * sizeof(float), spectrumBarShaders[shaderNo].SpectrumBarVertexes, BufferUsageHint.StreamDraw);

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, spectrumBarShaders[shaderNo].ElementBufferHandle);
                GL.BufferData(BufferTarget.ElementArrayBuffer, spectrumBarShaders[shaderNo].SpectrumBarVertexIndexes.Length * sizeof(uint), spectrumBarShaders[shaderNo].SpectrumBarVertexIndexes, BufferUsageHint.StreamDraw);
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

        private struct SIndexDistance
        {
            public uint Index;
            public float Distance;
        }
    }
}