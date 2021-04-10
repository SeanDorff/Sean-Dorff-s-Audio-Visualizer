using Common;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

using System;
using System.Reflection;

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
        private readonly int spectrumBarCount = 1024;
        private readonly int minFrequency = 20;
        private readonly int maxFrequency = 20000;

        private readonly int spectrumBarGenerations = 150;
        private SpectrumBars spectrumBars;

        private readonly int starCount = 15000;
        private Stars stars;
        private bool displayStars = true;

        private GenericShader genericShader;

        private double time;

        private const float ALPHA_DIMM = 0.97f;
        private const float MOUSE_SENSITIVITY = 0.2f;
        private const float CAMERA_SPEED = 1.0f;
        private const float DRIFT = 0.1f;

        public SDAV_Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            originalSize = nativeWindowSettings.Size;
        }

        protected override void OnLoad()
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                CursorGrabbed = true;

                InitGL();
                InitCamera();
                InitWasAPIAudio();

                spectrumBars = new(spectrumBarGenerations, spectrumBarCount);
                stars = new(starCount, spectrumBarGenerations);
                BuildShaders();

                base.OnLoad();
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                time += e.Time;
                if (time > MathHelper.TwoPi)
                    time -= MathHelper.TwoPi;

                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                spectrumBars.UpdateSpectrumBars(ref genericShader, spectrumData, camera.Position.Z);
                genericShader.Use();
                genericShader.SendData();
                genericShader.SetModelViewProjection(camera);
                genericShader.SetVertexAttribPointerAndArrays();
                genericShader.SetFloat("drift", DRIFT);
                genericShader.SetFloat("alphaDimm", ALPHA_DIMM);
                genericShader.SetVector3("cameraPosition", camera.Position);
                genericShader.SetInt("primitiveType", PrimitiveTypeHelper.IntValue(EPrimitiveType.Triangle));
                genericShader.DrawTriangleElements();

                if (displayStars)
                {
                    stars.UpdateStars(ref genericShader);
                    genericShader.Use();
                    genericShader.SendData();
                    genericShader.SetModelViewProjection(camera);
                    genericShader.SetVertexAttribPointerAndArrays();
                    genericShader.SetFloat("drift", DRIFT);
                    genericShader.SetFloat("alphaDimm", ALPHA_DIMM);
                    genericShader.SetVector3("cameraPosition", camera.Position);
                    genericShader.SetInt("primitiveType", PrimitiveTypeHelper.IntValue(EPrimitiveType.Point));
                    genericShader.DrawPointElements();
                }

                SwapBuffers();

                base.OnRenderFrame(e);
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                if (IsFocused)
                {
                    KeyboardState keyInput = KeyboardState;

                    if (keyInput.IsKeyReleased(Keys.F))
                        ToggleFullscreen();
                    if (keyInput.IsKeyReleased(Keys.R))
                        displayStars = !displayStars;

                    if (keyInput.IsAnyKeyDown)
                    {
                        if (keyInput.IsKeyDown(Keys.Escape))
                            Close();

                        if (keyInput.IsKeyDown(Keys.W))
                        {
                            camera.Position += camera.Front * CAMERA_SPEED * (float)e.Time; // Forward
                        }

                        if (keyInput.IsKeyDown(Keys.S))
                        {
                            camera.Position -= camera.Front * CAMERA_SPEED * (float)e.Time; // Backwards
                        }
                        if (keyInput.IsKeyDown(Keys.A))
                        {
                            camera.Position -= camera.Right * CAMERA_SPEED * (float)e.Time; // Left
                        }
                        if (keyInput.IsKeyDown(Keys.D))
                        {
                            camera.Position += camera.Right * CAMERA_SPEED * (float)e.Time; // Right
                        }
                        if (keyInput.IsKeyDown(Keys.Space))
                        {
                            camera.Position += camera.Up * CAMERA_SPEED * (float)e.Time; // Up
                        }
                        if (keyInput.IsKeyDown(Keys.LeftShift))
                        {
                            camera.Position -= camera.Up * CAMERA_SPEED * (float)e.Time; // Down
                        }
                    }
                    // Apply the camera pitch and yaw (we clamp the pitch in the camera class)
                    camera.Yaw += (MouseState.X - MouseState.PreviousX) * MOUSE_SENSITIVITY;
                    camera.Pitch -= (MouseState.Y - MouseState.PreviousY) * MOUSE_SENSITIVITY; // reversed since y-coordinates range from bottom to top
                }
                base.OnUpdateFrame(e);
            }
        }

        protected override void OnResize(ResizeEventArgs e)
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
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

        private void ToggleFullscreen()
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
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
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
                GL.Enable(EnableCap.DepthTest);
                GL.Enable(EnableCap.Blend);
                GL.Enable(EnableCap.ProgramPointSize);
                GL.BlendFunc((BlendingFactor)BlendingFactorSrc.SrcAlpha, (BlendingFactor)BlendingFactorDest.OneMinusSrcAlpha);
            }
        }

        private void InitCamera()
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                camera = new Camera(new Vector3(0, (Vector3.UnitY.Y / 2), Vector3.UnitZ.Z), Size.X / Size.Y);
            }
        }

        private void BuildShaders()
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                genericShader = new((uint)Math.Max(spectrumBars.SpectrumBarVertexesCount, stars.StarVertexesCount), (uint)Math.Max(spectrumBars.SpectrumBarIndexesCount, stars.StarIndexesCount));
            }
        }

        private void InitWasAPIAudio()
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                wasAPIAudio = new WasAPIAudio((int)spectrumBarCount, minFrequency, maxFrequency, spectrumData => { this.spectrumData = spectrumData; });
                wasAPIAudio.StartListen();
            }
        }
    }
}