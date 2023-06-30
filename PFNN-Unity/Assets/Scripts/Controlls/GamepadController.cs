using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Implementation of the controller class for the gamepad,
 * because the analog sticks of the gamepad cannot be represented with KeyCodes
 */
public class GamepadController : Controller
{
    private float gamepadBias = 0f;
    private bool crouch = false;

    /*
     * Determines the direction using the LB and RB keys
     * Angle of direction is tighter than at @QueryMove
     */
    public override float QueryTurn()
    {
        float turn = 0f;

        if (InputHandler.GetKey(KeyCode.JoystickButton4))
        {
            turn -= 2f;
            if (((GamepadStyle)base.Styles[0]).isActive)
            {
                SetStyleActive(1);
            }
        }
        if (InputHandler.GetKey(KeyCode.JoystickButton5))
        {
            turn += 2f;
            if (((GamepadStyle)base.Styles[0]).isActive)
            {
                SetStyleActive(1);
            }
        }
        
        return turn;
    }

    /*
     * Determines the direction and speed, based on the input of the gamepad
     */
    public override Vector3 QueryMove()
    {
        crouch = InputHandler.GetKey(KeyCode.JoystickButton0);
        Vector3 move = Vector3.zero;
        move.x = Input.GetAxis("HorizontalLeftStick");
        move.z = Input.GetAxis("VerticalLeftStick");

        if (move.x == 0f & move.z == 0) //stand
        {
            SetStyleActive(0);

            if(InputHandler.GetKey(KeyCode.JoystickButton4) || InputHandler.GetKey(KeyCode.JoystickButton5)) //case turn around while standing
            {
                SetStyleActive(1);
                move.z = 1f;
                return move;
            }
        }
        else if (move.x <= 0.5 && move.x >= -0.5 && move.y <= 0.5 && move.y >= -0.5)
        {
            if (Input.GetAxis("RightTrigger")<0.1)
            {
                SetStyleActive(1);//walk
                //float max = Mathf.Max(Mathf.Abs(move.x),Mathf.Abs(move.z)); 
                //bias = Mathf.Lerp(0f, base.Styles[1].Bias,max); // bugged, if bias is under 1 and style is set to "walk" 
                //--> possible fix: bias = Mathf.Lerp(1f, base.Styles[1]+1.Bias,max); //caution: walking speed is doubled with this fix
            }
            else
            {
                SetStyleActive(2);//run
                gamepadBias = Mathf.Lerp(base.Styles[1].Bias, base.Styles[2].Bias, Input.GetAxis("RightTrigger"));
                //Debug.Log("bias" + bias);
            }
           
        }

        //Debug.Log(move);
        return move;
    }

    /*
     *  Returns the Bias for the velocity. If the gamepad is connected this function will be called by BioAnimation_Original
     */
    public float GetBias()
    {
        return gamepadBias;
    }

    /*
     * Sets the value of the Styles
     * If you want to change the basic speed of an style, change it here
     */
    public void InitStyleArray()
    {
        base.Styles = new GamepadStyle[6];
        base.Styles[0] = new GamepadStyle("Stand",1f); // oder 0.99999
        base.Styles[1] = new GamepadStyle("Walk", 1f);
        base.Styles[2] = new GamepadStyle("Jog", 3.5f);
        base.Styles[3] = new GamepadStyle("Crouch", 2f);
        base.Styles[4] = new GamepadStyle("Jump",1f);
        base.Styles[5] = new GamepadStyle("Bump",1f);
    }

    /*
     *  Activate the style at param index
     */
    private void SetStyleActive(int index)
    {
        for(int i = 0;i<base.Styles.Length;i++)
        {
            if (i == index&&i!=3)
            {
                ((GamepadStyle)base.Styles[i]).isActive = true;

                gamepadBias = base.Styles[i].Bias;
            }
            else if(i!=3)
            {
                ((GamepadStyle)base.Styles[i]).isActive = false;
            }
            else
            {
                
                ((GamepadStyle)base.Styles[i]).isActive = crouch;
            }
        }
    }

    /*
     * GamepadStyle inherits from Style and overrides all funtions called in BioAnimation_Original.
     */
    public class GamepadStyle : Style
    {
        public bool isActive = false;

        public GamepadStyle(string name,float bias)
        {
            base.Name = name;
            base.Bias= bias;
        }
        public override bool Query()
        {
            return isActive;
        }
    }
}
