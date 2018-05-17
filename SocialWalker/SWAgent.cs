using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SWAgent : Agent {
    public int sw_id;
    private SW agent_;
    private SW neighbor_;
    private SWCrowd cr_;
    public GameObject target_;

    //parameters
    const float rewardTargetReached = 1.0f;
    const float rewardCollision = -1.0f;
    const float rewardOutOfBounds = -1.0f; 
    const float orientationGainedWeight= 0.03f;
    const float distanceGainedWeight = 0.05f;
    const float rewardEachStep = -0.01f;
    const int obsSpaceSize = 10;
    const float agentFOV = 60.0f;
    const float agentSensorLength = 30.0f;
    const int maxSteps = 1000;

    private int step;
    private bool agentDone = false;

    void Awake(){

    }

    void Start()
    {
        step = 0;
        cr_ = GameObject.Find("Crowd").GetComponent<SWCrowd>();
        cr_.getAgent(ref agent_, sw_id);
    }

    public override void CollectObservations()
    {
        // Agent only remains on the 2D XZ plane

        // since target direction is always assumed to be known in the general case
        // we also need to sample more
        //var sensorDataTar = cr_.getSensorsTarget(sw_id, 16, 360, agentSensorLength);

        float signedAngle = Vector3.SignedAngle(agent_.forward, agent_.target - agent_.pos, Vector3.up);
        AddVectorObs(1.0f / (agent_.target - agent_.pos).sqrMagnitude);
        AddVectorObs(agent_.cosineOrientation());
        AddVectorObs(signedAngle > 0.0f ? 1.0f : 0.0f);
        
        var sensorDataObs = cr_.getSensorsObstacle(sw_id, obsSpaceSize, agentFOV, agentSensorLength);
        // for(int i = 0; i < sensorDataTar.Count; i++){
        //     //Debug.Log("Sensor " + i + ":" + sensorData[i]);
        //     AddVectorObs(sensorDataTar[i]);
        // }
        for(int i = 0; i < sensorDataObs.Count; i++){
            //Debug.Log("Sensor " + i + ":" + sensorData[i]);
            AddVectorObs(sensorDataObs[i]);
        }
    }

    public override void AgentReset()
    {
        step = 0;
        Vector3 pos = new Vector3(Random.Range(cr_.minBound_.x, cr_.maxBound_.x), 0.5f, Random.Range(cr_.minBound_.z, cr_.maxBound_.z));
        Vector3 tar = new Vector3(Random.Range(cr_.minBound_.x, cr_.maxBound_.x), 0.5f, Random.Range(cr_.minBound_.z, cr_.maxBound_.z));
        agent_.init(pos, tar);
        cr_.setAgent(ref agent_, sw_id);
        gameObject.transform.position = pos;
        target_.transform.position = tar;
    }

    public override void AgentAction(float[] act, string textAction)
    {
        if(agentDone){
            if(!cr_.allDone_){
                return;
            } else {
                agentDone = false;
                cr_.setAgentActiveStatus(sw_id, true);
                return;
                //Done();
            }
        } 
        //Debug.Log("Actions act[0] " + act[0] + "act[1] " + act[1]);
        step++;
        // 0 -> move forward
        // 1 -> turn left
        // 2 -> turn right

        float distToTargetOld = (target_.transform.position - agent_.pos).magnitude;
        float orientationOld = agent_.cosineOrientation();

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
            if (action == 0) // walk forward
            {
                agent_.walkForward();
            }
            if (action == 1) // turn right
            {
                agent_.turnRight();
            }
            else if (action == 2) // turn left
            {
                agent_.turnLeft();
            }
            else if (action == 3) // stop
            {
                agent_.Brake();
            }
            
            gameObject.transform.position = agent_.pos;
            gameObject.transform.forward = agent_.forward;

        }

        cr_.setAgent(ref agent_, sw_id);

        float distToTarget = (target_.transform.position - agent_.pos).magnitude;
        float orientation = agent_.cosineOrientation();

        if (agent_.targetReached())
        {
            //Debug.Log("Reached Target! Agent Done.");
            AddReward(rewardTargetReached);
            AgentDoneStuff();            
            return;
        }
        
        if (!agent_.withinBounds(cr_.areaMinBound_, cr_.areaMaxBound_) || step > maxSteps)
        {
            //Debug.Log("Went out of Arena! Agent Done.");
            AddReward(rewardOutOfBounds);
            AgentDoneStuff();
            return;
        }

        if(cr_.doesCollide(sw_id)){
            Debug.Log("Collision!");
            AddReward(rewardCollision);
        }

        //reward for gaining distance towards the target
        AddReward(distanceGainedWeight * (distToTargetOld - distToTarget)); // should have come closer to target
        //reward for orienting towards the target
        AddReward(orientationGainedWeight * (orientation - orientationOld)); // should have aligned better with target
        //reward for each step (usually negative)
        AddReward(rewardEachStep);

    }

    void AgentDoneStuff(){
        Done();
        gameObject.SetActive(false);
        target_.SetActive(false);
        cr_.setAgentActiveStatus(sw_id, false);
        agentDone = true;
    }

}
