using UnityEngine;

public class HelperPointset 
{
    // the point set
    /*
                    c--d                         g--h     *referencing next (use lowercase letters)
                    |  |                         |  |
                 a--b  e------------ ------------f  i--j

          spline0                      spline1
           C--D                         G--H              *referencing current point set
           |  |                         |  |
        A--B  E------------ ------------F  I--J
          barrier                      barrier
           left                         right
    */
    public int index;
    public Vector3 A; //pointset at evaluation
    public Vector3 B;
    public Vector3 C;
    public Vector3 D;
    public Vector3 E;
    public Vector3 F;
    public Vector3 G;
    public Vector3 H;
    public Vector3 I;
    public Vector3 J;
    public Vector3 triangleUpVector = Vector3.zero; // the banked up vector based on bank of roadsurface (need current and next pointset to calculate)

    public Vector3 a
    {
        get { return A; }
    }
    public Vector3 b
    {
        get { return B; }
    }
    public Vector3 c
    {
        get { return C; }
    }
    public Vector3 d
    {
        get { return D; }
    }
    public Vector3 e
    {
        get { return E; }
    }
    public Vector3 f
    {
        get { return F; }
    }
    public Vector3 g
    {
        get { return G; }
    }
    public Vector3 h
    {
        get { return H; }
    }
    public Vector3 i
    {
        get { return I; }
    }
    public Vector3 j
    {
        get { return J; }
    }

    public HelperPointset(int _index, Vector3 _A, Vector3 _B, Vector3 _E, Vector3 _F, Vector3 _I, Vector3 _J)
    {
        index = _index;
        A = _A;
        B = _B;
        E = _E;
        F = _F;
        I = _I;
        J = _J;
    }
    public static Vector3 CalcPointDistanceFromA(Vector3 A, Vector3 J, float distance)
    {
        Vector3 direction = (J - A).normalized;
        Vector3 point = A + (direction * distance);
        return point;
    }
    public static Vector3 CalcPointDistanceFromJ(Vector3 A, Vector3 J, float distance)
    {
        Vector3 direction = (J - A).normalized;
        Vector3 point = J - (direction * distance);
        return point;
    }
    public static Vector3 UpVectorOfTriangle(Vector3 tri_A, Vector3 tri_J, Vector3 tri_a)
    {
        // A-----J      need current and previous pointset, thats why second loop
        // |  /           clockwise triangle: aAJ
        // a

        Vector3 edge1 = tri_a - tri_A;
        Vector3 edge2 = tri_A - tri_J;
        Vector3 triangleUpVector = Vector3.Cross(edge1, edge2).normalized;

        return triangleUpVector;
    }
    public override string ToString()
    {
        return string.Format("index:{0}, A:{1}, J:{2}, triangleUpVector:{3}", index, A, J, triangleUpVector);
    }
}
