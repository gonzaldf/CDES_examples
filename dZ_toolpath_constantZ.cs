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
  private void RunScript(Mesh m, double dZ, int resolution, int smoothF, ref object toolPath, ref object misc)
  {
    //main vars
    Plane[] intPlanes = new Plane[resolution];
    Curve[] intCurves = new Curve[resolution];
    var toolPts = new ArrayList();
    Interval[] ds = new Interval[resolution];

    //base info
    Point3d[] bbox = m.GetBoundingBox(false).GetCorners();
    Plane bPlane = new Plane(new Point3d(bbox[0].X, bbox[0].Y, bbox[0].Z + dZ), new Vector3d(1, 0, 0), new Vector3d(0, 1, 0));
    Polyline bCurve = Rhino.Geometry.Intersect.Intersection.MeshPlane(m, bPlane)[0];

    //doing this here for reasons
    int zDivs = (int) ((bbox[7].Z - bbox[0].Z) / dZ);
    Point3d[,] intPoints = new Point3d[resolution, zDivs];

    //divide base curve, get normal vectors on curve
    Point3d[] baseDivs;
    bCurve.ToPolylineCurve().DivideByCount(resolution, true, out baseDivs); //origin points
    double[] bCrvParams = bCurve.ToPolylineCurve().DivideByCount(resolution, true);
    Vector3d[] curvature = new Vector3d[bCrvParams.Length];

    for (int i = 0; i < resolution; i++)
    {
      //getting tangent curves
      curvature[i] = bCurve.ToPolylineCurve().TangentAt(bCrvParams[i]);

      //creating plane array, lets get weird
      intPlanes[i] = new Plane(baseDivs[i], curvature[i]); //kind of surprised this worked

      //use normal planes to construct ZCurves, find closest to pt, discard others
      Polyline[] intPCurves = Rhino.Geometry.Intersect.Intersection.MeshPlane(m, intPlanes[i]);
      int bestIndex = 0;
      for (int j = 1; j < intPCurves.Length; j++)
      {
        //MAKE SURE BOTTOM OF MESH IS PLANAR OR THIS PART ACTS VERY STRANGE
        Double distNsub = baseDivs[i].DistanceTo(intPCurves[j - 1].ClosestPoint(bCurve.CenterPoint()));
        Double distN = baseDivs[i].DistanceTo(intPCurves[j].ClosestPoint(bCurve.CenterPoint()));

        //check for closest curve fit
        if (distN <= distNsub)
        {
          bestIndex = j;
        }
      }

      intCurves[i] = intPCurves[bestIndex].ToPolylineCurve();

      //build curve domain list
      ds[i] = intCurves[i].Domain;
    }

    //build the big ole spiral
    for (int i = 0; i < zDivs * resolution * smoothF; i++)
    {
      double perc = (double) i / (zDivs * resolution * smoothF);
      toolPts.Add(intCurves[i % resolution].PointAt(perc * ds[i % resolution].Length));
    }

    //output
    misc = ds;
    toolPts.Reverse();
    toolPath = toolPts;
  }

  // <Custom additional code> 

  // </Custom additional code> 
}