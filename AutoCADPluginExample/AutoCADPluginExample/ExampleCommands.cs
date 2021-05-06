
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using Xbim.Ifc;

// AutoCAD 2020 API reference: https://help.autodesk.com/view/OARX/2020/ENU/?guid=OARX-ManagedRefGuide-Migration_Guide

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(AutoCADPluginExample.ExampleCommands))]

namespace AutoCADPluginExample
{
    // This class is instantiated by AutoCAD for each document when
    // a command is called by the user the first time in the context
    // of a given document. In other words, non static data in this class
    // is implicitly per-document!
    public class ExampleCommands
    {
        // The CommandMethod attribute can be applied to any public  member 
        // function of any public class.
        // The function should take no arguments and return nothing.
        // If the method is an intance member then the enclosing class is 
        // intantiated for each document. If the member is a static member then
        // the enclosing class is NOT intantiated.
        //
        // NOTE: CommandMethod has overloads where you can provide helpid and
        // context menu.
        //
        // The CommandMethod accepts a CommandFlag, which defines the behavior of the command
        // See the possible CommandFlags and the associated behavior here:
        // http://docs.autodesk.com/ACD/2011/ENU/filesMDG/WS1a9193826455f5ff-e569a0121d1945c08-17d4.htm
        [CommandMethod("ExHelloWorld")]
        public void HelloWorld()
        {
            // Get currently active document
            Document doc = Application.DocumentManager.MdiActiveDocument;

            // Get the editor for that document
            Editor ed = doc.Editor;
            ed.WriteMessage("Hello!");

            // List all the available hatching patterns
            //foreach (HatchRibbonItem hatchRibItm in HatchPatterns.Instance.AllPatterns)
            //{
            //    ed.WriteMessage("\n" + hatchRibItm.ToString());
            //}
        }

        // UsePickSet explained behavior:
        // https://spiderinnet1.typepad.com/blog/2011/12/autocad-net-commandflag-usepickset.html
        [CommandMethod("ExSolMassProp", CommandFlags.UsePickSet)]
        public void SolMassProp()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // The Database object contains all of the graphical and most of the nongraphical AutoCAD objects
            Database db = doc.Database;

            // Ask user to select a solid (which is an Entity)
            // To prompt, we use the PromptXXXOptions class, where XXX is the value type we want to prompt
            // (e.g. Angle, String, Distance, Entity ...)
            PromptEntityOptions peo = new PromptEntityOptions("Select a 3D solid.");

            // Set the message in case of an error (this MUST come before definind the allowed class)
            peo.SetRejectMessage("\nA 3D solid must be selected.");

            // Set the class of the entity we want to select
            peo.AddAllowedClass(typeof(Solid3d), true);

            // After creating the prompt, we actually show it using the GetXXX function of the Editor
            PromptEntityResult per = ed.GetEntity(peo);

            if (per.Status != PromptStatus.OK)
            {
                return;
            }

            ed.WriteMessage("Ok, cool, thanks.");
        }


        [CommandMethod("ExAddAnEnt")]
        public void AddAnEnt()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // Declare a PromptKeywordOptions variable and instantiate it by creating
            // a new PromptKeywordOptions. Use a string similar to the following for the 
            // messageAndKeywords string. 
            // "Which entity do you want to create? [Circle/Block] : ", "Circle Block" 
            PromptKeywordOptions poEntity = new PromptKeywordOptions(
                "Which entity do you want to create? [Circle/Block] : ",
                "Circle Block");    // String with the options separated with a space

            // Instantiate the PromptResult by making it equal to the return value of the GetKeywords method.
            PromptResult prEntity = ed.GetKeywords(poEntity);

            if (prEntity.Status == PromptStatus.OK)
            {
                switch (prEntity.StringResult)
                {
                    case "Circle":
                        // Ask for a point, which will be the center of the circle
                        PromptPointOptions poCenterPoint = new PromptPointOptions("Pick Center Point: ");

                        // Pass the prompt to the editor and get the resulting point
                        PromptPointResult prCenterPoint = ed.GetPoint(poCenterPoint);

                        if (prCenterPoint.Status == PromptStatus.OK)
                        {
                            // Ask for a distance (radius)
                            PromptDistanceOptions poRadius = new PromptDistanceOptions("Pick Radius: ");

                            // Make the point selected earlier the base point of the distance prompt
                            poRadius.BasePoint = prCenterPoint.Value;
                            // Tell the prompt to actually use the base point
                            poRadius.UseBasePoint = true;

                            // Get the distance
                            PromptDoubleResult prRadius = ed.GetDistance(poRadius);

                            if (prRadius.Status == PromptStatus.OK)
                            {
                                // Add the circle to the DWG
                                Database dwg = ed.Document.Database;

                                // Start a transaction (as you would with a database)
                                Transaction trans = dwg.TransactionManager.StartTransaction();

                                try
                                {
                                    // Create the circle.
                                    // The second parameter is the normal (perpendicular) vector.
                                    Circle circle = new Circle(prCenterPoint.Value, Vector3d.ZAxis, prRadius.Value);

                                    // Create a new block
                                    // A BlockTableRecord is similar to using the "BLOCK" command on AutoCAD.
                                    // When we add a record to the current space, we ARE NOT adding it to the "Blocks" utility.
                                    BlockTableRecord curSpace = (BlockTableRecord)trans.GetObject(dwg.CurrentSpaceId, OpenMode.ForWrite);

                                    // Add the circle to the new block
                                    curSpace.AppendEntity(circle);

                                    // Tell the transaction about the new object (entity)
                                    trans.AddNewlyCreatedDBObject(circle, true);

                                    // Commit
                                    trans.Commit();
                                }
                                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                                {
                                    // Write exception on the AutoCAD command line
                                    ed.WriteMessage("EXCEPTION: " + ex.Message);
                                }
                                finally
                                {
                                    // Dispose of the transaction (whether an error has occurred or not)
                                    trans.Dispose();
                                }
                            }
                        }
                        break;

                    case "Block":
                        // Add a prompt to name the block
                        PromptStringOptions poBlockName = new PromptStringOptions("Enter the name of the Block to create: ");

                        // Don't allow spaces, as a block's name can't have spaces
                        poBlockName.AllowSpaces = false;

                        // Get the name
                        PromptResult prBlockName = ed.GetString(poBlockName);

                        if (prBlockName.Status == PromptStatus.OK)
                        {
                            // Add the block to the dwg
                            Database dwg = ed.Document.Database;
                            Transaction trans = dwg.TransactionManager.StartTransaction();

                            try
                            {
                                // Create new BTR from scratch
                                BlockTableRecord btr = new BlockTableRecord();

                                // Set name to the prompt result
                                btr.Name = prBlockName.StringResult;

                                // First verify if a block with the same name already exists in the Block Table
                                // We open the Block Table in read, because we won't be changing it right now
                                BlockTable blockTable = (BlockTable)trans.GetObject(dwg.BlockTableId, OpenMode.ForRead);

                                if (blockTable.Has(prBlockName.StringResult))
                                {
                                    throw new Autodesk.AutoCAD.Runtime.Exception(
                                        ErrorStatus.InvalidInput,
                                        "Cannot create block. Block with same name already exists.");
                                }
                                else
                                {
                                    // Update OpenMode to ForWrite
                                    blockTable.UpgradeOpen();

                                    // Add to the block table (this will make the block available for the user in the "Block" utility)
                                    blockTable.Add(btr);

                                    // Tell the transaction about the new object, so that it auto-closes it
                                    trans.AddNewlyCreatedDBObject(btr, true);

                                    // We defined that the block consists of two circles, so we'll add them
                                    Circle circle1 = new Circle(new Point3d(0, 0, 0), Vector3d.ZAxis, 10);
                                    Circle circle2 = new Circle(new Point3d(20, 10, 0), Vector3d.ZAxis, 10);

                                    btr.AppendEntity(circle1);
                                    btr.AppendEntity(circle2);

                                    trans.AddNewlyCreatedDBObject(circle1, true);
                                    trans.AddNewlyCreatedDBObject(circle2, true);

                                    // Prompt for insertion point
                                    PromptPointOptions poPoint = new PromptPointOptions("Pick insertion point of BlockRef : ");
                                    PromptPointResult prPoint = ed.GetPoint(poPoint);

                                    if (prPoint.Status != PromptStatus.OK)
                                    {
                                        // If point is not valid, return

                                        trans.Dispose();
                                        return;
                                    }

                                    // The BlockTableRecord is the BLOCK (i.e., a template)
                                    // The BlockReference is the result of using INSERT (i.e., an instance of a block)
                                    BlockReference blockRef = new BlockReference(prPoint.Value, btr.ObjectId);

                                    // Add to the current space
                                    BlockTableRecord curSpace = (BlockTableRecord)trans.GetObject(dwg.CurrentSpaceId, OpenMode.ForWrite);
                                    curSpace.AppendEntity(blockRef);

                                    // Finish transaction
                                    trans.AddNewlyCreatedDBObject(blockRef, true);
                                    trans.Commit();
                                }
                            }
                            catch (Autodesk.AutoCAD.Runtime.Exception ex)
                            {
                                ed.WriteMessage("EXCEPTION: " + ex.Message);
                            }
                            finally
                            {
                                trans.Dispose();
                            }
                        }
                        break;
                }
            }
        }

        // Palettes are secondary AutoCAD windows that provide help and features for a certain tool. 
        // Create a global variable (created only once) for the PaletteSet.
        public PaletteSet myPaletteSet;

        // Create a global variable for the palette form.
        // The form is a System.Windows.Forms.UserControl item
        // We can create via right-click project > Add > User Control (WindowsForms)
        public AutoCADPluginExample.UserControl1 myPalette;

        // Create the command for showing a palette
        [CommandMethod("ExPalette")]
        public void Palette()
        {
            // If the PaletteSet isn't created, create it
            if (myPaletteSet == null)
            {
                // A palette has a Global Unique Identifier (a.k.a., GUID), which we can generate using Visual Studio
                // Go to Tools > Create GUID > Choose 4. Registry format > New GUID > Copy and Paste the value in the constructor
                myPaletteSet = new PaletteSet("My Palette Set", new System.Guid("AAFA30BD-1CFB-4CDF-B343-5332C8E3A024"));

                // Instantiate the palette form
                myPalette = new AutoCADPluginExample.UserControl1();

                // Add the form to the palette set
                myPaletteSet.Add("Palette1", myPalette);
            }

            // Display palette
            myPaletteSet.Visible = true;
        }

        // Add a command that makes use of the palette
        [CommandMethod("ExAddDBEvents")]
        public void AddDBEvents()
        {
            // Check if there is a palette
            if (myPalette == null)
            {
                Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
                ed.WriteMessage("\n" + "Please call the 'Palette' command first.");

                return;
            }

            Database currDwg = Application.DocumentManager.MdiActiveDocument.Database;

            // When an event happens in AutoCAD, handlers can be called to deal with it

            // Add a new handler to handle adding an object to the current DWG
            currDwg.ObjectAppended += new ObjectEventHandler(callback_ObjectAppended);

            // Add other handlers
            currDwg.ObjectErased += new ObjectErasedEventHandler(callback_ObjectErased);
            currDwg.ObjectReappended += new ObjectEventHandler(callback_ObjectReappended);
            currDwg.ObjectUnappended += new ObjectEventHandler(callback_ObjectUnappended);
        }

        private void callback_ObjectAppended(object sender, ObjectEventArgs e)
        {
            // A TreeView should be added to the User Control via the Visual Studio's Toolbox
            // NOTE: Remember to change the TreeView visibility to public via the .Designer.cs file
            // Add a TreeNode to the TreeView
            System.Windows.Forms.TreeNode newNode = myPalette.treeView1.Nodes.Add(e.DBObject.GetType().ToString());

            // Make the Tag property of the node equal to the ID of the appended object, so we can associate nodes with objects
            newNode.Tag = e.DBObject.ObjectId.ToString();
        }
        private void callback_ObjectErased(object sender, ObjectErasedEventArgs e)
        {
            if (e.Erased)
            {
                // If the object was erased from the drawing, then also erase the associated node
                foreach (System.Windows.Forms.TreeNode node in myPalette.treeView1.Nodes)
                {
                    if (node.Tag.ToString() == e.DBObject.ObjectId.ToString())
                    {
                        node.Remove();
                        break;
                    }
                }
            }
            else
            {
                // If the object was not erased, it was UNERASED (i.e., reversed deletion), so we add the node again
                System.Windows.Forms.TreeNode newNode = myPalette.treeView1.Nodes.Add(e.DBObject.GetType().ToString());
                newNode.Tag = e.DBObject.ObjectId.ToString();
            }
        }


        private void callback_ObjectReappended(object sender, ObjectEventArgs e)
        {
            // Do the same as if it was appended
            this.callback_ObjectAppended(sender, e);
        }
        private void callback_ObjectUnappended(object sender, ObjectEventArgs e)
        {
            // Do the same as if the object has been erased
            foreach (System.Windows.Forms.TreeNode node in myPalette.treeView1.Nodes)
            {
                if (node.Tag.ToString() == e.DBObject.ObjectId.ToString())
                {
                    node.Remove();
                    break;
                }
            }
        }

        [CommandMethod("ExSelectPolyline")]
        public void SelectPolyline()
        {
            // Code based on: https://forums.autodesk.com/t5/visual-lisp-autolisp-and-general/to-select-a-single-polyline-c/td-p/7982440
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // Ask user to select a solid (which is an Entity)
            // To prompt, we use the PromptXXXOptions class, where XXX is the value type we want to prompt
            // (e.g. Angle, String, Distance, Entity ...)
            PromptEntityOptions poPolyline = new PromptEntityOptions("Select a Polyline.");

            // Set the message in case of an error (this MUST come before definind the allowed class)
            poPolyline.SetRejectMessage("\nA Polyline must be selected.");

            // Set the class of the entity we want to select
            poPolyline.AddAllowedClass(typeof(Polyline), true);

            // After creating the prompt, we actually show it using the GetXXX function of the Editor
            PromptEntityResult prPolyline = ed.GetEntity(poPolyline);

            if (prPolyline.Status != PromptStatus.OK)
            {
                return;
            }

            // Change color of selected Polyline
            // at this point we know an entity have been selected and it is a Polyline
            using (var txn = db.TransactionManager.StartTransaction())
            {
                var pline = (Polyline) txn.GetObject(prPolyline.ObjectId, OpenMode.ForWrite);
                pline.ColorIndex = 3;
                txn.Commit();
            }

        }
    }
}
