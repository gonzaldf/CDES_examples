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
  private void RunScript(List<Mesh> tetra, List<Curve> crv, double depth, double gap, double range, ref object panels, ref object panelCt)
  {
    //init vars
    int count = tetra.Capacity;
    var mList = new ArrayList();

    Point3d faceCenter;
    Mesh mTemp;

    //for each tetra, get centerPoint of pyr face, check dist to crv in range,
    for (int i = 0; i < count; i++)
    {
      if (tetra[i].Vertices.Count == 5)
      {
        //5 faces on pyr
        for (int j = 0; j < 5; j++)
        {
          //check for base
          if (tetra[i].Faces[j].IsQuad)
          {
            faceCenter = getPtAvg(new Point3d[]{tetra[i].Vertices[tetra[i].Faces[j].A], tetra[i].Vertices[tetra[i].Faces[j].B], tetra[i].Vertices[tetra[i].Faces[j].C], tetra[i].Vertices[tetra[i].Faces[j].D]});
          }
          else
          {
            faceCenter = getPtAvg(new Point3d[]{tetra[i].Vertices[tetra[i].Faces[j].A], tetra[i].Vertices[tetra[i].Faces[j].B], tetra[i].Vertices[tetra[i].Faces[j].C]});
          }

          foreach (var c in crv)
          {
            double d; //not sure how to make things work without this mysterious double d - do not remove
            if (c.ClosestPoint(faceCenter, out d, range))
            {
              Vector3d toCenter = getDirection(faceCenter, VolumeMassProperties.Compute(tetra[i]).Centroid);
              toCenter = toCenter / toCenter.Length;

              mTemp = new Mesh();

              //get vertices w/offset of depth + reveal of gap
              mTemp.Vertices.Add(mvPoint(tetra[i].Vertices[tetra[i].Faces[j].A], faceCenter, gap));           //0
              mTemp.Vertices.Add(mvPoint(tetra[i].Vertices[tetra[i].Faces[j].B], faceCenter, gap));           //1
              mTemp.Vertices.Add(mvPoint(tetra[i].Vertices[tetra[i].Faces[j].C], faceCenter, gap));           //2
              mTemp.Vertices.Add(Point3d.Add(mvPoint(tetra[i].Vertices[tetra[i].Faces[j].A], faceCenter, gap), toCenter * -depth));     //3
              mTemp.Vertices.Add(Point3d.Add(mvPoint(tetra[i].Vertices[tetra[i].Faces[j].B], faceCenter, gap), toCenter * -depth));     //4
              mTemp.Vertices.Add(Point3d.Add(mvPoint(tetra[i].Vertices[tetra[i].Faces[j].C], faceCenter, gap), toCenter * -depth));     //5
              if (tetra[i].Faces[j].IsQuad){
                mTemp.Vertices.Add(mvPoint(tetra[i].Vertices[tetra[i].Faces[j].D], faceCenter, gap));                                   //6
                mTemp.Vertices.Add(Point3d.Add(mvPoint(tetra[i].Vertices[tetra[i].Faces[j].D], faceCenter, gap), toCenter * -depth));   //7
              }

              //build faces
              if (tetra[i].Faces[j].IsQuad){
                mTemp.Faces.AddFace(0, 1, 2, 6);
                mTemp.Faces.AddFace(0, 1, 4, 3);
                mTemp.Faces.AddFace(3, 4, 5, 7);
                mTemp.Faces.AddFace(2, 6, 7, 5);
                mTemp.Faces.AddFace(0, 6, 7, 3);
                mTemp.Faces.AddFace(1, 2, 5, 4);
              }
              else
              {
                mTemp.Faces.AddFace(0, 1, 2);
                mTemp.Faces.AddFace(0, 1, 4, 3);
                mTemp.Faces.AddFace(0, 2, 5, 3);
                mTemp.Faces.AddFace(1, 2, 5, 4);
                mTemp.Faces.AddFace(3, 4, 5);
              }

              //correct mesh
              mTemp.Normals.ComputeNormals();
              mTemp.UnifyNormals();
              mTemp.Compact();

              //add to master list
              mList.Add(mTemp);
            }
          }
        }
      }
      else
      {
        //4 faces on tetrahedron
        for (int j = 0; j < 4; j++)
        {
          faceCenter = getPtAvg(new Point3d[]{tetra[i].Vertices[tetra[i].Faces[j].A], tetra[i].Vertices[tetra[i].Faces[j].B], tetra[i].Vertices[tetra[i].Faces[j].C]});

          foreach (var c in crv)
          {
            double d; //not sure how to make things work without this mysterious double d - do not remove
            if (c.ClosestPoint(faceCenter, out d, range))
            {
              Vector3d toCenter = getDirection(faceCenter, VolumeMassProperties.Compute(tetra[i]).Centroid);
              toCenter = toCenter / toCenter.Length;

              mTemp = new Mesh();

              //get vertices w/offset of depth + reveal of gap
              mTemp.Vertices.Add(mvPoint(tetra[i].Vertices[tetra[i].Faces[j].A], faceCenter, gap));           //0
              mTemp.Vertices.Add(mvPoint(tetra[i].Vertices[tetra[i].Faces[j].B], faceCenter, gap));           //1
              mTemp.Vertices.Add(mvPoint(tetra[i].Vertices[tetra[i].Faces[j].C], faceCenter, gap));           //2
              mTemp.Vertices.Add(Point3d.Add(mvPoint(tetra[i].Vertices[tetra[i].Faces[j].A], faceCenter, gap), toCenter * -depth));     //3
              mTemp.Vertices.Add(Point3d.Add(mvPoint(tetra[i].Vertices[tetra[i].Faces[j].B], faceCenter, gap), toCenter * -depth));     //4
              mTemp.Vertices.Add(Point3d.Add(mvPoint(tetra[i].Vertices[tetra[i].Faces[j].C], faceCenter, gap), toCenter * -depth));     //5

              //build faces
              mTemp.Faces.AddFace(0, 1, 2);
              mTemp.Faces.AddFace(0, 1, 4, 3);
              mTemp.Faces.AddFace(0, 2, 5, 3);
              mTemp.Faces.AddFace(1, 2, 5, 4);
              mTemp.Faces.AddFace(3, 4, 5);

              //correct mesh
              mTemp.Normals.ComputeNormals();
              mTemp.UnifyNormals();
              mTemp.Compact();

              //add to master list
              mList.Add(mTemp);
            }
          }
        }
      }

    }
    //update outputs
    panelCt = mList.Capacity;
    panels = mList;
  }

  // <Custom additional code> 
  //gets weighted average of a list of points
  public Point3d getPtAvg(Point3d[] pts)
  {
    int len = pts.GetLength(0);
    double x = 0;
    double y = 0;
    double z = 0;

    for (int i = 0; i < len; i++)
    {
      x += pts[i].X;
      y += pts[i].Y;
      z += pts[i].Z;
    }
    return new Point3d(x / len, y / len, z / len);
  }

  //gets unit vector from A to B
  public Vector3d getDirection(Point3d a, Point3d b)
  {
    Vector3d direction = new Vector3d(b.X - a.X, b.Y - a.Y, b.Z - a.Z);
    return direction / direction.Length;
  }

  //move pt A towards B by dist
  public Point3d mvPoint(Point3d A, Point3d B, double dist)
  {
    Vector3d dir = getDirection(A, B);
    return Point3d.Add(A, dir * dist);
  }
}