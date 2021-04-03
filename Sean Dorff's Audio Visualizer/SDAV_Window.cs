using Common;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Sean_Dorff_s_Audio_Visualizer
{
    public class SDAV_Window : GameWindow
    {
        private Camera camera;
        private bool isFullScreen = false;
        private readonly Vector2i originalSize;

        public SDAV_Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            originalSize = nativeWindowSettings.Size;
        }

        protected override void OnLoad()
        {
            InitGL();
            InitCamera();

            // construct vertex shader

            base.OnLoad();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (!IsFocused)
                return;

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
    }
}
