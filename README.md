# Sean Dorff's Audio Visualizer
This Visual Studio solution taps into your Windows audio output or microphone input and visualizes it as a frequency spectrum. There's also a nice rotating star field in the background.

# Releases & Download
Untested releases can be found under the [Releases](https://github.com/SeanDorff/Sean-Dorff-s-Audio-Visualizer/releases) section.

At the moment they are not always up to date. If you want the latest version you have to clone or download the repository and build it yourself.

# Controls
Use `WSAD` and `Space`/`Shift` to move around or up and down. The mouse controls your viewing direction.  
Use `F` to toggle fullscreen mode on or off.  
Use `R` to toggle star display on or off.  
Use `C` to switch between loopback audio and microphone input.  
`E` and `T` increase and decrease the star rotation speed.  
Press `Esc` to quit the program.

# Used NuGet packages
This solution uses three NuGet packages:
- [CSCore](https://github.com/filoe/cscore)
- [Json.Net](https://github.com/JamesNK/Newtonsoft.Json)
- [OpenTK](https://github.com/opentk/opentk)

# Quality control
As a basic quality control this repository runs [CodeQL](https://github.com/github/codeql) on each commit.
