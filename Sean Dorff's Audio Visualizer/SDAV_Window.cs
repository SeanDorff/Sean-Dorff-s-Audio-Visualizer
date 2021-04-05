using Common;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

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
        private Vector2[] chunkBorders;

        private readonly uint spectrumBarGenerations = 5;
        private SpectrumBar[,] spectrumBars;

        private float[] spectrumBarVertexes;
        private uint[] spectrumBarVertexIndexes;
        private SpectrumBarShader spectrumBarShader;

        private double time;
        private bool mouseFirstMove = true;
        private Vector2 lastMousePos;

        public SDAV_Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            originalSize = nativeWindowSettings.Size;
        }

        protected override void OnLoad()
        {
            InitGL();
            InitCamera();
            InitWasAPIAudio();

            CursorGrabbed = true;

            chunkBorders = SplitRange(spectrumBarCount, -1.0f, 1.0f);
            spectrumBarVertexes = new float[spectrumBarCount * ((4 * 3) + (4 * 4)) * spectrumBarGenerations];
            spectrumBarVertexIndexes = new uint[2 * 3 * spectrumBarCount * spectrumBarGenerations];
            InitSpectrumBars();

            BuildShader();

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
                        spectrumBars[i, j].LowerLeft.Z = spectrumBars[i, j].LowerLeft.Z + 0.1f;
                        spectrumBars[i, j].LowerRight.Z = spectrumBars[i, j].LowerRight.Z + 0.1f;
                        spectrumBars[i, j].UpperLeft.Z = spectrumBars[i, j].UpperLeft.Z + 0.1f;
                        spectrumBars[i, j].UpperRight.Z = spectrumBars[i, j].UpperRight.Z + 0.1f;
                    }

                for (int i = 0; i < spectrumBarCount; i++)
                {
                    spectrumBars[0, i] = new SpectrumBar()
                    {
                        LowerLeft = new Vector3(chunkBorders[i].X, 0, 0),
                        LowerRight = new Vector3(chunkBorders[i].Y, 0, 0),
                        UpperLeft = new Vector3(chunkBorders[i].X, deNullifiedSpectrumData(i), 0),
                        UpperRight = new Vector3(chunkBorders[i].Y, deNullifiedSpectrumData(i), 0),
                        Color = new Vector4(i / (float)spectrumBarCount, 1 - (i / (float)spectrumBarCount), 1, 1)
                    };
                }

                int stride = 4 * 3 + 4 * 4;
                for (int j = 0; j < spectrumBarGenerations; j++)
                    for (int i = 0; i < spectrumBarCount; i++)
                    {
                        SpectrumBar spectrumBar = spectrumBars[j, i];
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride] = spectrumBar.LowerLeft.X;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 1] = spectrumBar.LowerLeft.Y;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 2] = spectrumBar.LowerLeft.Z;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 3] = spectrumBar.Color.X;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 4] = spectrumBar.Color.Y;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 5] = spectrumBar.Color.Z;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 6] = spectrumBar.Color.W;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 7] = spectrumBar.LowerRight.X;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 8] = spectrumBar.LowerRight.Y;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 9] = spectrumBar.LowerRight.Z;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 10] = spectrumBar.Color.X;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 11] = spectrumBar.Color.Y;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 12] = spectrumBar.Color.Z;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 13] = spectrumBar.Color.W;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 14] = spectrumBar.UpperLeft.X;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 15] = spectrumBar.UpperLeft.Y;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 16] = spectrumBar.UpperLeft.Z;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 17] = spectrumBar.Color.X;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 18] = spectrumBar.Color.Y;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 19] = spectrumBar.Color.Z;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 20] = spectrumBar.Color.W;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 21] = spectrumBar.UpperRight.X;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 22] = spectrumBar.UpperRight.Y;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 23] = spectrumBar.UpperRight.Z;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 24] = spectrumBar.Color.X;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 25] = spectrumBar.Color.Y;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 26] = spectrumBar.Color.Z;
                        spectrumBarVertexes[j * spectrumBarGenerations * stride + i * stride + 27] = spectrumBar.Color.W;
                        spectrumBarVertexIndexes[j * spectrumBarCount * 6 + i * 6] = (uint)i * 4 + (uint)j * spectrumBarCount;
                        spectrumBarVertexIndexes[j * spectrumBarCount * 6 + i * 6 + 1] = (uint)i * 4 + 1 + (uint)j * spectrumBarCount;
                        spectrumBarVertexIndexes[j * spectrumBarCount * 6 + i * 6 + 2] = (uint)i * 4 + 2 + (uint)j * spectrumBarCount;
                        spectrumBarVertexIndexes[j * spectrumBarCount * 6 + i * 6 + 3] = (uint)i * 4 + 1 + (uint)j * spectrumBarCount;
                        spectrumBarVertexIndexes[j * spectrumBarCount * 6 + i * 6 + 4] = (uint)i * 4 + 2 + (uint)j * spectrumBarCount;
                        spectrumBarVertexIndexes[j * spectrumBarCount * 6 + i * 6 + 5] = (uint)i * 4 + 3 + (uint)j * spectrumBarCount;
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

                UpdateSpectrumBars();
                SendSpectrumBarData();

                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                spectrumBarShader.Shader.Use();
                GL.BindVertexArray(spectrumBarShader.VertexArrayHandle);

                var model = Matrix4.Identity;

                spectrumBarShader.Shader.SetMatrix4("model", model);
                spectrumBarShader.Shader.SetMatrix4("view", camera.GetViewMatrix());
                spectrumBarShader.Shader.SetMatrix4("projection", camera.GetProjectionMatrix());

                GL.DrawElements(PrimitiveType.Triangles, spectrumBarVertexIndexes.Length, DrawElementsType.UnsignedInt, 0);

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
            spectrumBarShader.Unload();
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
            }
        }

        private void InitCamera()
        {
            using (new DisposableStopwatch("InitCamera", true))
            {
                camera = new Camera(new Vector3(0, (Vector3.UnitY.Y / 2), Vector3.UnitZ.Z), Size.X / Size.Y);
            }
        }

        private void BuildShader()
        {
            using (new DisposableStopwatch("BuildShader", true))
            {
                spectrumBarShader = new();
                spectrumBarShader.VertexArrayHandle = GL.GenVertexArray();
                GL.BindVertexArray(spectrumBarShader.VertexArrayHandle);

                spectrumBarShader.VertexBufferHandle = GL.GenBuffer();
                spectrumBarShader.ElementBufferHandle = GL.GenBuffer();

                SendSpectrumBarData();

                spectrumBarShader.Shader = new Shader("Shaders/spectrumBar.vert", "Shaders/spectrumBar.frag");
                spectrumBarShader.Shader.Use();

                SetVertexAttribPointerAndArray("aPosition", 3, 7 * sizeof(float), 0);
                SetVertexAttribPointerAndArray("aColor", 4, 7 * sizeof(float), 3 * sizeof(float));
            }

            void SetVertexAttribPointerAndArray(string attribute, int size, int stride, int offset)
            {
                int location = spectrumBarShader.Shader.GetAttribLocation(attribute);
                GL.VertexAttribPointer(location, size, VertexAttribPointerType.Float, false, stride, offset);
                GL.EnableVertexAttribArray(location);
            }
        }

        private void SendSpectrumBarData()
        {
            using (new DisposableStopwatch("SendSpectrumBarData", true))
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, spectrumBarShader.VertexBufferHandle);
                GL.BufferData(BufferTarget.ArrayBuffer, spectrumBarVertexes.Length * sizeof(float), spectrumBarVertexes, BufferUsageHint.DynamicDraw);

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, spectrumBarShader.ElementBufferHandle);
                GL.BufferData(BufferTarget.ElementArrayBuffer, spectrumBarVertexIndexes.Length * sizeof(uint), spectrumBarVertexIndexes, BufferUsageHint.DynamicDraw);
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