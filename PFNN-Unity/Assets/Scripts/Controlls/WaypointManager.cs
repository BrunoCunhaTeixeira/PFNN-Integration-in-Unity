using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/*
 * Determines the style and direction of the character based on the waypoints and passes the values to the WaypointController
 */
public class WaypointManager : MonoBehaviour
{
    public GameObject character;

    public WaypointController wpController;
    private List<GameObject> waypoints;
    private int waypointCounter;

    private bool onBreak = false;
    private bool isWalking = true;
    private bool isRunning = false;
    private bool isCrouching = false;
    
    // Start is called before the first frame update
    void Start()
    {
        InitWaypoints();
        waypointCounter = 0;
        isWalking = waypoints[waypointCounter].GetComponent<Waypoint>().Walk;
        isRunning = waypoints[waypointCounter].GetComponent<Waypoint>().Run;
        isCrouching = waypoints[waypointCounter].GetComponent<Waypoint>().Crouch;
    }

    // Update is called once per frame
    void Update()
    {
        if (waypointCounter!=-1 &&!onBreak)
        {

            if (Vector3.Distance(character.transform.position, waypoints[waypointCounter].transform.position) > 0.6) //if character is not close enough to the point
            {

                wpController.MoveValue = new Vector3(0, 0, 1); //move forward

                //determines the style based on the properties
                if(isWalking)
                {
                    wpController.SetStyleActive(1);
                }
                else if(isRunning)
                {
                    wpController.SetStyleActive(2);
                }

                wpController.Crouch = isCrouching;
                if (isCrouching)
                {
                    wpController.SetStyleActive(3);
                }

                wpController.WayPointBias = 1;
            }
            else
            {
                SelectNextWaypoint();
            }

        }
        else
        {
            wpController.MoveValue = new Vector3(0, 0, 0);
            wpController.SetStyleActive(0);
            wpController.WayPointBias = 0;
        }
    }

    /*
     * Sets the new waypoint and reads the properties of the waypoint, determines the style and break
     */
    private void SelectNextWaypoint()
    {
        if (waypointCounter < waypoints.Count-1)
        {
            Debug.Log("increase");
            waypointCounter++;

            isWalking = waypoints[waypointCounter].GetComponent<Waypoint>().Walk;
            isRunning = waypoints[waypointCounter].GetComponent<Waypoint>().Run;
            isCrouching = waypoints[waypointCounter].GetComponent<Waypoint>().Crouch;

            if (waypoints[waypointCounter].GetComponent<Waypoint>().BreakTime !=0)
            {
                Debug.Log("Break ");
                onBreak= true;
                StartCoroutine(StartBreak(waypoints[waypointCounter].GetComponent<Waypoint>().BreakTime));
            }
        }
        else
        {
            waypointCounter = -1;
        }

        Debug.Log("WaypointCounter: " + waypointCounter);
    }

    IEnumerator StartBreak(float breakTime) 
    {
    
        yield return new WaitForSeconds(breakTime);
        onBreak = false;

    }

    /*
     * Saves the childen of the waypointmanager GO as waypoints
     */
    private void InitWaypoints()
    {
        waypoints = new List<GameObject>();

        for (int i = 0; i < transform.childCount; i++)
        {
            waypoints.Add(transform.GetChild(i).gameObject);
        }

    }

    //returns the coords of the next Waypoint for BioAnimation_Original
    public Vector3 GetNextWaypointPos()
    {
        if (waypointCounter != -1)
        {
            return waypoints[waypointCounter].transform.position;
        }
        return waypoints[waypoints.Count - 1].transform.position;
    }

    public void SetWPController(Controller controller)
    {
        this.wpController = (WaypointController)controller;
    }
}
