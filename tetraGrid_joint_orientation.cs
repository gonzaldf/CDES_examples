using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;



/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public class Script_Instance : GH_ScriptInstance
{
#region Utility functions
  /// <summary>Print a String to the [Out] Parameter of the Script component.</summary>
  /// <param name="text">String to print.</param>
  private void Print(string text) { /* Implementation hidden. */ }
  /// <summary>Print a formatted String to the [Out] Parameter of the Script component.</summary>
  /// <param name="format">String format.</param>
  /// <param name="args">Formatting parameters.</param>
  private void Print(string format, params object[] args) { /* Implementation hidden. */ }
  /// <summary>Print useful information about an object instance to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj) { /* Implementation hidden. */ }
  /// <summary>Print the signatures of all the overloads of a specific method to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj, string method_name) { /* Implementation hidden. */ }
#endregion

#region Members
  /// <summary>Gets the current Rhino document.</summary>
  private readonly RhinoDoc RhinoDocument;
  /// <summary>Gets the Grasshopper document that owns this script.</summary>
  private readonly GH_Document GrasshopperDocument;
  /// <summary>Gets the Grasshopper script component that owns this script.</summary>
  private readonly IGH_Component Component;
  /// <summary>
  /// Gets the current iteration count. The first call to RunScript() is associated with Iteration==0.
  /// Any subsequent call within the same solution will increment the Iteration count.
  /// </summary>
  private readonly int Iteration;
#endregion

  /// <summary>
  /// This procedure contains the user code. Input parameters are provided as regular arguments,
  /// Output parameters as ref arguments. You don't have to assign output parameters,
  /// they will have a default value.
  /// </summary>
  private void RunScript(List<Mesh> pyr, Box cBox, Box pBox, double pZOff, ref object corners, ref object points, ref object misc)
  {
    Interval dimsXY = new Interval(0, cBox.X.Length);
    Interval dimsZ = new Interval(0, cBox.Z.Length);
    Interval dimPlateXY = new Interval(-pBox.X.Length / 2, pBox.X.Length / 2);
    Interval dimPlateZ = new Interval(-pBox.Z.Length, 0);
    var cornerBoxes = new ArrayList();
    var ptBoxes = new ArrayList();
    var plnList = new ArrayList();

    foreach (Mesh m in pyr)
    {
      if (m.Faces.Capacity > 4)
      {
        if (getPyrZ(m, m.Vertices[4]).Z == -1) //pt down - yes there is probably a smarter way to do this
        {
          Plane[] plns = new Plane[5];

          Plane ptPlane = new Plane(new Point3d(m.Vertices[0].X, m.Vertices[0].Y, m.Vertices[0].Z + pZOff), new Vector3d(0, 0, -1));
          Box bp = new Box(ptPlane, dimPlateXY, dimPlateXY, dimPlateZ);
          ptBoxes.Add(bp);

          for (int i = 1; i < 5; i++)
          {
            Vector3d dir = Point3d.Subtract(m.GetBoundingBox(false).Center, m.Vertices[i]);

            plns[i] = new Plane(m.Vertices[i], new Vector3d(1, 0, 0), new Vector3d(0, 1, 0));
            switch (i)
            {
              case 2:
                plns[i].Rotate(Math.PI / -2, plns[i].ZAxis);
                break;
              case 3:
                plns[i].Rotate(Math.PI / 2, plns[i].ZAxis);
                break;
              case 4:
                plns[i].Rotate(Math.PI, plns[i].ZAxis);
                break;
              default:
                break;
            }
            plns[i].Flip();
            plnList.Add(plns[i]);
            Box bt = new Box(plns[i], dimsXY, dimsXY, dimsZ);
            cornerBoxes.Add(bt);
          }

        }
        else //pt up
        {
          Plane[] plns = new Plane[5];

          Plane ptPlane = new Plane(new Point3d(m.Vertices[0].X, m.Vertices[0].Y, m.Vertices[0].Z - pZOff), new Vector3d(0, 0, 1));
          Box bp = new Box(ptPlane, dimPlateXY, dimPlateXY, dimPlateZ);
          ptBoxes.Add(bp);

          for (int i = 1; i < 5; i++)
          {
            Vector3d dir = Point3d.Subtract(m.GetBoundingBox(false).Center, m.Vertices[i]);

            plns[i] = new Plane(m.Vertices[i], new Vector3d(1, 0, 0), new Vector3d(0, 1, 0));
            switch (i)
            {
              case 2:
                plns[i].Rotate(Math.PI / -2, plns[i].ZAxis);
                break;
              case 3:
                plns[i].Rotate(Math.PI / 2, plns[i].ZAxis);
                break;
              case 4:
                plns[i].Rotate(Math.PI, plns[i].ZAxis);
                break;
              default:
                break;
            }

            plnList.Add(plns[i]);
            Box bt = new Box(plns[i], dimsXY, dimsXY, dimsZ);
            cornerBoxes.Add(bt);
          }

        }
      }
    }
    misc = plnList;
    corners = cornerBoxes;
    points = ptBoxes;
  }

  // <Custom additional code> 
  public Vector3d getPyrZ(Mesh m, Point3d pt)
  {
    BoundingBox b = m.GetBoundingBox(false);
    Vector3d v = Point3d.Subtract(b.Center, pt);
    v = v / v.Length;
    return new Vector3d(0, 0, Math.Round(v.Z, 0));
  }
}