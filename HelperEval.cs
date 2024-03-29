using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class HelperEval
{
    //Purpose:
    //  evaluate positions on spline0, spline1 at different t (store in list for quick retrieval)

    public int index;
    public float t; //percent along splines for evaluation
    public Vector3[] pos = new Vector3[2]; //each eval has a point on spline0, and spline1
   
    public HelperEval(int _index, float _t, Vector3 _pos0, Vector3 _pos1)
    {
        index = _index;        
        t = _t;
        pos[0] = _pos0;
        pos[1] = _pos1;
    }
    public HelperEval(int _index, float _t, float3 _pos0, float3 _pos1)
    {
        index = _index;
        t = _t;
        pos[0] = Convert(_pos0);
        pos[1] = Convert(_pos1);
    }
    public static Vector3 Convert(float3 itm)
    {
        return new Vector3(itm.x, itm.y, itm.z);
    }
    public static int Closest(Vector3 pos, int spline, ref List<HelperEval> Evals)
    {
        //Purpose: return the e index, of the closest eval
        float mindistance = float.MaxValue;
        int e = -1;
        foreach (HelperEval eval in Evals)
        {
            float distance = Vector3.Distance(pos, eval.pos[spline]);
            if (distance < mindistance)
            {
                mindistance = distance;
                e = eval.index;
            }
        }
        return e;
    }
    public override string ToString()
    {
        return string.Format("index:{0}, t:{1:F2}, A:{2}, B:{3}", index, t, pos[0], pos[1]);
    }
}
