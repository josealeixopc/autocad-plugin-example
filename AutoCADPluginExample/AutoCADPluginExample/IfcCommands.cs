
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using System.IO;
using System.Linq;
using Xbim.Ifc;
using Xbim.Ifc4.ProductExtension;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(AutoCADPluginExample.IfcCommands))]

namespace AutoCADPluginExample
{
    class IfcCommands
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
        [CommandMethod("HelloWorld")]
        public void HelloWorld()
        {
            // Get currently active document
            Document doc = Application.DocumentManager.MdiActiveDocument;

            // Get the editor for that document
            Editor ed = doc.Editor;
            ed.WriteMessage("Hello!");
        }

        [CommandMethod("CreateWalls")]
        public void CreateWalls()
        {
            // Code based on: https://forums.autodesk.com/t5/visual-lisp-autolisp-and-general/to-select-a-single-polyline-c/td-p/7982440
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // Ask user to select a solid (which is an Entity)
            // To prompt, we use the PromptXXXOptions class, where XXX is the value type we want to prompt
            // (e.g. Angle, String, Distance, Entity ...)
            PromptEntityOptions poPolyline = new PromptEntityOptions("Select a Polyline representing the walls.");

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

            // At this point we know an entity have been selected and it is a Polyline
            using (var txn = db.TransactionManager.StartTransaction())
            {
                var pline = (Polyline)txn.GetObject(prPolyline.ObjectId, OpenMode.ForRead);

                string drawingName = Path.GetFileName(doc.Name);
                IteratePolyline(ed, db, pline, IfcModelSingleton.GetInstance().model);

                // We don't want to commit any change
                txn.Abort();
            }
        }

        private static void IteratePolyline(Editor ed, Database db, Polyline pline, IfcStore model)
        {
            // at this point we know an entity have been selected and it is a Polyline
            using (var tr = db.TransactionManager.StartTransaction())
            {
                // iterte through all segments
                for (int i = 0; i < pline.NumberOfVertices; i++)
                {
                    switch (pline.GetSegmentType(i))
                    {
                        case SegmentType.Arc:
                            CircularArc2d arc = pline.GetArcSegment2dAt(i);
                            ed.WriteMessage($"\n\n Segment {i} - Arc -");
                            ed.WriteMessage("\nStart width: {0,-40}", pline.GetStartWidthAt(i));
                            ed.WriteMessage("\nEnd width: {0,-40}", pline.GetEndWidthAt(i));
                            ed.WriteMessage($"\nBulge:        {pline.GetBulgeAt(i)}");
                            ed.WriteMessage($"\nStart point:  {arc.StartPoint}");
                            ed.WriteMessage($"\nEnd point:    {arc.EndPoint}");
                            ed.WriteMessage($"\nRadius:       {arc.Radius}");
                            ed.WriteMessage($"\nCenter:       {arc.Center}");
                            break;
                        case SegmentType.Line:
                            LineSegment2d line = pline.GetLineSegment2dAt(i);

                            var storey = model.Instances.OfType<IfcBuildingStorey>().FirstOrDefault();
                            IfcModelSingleton.CreateWall(model, line.MidPoint.X, line.MidPoint.Y, line.Direction.X, line.Direction.Y, 0, line.Length, 0.5, 2, storey);
                            break;
                        default:
                            ed.WriteMessage($"\n\n Segment {i} : zero length segment");
                            ed.WriteMessage($"\nStart width:  {pline.GetStartWidthAt(i)}");
                            ed.WriteMessage($"\nEnd width:    {pline.GetEndWidthAt(i)}");
                            ed.WriteMessage($"\nBulge:        {pline.GetBulgeAt(i)}");
                            break;
                    }
                }

                IfcModelSingleton.GetInstance().ValidateAndSave();

                tr.Commit();
            }
            Application.DisplayTextScreen = true;
        }
    }
}
