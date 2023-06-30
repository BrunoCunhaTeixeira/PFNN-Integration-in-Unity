using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * This class contains the different cameras (front, side, rear, free) and manages their control 
 */
public class CameraControll : MonoBehaviour
{
    public GameObject follow;
    public float yPos = 1;
    private int counter;
    // Start is called before the first frame update
    void Start()
    {
        counter = 0;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.position = new Vector3(follow.transform.position.x, follow.transform.position.y + yPos, follow.transform.position.z);


        if (Input.GetKeyDown(KeyCode.Alpha1)) //changes the camera based on the counter
        {
            transform.GetChild(counter).gameObject.SetActive(false);
            DecreaseCounter();
            transform.GetChild(counter).gameObject.SetActive(true);

        }
        if (Input.GetKeyDown(KeyCode.Alpha2)) //changes the camera based on the counter
        {

            transform.GetChild(counter).gameObject.SetActive(false);
            IncreaseCounter();
            transform.GetChild(counter).gameObject.SetActive(true);
        }


    }

    //increases the counter and reset it if needed
    private int IncreaseCounter()
    {
        if (counter == transform.childCount - 1)
        {
            counter = 0;
        }
        else
        {
            counter++;
        }
        return counter;
    }

    //decreases the counter and reset it if needed
    private int DecreaseCounter()
    {
        if (counter == 0)
        {
            counter = transform.childCount - 1;
        }
        else
        {
            counter--;
        }
        return counter;
    }
}
