using Common;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

using System.Collections.Generic;
using System.Linq;

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
        private readonly Vector2[] chunkBorders;

        private readonly uint spectrumBarGenerations = 100;
        private SpectrumBar[,] spectrumBars;
        private readonly uint generationsPerShader = 100;

        private SpectrumBarShader[] spectrumBarShaders;

        private double time;
        private bool mouseFirstMove = true;
        private Vector2 lastMousePos;

        public SDAV_Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            originalSize = nativeWindowSettings.Size;
            chunkBorders = SplitRange(spectrumBarCount, -1.0f, 1.0f);
        }

        protected override void OnLoad()
        {
            InitGL();
            InitCamera();
            InitWasAPIAudio();

            CursorGrabbed = true;

            InitSpectrumBars();
            BuildShaders();

            base.OnLoad();
        }

        private void InitSpectrumBars()
        {
            using (new DisposableStopwatch("InitSpectrumBars", true))
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
            using (new DisposableStopwatch("UpdateSpectrumBars", true))
            {
                for (int i = (int)spectrumBarGenerations - 1; i > 0; i--)
                    for (int j = 0; j < spectrumBarCount; j++)
                    {
                        spectrumBars[i, j] = spectrumBars[i - 1, j];
                        spectrumBars[i, j].LowerLeft.Z = spectrumBars[i, j].LowerLeft.Z - 0.05f;
                        spectrumBars[i, j].LowerRight.Z = spectrumBars[i, j].LowerRight.Z - 0.05f;
                        spectrumBars[i, j].UpperLeft.Z = spectrumBars[i, j].UpperLeft.Z - 0.05f;
                        spectrumBars[i, j].UpperRight.Z = spectrumBars[i, j].UpperRight.Z - 0.05f;
                        spectrumBars[i, j].Color = new Vector4(spectrumBars[i, j].Color.Xyz, spectrumBars[i, j].Color.W * 0.99f);
                    }

                for (int i = 0; i < spectrumBarCount; i++)
                {
                    spectrumBars[0, i] = new SpectrumBar()
                    {
                        LowerLeft = new Vector3(chunkBorders[i].X, 0, 0),
                        LowerRight = new Vector3(chunkBorders[i].Y, 0, 0),
                        UpperLeft = new Vector3(chunkBorders[i].X, deNullifiedSpectrumData(i), 0),
                        UpperRight = new Vector3(chunkBorders[i].Y, deNullifiedSpectrumData(i), 0),
                        Color = new Vector4(i / (float)spectrumBarCount, 1 - (i / (float)spectrumBarCount), 1, 0.8f)
                    };
                }

                int stride = 4 * 3 + 4 * 4;
                for (int shaderNo = 0; shaderNo < (spectrumBarGenerations / generationsPerShader); shaderNo++)
                    for (int generation = 0; generation < generationsPerShader; generation++)
                        for (int bar = 0; bar < spectrumBarCount; bar++)
                        {
                            SpectrumBar spectrumBar = spectrumBars[shaderNo * generationsPerShader + generation, bar];
                            int generationOffset = generation * (int)spectrumBarCount * stride;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride] = spectrumBar.LowerLeft.X;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 1] = spectrumBar.LowerLeft.Y;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 2] = spectrumBar.LowerLeft.Z;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 3] = spectrumBar.Color.X;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 4] = spectrumBar.Color.Y;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 5] = spectrumBar.Color.Z;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 6] = spectrumBar.Color.W;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 7] = spectrumBar.LowerRight.X;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 8] = spectrumBar.LowerRight.Y;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 9] = spectrumBar.LowerRight.Z;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 10] = spectrumBar.Color.X;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 11] = spectrumBar.Color.Y;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 12] = spectrumBar.Color.Z;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 13] = spectrumBar.Color.W;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 14] = spectrumBar.UpperLeft.X;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 15] = spectrumBar.UpperLeft.Y;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 16] = spectrumBar.UpperLeft.Z;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 17] = spectrumBar.Color.X;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 18] = spectrumBar.Color.Y;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 19] = spectrumBar.Color.Z;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 20] = spectrumBar.Color.W;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 21] = spectrumBar.UpperRight.X;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 22] = spectrumBar.UpperRight.Y;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 23] = spectrumBar.UpperRight.Z;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 24] = spectrumBar.Color.X;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 25] = spectrumBar.Color.Y;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 26] = spectrumBar.Color.Z;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexes[generationOffset + bar * stride + 27] = spectrumBar.Color.W;
                            generationOffset = generation * (int)spectrumBarCount * 6;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexIndexes[generationOffset + bar * 6] = (uint)bar * 4 + spectrumBarCount * 4 * (uint)generation;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexIndexes[generationOffset + bar * 6 + 1] = (uint)bar * 4 + 1 + spectrumBarCount * 4 * (uint)generation;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexIndexes[generationOffset + bar * 6 + 2] = (uint)bar * 4 + 2 + spectrumBarCount * 4 * (uint)generation;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexIndexes[generationOffset + bar * 6 + 3] = (uint)bar * 4 + 1 + spectrumBarCount * 4 * (uint)generation;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexIndexes[generationOffset + bar * 6 + 4] = (uint)bar * 4 + 2 + spectrumBarCount * 4 * (uint)generation;
                            spectrumBarShaders[shaderNo].SpectrumBarVertexIndexes[generationOffset + bar * 6 + 5] = (uint)bar * 4 + 3 + spectrumBarCount * 4 * (uint)generation;
                        }

                Dictionary<uint, float> distances = new Dictionary<uint, float>();
                for (int shaderNo = 0; shaderNo < (spectrumBarGenerations / generationsPerShader); shaderNo++)
                {
                    for (int generation = 0; generation < generationsPerShader; generation++)
                        for (int bar = 0; bar < spectrumBarCount; bar++)
                        {
                            int generationOffset = generation * (int)spectrumBarCount * 6;
                            uint index = spectrumBarShaders[shaderNo].SpectrumBarVertexIndexes[generationOffset + bar * 6];
                            distances.Add(index, camera.Position.Z - spectrumBarShaders[shaderNo].SpectrumBarVertexes[index + 3]);
                        }
                    uint[] newIndexes = new uint[spectrumBarShaders[shaderNo].SpectrumBarVertexIndexes.Length];
                    uint newIndex = 0;
                    foreach (var distance in distances.OrderByDescending(i => i.Value))
                    {
                        newIndexes[newIndex++] = distance.Key;
                        newIndexes[newIndex++] = distance.Key + 1;
                        newIndexes[newIndex++] = distance.Key + 2;
                        newIndexes[newIndex++] = distance.Key + 1;
                        newIndexes[newIndex++] = distance.Key + 2;
                        newIndexes[newIndex++] = distance.Key + 3;
                    }
                    spectrumBarShaders[shaderNo].SpectrumBarVertexIndexes = newIndexes;
                }

                float deNullifiedSpectrumData(int i) => (spectrumData != null) ? spectrumData[i] : 0.0f;
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            using (new DisposableStopwatch("OnRenderFrame", true))
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
            using (new DisposableStopwatch("OnUpdateFrame", true))
            {
                if (IsFocused)
                {
                    KeyboardState keyInput = KeyboardState;

                    if (keyInput.IsKeyDown(Keys.Escape))
                        Close();

                    if (keyInput.IsKeyReleased(Keys.F))
                        ToggleFullscreen();

                    const float cameraSpeed = 0.5f;
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
            using (new DisposableStopwatch("OnResize", true))
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
            using (new DisposableStopwatch("ToggleFullscreen", true))
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
            using (new DisposableStopwatch("InitGL", true))
            {
                GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
                GL.Enable(EnableCap.DepthTest);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc((BlendingFactor)BlendingFactorSrc.SrcAlpha, (BlendingFactor)BlendingFactorDest.OneMinusSrcAlpha);
                //GL.BlendFunc((BlendingFactor)BlendingFactorSrc.SrcAlpha, (BlendingFactor)BlendingFactorDest.OneMinusSrcColor);
            }
        }

        private void InitCamera()
        {
            using (new DisposableStopwatch("InitCamera", true))
            {
                camera = new Camera(new Vector3(0, (Vector3.UnitY.Y / 2), Vector3.UnitZ.Z), Size.X / Size.Y);
            }
        }

        private void BuildShaders()
        {
            using (new DisposableStopwatch("BuildShaders", true))
            {
                spectrumBarShaders = new SpectrumBarShader[spectrumBarGenerations / generationsPerShader];
                for (int shaderNo = 0; shaderNo < (spectrumBarGenerations / generationsPerShader); shaderNo++)
                    BuildShader(shaderNo);
            }
        }

        private void BuildShader(int shaderNo)
        {
            using (new DisposableStopwatch("BuildShader (shaderNo: " + shaderNo + ")", true))
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
            using (new DisposableStopwatch("SendSpectrumBarData", true))
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, spectrumBarShaders[shaderNo].VertexBufferHandle);
                GL.BufferData(BufferTarget.ArrayBuffer, spectrumBarShaders[shaderNo].SpectrumBarVertexes.Length * sizeof(float), spectrumBarShaders[shaderNo].SpectrumBarVertexes, BufferUsageHint.StreamDraw);

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, spectrumBarShaders[shaderNo].ElementBufferHandle);
                GL.BufferData(BufferTarget.ElementArrayBuffer, spectrumBarShaders[shaderNo].SpectrumBarVertexIndexes.Length * sizeof(uint), spectrumBarShaders[shaderNo].SpectrumBarVertexIndexes, BufferUsageHint.StreamDraw);
            }
        }

        private void InitWasAPIAudio()
        {
            using (new DisposableStopwatch("InitWasAPIAudio", true))
            {
                wasAPIAudio = new WasAPIAudio((int)spectrumBarCount, minFrequency, maxFrequency, spectrumData => { this.spectrumData = spectrumData; });
                wasAPIAudio.StartListen();
            }
        }
    }
}