using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * This class manages the copying of the calculated rotations and assigns them to the character (Rig with mesh)
 */
public class MirrorSkeletonScript : MonoBehaviour
{
    [Tooltip("Please insert the bone character_pirate:joint_0 of the rig with the mesh ")]
    public GameObject mirrorSkeletonRoot;

    public List<GameObject> originalSkeleton = new List<GameObject>(); //Rig build by BioAnimation_Original (PFNN)
    private List<GameObject> mirrorSkeleton = new List<GameObject>(); //Rig with Mesh which is attached (Pirate, Steve ...)



    /*
     * Returns the original Rotation of the rig, called by BioAnimation_Original
     */
    public Matrix4x4[] getOriginRotation()
    {
        Matrix4x4[] response = new Matrix4x4[mirrorSkeleton.Count];
        for (int i = 0; i < mirrorSkeleton.Count; i++)
        {
            response[i] = Matrix4x4.TRS(mirrorSkeleton[i].transform.position, mirrorSkeleton[i].transform.rotation, new Vector3(1, 1, 1));
        }

        return response;
    }

    /*
     * Gets the calculated rotations from BioAnimation_Original and assigns them to the Rig
     */
    public void mirror(Matrix4x4[] mat)
    {

        for (int i = 0; i < originalSkeleton.Count; i++)
        {
            mirrorSkeleton[i].transform.position = originalSkeleton[i].transform.position;
            mirrorSkeleton[i].transform.rotation = mat[i].GetRotation();

        }
    }

    /*
     * Attempt to correct the Position of the Foots
     */
    public void CorrectFootPosition(bool leftFoot, float angle)
    {
        if(leftFoot)
        {
            mirrorSkeleton[9].transform.localRotation = Quaternion.Euler(mirrorSkeleton[9].transform.localEulerAngles.x, mirrorSkeleton[9].transform.localEulerAngles.y, angle);

        }
        else
        {
            mirrorSkeleton[4].transform.localRotation = Quaternion.Euler(mirrorSkeleton[4].transform.localEulerAngles.x, mirrorSkeleton[4].transform.localEulerAngles.y, angle);
            mirrorSkeleton[5].transform.localRotation = Quaternion.Euler(mirrorSkeleton[5].transform.localEulerAngles.x, mirrorSkeleton[5].transform.localEulerAngles.y, angle);
        }
    }

    /*
     * Is executed at the beginning by BioAnimation_Original
     */
    public void InitMirrorSkeleton()
    {
        GetAllBonesOfRig(mirrorSkeletonRoot);

    }

    /*
     * Builds the rig recursively, starts at @bone
     */
    private void GetAllBonesOfRig(GameObject bone)
    {
        mirrorSkeleton.Add(bone);
        if (bone.transform.childCount > 0)
        {
            for (int i = 0; i < bone.transform.childCount; i++)
            {
                GetAllBonesOfRig(bone.transform.GetChild(i).gameObject);
            }
        }
    }

}
