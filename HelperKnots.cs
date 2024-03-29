using UnityEngine;
using UnityEngine.Splines;

public class HelperKnots
{
    public int index; //knot index
    public int[] e = new int[2]; //the closest evaluation index 'e' (per spline)
    public BezierKnot[] knot = new BezierKnot[2];

    public HelperKnots(int i, int e0, int e1, BezierKnot knot0, BezierKnot knot1)
    {
        index = i;
        e[0] = e0;
        e[1] = e1;
        knot[0] = knot0;
        knot[1] = knot1;
    }
    public override string ToString()
    {
        return string.Format("index:{0}, e[0]:{1}, e[1]:{2}", index, e[0], e[1]);
    }
}
