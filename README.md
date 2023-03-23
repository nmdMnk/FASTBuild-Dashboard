# FASTBuild Dashboard - Nine Worlds Studios Edition
This is an updated version of the forked FASTBuild Dashboard. The intention is to enhance it with new features and fixes. It is expected that the related fork of [FASTBuild](https://github.com/NineWorldsStudios/FASTBuild) is used for compiling C++ and shaders for Unreal Engine 5.0+.

We are actively using the Dashboard and FASTBuild for Unreal based projects and we plan to continue working on this to work with Unreal Engine 5. 

__New Features__
- Support for Work Proportional in tray
- Broker Agents View
- Change UI to Dark Mode!
- Exposed mininum free memory setting to UI (worker/dashboard restart required!)

__New Improvements__
- Close button sends dashboard to tray (force close only possible via right click)
- Some UX improvements
- Removed start with windows (done via installer)
- Bugfixes

![Screenshot of FBD 1.0.0](https://github.com/NineWorldsStudios/FASTBuild-Dashboard/blob/master/Documentations/Screenshots/FASTBuild-Dashboard.1.0.0.png?raw=true)

## Changelog 
### 1.0.0.106
New
- Added dark mode as default (no dynamic theme change possible)
- Exposed minimum free memory setting to UI settings (worker/dashboard restart required!)

Improvements
- Updated solution to .NET4.8 + updated all nuget packages to recent versions
- Updated FastBuild to v1.09

Fixes
- Fixed auto scrolling in build view
- Fixed tray icon exit sometimes not exiting

---

## Original README

FASTBuild ([website](http://www.fastbuild.org/) or [GitHub repository](https://github.com/fastbuild/fastbuild)) is an amazing distributed building system. It can drastically shorten your build time by utilizing its distributed and cached building mechanisms.

FASTBuild Dashboard (FBD) is a GUI program for FASTBuild. It can watch and report FASTBuild's build progress in a friendly timeline interface; track your local worker's activities; and provide a simple setting interface to configure how FASTBuild works.

![Screenshot of FBD 0.93.1](https://github.com/hillin/FASTBuilder/blob/master/Documentations/Screenshots/FASTBuild-Dashboard.0.93.1.png)

## Get FASTBuild Dashboard
You can get the latest release of FBD at the [Release Page](https://github.com/hillin/FASTBuild-Dashboard/releases). You'll need [.NET Framework 4.6](https://www.microsoft.com/en-us/download/details.aspx?id=48130) or newer version installed on your Windows system. 

Please note that FBD is still in its early development stage (although already suffice daily use), so everything is prone to change.

## Development
FBD is developed with .NET and WPF technology.

Third-party libraries:
- [Caliburn.Micro](http://caliburnmicro.com/)
- [Caliburn.Micro.Validation](https://github.com/AIexandr/Caliburn.Micro.Validation)
- [Fody Costura](https://github.com/Fody/Costura)
- [Material Design in Xaml Toolkit](https://github.com/ButchersBoy/MaterialDesignInXamlToolkit)

This project is partially based on [FASTBuild Monitor](https://github.com/yass007/FASTBuildMonitor); especially their work on [defining and implementing the log protocol](https://github.com/fastbuild/fastbuild/issues/127) should really be thanked.
