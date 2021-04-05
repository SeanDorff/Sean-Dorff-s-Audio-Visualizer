using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;

namespace Sean_Dorff_s_Audio_Visualizer
{
    class Program
    {
        static void Main()
        {
            NativeWindowSettings nativeWindowSettings = new()
            {
                Size = new Vector2i(920, 517),
                Location = new Vector2i(40, 60),
                Title = "Sean Dorff's Audio Visualizer"
            };

            using SDAV_Window sdavWindow = new(GameWindowSettings.Default, nativeWindowSettings);
            sdavWindow.Run();
        }
    }
}
