using Common;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

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
        private ECaptureType captureType;

        private readonly int spectrumBarCount = Configuration.GetIntProperty("spectrumBarCount");
        private readonly int minFrequency = Configuration.GetIntProperty("minFrequency");
        private readonly int maxFrequency = Configuration.GetIntProperty("maxFrequency");

        private readonly int spectrumBarGenerations = Configuration.GetIntProperty("spectrumBarGenerations");
        private SpectrumBars spectrumBars;

        private readonly int starsPerGeneration = Configuration.GetIntProperty("starsPerGeneration");
        private readonly int spectrumBarGenerationMultiplier = Configuration.GetIntProperty("spectrumBarGenerationMultiplier");
        private Stars stars;
        private bool displayStars = Configuration.GetBoolProperty("displayStars");

        private TriangleAndPointShader triangleAndPointShader;

        private double time;

        private const float ALPHA_DIMM = 0.99f;
        private const float MOUSE_SENSITIVITY = 0.2f;
        private const float CAMERA_SPEED = 1.0f;
        private const float DRIFT = 0.1f;

        public SDAV_Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            originalSize = nativeWindowSettings.Size;
            if (Configuration.GetStringProperty("captureType").Equals("Microphone"))
                captureType = ECaptureType.Microphone;
            else
                captureType = ECaptureType.Loopback;
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

                stars = new(spectrumBarGenerations, starsPerGeneration, spectrumBarGenerationMultiplier);
                spectrumBars = new(spectrumBarGenerations, spectrumBarCount);

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

                stars.UpdateRotationHistory();

                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                triangleAndPointShader.Use();

                triangleAndPointShader.CurrentBuffer = 0;
                triangleAndPointShader.SetModelViewProjection(camera);
                triangleAndPointShader.SetFloat("drift", DRIFT);
                triangleAndPointShader.SetFloat("alphaDimm", ALPHA_DIMM);
                triangleAndPointShader.SetVector3("cameraPosition", camera.Position);
                triangleAndPointShader.SetFloatArray("rotationHistory[0]", stars.RotationHistory);

                if (displayStars)
                {
                    stars.UpdateStars(ref triangleAndPointShader);
                    triangleAndPointShader.SendData();
                    triangleAndPointShader.SetVertexAttribPointerAndArrays();
                    triangleAndPointShader.SetInt("primitiveType", (int)PrimitiveType.Points);
                    triangleAndPointShader.DrawElements();
                }
                triangleAndPointShader.Use();

                triangleAndPointShader.CurrentBuffer = 1;
                spectrumBars.UpdateSpectrumBars(ref triangleAndPointShader, camera.Position.Z);
                triangleAndPointShader.SendData();
                triangleAndPointShader.SetVertexAttribPointerAndArrays();
                triangleAndPointShader.SetInt("primitiveType", (int)PrimitiveType.Triangles);
                triangleAndPointShader.DrawElements();

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
                        ToggleStars();
                    if (keyInput.IsKeyReleased(Keys.C))
                        ToggleCaptureType();

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
                        if (keyInput.IsKeyDown(Keys.E))
                        {
                            stars.ChangeRotationSpeed(1);
                        }
                        if (keyInput.IsKeyDown(Keys.T))
                        {
                            stars.ChangeRotationSpeed(-1);
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
            triangleAndPointShader.Unload();
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

        private void ToggleStars()
        {
            displayStars = !displayStars;
            if (displayStars)
                GL.Enable(EnableCap.ProgramPointSize);
            else
                GL.Disable(EnableCap.ProgramPointSize);
        }

        private void ToggleCaptureType()
        {
            if (captureType == ECaptureType.Loopback)
                captureType = ECaptureType.Microphone;
            else
                captureType = ECaptureType.Loopback;

            wasAPIAudio.SwitchCaptureType(captureType);
        }

        /// <summary>
        /// Initializes GL with required values
        /// </summary>
        /// <remarks>
        /// <see cref="EnableCap.ProgramPointSize"/> will be switched on and off by <see cref="ToggleStars"/>.
        /// </remarks>
        private static void InitGL()
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                GL.ClearColor(Color4.Black); // Background
                GL.Enable(EnableCap.DepthTest); // Transparency
                GL.Enable(EnableCap.Blend); // Transparency
                GL.Enable(EnableCap.ProgramPointSize); // Star scaling
                GL.Enable(EnableCap.Texture2D); // Font display
                GL.BlendFunc((BlendingFactor)BlendingFactorSrc.SrcAlpha, (BlendingFactor)BlendingFactorDest.OneMinusSrcAlpha); // Transparency
            }
        }

        /// <summary>
        /// Initializes camera at a defined position.
        /// </summary>
        private void InitCamera()
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                camera = new Camera(new Vector3(0, (Vector3.UnitY.Y / 2), Vector3.UnitZ.Z), Size.X / Size.Y);
            }
        }

        /// <summary>
        /// Builds the shader(s) and passes required buffer array sizes.
        /// </summary>
        private void BuildShaders()
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                triangleAndPointShader = ShaderProgramFactory.BuildTriangleAndPointShaderProgram("shaders/shader.vert", "shaders/shader.frag", 2);
                triangleAndPointShader.CurrentBuffer = 0;
                triangleAndPointShader.Vertexes = new float[stars.StarVertexesCount];
                triangleAndPointShader.Indexes = new uint[stars.StarIndexesCount];
                triangleAndPointShader.PrimitiveType = PrimitiveType.Points;
                triangleAndPointShader.CurrentBuffer = 1;
                triangleAndPointShader.Vertexes = new float[spectrumBars.SpectrumBarVertexesCount];
                triangleAndPointShader.Indexes = new uint[spectrumBars.SpectrumBarIndexesCount];
                triangleAndPointShader.PrimitiveType = PrimitiveType.Triangles;
            }
        }

        /// <summary>
        /// Initializes audio grabbing.
        /// </summary>
        private void InitWasAPIAudio()
        {
#if (DEBUG)
            using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
#endif
            {
                wasAPIAudio = new WasAPIAudio(captureType, (int)spectrumBarCount, minFrequency, maxFrequency, spectrumData =>
                 {
                     SpectrumDataHelper.SpectrumData = spectrumData;
                 });
                wasAPIAudio.StartListen();
            }
        }
    }
}