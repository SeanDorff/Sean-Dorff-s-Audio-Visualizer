using Common;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

using System.Diagnostics;

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
        private int spectrumBars = 64;
        private int minFrequency = 20;
        private int maxFrequency = 20000;

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

            // construct vertex shader

            base.OnLoad();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            GetCurrentSpectrumData();

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

        private void GetCurrentSpectrumData()
        {
            using (new DisposableStopwatch("Getting current spectrum data", true))
            {
                if (spectrumData != null)
                    if (spectrumData.Length > 0)
                        Debug.WriteLine(spectrumData.Length);
            }
        }

        protected override void OnUnload()
        {
            wasAPIAudio.StopListen();
            base.OnUnload();
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

        private void InitWasAPIAudio()
        {
            using (new DisposableStopwatch("Initializing WasAPIAudio", true))
            {
                wasAPIAudio = new WasAPIAudio(spectrumBars, minFrequency, maxFrequency, spectrumData => { this.spectrumData = spectrumData; });
                wasAPIAudio.StartListen();
            }
        }
    }
}
