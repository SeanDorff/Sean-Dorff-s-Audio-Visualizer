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
        private int spectrumBarCount = 64;
        private int minFrequency = 20;
        private int maxFrequency = 20000;
        private Vector2[] chunkBorders;

        private int spectrumBarGenerations = 1;
        private SpectrumBar[,] spectrumBars;

        private float[] spectrumBarVertexes;
        private uint[] spectrumBarVertexIndexes;

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
            chunkBorders = splitRange(spectrumBarCount, -1.0f, 1.0f);
            spectrumBarVertexes = new float[spectrumBarCount * ((4 * 3) + (4 * 4))];
            spectrumBarVertexIndexes = new uint[2 * 3 * spectrumBarCount];

            BuildShader();

            // construct vertex shader

            base.OnLoad();
        }

        private void UpdateSpectrumBars()
        {
            using (new DisposableStopwatch("Updating spectrum bar data", true))
            {
                if (spectrumData != null)
                {
                    spectrumBars = new SpectrumBar[spectrumBarGenerations, spectrumBarCount * 2 * 3 * 2];
                    for (int i = 0; i < spectrumBarCount; i++)
                    {
                        spectrumBars[0, i] = new SpectrumBar()
                        {
                            LowerLeft = new Vector3(chunkBorders[i].X, 0, 0),
                            LowerRight = new Vector3(chunkBorders[i].Y, 0, 0),
                            UpperLeft = new Vector3(chunkBorders[i].X, spectrumData[i], 0),
                            UpperRight = new Vector3(chunkBorders[i].Y, spectrumData[i], 0),
                            Color = new Color4(1, 1, 1, 1)
                        };
                    }

                    int stride = 4 * 3 + 4 * 4;
                    for (int i = 0; i < spectrumBarCount; i++)
                    {
                        SpectrumBar spectrumBar = spectrumBars[0, i];
                        spectrumBarVertexes[i * stride] = spectrumBar.LowerLeft.X;
                        spectrumBarVertexes[i * stride + 1] = spectrumBar.LowerLeft.Y;
                        spectrumBarVertexes[i * stride + 2] = spectrumBar.LowerLeft.Z;
                        spectrumBarVertexes[i * stride + 3] = spectrumBar.Color.R;
                        spectrumBarVertexes[i * stride + 4] = spectrumBar.Color.G;
                        spectrumBarVertexes[i * stride + 5] = spectrumBar.Color.B;
                        spectrumBarVertexes[i * stride + 6] = spectrumBar.Color.A;
                        spectrumBarVertexes[i * stride + 7] = spectrumBar.LowerRight.X;
                        spectrumBarVertexes[i * stride + 8] = spectrumBar.LowerRight.Y;
                        spectrumBarVertexes[i * stride + 9] = spectrumBar.LowerRight.Z;
                        spectrumBarVertexes[i * stride + 10] = spectrumBar.Color.R;
                        spectrumBarVertexes[i * stride + 11] = spectrumBar.Color.G;
                        spectrumBarVertexes[i * stride + 12] = spectrumBar.Color.B;
                        spectrumBarVertexes[i * stride + 13] = spectrumBar.Color.A;
                        spectrumBarVertexes[i * stride + 14] = spectrumBar.UpperLeft.X;
                        spectrumBarVertexes[i * stride + 15] = spectrumBar.UpperLeft.Y;
                        spectrumBarVertexes[i * stride + 16] = spectrumBar.UpperLeft.Z;
                        spectrumBarVertexes[i * stride + 17] = spectrumBar.Color.R;
                        spectrumBarVertexes[i * stride + 18] = spectrumBar.Color.G;
                        spectrumBarVertexes[i * stride + 19] = spectrumBar.Color.B;
                        spectrumBarVertexes[i * stride + 20] = spectrumBar.Color.A;
                        spectrumBarVertexes[i * stride + 21] = spectrumBar.UpperRight.X;
                        spectrumBarVertexes[i * stride + 22] = spectrumBar.UpperRight.Y;
                        spectrumBarVertexes[i * stride + 23] = spectrumBar.UpperRight.Z;
                        spectrumBarVertexes[i * stride + 24] = spectrumBar.Color.R;
                        spectrumBarVertexes[i * stride + 25] = spectrumBar.Color.G;
                        spectrumBarVertexes[i * stride + 26] = spectrumBar.Color.B;
                        spectrumBarVertexes[i * stride + 27] = spectrumBar.Color.A;
                        spectrumBarVertexIndexes[i * 6] = (uint)i * 4;
                        spectrumBarVertexIndexes[i * 6 + 1] = (uint)i * 4 + 1;
                        spectrumBarVertexIndexes[i * 6 + 2] = (uint)i * 4 + 2;
                        spectrumBarVertexIndexes[i * 6 + 3] = (uint)i * 4;
                        spectrumBarVertexIndexes[i * 6 + 4] = (uint)i * 4 + 2;
                        spectrumBarVertexIndexes[i * 6 + 5] = (uint)i * 4 + 3;
                    }
                }
            }
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            UpdateSpectrumBars();

            base.OnRenderFrame(args);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            var input = KeyboardState;

            if (input.IsKeyDown(Keys.Escape))
                Close();

            if (input.IsKeyReleased(Keys.F))
                ToggleFullscreen();

            base.OnUpdateFrame(e);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            using (new DisposableStopwatch("Processing OnResize", true))
            {
                GL.Viewport(0, 0, e.Width, e.Height);
                camera.AspectRatio = Size.X / (float)Size.Y;
                base.OnResize(e);
            }
        }

        protected override void OnUnload()
        {
            wasAPIAudio.StopListen();
            base.OnUnload();
        }

        private Vector2[] splitRange(int chunks, float min, float max)
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
            using (new DisposableStopwatch("Resizing window", true))
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
            using (new DisposableStopwatch("Initializing GL", true))
            {
                GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
                GL.Enable(EnableCap.DepthTest);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc((BlendingFactor)BlendingFactorSrc.SrcAlpha, (BlendingFactor)BlendingFactorDest.OneMinusSrcAlpha);
            }
        }

        private void InitCamera()
        {
            using (new DisposableStopwatch("Iniatilizing camera", true))
            {
                camera = new Camera(Vector3.UnitZ, Size.X / Size.Y);
            }
        }

        private void BuildShader()
        {
            using (new DisposableStopwatch("Building Shader", true)) { }
        }

        private void InitWasAPIAudio()
        {
            using (new DisposableStopwatch("Initializing WasAPIAudio", true))
            {
                wasAPIAudio = new WasAPIAudio(spectrumBarCount, minFrequency, maxFrequency, spectrumData => { this.spectrumData = spectrumData; });
                wasAPIAudio.StartListen();
            }
        }
    }
}