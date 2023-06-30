using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * This class is attached to each waypoint. Saves the style and the break of the Waypoint
 */
public class Waypoint : MonoBehaviour
{
    [SerializeField]
    private float breakTime =0f;
    [SerializeField]
    private bool walk = true;
    [SerializeField]
    private bool run = false;
    [SerializeField]
    private bool crouch = false;

    //Getters and setters
    public float BreakTime { get => breakTime; set => breakTime = value; }
    public bool Walk { get => walk; set => walk = value; }
    public bool Run { get => run; set => run = value; }
    public bool Crouch { get => crouch; set => crouch = value; }

}
