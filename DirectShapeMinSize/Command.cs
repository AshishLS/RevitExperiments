#region Namespaces
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion

namespace DirectShapeMinSize
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        /// <summary>
        /// Set this to true to iterate through smaller 
        /// and smaller tetrahedron sizes until we hit
        /// Revit's precision limit.
        /// </summary>
        static bool Iterate_until_crash = false;

        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // Find GraphicsStyle

            FilteredElementCollector collector
              = new FilteredElementCollector(doc)
                .OfClass(typeof(GraphicsStyle));

            GraphicsStyle style = collector.Cast<GraphicsStyle>()
              .FirstOrDefault<GraphicsStyle>(gs => gs.Name.Equals("<Sketch>"));

            ElementId graphicsStyleId = null;

            if (style != null)
            {
                graphicsStyleId = style.Id;
            }

            // Modify document within a transaction

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Create DirectShape");

                double length = 20; // foot
                double breadth = 10; // foot
                double height = 5; // foot

                try
                {

                    Debug.Print("Creating DirectShape box with side length (along X): {0}, breadth: {1}, height: {2}",
                        length, breadth, height);

                    List<XYZ> args = new List<XYZ>(4);

                    TessellatedShapeBuilder builder = new TessellatedShapeBuilder();

                    builder.OpenConnectedFaceSet(false);

                    // Define the points of a box
                    XYZ pt0 = XYZ.Zero;
                    XYZ pt1 = pt0 + length * XYZ.BasisX;
                    XYZ pt2 = pt1 + breadth * XYZ.BasisY;
                    XYZ pt3 = pt0 + breadth * XYZ.BasisY;

                    XYZ pt4 = pt0 + height * XYZ.BasisZ;
                    XYZ pt5 = pt1 + height * XYZ.BasisZ;
                    XYZ pt6 = pt2 + height * XYZ.BasisZ;
                    XYZ pt7 = pt3 + height * XYZ.BasisZ;

                    // Keep face normal in consideration
                    // face 0
                    args.Clear();
                    args.Add(pt3);
                    args.Add(pt2);
                    args.Add(pt1);
                    args.Add(pt0);
                    builder.AddFace(new TessellatedFace(args, ElementId.InvalidElementId));

                    // face 1
                    args.Clear();
                    args.Add(pt4);
                    args.Add(pt5);
                    args.Add(pt6);
                    args.Add(pt7);
                    builder.AddFace(new TessellatedFace(args, ElementId.InvalidElementId));

                    // face 2
                    args.Clear();
                    args.Add(pt2);
                    args.Add(pt3);
                    args.Add(pt7);
                    args.Add(pt6);
                    builder.AddFace(new TessellatedFace(args, ElementId.InvalidElementId));

                    // face 3
                    args.Clear();
                    args.Add(pt0);
                    args.Add(pt1);
                    args.Add(pt5);
                    args.Add(pt4);
                    builder.AddFace(new TessellatedFace(args, ElementId.InvalidElementId));

                    // face 4
                    args.Clear();
                    args.Add(pt1);
                    args.Add(pt2);
                    args.Add(pt6);
                    args.Add(pt5);
                    builder.AddFace(new TessellatedFace(args, ElementId.InvalidElementId));

                    // face 5
                    args.Clear();
                    args.Add(pt0);
                    args.Add(pt4);
                    args.Add(pt7);
                    args.Add(pt3);
                    builder.AddFace(new TessellatedFace(args, ElementId.InvalidElementId));

                    builder.CloseConnectedFaceSet();

                    //TessellatedShapeBuilderResult result
                    //  = builder.Build(
                    //    TessellatedShapeBuilderTarget.Solid,
                    //    TessellatedShapeBuilderFallback.Abort,
                    //    graphicsStyleId );

                    builder.GraphicsStyleId = graphicsStyleId;
                    builder.Build();
                    // Pre-release code from DevDays

                    //DirectShape ds = DirectShape.CreateElement(
                    //  doc, result.GetGeometricalObjects(), "A", "B");

                    //ds.SetCategoryId(new ElementId(
                    //  BuiltInCategory.OST_GenericModel));

                    // Code updated for Revit UR1

                    ElementId categoryId = new ElementId(
          BuiltInCategory.OST_GenericModel);

                    DirectShape ds = DirectShape.CreateElement(
                      doc, categoryId);

                    ds.SetShape(builder.GetBuildResult().GetGeometricalObjects());

                    ds.Name = "Test";
                }
                catch (Exception e)
                {
                    Debug.Print(
                      "Creating DirectShape tetrahedron with side length {0} "
                      + "threw exception '{1}'",
                      length, e.Message);

                    message = e.Message;
                    return Result.Failed;
                }
                tx.Commit();
            }
            return Result.Succeeded;
        }
    }
}
