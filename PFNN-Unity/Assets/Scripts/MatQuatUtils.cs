using UnityEngine;

/*
 * Contains functions for Matrix4x4 and Quaternion 
 */
public static class MatQuatUtils 
{
    private static readonly float eps = 1e-8f; //smallest representable floating-point number
    /*
     * Converts a Exponential Map to a Quaternion
     * 
     * C# Version of the QuatExp method form the original PFNN (Line 1016 in pfnn.cpp)
     * + inspired by https://theorangeduck.com/page/exponential-map-angle-axis-angular-velocity
     */
    public static Quaternion ExpToQuat(Vector3 vec)
    {
        float w = Vector3.Magnitude(vec);

        Quaternion response = (w < eps)? Quaternion.identity : new Quaternion(
                (vec.x * (Mathf.Sin(w) / w)),
                (vec.y * (Mathf.Sin(w) / w)),
                (vec.z * (Mathf.Sin(w) / w)),
                Mathf.Cos(w));

        float magnitude = Mathf.Sqrt(response.w * response.w + response.x * response.x + response.y * response.y + response.z * response.z);

        response.x /= magnitude;
        response.y /= magnitude;
        response.z /= magnitude;
        response.w /= magnitude;
        
        return response;
    }

    //return Quaternion.Euler(response.eulerAngles.x,response.eulerAngles.y,response.eulerAngles.z);
    public static Matrix4x4 QuatToMatri4x4(Quaternion q, Vector3 pos)
    {
        Matrix4x4 response;

        float qw = q.w;
        float qx = q.x;
        float qy = q.y;
        float qz = q.z;

        //Quaternion to Mat3x3 -->inspired by https://automaticaddison.com/how-to-convert-a-quaternion-to-a-rotation-matrix/

        //first row
        float r00 = 2 * (qw * qw+ qx * qx) - 1;
        float r01 = 2 * (qx * qy - qw * qz);
        float r02 = 2 * (qx * qz + qw * qy);

        //second row
        float r10 = 2 * (qx * qy + qw * qz);
        float r11 = 2 * (qw * qw + qy * qy) - 1;
        float r12 = 2 * (qy * qz - qw * qx);

        //third row
        float r20 = 2 * (qx * qz - qw * qy);
        float r21 = 2 * (qy * qz + qw * qx);
        float r22 = 2 * (qw * qw + qz * qz) - 1;

        // Unity´s 4x4Matrix order is colmun-major, Holden et al. original PFNN works with 4x4Matrix (GLM) which is in row-major order
        // to prevent calculation errors the Matrix is filled like this
        Vector4 r0 = new Vector4(r00, r10, r20, 0);
        Vector4 r1 = new Vector4(r01, r11, r21, 0);
        Vector4 r2 = new Vector4(r02, r12, r22, 0);
        Vector4 r3 = new Vector4(pos.x, pos.y, pos.z, 1f); //position
       
        response = new Matrix4x4(r0, r1, r2, r3);

        return response;
    }

    /*
     *  Extracts the Position from the @matrix
     */
    public static Vector3 ExtractPosition(Matrix4x4 matrix)
    {
        Vector3 position;
        position.x = matrix.m30;
        position.y = matrix.m13;
        position.z = matrix.m32;
        return position;
    }

    /*
     *  Extracts the Scale from the @matrix
     */
    public static Vector3 ExtractScale(Matrix4x4 matrix)
    {
        Vector3 scale;
        scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
        scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
        scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
        return scale;
    }
}
