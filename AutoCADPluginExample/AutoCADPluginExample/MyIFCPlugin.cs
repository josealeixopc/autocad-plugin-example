
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using System;
using Xbim.Ifc;

// AutoCAD 2020 API reference: https://help.autodesk.com/view/OARX/2020/ENU/?guid=OARX-ManagedRefGuide-Migration_Guide

// This line is not mandatory, but improves loading performances
[assembly: ExtensionApplication(typeof(AutoCADPluginExample.MyIfcPlugin))]

namespace AutoCADPluginExample
{
    // This class is instantiated by AutoCAD once and kept alive for the 
    // duration of the session. If you don't do any one time initialization 
    // then you should remove this class.
    class MyIfcPlugin : IExtensionApplication
    {
        public void Initialize()
        {
            // Add one time initialization here
            // One common scenario is to setup a callback function here that 
            // unmanaged code can call. 
            // To do this:
            // 1. Export a function from unmanaged code that takes a function
            //    pointer and stores the passed in value in a global variable.
            // 2. Call this exported function in this function passing delegate.
            // 3. When unmanaged code needs the services of this managed module
            //    you simply call acrxLoadApp() and by the time acrxLoadApp 
            //    returns  global function pointer is initialized to point to
            //    the C# delegate.
            // For more info see: 
            // http://msdn2.microsoft.com/en-US/library/5zwkzwf4(VS.80).aspx
            // http://msdn2.microsoft.com/en-us/library/44ey4b32(VS.80).aspx
            // http://msdn2.microsoft.com/en-US/library/7esfatk4.aspx
            // as well as some of the existing AutoCAD managed apps.

            // Initialize your plug-in application here

            // Add the Tab for the IFC/BIM stuff
            AddIfcTab();
        }

        public void Terminate()
        {
            // Do plug-in application clean up here
            // TODO
        }

        // An article with an image explaining the Tab/Ribbon components is here:
        // https://www.keanw.com/2008/04/the-new-ribbonb.html
        static void AddIfcTab()
        {
            RibbonControl ribbon = ComponentManager.Ribbon;
            if (ribbon != null)
            {
                string tabId = "TabIFC";

                RibbonTab rtab = ribbon.FindTab(tabId);
                if (rtab != null)
                {
                    ribbon.Tabs.Remove(rtab);
                }
                rtab = new RibbonTab();
                rtab.Title = "IFC";
                rtab.Id = tabId;

                //Add the Tab
                ribbon.Tabs.Add(rtab);
                AddIfcContent(rtab);
            }
        }

        static void AddIfcContent(RibbonTab rtab)
        {
            rtab.Panels.Add(AddRoomAnnotationPanel());
        }

        static RibbonPanel AddRoomAnnotationPanel()
        {
            RibbonPanelSource rps = new RibbonPanelSource();
            rps.Title = "IFC Spaces";
            RibbonPanel rp = new RibbonPanel();
            rp.Source = rps;

            // Create a Command Item that the Dialog Launcher can use,
            // for this test it is just a place holder.
            RibbonButton rci = new RibbonButton();
            rci.Name = "TestCommand";

            //assign the Command Item to the DialgLauncher which auto-enables
            // the little button at the lower right of a Panel
            rps.DialogLauncher = rci;

            // Create a RibbonButton
            RibbonButton rb = new RibbonButton();
            rb.Name = "Create Storey";
            rb.ShowText = true;
            rb.Text = "Create Storey";

            // The space character in a macro/command acts as if the user is pressing space after writing the command (i.e., excuting the command)
            // See "About Macros": https://knowledge.autodesk.com/support/autocad-lt/learn-explore/caas/CloudHelp/cloudhelp/2020/ENU/AutoCAD-LT/files/GUID-D991386C-FBAA-4094-9FCB-AADD98ACD3EF-htm.html
            // "Debugging macros": https://www.cad-notes.com/how-to-automate-autocad-with-command-macros/
            rb.CommandParameter = "CreateBim "; //REMEMBER: space after commandname
            rb.CommandHandler = new AdskCommandHandler();

            //Add the Button to the Tab
            rps.Items.Add(rb);
            return rp;
        }

        class AdskCommandHandler : System.Windows.Input.ICommand
        {
            public bool CanExecute(object parameter)
            {
                return true;
            }


            public event EventHandler CanExecuteChanged
            {
                // This is just so the CS0067 warning is suppressed
                // The alernative is simply: public event EventHandler CanExecuteChanged;
                // See: https://stackoverflow.com/questions/29596781/compiler-warning-cs0067-the-event-is-never-used
                add { }
                remove { }
            }
            public void Execute(object parameter)
            {
                //is from Ribbon Button
                RibbonButton ribBtn = parameter as RibbonButton;
                if (ribBtn != null)
                {
                    //execute the command 
                    Application.DocumentManager.MdiActiveDocument
                    .SendStringToExecute(
                       (string)ribBtn.CommandParameter, true, false, true);
                }
            }
        }
    }


}
