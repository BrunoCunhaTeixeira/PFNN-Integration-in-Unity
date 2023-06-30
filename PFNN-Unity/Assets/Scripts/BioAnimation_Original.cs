using DeepLearning;
using UnityEngine;
using System;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;
using System.Security.Cryptography;
using Unity.VisualScripting;
#if UNITY_EDITOR
using UnityEditor;
#endif


/*
 * This class determines the action of the character. It performs the following functions:

- PFNN handling

- Manage the trajectory and the rig

- Calculates the pose of the rig (position and rotations)

- Receives the required values from other classes and processes them
 */
public class BioAnimation_Original : MonoBehaviour
{

    public bool Inspect = false;

    public float TargetBlending = 0.25f;
    public float GaitTransition = 0.25f;
    public float TrajectoryCorrection = 1f;
    public bool visualizeTrajectory;
    public bool gamepadConnected = false;
    public bool wayPointActive = false;
    public float speedMultiplier = 1f;

    public Controller Controller;

    private Actor Actor;
    private PFNN NN;
    private Trajectory Trajectory;
    private MirrorSkeletonScript mirrorSkeletonScript;
    private WaypointManager waypointManager;

    private Vector3 TempTargetDirection;
    private Vector3 TargetDirection;
    private Vector3 TargetVelocity;

    //Rescaling for character (cm to m)
    private float UnitScale = 100f;

    //State
    private Vector3[] Positions = new Vector3[0];
    private Vector3[] Forwards = new Vector3[0];
    private Vector3[] Ups = new Vector3[0];
    private Vector3[] Velocities = new Vector3[0];

    private Quaternion[] Rotations = new Quaternion[0];

    private Matrix4x4[] joint_rest_xform;
    private Matrix4x4[] joint_mesh_xform;
    private Matrix4x4[] joint_anim_xform;


    //foot correction
    private float leftFoot = 0f;
    private float rightFoot = 0f;
    

    //Trajectory for 60 Hz framerate
    private const int PointSamples = 12; //12
    private const int RootSampleIndex = 6; //6
    private const int RootPointIndex = 60;//60
    private const int FuturePoints = 5;//5
    private const int PreviousPoints = 6;//6
    private const int PointDensity = 10;//10

    private int[] joint_parents = new int[31];

    private int fpsCounter = 0;

    /*
    * 
    */
    void Reset()
    {
        Controller = new Controller();

    }

    /*
    * Initializes the required variables
    */
    void Awake()
    {
        if(gamepadConnected)
        {
            Debug.Log("Gamepad connected");
            Controller = new GamepadController();
            ((GamepadController)Controller).InitStyleArray();
        }

        if(wayPointActive)
        {
            Debug.Log("Waypoints active");
            Controller = new WaypointController();
            
            ((WaypointController)Controller).InitStyleArray();
            waypointManager = GameObject.Find("WaypointManager").GetComponent<WaypointManager>();
            waypointManager.SetWPController(Controller);


        }
        Actor = GetComponent<Actor>();
        NN = GetComponent<PFNN>();
        TargetDirection = new Vector3(transform.forward.x, 0f, transform.forward.z);
        TargetVelocity = Vector3.zero;
        Positions = new Vector3[Actor.Bones.Length];
        Forwards = new Vector3[Actor.Bones.Length];
        Ups = new Vector3[Actor.Bones.Length];
        Velocities = new Vector3[Actor.Bones.Length];

        Rotations = new Quaternion[Actor.Bones.Length];
        joint_rest_xform = new Matrix4x4[Actor.Bones.Length];
        joint_mesh_xform = new Matrix4x4[Actor.Bones.Length];
        joint_anim_xform = new Matrix4x4[Actor.Bones.Length];

        InitJointParents();
        mirrorSkeletonScript = this.GetComponent<MirrorSkeletonScript>();
        mirrorSkeletonScript.InitMirrorSkeleton();
        InitJointRestXForm(mirrorSkeletonScript.getOriginRotation());


        Trajectory = new Trajectory(111, Controller.GetNames(), transform.position, TargetDirection);
        Trajectory.Postprocess();
        if (Controller.Styles.Length > 0)
        {
            for (int i = 0; i < Trajectory.Points.Length; i++)
            {
                Trajectory.Points[i].Styles[0] = 1f;
            }
        }

        for (int i = 0; i < Actor.Bones.Length; i++)
        {
            Positions[i] = Actor.Bones[i].Transform.position;
            Forwards[i] = Actor.Bones[i].Transform.forward;
            Ups[i] = Actor.Bones[i].Transform.up;
            Velocities[i] = Vector3.zero;
        }

        if (NN.Parameters == null)
        {
            Debug.Log("No parameters saved.");
            return;
        }
        NN.LoadParameters();
    }

    /*
    * Sets FPS to 120
    */
    void Start()
    {
        Utility.SetFPS(120);
    }

    /*
    *  Everything happens here.
    */
    void Update()
    {
        fpsCounter++;
        if (NN.Parameters == null ||fpsCounter%2==0) //In order not to play the animation too fast, this function is executed only in every second frame
        {
            return;
        }


        //Update Target Direction / Velocity

        if (wayPointActive)
        {
            TargetDirection=waypointManager.GetNextWaypointPos() - Trajectory.Points[RootPointIndex].GetPosition();
            TargetVelocity = Vector3.Lerp(TargetVelocity, (Quaternion.LookRotation(TargetDirection, Vector3.up) * Controller.QueryMove()).normalized, TargetBlending);

        }else if (gamepadConnected)
        {
            TempTargetDirection = Vector3.Lerp(TempTargetDirection, Quaternion.AngleAxis(Controller.QueryTurn() * 60f, Vector3.up) * Trajectory.Points[RootPointIndex].GetDirection(), TargetBlending);
            TargetVelocity = Vector3.Lerp(TargetVelocity, (Quaternion.LookRotation(TempTargetDirection, Vector3.up) * Controller.QueryMove()).normalized, TargetBlending);
            TargetDirection = TargetVelocity;
        }
        else
        {
            TargetDirection = Vector3.Lerp(TargetDirection, Quaternion.AngleAxis(Controller.QueryTurn() * 60f, Vector3.up) * Trajectory.Points[RootPointIndex].GetDirection(), TargetBlending);
            TargetVelocity = Vector3.Lerp(TargetVelocity, (Quaternion.LookRotation(TargetDirection, Vector3.up) * Controller.QueryMove()).normalized, TargetBlending);  
        }
        
        //Update Gait
        for (int i = 0; i < Controller.Styles.Length; i++)
        {
            Trajectory.Points[RootPointIndex].Styles[i] = Utility.Interpolate(Trajectory.Points[RootPointIndex].Styles[i], Controller.Styles[i].Query() ? 1f : 0f, GaitTransition);
            //Trajectory.Points[RootPointIndex].Styles[i] = Utility.Interpolate(Trajectory.Points[RootPointIndex].Styles[i], 0, GaitTransition);

        }
        //For Human Only
        //Trajectory.Points[RootPointIndex].Styles[0] = Utility.Interpolate(Trajectory.Points[RootPointIndex].Styles[0], 1.0f - Mathf.Clamp(Vector3.Magnitude(TargetVelocity) / 0.1f, 0.0f, 1.0f), GaitTransition);
        //Trajectory.Points[RootPointIndex].Styles[1] = Mathf.Max(Trajectory.Points[RootPointIndex].Styles[1] - Trajectory.Points[RootPointIndex].Styles[2], 0f);
        //

        /*
        //Blend Trajectory Offset
        Vector3 positionOffset = transform.position - Trajectory.Points[RootPointIndex].GetPosition();
        Quaternion rotationOffset = Quaternion.Inverse(Trajectory.Points[RootPointIndex].GetRotation()) * transform.rotation;
        Trajectory.Points[RootPointIndex].SetPosition(Trajectory.Points[RootPointIndex].GetPosition() + positionOffset);
        Trajectory.Points[RootPointIndex].SetDirection(rotationOffset * Trajectory.Points[RootPointIndex].GetDirection());

        for(int i=RootPointIndex; i<Trajectory.Points.Length; i++) {
            float factor = 1f - (i - RootPointIndex)/(RootPointIndex - 1f);
            Trajectory.Points[i].SetPosition(Trajectory.Points[i].GetPosition() + factor*positionOffset);
        }
        */

        //Predict Future Trajectory
        Vector3[] trajectory_positions_blend = new Vector3[Trajectory.Points.Length];
        trajectory_positions_blend[RootPointIndex] = Trajectory.Points[RootPointIndex].GetPosition();

        for (int i = RootPointIndex + 1; i < Trajectory.Points.Length; i++)
        {
            float bias_pos = 0.75f;
            float bias_dir = 1.25f;
            float scale_pos = (1.0f - Mathf.Pow(1.0f - ((float)(i - RootPointIndex) / (RootPointIndex)), bias_pos));
            float scale_dir = (1.0f - Mathf.Pow(1.0f - ((float)(i - RootPointIndex) / (RootPointIndex)), bias_dir));
            float vel_boost = PoolBias();
            

            float rescale = 1f / (Trajectory.Points.Length - (RootPointIndex + 1f));

            trajectory_positions_blend[i] = trajectory_positions_blend[i - 1] + Vector3.Lerp(
                Trajectory.Points[i].GetPosition() - Trajectory.Points[i - 1].GetPosition(),
                vel_boost * rescale * TargetVelocity,
                scale_pos);

            Trajectory.Points[i].SetDirection(Vector3.Lerp(Trajectory.Points[i].GetDirection(), TargetDirection, scale_dir));

            for (int j = 0; j < Trajectory.Points[i].Styles.Length; j++)
            {
                Trajectory.Points[i].Styles[j] = Trajectory.Points[RootPointIndex].Styles[j];
            }
        }

        for (int i = RootPointIndex + 1; i < Trajectory.Points.Length; i++)
        {
            Trajectory.Points[i].SetPosition(trajectory_positions_blend[i]);
        }

        for (int i = RootPointIndex; i < Trajectory.Points.Length; i += PointDensity)
        {
            Trajectory.Points[i].Postprocess();
        }

        for (int i = RootPointIndex + 1; i < Trajectory.Points.Length; i++)
        {
            //ROOT	1		2		3		4		5
            //.x....x.......x.......x.......x.......x
            Trajectory.Point prev = GetPreviousSample(i);
            Trajectory.Point next = GetNextSample(i);
            float factor = (float)(i % PointDensity) / PointDensity;

            Trajectory.Points[i].SetPosition((1f - factor) * prev.GetPosition() + factor * next.GetPosition());
            Trajectory.Points[i].SetDirection((1f - factor) * prev.GetDirection() + factor * next.GetDirection());
            Trajectory.Points[i].SetLeftsample((1f - factor) * prev.GetLeftSample() + factor * next.GetLeftSample());
            Trajectory.Points[i].SetRightSample((1f - factor) * prev.GetRightSample() + factor * next.GetRightSample());
            Trajectory.Points[i].SetSlope((1f - factor) * prev.GetSlope() + factor * next.GetSlope());
        }

        //Avoid Collisions
        CollisionChecks(RootPointIndex + 1);

        if (NN.Parameters != null)
        {
            //Calculate Root
            Matrix4x4 currentRoot = Trajectory.Points[RootPointIndex].GetTransformation();
            Matrix4x4 previousRoot = Trajectory.Points[RootPointIndex - 1].GetTransformation();

            //Input Trajectory Positions / Directions
            for (int i = 0; i < PointSamples; i++)
            {
                Vector3 pos = Trajectory.Points[i * PointDensity].GetPosition().GetRelativePositionTo(currentRoot);
                Vector3 dir = Trajectory.Points[i * PointDensity].GetDirection().GetRelativeDirectionTo(currentRoot);
                NN.SetInput(PointSamples * 0 + i, UnitScale * pos.x);
                NN.SetInput(PointSamples * 1 + i, UnitScale * pos.z);
                NN.SetInput(PointSamples * 2 + i, dir.x);
                NN.SetInput(PointSamples * 3 + i, dir.z);
            }

            //Input Trajectory Gaits
            for (int i = 0; i < PointSamples; i++)
            {
                for (int j = 0; j < Trajectory.Points[i * PointDensity].Styles.Length; j++)
                {
                    NN.SetInput(PointSamples * (4 + j) + i, Trajectory.Points[i * PointDensity].Styles[j]);
                }
                //FOR HUMAN ONLY
                NN.SetInput(PointSamples * 8 + i, Trajectory.Points[i * PointDensity].GetSlope());
                //
            }

            //Input Previous Bone Positions / Velocities
            for (int i = 0; i < Actor.Bones.Length; i++)
            {
                int o = 10 * PointSamples;
                Vector3 pos = Positions[i].GetRelativePositionTo(previousRoot);
                Vector3 vel = Velocities[i].GetRelativeDirectionTo(previousRoot);
                NN.SetInput(o + Actor.Bones.Length * 3 * 0 + i * 3 + 0, UnitScale * pos.x);
                NN.SetInput(o + Actor.Bones.Length * 3 * 0 + i * 3 + 1, UnitScale * pos.y);
                NN.SetInput(o + Actor.Bones.Length * 3 * 0 + i * 3 + 2, UnitScale * pos.z);
                NN.SetInput(o + Actor.Bones.Length * 3 * 1 + i * 3 + 0, UnitScale * vel.x);
                NN.SetInput(o + Actor.Bones.Length * 3 * 1 + i * 3 + 1, UnitScale * vel.y);
                NN.SetInput(o + Actor.Bones.Length * 3 * 1 + i * 3 + 2, UnitScale * vel.z);
            }

            //Input Trajectory Heights
            for (int i = 0; i < PointSamples; i++)
            {
                int o = 10 * PointSamples + Actor.Bones.Length * 3 * 2;
                NN.SetInput(o + PointSamples * 0 + i, UnitScale * (Trajectory.Points[i * PointDensity].GetRightSample().y - currentRoot.GetPosition().y));
                NN.SetInput(o + PointSamples * 1 + i, UnitScale * (Trajectory.Points[i * PointDensity].GetPosition().y - currentRoot.GetPosition().y));
                NN.SetInput(o + PointSamples * 2 + i, UnitScale * (Trajectory.Points[i * PointDensity].GetLeftSample().y - currentRoot.GetPosition().y));
            }

            //Predict
            float rest = Mathf.Pow(1.0f - Trajectory.Points[RootPointIndex].Styles[0], 0.25f);
            NN.SetDamping(1f - (rest * 0.9f + 0.1f));
            NN.Predict();

            //Update Past Trajectory
            for (int i = 0; i < RootPointIndex; i++)
            {
                Trajectory.Points[i].SetPosition(Trajectory.Points[i + 1].GetPosition());
                Trajectory.Points[i].SetDirection(Trajectory.Points[i + 1].GetDirection());
                Trajectory.Points[i].SetLeftsample(Trajectory.Points[i + 1].GetLeftSample());
                Trajectory.Points[i].SetRightSample(Trajectory.Points[i + 1].GetRightSample());
                Trajectory.Points[i].SetSlope(Trajectory.Points[i + 1].GetSlope());
                for (int j = 0; j < Trajectory.Points[i].Styles.Length; j++)
                {
                    Trajectory.Points[i].Styles[j] = Trajectory.Points[i + 1].Styles[j];
                }
            }

            //Update Current Trajectory
            Trajectory.Points[RootPointIndex].SetPosition((rest * new Vector3(NN.GetOutput(0) / UnitScale, 0f, NN.GetOutput(1) / UnitScale)).GetRelativePositionFrom(currentRoot));
            Trajectory.Points[RootPointIndex].SetDirection(Quaternion.AngleAxis(rest * Mathf.Rad2Deg * (-NN.GetOutput(2)), Vector3.up) * Trajectory.Points[RootPointIndex].GetDirection());
            Trajectory.Points[RootPointIndex].Postprocess();
            Matrix4x4 nextRoot = Trajectory.Points[RootPointIndex].GetTransformation();

            //Update Future Trajectory
            for (int i = RootPointIndex + 1; i < Trajectory.Points.Length; i++)
            {
                Trajectory.Points[i].SetPosition(Trajectory.Points[i].GetPosition() + (rest * new Vector3(NN.GetOutput(0) / UnitScale, 0f, NN.GetOutput(1) / UnitScale)).GetRelativeDirectionFrom(nextRoot));
            }
            for (int i = RootPointIndex + 1; i < Trajectory.Points.Length; i++)
            {
                int w = RootSampleIndex;
                float m = Mathf.Repeat(((float)i - (float)RootPointIndex) / (float)PointDensity, 1.0f);
                float posX = (1 - m) * NN.GetOutput(8 + (w * 0) + (i / PointDensity) - w) + m * NN.GetOutput(8 + (w * 0) + (i / PointDensity) - w + 1);
                float posZ = (1 - m) * NN.GetOutput(8 + (w * 1) + (i / PointDensity) - w) + m * NN.GetOutput(8 + (w * 1) + (i / PointDensity) - w + 1);
                float dirX = (1 - m) * NN.GetOutput(8 + (w * 2) + (i / PointDensity) - w) + m * NN.GetOutput(8 + (w * 2) + (i / PointDensity) - w + 1);
                float dirZ = (1 - m) * NN.GetOutput(8 + (w * 3) + (i / PointDensity) - w) + m * NN.GetOutput(8 + (w * 3) + (i / PointDensity) - w + 1);
                Trajectory.Points[i].SetPosition(
                    Utility.Interpolate(
                        Trajectory.Points[i].GetPosition(),
                        new Vector3(posX / UnitScale, 0f, posZ / UnitScale).GetRelativePositionFrom(nextRoot),
                        TrajectoryCorrection
                        )
                    );
                Trajectory.Points[i].SetDirection(
                    Utility.Interpolate(
                        Trajectory.Points[i].GetDirection(),
                        new Vector3(dirX, 0f, dirZ).normalized.GetRelativeDirectionFrom(nextRoot),
                        TrajectoryCorrection
                        )
                    );
            }

            for (int i = RootPointIndex + PointDensity; i < Trajectory.Points.Length; i += PointDensity)
            {
                Trajectory.Points[i].Postprocess();
            }

            for (int i = RootPointIndex + 1; i < Trajectory.Points.Length; i++)
            {
                //ROOT	1		2		3		4		5
                //.x....x.......x.......x.......x.......x
                Trajectory.Point prev = GetPreviousSample(i);
                Trajectory.Point next = GetNextSample(i);
                float factor = (float)(i % PointDensity) / PointDensity;

                Trajectory.Points[i].SetPosition((1f - factor) * prev.GetPosition() + factor * next.GetPosition());
                Trajectory.Points[i].SetDirection((1f - factor) * prev.GetDirection() + factor * next.GetDirection());
                Trajectory.Points[i].SetLeftsample((1f - factor) * prev.GetLeftSample() + factor * next.GetLeftSample());
                Trajectory.Points[i].SetRightSample((1f - factor) * prev.GetRightSample() + factor * next.GetRightSample());
                Trajectory.Points[i].SetSlope((1f - factor) * prev.GetSlope() + factor * next.GetSlope());
            }

            //Avoid Collisions
            CollisionChecks(RootPointIndex);

            //Compute Posture
            int opos = 8 + 4 * RootSampleIndex + Actor.Bones.Length * 3 * 0;
            int ovel = 8 + 4 * RootSampleIndex + Actor.Bones.Length * 3 * 1;
            int orot = 8 + 4 * RootSampleIndex + Actor.Bones.Length * 3 * 2; //line 1680 of original PFNN implementation by Holden et al.


            for (int i = 0; i < Actor.Bones.Length; i++)
            {
                Vector3 position = new Vector3(NN.GetOutput(opos + i * 3 + 0), NN.GetOutput(opos + i * 3 + 1), NN.GetOutput(opos + i * 3 + 2)) / UnitScale;
                Vector3 velocity = new Vector3(NN.GetOutput(ovel + i * 3 + 0), NN.GetOutput(ovel + i * 3 + 1), NN.GetOutput(ovel + i * 3 + 2)) / UnitScale;
                Quaternion rotation = MatQuatUtils.ExpToQuat(new Vector3(NN.GetOutput(orot + i * 3 + 0), NN.GetOutput(orot + i * 3 + 1), NN.GetOutput(orot + i * 3 + 2)));
                Positions[i] = Vector3.Lerp(Positions[i].GetRelativePositionTo(currentRoot) + velocity, position, 0.5f).GetRelativePositionFrom(currentRoot);
                Velocities[i] = velocity.GetRelativeDirectionFrom(currentRoot);
                Rotations[i] = rotation.GetRelativeRotationFrom(currentRoot);

            }

            //Update Posture
            transform.position = nextRoot.GetPosition();
            transform.rotation = nextRoot.GetRotation();
            
            for (int i = 0; i < Actor.Bones.Length; i++)
            {
                Actor.Bones[i].Transform.position = Positions[i];
                Actor.Bones[i].Transform.rotation = Quaternion.LookRotation(Forwards[i], Ups[i]);
                
                joint_anim_xform[i] = Matrix4x4.TRS(Positions[i], Rotations[i], new Vector3(1, 1, 1)); 
                joint_mesh_xform[i] = joint_anim_xform[i];
            }

            AssignJointRot();;

            mirrorSkeletonScript.mirror(joint_mesh_xform);

            //Footpositon Correction
            //if (NN.GetOutput(4) < 0.6) //NN-Output at 4,5,6,7 contains a float which tell if the foot has contact with the ground
            //{
            //    mirrorSkeletonScript.CorrectFootPosition(false, CorrectFootAngle(joint_mesh_xform[3], NN.GetOutput(4)));
            //}

        }
    }

    /*
     * Assigns the rotations (arms and legs must be mirrored)
     */
    private void AssignJointRot()
    {
        //center
        joint_mesh_xform[0] = joint_anim_xform[0]* joint_rest_xform[0].inverse;

        //leftleg
        joint_mesh_xform[6] = joint_anim_xform[1];
        joint_mesh_xform[7] = joint_anim_xform[2];
        joint_mesh_xform[8] = joint_anim_xform[3];
        joint_mesh_xform[9] = joint_anim_xform[4];
        joint_mesh_xform[10] = joint_anim_xform[5];

        //Rightleg
        joint_mesh_xform[1] = joint_anim_xform[6];
        joint_mesh_xform[2] = joint_anim_xform[7];
        joint_mesh_xform[3] = joint_anim_xform[8];
        joint_mesh_xform[4] = joint_anim_xform[9];
        joint_mesh_xform[5] = joint_anim_xform[10];
        joint_mesh_xform[6] = joint_anim_xform[11];

        //CorrectFootAngle(joint_mesh_xform[3], NN.GetOutput(4));

        //Center
        joint_mesh_xform[12] = joint_anim_xform[12];
        joint_mesh_xform[13] = joint_anim_xform[13];
        joint_mesh_xform[14] = joint_anim_xform[14];
        joint_mesh_xform[15] = joint_anim_xform[15];
        joint_mesh_xform[16] = joint_anim_xform[16] * joint_rest_xform[16] * joint_rest_xform[16]; //head

        //Left Shoulder
        joint_mesh_xform[24] = joint_anim_xform[17];
        joint_mesh_xform[25] = joint_anim_xform[18];
        joint_mesh_xform[26] = joint_anim_xform[19];
        joint_mesh_xform[27] = joint_anim_xform[20];
        joint_mesh_xform[28] = joint_anim_xform[21];
        joint_mesh_xform[29] = joint_anim_xform[22];
        joint_mesh_xform[30] = joint_anim_xform[23];

        //Right shoulder
        joint_mesh_xform[17] = joint_anim_xform[24];
        joint_mesh_xform[18] = joint_anim_xform[25];
        joint_mesh_xform[19] = joint_anim_xform[26];
        joint_mesh_xform[20] = joint_anim_xform[27];
        joint_mesh_xform[21] = joint_anim_xform[28];
        joint_mesh_xform[22] = joint_anim_xform[29];
        joint_mesh_xform[23] = joint_anim_xform[30];


        
    }

    /*
     * First try to correct the footplacing
     * Not working
     */
    private float CorrectFootAngle(Matrix4x4 currentAngle, float footContact)
    {
        
        RaycastHit hit;

        if (Physics.Raycast(currentAngle.GetPosition()+new Vector3(0,0.005f,0), Vector3.down, out hit, Mathf.Infinity))
        {
            return Vector3.Angle(hit.normal, Vector3.up); //get angle of surface
            
        }
        else
        {
            Debug.DrawRay(currentAngle.GetPosition(), Vector3.down * 1000, Color.white);
            Debug.Log("Did not Hit");
        }

        return -190f;
    }

    /*
     * inits the array with the index of the parent of the joints
     */
    private void InitJointParents()
    {
        //original
        joint_parents[0] = -1;  //j1 Center
        joint_parents[1] = 0;   //...
        joint_parents[2] = 1;
        joint_parents[3] = 2;
        joint_parents[4] = 3;
        joint_parents[5] = 4;
        joint_parents[6] = 0;
        joint_parents[7] = 6;
        joint_parents[8] = 7;
        joint_parents[9] = 8;
        joint_parents[10] = 9;
        joint_parents[11] = 0;
        joint_parents[12] = 11;
        joint_parents[13] = 12;
        joint_parents[14] = 13;
        joint_parents[15] = 14;
        joint_parents[16] = 15;
        joint_parents[17] = 13;
        joint_parents[18] = 17;
        joint_parents[19] = 18;
        joint_parents[20] = 19;
        joint_parents[21] = 20;
        joint_parents[22] = 21;
        joint_parents[23] = 20;
        joint_parents[24] = 13;
        joint_parents[25] = 24;
        joint_parents[26] = 25;
        joint_parents[27] = 26;
        joint_parents[28] = 27;
        joint_parents[29] = 28;
        joint_parents[30] = 27;
    }

    /*
     * Initializes the @joint_rest_xform with the rotation from the rig
     */
    private void InitJointRestXForm(Matrix4x4[] jointRots)
    {
        for (int i = 0; i < jointRots.Length; i++)
        {
            if (i == 0)
            {
                joint_rest_xform[i] = jointRots[i];
            }
            else
            {
                joint_rest_xform[i] = jointRots[joint_parents[i]];
            }
        }
    }


    /*
     * This function calcs Bias for the velocity	 
     */
    private float PoolBias()
    {
        if(gamepadConnected) {

            return ((GamepadController)Controller).GetBias()*speedMultiplier;
        }

        if (wayPointActive)
        {
            return ((WaypointController)Controller).GetBias() * speedMultiplier;
        }

        float[] styles = Trajectory.Points[RootPointIndex].Styles;
        float bias = 0f;
        for (int i = 0; i < styles.Length; i++)
        {
            float _bias = Controller.Styles[i].Bias;
            float max = 0f;
            for (int j = 0; j < Controller.Styles[i].Multipliers.Length; j++)
            {
                if (Input.GetKey(Controller.Styles[i].Multipliers[j].Key))
                {
                    max = Mathf.Max(max, Controller.Styles[i].Bias * Controller.Styles[i].Multipliers[j].Value);
                }
            }
            for (int j = 0; j < Controller.Styles[i].Multipliers.Length; j++)
            {
                if (Input.GetKey(Controller.Styles[i].Multipliers[j].Key))
                {
                    _bias = Mathf.Min(max, _bias * Controller.Styles[i].Multipliers[j].Value);
                }
            }
            bias += styles[i] * _bias;
        }
        return bias*speedMultiplier;
    }

    private Trajectory.Point GetSample(int index)
    {
        return Trajectory.Points[Mathf.Clamp(index * 10, 0, Trajectory.Points.Length - 1)];
    }

    private Trajectory.Point GetPreviousSample(int index)
    {
        return GetSample(index / 10);
    }

    private Trajectory.Point GetNextSample(int index)
    {
        if (index % 10 == 0)
        {
            return GetSample(index / 10);
        }
        else
        {
            return GetSample(index / 10 + 1);
        }
    }

    /*
    * Checks for collison
    */
    private void CollisionChecks(int start)
    {
        for (int i = start; i < Trajectory.Points.Length; i++)
        {
            float safety = 0.5f;
            Vector3 previousPos = Trajectory.Points[i - 1].GetPosition();
            Vector3 currentPos = Trajectory.Points[i].GetPosition();
            Vector3 testPos = previousPos + safety * (currentPos - previousPos).normalized;
            Vector3 projectedPos = Utility.ProjectCollision(previousPos, testPos, LayerMask.GetMask("Obstacles"));
            if (testPos != projectedPos)
            {
                Vector3 correctedPos = testPos + safety * (previousPos - testPos).normalized;
                Trajectory.Points[i].SetPosition(correctedPos);
            }
        }
    }

    void OnGUI()
    {
        //GUI Controlls visualisation

        //GUI.color = UltiDraw.Mustard;
        //GUI.backgroundColor = UltiDraw.Black;
        //float height = 0.05f;
        //GUI.Box(Utility.GetGUIRect(0.025f, 0.05f, 0.3f, Controller.Styles.Length * height), "");
        //for (int i = 0; i < Controller.Styles.Length; i++)
        //{
        //    GUI.Label(Utility.GetGUIRect(0.05f, 0.075f + i * 0.05f, 0.025f, height), Controller.Styles[i].Name);
        //    string keys = string.Empty;
        //    for (int j = 0; j < Controller.Styles[i].Keys.Length; j++)
        //    {
        //        keys += Controller.Styles[i].Keys[j].ToString() + " ";
        //    }
        //    GUI.Label(Utility.GetGUIRect(0.075f, 0.075f + i * 0.05f, 0.05f, height), keys);
        //    GUI.HorizontalSlider(Utility.GetGUIRect(0.125f, 0.075f + i * 0.05f, 0.15f, height), Trajectory.Points[RootPointIndex].Styles[i], 0f, 1f);
        //}
    }

    /*
    *  Draws the trajectory if the option is turned on
    */
    void OnRenderObject()
    {
        /*
        UltiDraw.Begin();
        UltiDraw.DrawGUICircle(new Vector2(0.5f, 0.85f), 0.075f, UltiDraw.Black.Transparent(0.5f));
        Quaternion rotation = Quaternion.AngleAxis(-360f * NN.GetPhase() / (2f * Mathf.PI), Vector3.forward);
        Vector2 a = rotation * new Vector2(-0.005f, 0f);
        Vector2 b = rotation *new Vector3(0.005f, 0f);
        Vector3 c = rotation * new Vector3(0f, 0.075f);
        UltiDraw.DrawGUITriangle(new Vector2(0.5f + b.x/Screen.width*Screen.height, 0.85f + b.y), new Vector2(0.5f + a.x/Screen.width*Screen.height, 0.85f + a.y), new Vector2(0.5f + c.x/Screen.width*Screen.height, 0.85f + c.y), UltiDraw.Cyan);
        UltiDraw.End();
        */

        if (Application.isPlaying&& visualizeTrajectory)
        {
            if (NN.Parameters == null)
            {
                return;
            }

            UltiDraw.Begin();
            UltiDraw.DrawLine(Trajectory.Points[RootPointIndex].GetPosition(), Trajectory.Points[RootPointIndex].GetPosition() + TargetDirection, 0.05f, 0f, UltiDraw.Red.Transparent(0.75f));
            UltiDraw.DrawLine(Trajectory.Points[RootPointIndex].GetPosition(), Trajectory.Points[RootPointIndex].GetPosition() + TargetVelocity, 0.05f, 0f, UltiDraw.Green.Transparent(0.75f));
            UltiDraw.End();
            Trajectory.Draw(10);

            UltiDraw.Begin();
            for (int i = 0; i < Actor.Bones.Length; i++)
            {
                UltiDraw.DrawArrow(
                    Actor.Bones[i].Transform.position,
                    Actor.Bones[i].Transform.position + Velocities[i],
                    0.75f,
                    0.0075f,
                    0.05f,
                    UltiDraw.Purple.Transparent(0.5f)
                );
            }
            UltiDraw.End();
        }
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            OnRenderObject();
        }
    }
}

/*
* Handles the user interface of the Inspector
*/
#if UNITY_EDITOR
[CustomEditor(typeof(BioAnimation_Original))]
public class BioAnimation_Original_Editor : Editor
{

    public BioAnimation_Original Target;

    void Awake()
    {
        Target = (BioAnimation_Original)target;
    }

    public override void OnInspectorGUI()
    {
        Undo.RecordObject(Target, Target.name);

        Inspector();
        Target.Controller.Inspector();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(Target);
        }
    }

    private void Inspector()
    {
        Utility.SetGUIColor(UltiDraw.Grey);
        using (new EditorGUILayout.VerticalScope("Box"))
        {
            Utility.ResetGUIColor();

            if (Utility.GUIButton("Animation", UltiDraw.DarkGrey, UltiDraw.White))
            {
                Target.Inspect = !Target.Inspect;
            }

            if (Target.Inspect)
            {
                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    Target.visualizeTrajectory = EditorGUILayout.Toggle("Visualize Trajectory?", Target.visualizeTrajectory);
                    Target.wayPointActive = EditorGUILayout.Toggle("Waypoints active?", Target.wayPointActive);
                    Target.gamepadConnected = EditorGUILayout.Toggle("Gamepad connected?", Target.gamepadConnected);
                    Target.speedMultiplier = EditorGUILayout.Slider("Speed Multiplier", Target.speedMultiplier, 0f, 15f);
                    Target.TargetBlending = EditorGUILayout.Slider("Target Blending", Target.TargetBlending, 0f, 1f);
                    Target.GaitTransition = EditorGUILayout.Slider("Gait Transition", Target.GaitTransition, 0f, 1f);
                    Target.TrajectoryCorrection = EditorGUILayout.Slider("Trajectory Correction", Target.TrajectoryCorrection, 0f, 1f);
                }
            }

        }
    }
}
#endif