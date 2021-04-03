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
        public SDAV_Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            InitGL();
            camera = new Camera(Vector3.UnitZ, Size.X / Size.Y);

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

            base.OnUpdateFrame(e);
        }

        protected override void OnUnload()
        {
            base.OnUnload();
        }

        private static void InitGL()
        {
            using (new DisposableStopwatch("Initializing GL"))
            {
                GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
                GL.Enable(EnableCap.DepthTest);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc((BlendingFactor)BlendingFactorSrc.SrcAlpha, (BlendingFactor)BlendingFactorDest.OneMinusSrcAlpha);
            }
        }
    }
}
