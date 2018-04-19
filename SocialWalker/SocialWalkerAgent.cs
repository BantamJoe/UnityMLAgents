using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SocialWalkerAgent : Agent {
    public int sw_id;
    private SocialWalker agent_;
    private SocialWalker neighbor_;
    private SocialWalkerCrowd cr_;
    public GameObject target_;

    private Vector3 minBound = new Vector3(-10f, 0f, -10f);
    private Vector3 maxBound = new Vector3(10f, 0f, 10f);

    private int step;

    void Awake(){

    }

    void Start()
    {
        step = 0;
        cr_ = GameObject.Find("Crowd").GetComponent<SocialWalkerCrowd>();
        cr_.getAgent(ref agent_, sw_id);
    }

    public override void CollectObservations()
    {
        // Agent only remains on the 2D XZ plane
        AddVectorObs(agent_.target.x);
        AddVectorObs(agent_.target.z);

        AddVectorObs(agent_.pos.x);
		AddVectorObs(agent_.pos.z);

        AddVectorObs(agent_.vel.x);
		AddVectorObs(agent_.vel.z);

        for(int i = 0; i < cr_.numAgents_; i++){
            if(i == sw_id){
                continue;
            }
            
            cr_.getAgent(ref neighbor_, i);

            AddVectorObs(neighbor_.pos.x);
    		AddVectorObs(neighbor_.pos.z);
            AddVectorObs(neighbor_.vel.x);
    		AddVectorObs(neighbor_.vel.z);
        }
    }

    public override void AgentReset()
    {
        step = 0;
        Vector3 pos = new Vector3(Random.Range(minBound.x, maxBound.x), 0.5f, Random.Range(minBound.z, maxBound.z));
        Vector3 tar = new Vector3(Random.Range(minBound.x, maxBound.x), 0.5f, Random.Range(minBound.z, maxBound.z));
        agent_.init(pos, tar);
        cr_.setAgent(ref agent_, sw_id);
        gameObject.transform.position = pos;
        target_.transform.position = tar;
    }

    public override void AgentAction(float[] act, string textAction)
    {
        //Debug.Log("Actions act[0] " + act[0] + "act[1] " + act[1]);
        step++;
        // 0 -> move forward
        // 1 -> turn left
        // 2 -> turn right
        if (brain.brainParameters.vectorActionSpaceType == SpaceType.continuous)
        {
            gameObject.transform.position += gameObject.transform.forward.normalized * Mathf.Clamp(act[0], 0f, 1f);

            float rotAngle = 90.0f * Mathf.Clamp(act[1], -1f, 1f); // opposite ends, reachable from [-90, 90] rotation 

            gameObject.transform.Rotate(0f, rotAngle, 0f);
        }
        else
        {
            int action = (int)act[0];
            //Debug.Log(action);
            if (action == 0) // accelerate forward
            {
                agent_.accelerateForward(0.001f);
            }
            if (action == 1) // accelerate backward
            {
                agent_.accelerateForward(-0.005f);
            }
            else if (action == 2) // accelerate right
            {
                agent_.accelerateRight(0.001f);
            }
            else if (action == 3) // accelerate left
            {
                agent_.accelerateRight(-0.001f);
            }
            else if (action == 4)
            {
                agent_.maintainSpeed();
            }

            gameObject.transform.position = agent_.pos;
            gameObject.transform.forward = agent_.forward;

        }

        cr_.setAgent(ref agent_, sw_id);

        if (agent_.targetReached())
        {
            Debug.Log("Reached Target!");
            AddReward(1.0f);
            Done();
        }
        else if (!agent_.withinBounds(minBound, maxBound))
        {
            Debug.Log("Went out of Arena!");
            AddReward(-0.5f);
            Done();
        } 
        else if(cr_.doesCollide(sw_id)){
            Debug.Log("Collision!");
            AddReward(-0.5f);
        }

        // Add a net zero reward if the forward direction is point towards the target
        // else add a small negative reward
        // A = tar - pos
        // B = forward
        // cos theta = A dot B / |A||B|

        AddReward((agent_.cosineOrientation() - 1.0f) / 200.0f); // a reward between [-0.01, 0]

    }

}
