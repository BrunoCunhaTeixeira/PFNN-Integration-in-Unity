using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GamepadController;

/*
 * Serves as interface, since the values are determined by WaypointManager and should only be forwarded to BioAnimation_Original
 */
public class WaypointController : Controller
{
    private Vector3 moveValue = Vector3.zero;
    private float wayPointBias = 0;
    private bool crouch;

    //returns the direction value
    public override Vector3 QueryMove()
    {
        return moveValue;
    }

    //returns the Bias (velocity)
    public float GetBias()
    {
        return WayPointBias;
    }

    /*
     * Sets the value of the Styles
     * If you want to change the basic speed of an style, change it here
     */
    public void InitStyleArray()
    {
        base.Styles = new WaypointStyle[6];
        base.Styles[0] = new WaypointStyle("Stand", 1f);
        base.Styles[1] = new WaypointStyle("Walk", 1f);
        base.Styles[2] = new WaypointStyle("Jog", 3.5f);
        base.Styles[3] = new WaypointStyle("Crouch", 2f);
        base.Styles[4] = new WaypointStyle("Jump", 1f);
        base.Styles[5] = new WaypointStyle("Bump", 1f);
    }


    /*
     *  Activate the style at param index
     */
    public void SetStyleActive(int index)
    {
        for (int i = 0; i < base.Styles.Length; i++)
        {
            if (i == index && i != 3)
            {
                ((WaypointStyle)base.Styles[i]).isActive = true;

                WayPointBias = base.Styles[i].Bias;
            }
            else if (i != 3)
            {
                ((WaypointStyle)base.Styles[i]).isActive = false;
            }
            else
            {
                ((WaypointStyle)base.Styles[i]).isActive = Crouch;
            }
        }
    }

    /*
     * Overrides the class style
     */
    public class WaypointStyle : Style
    {
        public bool isActive = false;

        public WaypointStyle(string name, float bias)
        {
            base.Name = name;
            base.Bias = bias;
        }
        public override bool Query() //activation of the new style
        {
            return isActive;
        }
    }
    //Getters and setters
    public Vector3 MoveValue { get => moveValue; set => moveValue = value; }
    public bool Crouch { get => crouch; set => crouch = value; }
    public float WayPointBias { get => wayPointBias; set => wayPointBias = value; }
}
