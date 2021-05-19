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
  private void RunScript(List<Mesh> tetra, double bWidth, double bHeight, double jOffset, int jDim, ref object beams, ref object joints, ref object tCenters)
  {
    //vars, etc.
    int count = tetra.Capacity;
    Point3d[] cPts = new Point3d[count];
    Point3d[,,] beamPts = new Point3d[count, 8, 3]; //count # of tetra, 8 edges each tetra, 2 points for each edge + avg for non-planar
    Vector3d[,] edgeToCenter = new Vector3d[count, 8]; //count # of tetra, 4 non-planar edges to find vector to center of tetra
    var beamMeshList = new ArrayList();
    var jointMeshList = new ArrayList();

    //Get mesh edges, centers, edge to center vectors for non-planar (XY) edges, beam offset endpoints
    for (int i = 0; i < count; i++)
    {
      if (tetra[i].Vertices.Count == 5)
      {
        Point3d[] ptBucket = new Point3d[2];

        //non planar edges, ref tetra gen c# for vertex layout
        ptBucket = ptCenterOffset(new Point3d[]{tetra[i].Vertices[0], tetra[i].Vertices[1]}, jOffset);
        beamPts[i, 0, 0] = ptBucket[0];
        beamPts[i, 0, 1] = ptBucket[1];
        beamPts[i, 0, 2] = getPtAvg(new Point3d[]{beamPts[i, 0, 0], beamPts[i, 0, 1]});
        ptBucket = ptCenterOffset(new Point3d[]{tetra[i].Vertices[0], tetra[i].Vertices[2]}, jOffset);
        beamPts[i, 1, 0] = ptBucket[0];
        beamPts[i, 1, 1] = ptBucket[1];
        beamPts[i, 1, 2] = getPtAvg(new Point3d[]{beamPts[i, 1, 0], beamPts[i, 1, 1]});
        ptBucket = ptCenterOffset(new Point3d[]{tetra[i].Vertices[0], tetra[i].Vertices[3]}, jOffset);
        beamPts[i, 2, 0] = ptBucket[0];;
        beamPts[i, 2, 1] = ptBucket[1];
        beamPts[i, 2, 2] = getPtAvg(new Point3d[]{beamPts[i, 2, 0], beamPts[i, 2, 1]});
        ptBucket = ptCenterOffset(new Point3d[]{tetra[i].Vertices[0], tetra[i].Vertices[4]}, jOffset);
        beamPts[i, 3, 0] = ptBucket[0];
        beamPts[i, 3, 1] = ptBucket[1];
        beamPts[i, 3, 2] = getPtAvg(new Point3d[]{beamPts[i, 3, 0], beamPts[i, 3, 1]});

        //planar edges
        ptBucket = ptCenterOffset(new Point3d[]{tetra[i].Vertices[1], tetra[i].Vertices[2]}, jOffset);
        beamPts[i, 4, 0] = ptBucket[0];
        beamPts[i, 4, 1] = ptBucket[1];
        beamPts[i, 4, 2] = getPtAvg(new Point3d[]{beamPts[i, 4, 0], beamPts[i, 4, 1]});
        ptBucket = ptCenterOffset(new Point3d[]{tetra[i].Vertices[2], tetra[i].Vertices[4]}, jOffset);
        beamPts[i, 5, 0] = ptBucket[0];
        beamPts[i, 5, 1] = ptBucket[1];
        beamPts[i, 5, 2] = getPtAvg(new Point3d[]{beamPts[i, 5, 0], beamPts[i, 5, 1]});
        ptBucket = ptCenterOffset(new Point3d[]{tetra[i].Vertices[4], tetra[i].Vertices[3]}, jOffset);
        beamPts[i, 6, 0] = ptBucket[0];
        beamPts[i, 6, 1] = ptBucket[1];
        beamPts[i, 6, 2] = getPtAvg(new Point3d[]{beamPts[i, 6, 0], beamPts[i, 6, 1]});
        ptBucket = ptCenterOffset(new Point3d[]{tetra[i].Vertices[3], tetra[i].Vertices[1]}, jOffset);
        beamPts[i, 7, 0] = ptBucket[0];
        beamPts[i, 7, 1] = ptBucket[1];
        beamPts[i, 7, 2] = getPtAvg(new Point3d[]{beamPts[i, 7, 0], beamPts[i, 7, 1]});

        //centerpoints
        cPts[i] = getPtAvg(tetra[i].Vertices.ToPoint3dArray());

        //edge center vectors to tetrahedron center
        for (int j = 0; j < 8; j++)
        {
          edgeToCenter[i, j] = getDirection(beamPts[i, j, 2], cPts[i]);
        }
      }
    }
    //create beam meshes
    for (int i = 0; i < count; i++)
    {
      for (int j = 0; j < 8; j++)
      {

        Mesh mTemp = new Mesh();

        //construct 3 axes of beam, x perpendicular to beam, y points to center, z is parallel to beam
        Vector3d[] xyz = new Vector3d[3];

        //for non-planar members
        if (j < 4)
        {
          xyz[1] = edgeToCenter[i, j] / edgeToCenter[i, j].Length;
          xyz[2] = getDirection(beamPts[i, j, 0], beamPts[i, j, 1]);
          xyz[0] = Vector3d.CrossProduct(xyz[1], xyz[2]);

          //calc vertices from new xyz
          mTemp.Vertices.Add(Point3d.Add(beamPts[i, j, 0], 0.5 * bWidth * xyz[0]));   //0
          mTemp.Vertices.Add(Point3d.Add(beamPts[i, j, 0], -0.5 * bWidth * xyz[0]));  //1
          mTemp.Vertices.Add(Point3d.Add(beamPts[i, j, 0], Vector3d.Add(0.5 * bWidth * xyz[0], bHeight * xyz[1])));   //2
          mTemp.Vertices.Add(Point3d.Add(beamPts[i, j, 0], Vector3d.Add(-0.5 * bWidth * xyz[0], bHeight * xyz[1])));  //3
          mTemp.Vertices.Add(Point3d.Add(beamPts[i, j, 1], 0.5 * bWidth * xyz[0]));   //4
          mTemp.Vertices.Add(Point3d.Add(beamPts[i, j, 1], -0.5 * bWidth * xyz[0]));  //5
          mTemp.Vertices.Add(Point3d.Add(beamPts[i, j, 1], Vector3d.Add(0.5 * bWidth * xyz[0], bHeight * xyz[1])));   //6
          mTemp.Vertices.Add(Point3d.Add(beamPts[i, j, 1], Vector3d.Add(-0.5 * bWidth * xyz[0], bHeight * xyz[1])));  //7
        }
          //planar beams (no parallel comp)
        else
        {
          //beam along Y axis
          if (beamPts[i, j, 0].Y == beamPts[i, j, 1].Y)
          {
            xyz[0] = new Vector3d(0, edgeToCenter[i, j].Y, 0);  //discard x, z
            xyz[0] = xyz[0] / xyz[0].Length;                    //unitize +/- y
          }
            //beam along X axis
          else
          {
            xyz[0] = new Vector3d(edgeToCenter[i, j].X, 0, 0);  //discard y, z
            xyz[0] = xyz[0] / xyz[0].Length;                    //unitize +/- x
          }

          //get Z comp
          if (cPts[i].Z > beamPts[i, j, 0].Z){
            xyz[2] = new Vector3d(0, 0, 1);
          }
          else{
            xyz[2] = new Vector3d(0, 0, -1);
          }

          //calc vertices from new xyz, x or y is index 0 in this case: too lazy to make new var
          Print(xyz[0].ToString() + xyz[2].ToString());
          mTemp.Vertices.Add(Point3d.Add(beamPts[i, j, 0], bWidth * xyz[0]));                                   //0
          mTemp.Vertices.Add(beamPts[i, j, 0]);                                                                 //1
          mTemp.Vertices.Add(Point3d.Add(beamPts[i, j, 0], Vector3d.Add(bWidth * xyz[0], bHeight * xyz[2])));   //2
          mTemp.Vertices.Add(Point3d.Add(beamPts[i, j, 0], bHeight * xyz[2]));                                  //3
          mTemp.Vertices.Add(Point3d.Add(beamPts[i, j, 1], bWidth * xyz[0]));                                   //4
          mTemp.Vertices.Add(beamPts[i, j, 1]);                                                                 //5
          mTemp.Vertices.Add(Point3d.Add(beamPts[i, j, 1], Vector3d.Add(bWidth * xyz[0], bHeight * xyz[2])));   //6
          mTemp.Vertices.Add(Point3d.Add(beamPts[i, j, 1], bHeight * xyz[2]));                                  //7
        }

        //assign faces
        mTemp.Faces.AddFace(0, 1, 5, 4);
        mTemp.Faces.AddFace(0, 4, 6, 2);
        mTemp.Faces.AddFace(2, 6, 7, 3);
        mTemp.Faces.AddFace(3, 1, 5, 7);
        mTemp.Faces.AddFace(0, 1, 3, 2);
        mTemp.Faces.AddFace(4, 5, 7, 6);

        //correct mesh
        mTemp.Normals.ComputeNormals();
        mTemp.UnifyNormals();
        mTemp.Compact();

        //add mesh to master list
        beamMeshList.Add(mTemp);
      }
    }

    

    //update outputs
    tCenters = cPts;
    beams = beamMeshList;

    //joints = jointMeshList; //reserved
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

  //2pt offset - takes 2 points and offsets them towards their average by bOffset
  public Point3d[] ptCenterOffset(Point3d[] endPts, double bOffset)
  {
    //get points
    Point3d p1 = endPts[0];
    Point3d p2 = endPts[1];
    Point3d pAvg = getPtAvg(endPts);
    double pDist = Math.Sqrt(Math.Pow(pAvg.X - p1.X, 2) + Math.Pow(pAvg.Y - p1.Y, 2) + Math.Pow(pAvg.Z - p1.Z, 2));

    //offset points
    Vector3d p1vect = new Vector3d(pAvg.X - p1.X, pAvg.Y - p1.Y, pAvg.Z - p1.Z);
    Vector3d p2vect = new Vector3d(pAvg.X - p2.X, pAvg.Y - p2.Y, pAvg.Z - p2.Z);
    p1vect = (p1vect / pDist) * bOffset;
    p2vect = (p2vect / pDist) * bOffset;

    p1 = Point3d.Add(p1, p1vect);
    p2 = Point3d.Add(p2, p2vect);

    //rebuild return array
    return new Point3d[]{p1, p2};
  }

  //gets unit vector from A to B
  public Vector3d getDirection(Point3d a, Point3d b)
  {
    Vector3d direction = new Vector3d(b.X - a.X, b.Y - a.Y, b.Z - a.Z);
    return direction / direction.Length;
  }
}