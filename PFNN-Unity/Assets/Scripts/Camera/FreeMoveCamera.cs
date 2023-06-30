using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/*
 * This camera is freely maneuverable with the mouse. It is part of the Camera rig. 
 * Inspired by https://gist.github.com/ashleydavis/f025c03a9221bc840a2b
 */
public class FreeMoveCamera : MonoBehaviour
{
    public float mouseSensitivity = 2.5f;
    public float distanceToFollow = 5.0f;

    private GameObject follow;
    private float rotY;
    private float rotX;
    private Vector3 currentRot;
    private Vector3 smoothVelocity = Vector3.zero;
    private float smoothTime = 0.2f;
    private Vector2 minMaxRot = new Vector2(-45, 45);

    private void Awake()
    {
        follow = transform.parent.GetComponent<CameraControll>().follow;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        rotY += mouseX;
        rotX += mouseY;

        //caps the rotation between the min and max 
        rotX = Mathf.Clamp(rotX, minMaxRot.x, minMaxRot.y);
        Vector3 nextRot = new Vector3(rotX, rotY);
        currentRot = Vector3.SmoothDamp(currentRot, nextRot, ref smoothVelocity, smoothTime);// smooths the rotation change
        transform.localEulerAngles = currentRot;
        transform.position = new Vector3(follow.transform.position.x, follow.transform.position.y+0.9f, follow.transform.position.z) - transform.forward * distanceToFollow;
    }
}
