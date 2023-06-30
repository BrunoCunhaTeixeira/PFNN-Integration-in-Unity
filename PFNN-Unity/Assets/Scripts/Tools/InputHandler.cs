using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Collects the keyboard inputs and makes them available to the controller
 */
public class InputHandler : MonoBehaviour
{

    private static List<HashSet<KeyCode>> Keys = new List<HashSet<KeyCode>>();
    private static int Capacity = 2;
    private static int Clients = 0;


    void OnEnable()
    {
        Clients += 1;
    }

    void OnDisable()
    {
        Clients -= 1;
    }

    public static bool anyKey
    {
        get
        {
            for (int i = 0; i < Keys.Count; i++)
            {
                if (Keys[i].Count > 0)
                {
                    return true;
                }
            }
            return false;
        }
    }

    void Update()
    {
        while (Keys.Count >= Capacity)
        {
            Keys.RemoveAt(0);
        }
        HashSet<KeyCode> state = new HashSet<KeyCode>();
        foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
        {
            if (k != KeyCode.Alpha1 && k != KeyCode.Alpha2 && Input.GetKey(k))// Key "1" and "2" are blocked because they are used to controll the camera
            {
                //Debug.Log("key added: " + k);
                state.Add(k);
            }
        }

        Keys.Add(state);
    }

    public static bool GetKey(KeyCode k)
    {
        if (Clients == 0)
        {
            return Input.GetKey(k);
        }
        else
        {
            for (int i = 0; i < Keys.Count; i++)
            {
                if (Keys[i].Contains(k))
                {
                    return true;
                }
            }
            return false;
        }
    }

}
