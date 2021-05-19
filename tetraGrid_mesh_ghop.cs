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
  private void RunScript(Point3d lowCorner, Point3d highCorner, double dS, double dZ, ref object pyr, ref object ptMatrix, ref object tet, ref object meshCt)
  {
    // get grid count + dims
    double xDim = highCorner.X - lowCorner.X;
    int xCt = (int) Math.Ceiling(xDim / dS);

    double yDim = highCorner.Y - lowCorner.Y;
    int yCt = (int) Math.Ceiling(yDim / dS);

    double zDim = highCorner.Z - lowCorner.Z;
    int zCt = (int) Math.Ceiling(zDim / dZ);

    //create ptArray and new origin to account for boundary conditions
    Point3d[,,] ptArray = new Point3d[xCt + 2, yCt + 2, zCt];
    double xIe = lowCorner.X;
    double yIe = lowCorner.Y; //nIe = even z plane origin
    double zI = lowCorner.Z;

    double xIo = lowCorner.X - (dS / 2);
    double yIo = lowCorner.Y - (dS / 2); //nIo = odd z plane origin

    //generate pt matrix
    for (int z = 0; z < zCt; z++)
    {
      if (z % 2 == 0) //for layers
      {
        for (int x = 0; x < xCt; x++)
        {
          for (int y = 0; y < yCt; y++)
          {
            ptArray[x, y, z] = new Point3d(xIe + (x * dS), yIe + (y * dS), zI + (z * dZ));
          }
        }
      }
      else // odd layers
      {
        for (int x = 0; x <= xCt; x++)
        {
          for (int y = 0; y <= yCt; y++)
          {
            ptArray[x, y, z] = new Point3d(xIo + (x * dS), yIo + (y * dS), zI + (z * dZ));
          }
        }
      }
    }

    //generate meshes
    var meshList = new ArrayList();
    var tetList = new ArrayList();
    for (int z = 0; z < zCt; z++)
    {
      if (z % 2 == 0)
      {
        for (int x = 0; x < xCt; x++)
        {
          for (int y = 0; y < yCt; y++)
          {
            //point-down pyramids
            if (z < zCt - 1)
            {
              Mesh pDown = new Mesh();
              pDown.Vertices.Add(ptArray[x, y, z]);             //0
              pDown.Vertices.Add(ptArray[x, y, z + 1]);         //1
              pDown.Vertices.Add(ptArray[x, y + 1, z + 1]);     //2
              pDown.Vertices.Add(ptArray[x + 1, y, z + 1]);     //3
              pDown.Vertices.Add(ptArray[x + 1, y + 1, z + 1]); //4

              pDown.Faces.AddFace(0, 1, 2);
              pDown.Faces.AddFace(0, 1, 3);
              pDown.Faces.AddFace(0, 2, 4);
              pDown.Faces.AddFace(0, 3, 4);
              pDown.Faces.AddFace(1, 3, 4, 2);

              pDown.Normals.ComputeNormals();
              pDown.UnifyNormals();
              pDown.Compact();

              meshList.Add(pDown);
            }

            //point-up pyramids
            if (z >= 1 && z <= zCt)
            {
              Print(zCt.ToString());
              Mesh pUp = new Mesh();
              pUp.Vertices.Add(ptArray[x, y, z]);             //0
              pUp.Vertices.Add(ptArray[x, y, z - 1]);         //1
              pUp.Vertices.Add(ptArray[x, y + 1, z - 1]);     //2
              pUp.Vertices.Add(ptArray[x + 1, y, z - 1]);     //3
              pUp.Vertices.Add(ptArray[x + 1, y + 1, z - 1]); //4

              pUp.Faces.AddFace(0, 1, 2);
              pUp.Faces.AddFace(0, 1, 3);
              pUp.Faces.AddFace(0, 2, 4);
              pUp.Faces.AddFace(0, 3, 4);
              pUp.Faces.AddFace(1, 3, 4, 2);

              pUp.Normals.ComputeNormals();
              pUp.UnifyNormals();
              pUp.Compact();

              meshList.Add(pUp);
            }
          }
        }
      }
      else
      {
        //interstitial pyramids
        for (int x = 0; x < xCt - 1; x++)
        {
          for (int y = 0; y < yCt - 1; y++)
          {
            //point-up pyramids
            Mesh pUp = new Mesh();
            pUp.Vertices.Add(ptArray[x + 1, y + 1, z]);     //0
            pUp.Vertices.Add(ptArray[x, y, z - 1]);         //1
            pUp.Vertices.Add(ptArray[x, y + 1, z - 1]);     //2
            pUp.Vertices.Add(ptArray[x + 1, y, z - 1]);     //3
            pUp.Vertices.Add(ptArray[x + 1, y + 1, z - 1]); //4

            pUp.Faces.AddFace(0, 1, 2);
            pUp.Faces.AddFace(0, 1, 3);
            pUp.Faces.AddFace(0, 2, 4);
            pUp.Faces.AddFace(0, 3, 4);
            pUp.Faces.AddFace(1, 3, 4, 2);

            pUp.Normals.ComputeNormals();
            pUp.UnifyNormals();
            pUp.Compact();

            meshList.Add(pUp);

            //point-down pyramids
            if (z < zCt - 1)
            {
              Mesh pDown = new Mesh();
              pDown.Vertices.Add(ptArray[x + 1, y + 1, z]);     //0
              pDown.Vertices.Add(ptArray[x, y, z + 1]);         //1
              pDown.Vertices.Add(ptArray[x, y + 1, z + 1]);     //2
              pDown.Vertices.Add(ptArray[x + 1, y, z + 1]);     //3
              pDown.Vertices.Add(ptArray[x + 1, y + 1, z + 1]); //4

              pDown.Faces.AddFace(0, 1, 2);
              pDown.Faces.AddFace(0, 1, 3);
              pDown.Faces.AddFace(0, 2, 4);
              pDown.Faces.AddFace(0, 3, 4);
              pDown.Faces.AddFace(1, 3, 4, 2);

              pDown.Normals.ComputeNormals();
              pDown.UnifyNormals();
              pDown.Compact();

              meshList.Add(pDown);
            }
          }
        }
      }
      //tetrahedra
      for (int x = 0; x < xCt - 1; x++)
      {
        for (int y = 0; y < yCt - 1; y++)
        {

          if (z != zCt - 1 && z % 2 == 0)
          {
            //Tet bottom line along Y
            Mesh tetY = new Mesh();
            tetY.Vertices.Add(ptArray[x, y, z]);            //0
            tetY.Vertices.Add(ptArray[x, y + 1, z]);        //1
            tetY.Vertices.Add(ptArray[x, y + 1, z + 1]);    //2
            tetY.Vertices.Add(ptArray[x + 1, y + 1, z + 1]);//3

            tetY.Faces.AddFace(0, 1, 2);
            tetY.Faces.AddFace(0, 1, 3);
            tetY.Faces.AddFace(0, 2, 3);
            tetY.Faces.AddFace(1, 2, 3);

            tetY.Normals.ComputeNormals();
            tetY.UnifyNormals();
            tetY.Compact();

            tetList.Add(tetY);

            //Tet bottom line along X
            Mesh tetX = new Mesh();
            tetX.Vertices.Add(ptArray[x, y, z]);            //0
            tetX.Vertices.Add(ptArray[x + 1, y, z]);        //1
            tetX.Vertices.Add(ptArray[x + 1, y, z + 1]);    //2
            tetX.Vertices.Add(ptArray[x + 1, y + 1, z + 1]);//3

            tetX.Faces.AddFace(0, 1, 2);
            tetX.Faces.AddFace(0, 1, 3);
            tetX.Faces.AddFace(0, 2, 3);
            tetX.Faces.AddFace(1, 2, 3);

            tetX.Normals.ComputeNormals();
            tetX.UnifyNormals();
            tetX.Compact();

            tetList.Add(tetX);

            if (z > 1)
            {
              //Tet top line along Y
              Mesh tetYT = new Mesh();
              tetYT.Vertices.Add(ptArray[x, y, z]);            //0
              tetYT.Vertices.Add(ptArray[x, y + 1, z]);        //1
              tetYT.Vertices.Add(ptArray[x, y + 1, z - 1]);    //2
              tetYT.Vertices.Add(ptArray[x + 1, y + 1, z - 1]);//3

              tetYT.Faces.AddFace(0, 1, 2);
              tetYT.Faces.AddFace(0, 1, 3);
              tetYT.Faces.AddFace(0, 2, 3);
              tetYT.Faces.AddFace(1, 2, 3);

              tetYT.Normals.ComputeNormals();
              tetYT.UnifyNormals();
              tetYT.Compact();

              tetList.Add(tetYT);

              //Tet top line along X
              Mesh tetXT = new Mesh();
              tetXT.Vertices.Add(ptArray[x, y, z]);            //0
              tetXT.Vertices.Add(ptArray[x + 1, y, z]);        //1
              tetXT.Vertices.Add(ptArray[x + 1, y, z - 1]);    //2
              tetXT.Vertices.Add(ptArray[x + 1, y + 1, z - 1]);//3

              tetXT.Faces.AddFace(0, 1, 2);
              tetXT.Faces.AddFace(0, 1, 3);
              tetXT.Faces.AddFace(0, 2, 3);
              tetXT.Faces.AddFace(1, 2, 3);

              tetXT.Normals.ComputeNormals();
              tetXT.UnifyNormals();
              tetXT.Compact();

              tetList.Add(tetXT);
            }
          }
        }
      }
    }

    //update outputs
    pyr = meshList;
    tet = tetList;
    ptMatrix = ptArray;
    meshCt = meshList.Count;

  }

  // <Custom additional code> 
  // Maybe someday I'll make that recursive method...

  // </Custom additional code> 
}