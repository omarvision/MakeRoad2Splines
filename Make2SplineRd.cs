using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class Make2SplineRd : MonoBehaviour
{
    public SplineContainer splinecontainer = null;
    public Terrain terrain = null;
    public float precision = 0.01f;
    public bool debug_eval = true;
    public bool debug_knot = true;
    public bool debug_pointset = true;
    public bool debug_road = true;
    public bool debug_barrier_left = true;
    public bool debug_barrier_right = true;
    public float barrier_height = 2.0f;
    public float[] distance_offset = { 2.00f, 2.50f };
    public Material material_road = null;
    public Material material_barrier_left = null;
    public Material material_barrier_right = null;

    private List<HelperEval> Evals = new List<HelperEval>();
    private List<HelperKnots> Knots = new List<HelperKnots>();
    private List<HelperPointset> Points = new List<HelperPointset>();
    private GameObject Road = null;
    private GameObject BarrierLeft = null;
    private GameObject BarrierRight = null;

    private void Start()
    {
        //recall the terrain so dont have to keep resetting it as we code
        HelperTerrain.SaveHeightmapToFile("heightmap.dat", terrain);
        HelperTerrain.SaveAlphamapToFile("alphamap.dat", terrain);

        CalcArraysOffSplines();

        //make meshes from the pointsets
        Road = HelperMesh.MakeMesh_Road(ref Points, "Mesh_Road", material_road);
        BarrierLeft = HelperMesh.MakeMesh_BarrierLeft(ref Points, "Mesh_BarrierLeft", material_barrier_left);
        BarrierRight = HelperMesh.MakeMesh_BarrierRight(ref Points, "Mesh_BarrierRight", material_barrier_right);

        HelperTerrain.Modify_TerrainHeight_to_GameObject("Mesh_Road", terrain);
        HelperTerrain.Modify_TerrainAlphamap_to_GameObject("Mesh_Road", HelperTerrain.layer_asphalt, terrain);
    }
    private void OnApplicationQuit()
    {
        HelperTerrain.LoadHeightmapToFile("heightmap.dat", terrain);
        HelperTerrain.LoadAlphamapToFile("alphamap.dat", terrain);
    }
    public void CalcArraysOffSplines()
    {
        GetEvals(); //positions along spline0 & spline1
        GetKnots(); //the knots of spline0 & spline1 (and the closest eval)
        GetPointsets(); //go knot by knot and add define sets of points for making meshes we need
    }
    private void GetEvals()
    {
        //Purpose: evaluate some positions along the splines at t precision intervals

        float3[] position = new float3[2];
        float3[] tangent = new float3[2];
        float3[] upvector = new float3[2];

        Evals.Clear();
        int index = 0;
        for (float t = 0.0f; t < 1.0f; t += precision)
        {
            splinecontainer.Evaluate(0, t, out position[0], out tangent[0], out upvector[0]);
            splinecontainer.Evaluate(1, t, out position[1], out tangent[1], out upvector[1]);

            Evals.Add(new HelperEval(index++, t, position[0], position[1]));
        }
    }
    private void GetKnots()
    {
        //Purpose: each knot is closest to an eval point along it's spline

        List<BezierKnot> knot0 = splinecontainer.Splines[0].Knots.ToList();
        List<BezierKnot> knot1 = splinecontainer.Splines[1].Knots.ToList();

        if (knot0.Count != knot1.Count)
            Debug.LogError("GetKnots_byEval() Error: The splines have different knot counts. They should be the same.");

        Knots.Clear();
        int index = 0;
        int count = knot0.Count;
        for (int k = 0; k < count; k++)
        {
            //actual knot position in worldspace
            Vector3 pos0 = splinecontainer.transform.position + HelperEval.Convert(knot0[k].Position);
            Vector3 pos1 = splinecontainer.transform.position + HelperEval.Convert(knot1[k].Position);

            int closest_e0 = HelperEval.Closest(pos0, 0, ref Evals); //the closest eval for knot on spline0
            int closest_e1 = HelperEval.Closest(pos1, 1, ref Evals); //...              for knot on spline1

            //each spline knot's closest eval index
            Knots.Add(new HelperKnots(index, closest_e0, closest_e1, knot0[k], knot1[k]));
        }
    }
    private void GetPointsets()
    {
        //Purpose: get sets of points (pointset) so we can connect the space between the two splines 0, 1
        //         and make mesh shapes for the road. there is:
        //         1. road mesh
        //         2. barrier left mesh     * barriers are seperate meshes for Ai cars to detect and learn how to drive
        //         3. barrier right mesh

        /*      [spline0]   [spline1]
                  |            |               * direction of road travel is from low to high knots
                  |            |               * we will keep pointsets between knots to avoid swirly twisting point connections
                toknot0     toknot1  [1]       * spline 0 will have amount of evals it increments (float)
                   \            \                spline 1 will have it's own
                    \            \             
                    |            |             
                 fromknot0     fromknot1  [0]
                    |            |
        */
        Points.Clear();
        for (int k = 0; k < Knots.Count; k++)
        {
            //from - to knot indexes?
            int k_from;
            int k_to;
            if (k + 1 < Knots.Count)
            {
                k_from = k;
                k_to = k + 1;
            }
            else
            {
                k_from = k;
                k_to = 0;
            }

            //which spline has more eval points between the knots?
            int spline0_from_e = Knots[k_from].e[0];
            int spline0_to_e = Knots[k_to].e[0];
            int spline1_from_e = Knots[k_from].e[1];
            int spline1_to_e = Knots[k_to].e[1];
            float HowManyEvalsSpline0 = Mathf.Abs(Evals[spline0_from_e].index - Evals[spline0_to_e].index);
            float HowManyEvalsSpline1 = Mathf.Abs(Evals[spline1_from_e].index - Evals[spline1_to_e].index);
            //use that to determine the large increment (the one that increments by more than 1.0f)
            float largeInc;
            if (HowManyEvalsSpline0 > HowManyEvalsSpline1)
            {
                largeInc = HowManyEvalsSpline0 / HowManyEvalsSpline1;
                AddSegmentPointsets(k_from, k_to, largeInc, 1.0f);  //add pointsets for the space between the from-to knots
            }
            else
            {
                largeInc = HowManyEvalsSpline1 / HowManyEvalsSpline0;
                AddSegmentPointsets(k_from, k_to, 1.0f, largeInc);
            }
        }
    }
    private void AddSegmentPointsets(int k_from, int k_to, float inc0, float inc1)
    {
        /*
                        c--d                         g--h     *next (lowercase)
                        |  |                         |  |
                     a--b  e------------ ------------f  i--j

              spline0                      spline1
               C--D                         G--H              *current pointset (capital letters)
               |  |                         |  |
            A--B  E------------ ------------F  I--J
              barrier                     barrier
               left                        right
        */

        // step 1. loop for baseline points  A B E F I J
        // --------------------------------       
        int spline0_last_e = (k_to > 0) ? Knots[k_to].e[0] : Evals[Evals.Count - 1].index; //loop ending 'e'
        int spline1_last_e = (k_to > 0) ? Knots[k_to].e[1] : Evals[Evals.Count - 1].index;
        float spline0_current_e = (float)Knots[k_from].e[0]; //loop starting 'e'
        float spline1_current_e = (float)Knots[k_from].e[1];
        int index = 0;
        while (spline0_current_e <= spline0_last_e && spline1_current_e <= spline1_last_e)
        {
            Vector3 A = Evals[(int)spline0_current_e].pos[0];
            Vector3 J = Evals[(int)spline1_current_e].pos[1];
            Vector3 B = HelperPointset.CalcPointDistanceFromA(A, J, distance_offset[0]);
            Vector3 E = HelperPointset.CalcPointDistanceFromA(A, J, distance_offset[1]);
            Vector3 F = HelperPointset.CalcPointDistanceFromJ(A, J, distance_offset[1]);
            Vector3 I = HelperPointset.CalcPointDistanceFromJ(A, J, distance_offset[0]);

            Points.Add(new HelperPointset(index++, A, B, E, F, I, J)); //we will set C D G H in second loop. need triangleUpVector for those

            spline0_current_e += inc0;
            spline1_current_e += inc1;
        }

        // step 2. loop again for barrier up points  C D G H
        // ----------------------------------------
        Vector3 tri_A;  // A-----J      need current and previous pointset, thats why second loop
        Vector3 tri_J;  // |  /
        Vector3 tri_a;  // a
        for (int p = 0; p < Points.Count; p++)
        {
            if (p == 0)
            {
                tri_A = Points[p].A;
                tri_J = Points[p].J;
                tri_a = Points[Points.Count - 1].a;
            }
            else
            {
                tri_A = Points[p].A;
                tri_J = Points[p].J;
                tri_a = Points[p - 1].a;
            }
            Points[p].triangleUpVector = HelperPointset.UpVectorOfTriangle(tri_A, tri_J, tri_a);

            Points[p].C = Points[p].B + (Points[p].triangleUpVector * barrier_height); //C comes up from B
            Points[p].D = Points[p].E + (Points[p].triangleUpVector * barrier_height); //D up from E
            Points[p].G = Points[p].F + (Points[p].triangleUpVector * barrier_height); //G up from F
            Points[p].H = Points[p].I + (Points[p].triangleUpVector * barrier_height); //H up from I
        }
    }

    private void OnDrawGizmos()
    {
        if (Evals.Count > 0)
        {
            if (debug_eval == true)
            {
                Gizmos.color = Color.green;
                for (int e = 0; e < Evals.Count; e++)
                {
                    Gizmos.DrawWireCube(Evals[e].pos[0], new Vector3(0.5f, 4.0f, 0.5f));
                    Gizmos.DrawWireCube(Evals[e].pos[1], new Vector3(0.5f, 4.0f, 0.5f));
                }
            }
        }

        if (Knots.Count > 0)
        {
            if (debug_knot == true)
            {
                List<BezierKnot> knot0 = splinecontainer.Splines[0].Knots.ToList();
                List<BezierKnot> knot1 = splinecontainer.Splines[1].Knots.ToList();

                Gizmos.color = Color.white;
                for (int k = 0; k < Knots.Count; k++)
                {
                    Vector3 pos0 = splinecontainer.transform.position + HelperEval.Convert(knot0[k].Position);
                    Vector3 pos1 = splinecontainer.transform.position + HelperEval.Convert(knot1[k].Position);
                    Gizmos.DrawWireCube(pos0, new Vector3(2, 4, 2));
                    Gizmos.DrawWireCube(pos1, new Vector3(2, 4, 2));
                }
            }
        }

        if (Points.Count > 0)
        {
            for (int p = 0; p < Points.Count; p++)
            {
                /*     C--D                         G--H  
                       |  |                         |  |
                    A--B  E------------ ------------F  I--J
                */
                if (debug_road == true)
                {
                    Gizmos.color = Color.cyan;
                    if (p < Points.Count - 1)
                    {
                        Gizmos.DrawLine(Points[p].A, Points[p + 1].A);      // a---j
                        Gizmos.DrawLine(Points[p + 1].A, Points[p + 1].J);  // | / |
                        Gizmos.DrawLine(Points[p + 1].J, Points[p].J);      // A---J
                        Gizmos.DrawLine(Points[p].J, Points[p].A);
                        Gizmos.DrawLine(Points[p].A, Points[p + 1].J);
                    }
                }
                if (debug_barrier_left == true)
                {
                    Gizmos.color = Color.cyan;
                    int x; //ne'X't pointset    //  C---D
                    if (p < Points.Count - 1)   //  |   |  inside
                        x = p + 1;              // -B   E-
                    else
                        x = 0;
                    Gizmos.DrawLine(Points[p].E, Points[p].D);  // D---d  inside
                    Gizmos.DrawLine(Points[p].D, Points[x].d);  // | / |
                    Gizmos.DrawLine(Points[x].d, Points[x].e);  // E---e
                    Gizmos.DrawLine(Points[x].e, Points[p].E);
                    Gizmos.DrawLine(Points[p].E, Points[x].d);

                    Gizmos.DrawLine(Points[p].C, Points[x].c);  // c---d  top
                    Gizmos.DrawLine(Points[x].c, Points[x].d);  // | / |
                    Gizmos.DrawLine(Points[x].d, Points[p].D);  // C---D
                    Gizmos.DrawLine(Points[p].D, Points[p].C);
                    Gizmos.DrawLine(Points[p].C, Points[x].d);

                    Gizmos.DrawLine(Points[x].b, Points[x].c);  // c---C  outside
                    Gizmos.DrawLine(Points[x].c, Points[p].C);  // | / |
                    Gizmos.DrawLine(Points[p].C, Points[p].B);  // b---B
                    Gizmos.DrawLine(Points[p].B, Points[x].b);
                    Gizmos.DrawLine(Points[x].b, Points[p].C);
                }
                if (debug_barrier_right == true)
                {
                    Gizmos.color = Color.cyan;
                    int x; //ne'X't pointset    //  G---H
                    if (p < Points.Count - 1)   //  |   |
                        x = p + 1;              // -F   J-
                    else
                        x = 0;
                    Gizmos.DrawLine(Points[x].f, Points[x].g);  // g---G  inside
                    Gizmos.DrawLine(Points[x].g, Points[p].G);  // | / |
                    Gizmos.DrawLine(Points[p].G, Points[p].F);  // f---F
                    Gizmos.DrawLine(Points[p].F, Points[x].f);
                    Gizmos.DrawLine(Points[x].f, Points[p].G);

                    Gizmos.DrawLine(Points[p].G, Points[x].g);  // g---h  top
                    Gizmos.DrawLine(Points[x].g, Points[x].h);  // | / |
                    Gizmos.DrawLine(Points[x].h, Points[p].H);  // G---H
                    Gizmos.DrawLine(Points[p].H, Points[p].G);
                    Gizmos.DrawLine(Points[p].G, Points[x].h);

                    Gizmos.DrawLine(Points[x].i, Points[p].H);  // H---h  outside
                    Gizmos.DrawLine(Points[p].H, Points[x].h);  // | / |
                    Gizmos.DrawLine(Points[x].h, Points[x].i);  // I---i
                    Gizmos.DrawLine(Points[x].i, Points[p].I);
                    Gizmos.DrawLine(Points[p].I, Points[x].h);
                }

                if (debug_pointset == true)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(Points[p].A, Points[p].B);
                    Gizmos.DrawLine(Points[p].B, Points[p].C);
                    Gizmos.DrawLine(Points[p].C, Points[p].D);
                    Gizmos.DrawLine(Points[p].D, Points[p].E);
                    Gizmos.DrawLine(Points[p].E, Points[p].F);
                    Gizmos.DrawLine(Points[p].F, Points[p].G);
                    Gizmos.DrawLine(Points[p].G, Points[p].H);
                    Gizmos.DrawLine(Points[p].H, Points[p].I);
                    Gizmos.DrawLine(Points[p].I, Points[p].J);
                }
            }            
        }
    }
}
