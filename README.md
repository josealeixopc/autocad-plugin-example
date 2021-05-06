# AutoCAD Plugin Example

This is a simple project for developing a plugin for AutoCAD 2020 using .NET Framework and [xBim](https://github.com/xBimTeam/). 

The plugin contains a command for creating an IFC file containing walls derived from a polyline.

Note: This is not in a "ready" state, but has examples of some of the AutoCAD's API features.

## Installing

- Have AutoCAD 2020 and Visual Studio 2019 installed.
- **Optional but recommended**: Install the ObjectARX SDK. The libraries provided by ObjectARX already include documentation, which is integrated in Visual Studio's IntelliSense.
    - Go to this [link](https://www.autodesk.com/developer-network/platform-technologies/autocad/objectarx-license-download) and agree with the conditions.
    - Download the relevant version.
    - Install.

### Adding required references

- Add references to AutoCAD libraries (preferably from the ObjctARX folder):
    - `AcCoreMgd` 
    - `AcCui` (AutoCAD Custom User Interface)
    - `AcDbMgd` (AutoCAD ObjectDBX Wrapper)
    - `AcMgd`
	- `AcWindows` (AutoCAD Windows)
    - `AdWindows` (Autodesk Windows)
- Add other references by right-clicking `References` > `Add Reference` > `Assemblies` > Search for "[reference name]" > Add:
  - `PresentationCore`
  - `PresentationFramework`
  - `System.Windows.Forms`
  - `System.Drawing`
  - `WindowsBase`

### Adding xBim

In this example, we are using the [xBimToolkit](https://docs.xbim.net/index.html) to create a simple IFC file by running a command inside AutoCAD.

To add xBim to the project, we install the following NuGet package:

- [Xbim.Essentials](https://www.nuget.org/packages/Xbim.Essentials/)
    - This package should automatically install other xBim packages from which it depends.

## Running

- Running the Debug configuration:
  1. Start and wait for AutoCAD to boot
  1. Open a file and run the `NETLOAD` command.
  1. Search for the `AutoCADPluginExample.dll` in the `debug/release` folder.
  1. Run "ExHelloWorld" in AutoCAD console and verify that the command prints "Hello World".  

## Creating an AutoCAD project in Visual Studio

1. Create a new solution. Choose `Class Library` (**for .NET Framework**, not .NET core) as a template. Choose v4.7.
    - Note: in VS, a solution **contains** one or more projects.
1. Include AutoCAD libraries
    1. Right-click project > `Add` > `Project reference...` > `Browse`.
    1. Either:
        1. Open the ObjectARX `inc` folder (if you have installed ObjectARX SDK).
        1. **OR** Open the AutoCAD installation folder (probably `C:\Program Files\Autodesk\AutoCAD 2020`).
    1. Add the **at least** the following files: `accoremgd.dll`, `acmgd.dll`, `acdbmgd.dll`. Then you will be able to import AutoCAD classes with `using` in your code.
1. Set up debug configuration.
    1. Right click the project > Properties.
    1. In the Debug tab, set the `Start external program` to the path to the `acad.exe` file (probably in the folder `Programs/Autodesk/AutoCAD20XX`).
1. After creating a `Hello World` class (see the AutoCAD .NET training labs [here](https://www.autodesk.com/developer-network/platform-technologies/autocad)), let's try to see if the library is being built correctly.
    1. Run debug and wait for AutoCAD to open.
    1. Run the `NETLOAD` command and navigate to the `bin/Debug` folder of your project, and choose the `.dll` file with the same name as the project.
    1. Run the hello world command and check if it prints.
    1. Hurray! (or not)
