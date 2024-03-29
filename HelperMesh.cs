using System.Collections.Generic;
using UnityEngine;

public class HelperMesh 
{
    // to help with mapping the UV coordinates
    public static Vector2 uv00 = new Vector2(0, 0);   // 00 --- 10 uv mapping coord readability helper
    public static Vector2 uv10 = new Vector2(1, 0);   //  |     |
    public static Vector2 uv01 = new Vector2(0, 1);   //  |     |
    public static Vector2 uv11 = new Vector2(1, 1);   // 01 --- 11


    /*                  c--d                         g--h     *next
                        |  |                         |  |
                     a--b  e------------ ------------f  i--j

              spline0                      spline1

               C--D                         G--H              *current pointset (capital letters)
               |  |                         |  |
            A--B  E------------ ------------F  I--J

              barrier                     barrier
               left                        right
        */
    public static GameObject MakeMesh_Road(ref List<HelperPointset> Pointsets, string name, Material material)
    {
        // step 1. data for mesh
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();
        int index = 0;
        for (int p = 0; p < Pointsets.Count; p++)
        {
            Vector3 A = Pointsets[p].A; //  a-----j    clockwise triangles:
            Vector3 J = Pointsets[p].J; //  |  /  |      Aaj         
            Vector3 a;                  //  A-----J      JAj
            Vector3 j;
            if (p == Pointsets.Count - 1)
            {
                a = Pointsets[0].A;
                j = Pointsets[0].J;
            }
            else
            {
                a = Pointsets[p + 1].A;
                j = Pointsets[p + 1].J;
            }

            vertices.Add(A); //Aaj
            vertices.Add(a);
            vertices.Add(j);
            vertices.Add(J); //JAj
            vertices.Add(A);
            vertices.Add(j);

            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);

            uv.Add(uv00); //Aaj
            uv.Add(uv01);
            uv.Add(uv11);
            uv.Add(uv10); //JAj
            uv.Add(uv00);
            uv.Add(uv11);
        }

        // step 2. create mesh gameobject
        GameObject gameobject = create_GameObject(name);
        set_DataToMesh(gameobject, vertices, triangles, uv, material);

        return gameobject;
    }
    public static GameObject MakeMesh_BarrierLeft(ref List<HelperPointset> Pointsets, string name, Material material)
    {
        // step 1. data for mesh
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();
        int index = 0;
        for (int p = 0; p < Pointsets.Count; p++)
        {
            /*  outside     top      inside
             *   c---C     c---d     D---d          C---D
             *   | / |     | / |     | / |          |   | (inside)
             *   b---B     C---D     E---e         -B   E-
             */
            Vector3 B = Pointsets[p].B; 
            Vector3 C = Pointsets[p].C;
            Vector3 D = Pointsets[p].D;
            Vector3 E = Pointsets[p].E;
            Vector3 b;                 
            Vector3 c;
            Vector3 d;
            Vector3 e;
            if (p == Pointsets.Count - 1)
            {
                b = Pointsets[0].B;
                c = Pointsets[0].C;
                d = Pointsets[0].D;
                e = Pointsets[0].E;
            }
            else
            {
                b = Pointsets[p + 1].B;
                c = Pointsets[p + 1].C;
                d = Pointsets[p + 1].D;
                e = Pointsets[p + 1].E;
            }

            vertices.Add(b); //bcC (outside)
            vertices.Add(c);
            vertices.Add(C);
            vertices.Add(B); //BbC
            vertices.Add(b);
            vertices.Add(C);
            vertices.Add(C); //Ccd (top)
            vertices.Add(c);
            vertices.Add(d);
            vertices.Add(D); //DCd
            vertices.Add(C);
            vertices.Add(d);
            vertices.Add(E); //EDd (inside)
            vertices.Add(D);
            vertices.Add(d);
            vertices.Add(e); //eEd
            vertices.Add(E);
            vertices.Add(d);

            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);

            uv.Add(uv00); //bcC (outside)
            uv.Add(uv01);
            uv.Add(uv11);
            uv.Add(uv10); //BbC
            uv.Add(uv00);
            uv.Add(uv11);
            uv.Add(uv00); //Ccd (top)
            uv.Add(uv01);
            uv.Add(uv11);
            uv.Add(uv10); //DCd
            uv.Add(uv00);
            uv.Add(uv11);
            uv.Add(uv00); //EDd (inside)
            uv.Add(uv01);
            uv.Add(uv11);
            uv.Add(uv10); //eEd
            uv.Add(uv00);
            uv.Add(uv11);
        }

        // step 2. create mesh gameobject
        GameObject gameobject = create_GameObject(name);
        set_DataToMesh(gameobject, vertices, triangles, uv, material);

        return gameobject;
    }
    public static GameObject MakeMesh_BarrierRight(ref List<HelperPointset> Pointsets, string name, Material material)
    {
        // step 1. data for mesh
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();
        int index = 0;
        for (int p = 0; p < Pointsets.Count; p++)
        {
            /*  inside      top     outside
             *   g---G     g---h     H---h               G---H
             *   | / |     | / |     | / |      (inside) |   |
             *   f---F     G---H     I---i              -F   I-
             */
            Vector3 F = Pointsets[p].F;
            Vector3 G = Pointsets[p].G;
            Vector3 H = Pointsets[p].H;
            Vector3 I = Pointsets[p].I;
            Vector3 f;
            Vector3 g;
            Vector3 h;
            Vector3 i;
            if (p == Pointsets.Count - 1)
            {
                f = Pointsets[0].f;
                g = Pointsets[0].g;
                h = Pointsets[0].h;
                i = Pointsets[0].i;
            }
            else
            {
                f = Pointsets[p + 1].f;
                g = Pointsets[p + 1].g;
                h = Pointsets[p + 1].h;
                i = Pointsets[p + 1].i;
            }

            vertices.Add(f); //fgG (inside)
            vertices.Add(g);
            vertices.Add(G);
            vertices.Add(F); //FfG
            vertices.Add(f);
            vertices.Add(G);
            vertices.Add(G); //Ggh (top)
            vertices.Add(g);
            vertices.Add(h);
            vertices.Add(H); //HGh
            vertices.Add(G);
            vertices.Add(h);
            vertices.Add(I); //IHh (outside)
            vertices.Add(H);
            vertices.Add(h);
            vertices.Add(i); //iIh
            vertices.Add(I);
            vertices.Add(h);

            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);
            triangles.Add(index++);

            uv.Add(uv00); //bcC (outside)
            uv.Add(uv01);
            uv.Add(uv11);
            uv.Add(uv10); //BbC
            uv.Add(uv00);
            uv.Add(uv11);
            uv.Add(uv00); //Ccd (top)
            uv.Add(uv01);
            uv.Add(uv11);
            uv.Add(uv10); //DCd
            uv.Add(uv00);
            uv.Add(uv11);
            uv.Add(uv00); //EDd (inside)
            uv.Add(uv01);
            uv.Add(uv11);
            uv.Add(uv10); //eEd
            uv.Add(uv00);
            uv.Add(uv11);
        }

        // step 2. create mesh gameobject
        GameObject gameobject = create_GameObject(name);
        set_DataToMesh(gameobject, vertices, triangles, uv, material);

        return gameobject;
    }

    #region --- Helper Functions ---
    private static GameObject create_GameObject(string name)
    {
        //Purpose: create an empty gameobject with "name"
        GameObject gameobject = GameObject.Find(name);
        if (gameobject == null)
        {
            gameobject = new GameObject(name);
        }
        return gameobject;
    }
    private static MeshRenderer create_MeshRenderer(GameObject gobj, Material material)
    {
        //Purpose: create meshrenderer on gameobject
        MeshRenderer meshrenderer = gobj.GetComponent<MeshRenderer>();
        if (meshrenderer == null)
        {
            meshrenderer = gobj.AddComponent<MeshRenderer>();
        }
        meshrenderer.material = material;
        return meshrenderer;
    }
    private static MeshFilter create_MeshFilter(GameObject gobj)
    {
        //Purpose: create meshfilter on gameobject
        MeshFilter fltr = gobj.GetComponent<MeshFilter>();
        if (fltr == null)
        {
            fltr = gobj.AddComponent<MeshFilter>();
        }
        return fltr;
    }
    private static Mesh create_Mesh(MeshFilter meshfilter)
    {
        //Purpose: create mesh object on meshfilter
        Mesh mesh = null;
        if (Application.isEditor == true)
        {
            mesh = meshfilter.sharedMesh;
            if (mesh == null)
            {
                meshfilter.sharedMesh = new Mesh();
                mesh = meshfilter.sharedMesh;
            }
        }
        else
        {
            mesh = meshfilter.mesh;
            if (mesh == null)
            {
                meshfilter.mesh = new Mesh();
                mesh = meshfilter.mesh;
            }
        }
        return mesh;
    }
    private static MeshCollider create_Collider(GameObject gobj)
    {
        //Purpose: create meshcollider on gameobject 
        MeshCollider coll = gobj.GetComponent<MeshCollider>();
        if (coll == null)
        {
            coll = gobj.AddComponent<MeshCollider>();
            coll.convex = false;
        }
        return coll;
    }
    private static void set_DataToMesh(GameObject gameobject, List<Vector3> vertices, List<int> triangles, List<Vector2> uv, Material material)
    {
        create_MeshRenderer(gameobject, material);

        MeshFilter meshfilter = create_MeshFilter(gameobject);

        Mesh mesh = create_Mesh(meshfilter);
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uv.ToArray();

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        create_Collider(gameobject);
    }
    #endregion
}
