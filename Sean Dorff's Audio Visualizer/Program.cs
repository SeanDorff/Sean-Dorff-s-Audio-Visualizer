using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;

namespace Sean_Dorff_s_Audio_Visualizer
{
    class Program
    {
        static void Main()
        {

            var nativeWindowSettings = new NativeWindowSettings()
            {
                Size = new Vector2i(920, 500),
                Location = new Vector2i(40, 40),
                Title = "Sean Dorff's Audio Visualizer"
            };

            using (SDAV_Window sdavWindow = new(GameWindowSettings.Default, nativeWindowSettings))
            {
                sdavWindow.Run();
            }
        }
    }
}
