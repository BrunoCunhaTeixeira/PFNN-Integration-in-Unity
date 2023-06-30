using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class GLMTESTScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        ////Quaternion q1 = QuatExp(new Vector3(0.08f, 0.18f, 0.0f));
        ////Quaternion q2 = QuatExp(new Vector3(0.2f, 0.9f, 0.3f));
        ////Matrix4x4 m1 = QuatToMatri4x4(q1);
        ////Matrix4x4 m2 = QuatToMatri4x4(q2);

        ////first row
        ////float r00 = 0;
        ////float r01 = -4;
        ////float r02 = -2;
        ////float r03 = 3;

        ////second row
        ////float r10 = 4;
        ////float r11 = 3;
        ////float r12 = 3;
        ////float r13 = 6;

        ////third row
        ////float r20 = 3;
        ////float r21 = 9;
        ////float r22 = 10;
        ////float r23 = 8;

        ////third row
        ////float r30 = 1.2f;
        ////float r31 = 1.3f;
        ////float r32 = 2.8f;
        ////float r33 = 4.3f;

        ////Vector4 r0 = new Vector4(r00, r10, r20, r30);
        ////Vector4 r1 = new Vector4(r01, r11, r21, r31);
        ////Vector4 r2 = new Vector4(r02, r12, r22, r32);
        ////Vector4 r3 = new Vector4(r03, r13, r23, r33); //position

        ////Matrix4x4 m3 = new Matrix4x4(r0, r1, r2, r3);


        ////Debug.Log(m3.inverse);
        ////Debug.Log(m3[0, 2]);
        ////Debug.Log(m2);
        ////Debug.Log(m3.transpose[0, 2]);
        ////Debug.Log(m3.transpose);
        ////Debug.Log(MajorRowMatrixMultiplication(m3, m3));
        ////Debug.Log(m3 * m3);

        ////multiplikation funktioniert
        ////inverse funktioniert
        ////transpose funktioniert
        ////-> aufpassen wie man Matrix füllt, Matrix muss andersrum gefüllt werden
        //// Ergebnisse noch in lefthand umwandeln https://stackoverflow.com/questions/28673777/convert-quaternion-from-right-handed-to-left-handed-coordinate-system
        //
        Quaternion q = new Quaternion(-0.0011831670f, -0.4474018000f, 0.5939867000f, 0.6685882000f);
        transform.rotation = Quaternion.Euler(q.eulerAngles.x, q.eulerAngles.y, q.eulerAngles.z);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.A))
        {
            transform.rotation = new Quaternion(-0.0011831670f, -0.4474018000f, 0.5939867000f, 0.6685882000f);
            Debug.Log(transform.rotation);
        }
    }

    private Quaternion QuatExp(Vector3 vec)
    {
        float w = Vector3.Magnitude(vec);

        Quaternion response = (w < 0.01f) ? Quaternion.identity : new Quaternion(
                (vec.x * (Mathf.Sin(w) / w)),
                (vec.y * (Mathf.Sin(w) / w)),
                (vec.z * (Mathf.Sin(w) / w)),
                Mathf.Cos(w));

        float magnitude = Mathf.Sqrt(response.w * response.w + response.x * response.x + response.y * response.y + response.z * response.z);

        response.x /= magnitude;
        response.y /= magnitude;
        response.z /= magnitude;
        response.w /= magnitude;

        //Debug.Log(vec + "  quat: " + response);
        return response;
    }

    private Matrix4x4 QuatToMatri4x4(Quaternion q)
    {
        Matrix4x4 response;

        float qw = q.w;
        float qx = q.x;
        float qy = q.y;
        float qz = q.z;

        //Quaternion to Mat3x3 -->inspired by https://automaticaddison.com/how-to-convert-a-quaternion-to-a-rotation-matrix/

        //first row
        float r00 = 1 - 2 * (qy*qy) - 2 * (qz*qz);
        float r01 = 2 * qx * qy - 2 * qz * qw;
        float r02 = 2 * 2 * qx * qz + 2 * qy * qw;

        //first row
        float r10 = 2 * qx * qy + 2 * qz * qw;
        float r11 = 1 - 2 * (qx*qx) - 2 * (qz*qz);
        float r12 = 2 * qy * qz - 2 * qx * qw;

        //first row
        float r20 = 2 * qx * qz - 2 * qy * qw;
        float r21 = 2 * qy * qz + 2 * qx * qw;
        float r22 = 1 - 2 * (qx*qx) - 2 * (qy*qy);

        Debug.Log("R00: " + r00 + "R01: " + r01 + "R02: " + r02);
        Debug.Log("R10: " + r10 + "R11: " + r11 + "R12: " + r12);
        Debug.Log("R20: " + r20 + "R21: " + r21 + "R22: " + r22);

        Vector4 r0 = new Vector4(r00,r10,r20,0);
        Vector4 r1 = new Vector4(r01,r11,r21,0);
        Vector4 r2 = new Vector4(r02,r12,r22,0);
        Vector4 r3 = new Vector4(1.2f, 1.3f, 1.3f, 1f); //position

        response = new Matrix4x4(r0, r1, r2,r3);
        //Debug.Log(response.transpose);
        //Debug.Log(response.inverse);
        //Debug.Log(response.ToString());
        //Debug.Log(r0);

        return response;
    }

    private Matrix4x4 MajorRowMatrixMultiplication(Matrix4x4 m1, Matrix4x4 m2)
    {
        Matrix4x4 response = new Matrix4x4();
       for(int i = 0;i<4;i++)
        {
            for(int j = 0; j < 4; j++)
            {
                response[i,j] = 0;

                for (int k = 0; k<4; k++)
                {
                    response[i,j] += m1[k, i] * m2[j,k];
                }
            }
        }

       return response;
    }
}
