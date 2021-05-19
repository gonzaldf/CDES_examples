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
  private void RunScript(Mesh srfMesh, List<Point3d> pts, List<Vector3d> vects, double radHub, double radArms, double lenArms, double searchDist, ref object hubs, ref object arms, ref object misc)
  {
    // vars
    Circle[] circs = new Circle[pts.Capacity];
    Plane[] plns = new Plane[pts.Capacity];
    ArrayList[] nearPts = new ArrayList[pts.Capacity]; //holy shit an array of lists, forgive me RAM
    String[] stOut = new String[pts.Capacity];

    var hubList = new ArrayList();
    var armList = new ArrayList();


    // hub geo
    // circles on each point w/plane for orientation
    for (int i = 0; i < pts.Capacity; i++)
    {
      plns[i] = new Plane(Point3d.Subtract(pts[i], -vects[i] / 16), vects[i]);
      circs[i] = new Circle(plns[i], radHub);
      Cylinder c = new Cylinder(circs[i], vects[i].Length / 8);
      hubList.Add(Mesh.CreateFromCylinder(c, 1, 12));
    }


    // arm geo

    // arm from grid to glass
    for (int i = 0; i < pts.Capacity; i++)
    {
      Interval dXY = new Interval(radHub / -10, radHub / 10);
      Box bt = new Box(plns[i], dXY, dXY, new Interval(0, (15 * vects[i].Length) / 16));
      armList.Add(Mesh.CreateFromBox(bt, 1, 1, 1));
    }

    hubs = hubList;
    arms = armList;
    misc = stOut;
  }

  // <Custom additional code> 
  public Mesh getCyl(Point3d p, Vector3d n, double rad, double depth)
  {
    Plane thisPlane = new Plane(p, n);
    Circle thisCirc = new Circle(thisPlane, rad);
    Cylinder c = new Cylinder(thisCirc, depth);
    return Mesh.CreateFromCylinder(c, 1, 12);
  }
  // </Custom additional code> 
}