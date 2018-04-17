using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SocialWalkerAgent : Agent {

    public GameObject target;

    private Vector3 startPos;
    private Vector3 forward;
    private float forwardSpeed;
    private float rotSpeed;
    private float forwardAcc;
    private float rotAcc;

    private float maxForwardSpeed = 0.5f;
    private float minForwardSpeed = -0.2f;
    private float maxRotSpeed = 90.0f;

    private int history = 2;

    private int step;
    private int episode;
    private int numEpisodeToRepeat = 10;

    private float bound = 10f;

    private List<Vector3> pastPositions;
    private List<Vector3> pastForwardDirections;

    float DistXZ(Vector3 a, Vector3 b)
    {
        float X = a.x - b.x;
        float Z = a.z - b.z;
        return Mathf.Sqrt(X * X + Z * Z);
    }

    float DistGained()
    {
        float sumDistGained = 0f;
        float distToTarget = DistXZ(target.transform.position, pastPositions[0]);

        for (int i = 1; i < pastPositions.Count; i++)
        {
            float distToTargetTmp = DistXZ(target.transform.position, pastPositions[i]);
            sumDistGained += (distToTarget - distToTargetTmp);
        }
        return sumDistGained / (pastPositions.Count - 1);
    }

    void Start()
    {
        Debug.Log("Input feature size " + (4 * history + 8));
        step = 0;
        episode = 0;
        //startPos = new Vector3(0f, 0f, 0f);
        startPos = transform.position;
        forward = new Vector3(0f, 0f, 1f);
        forwardSpeed = 0f;
        forwardAcc = 0f;
        rotSpeed = 0f;
        rotAcc = 0f;

        pastPositions = new List<Vector3>();
        pastForwardDirections = new List<Vector3>();

        for(int i = 0; i < history; i++)
        {
            pastPositions.Add(startPos);
            pastForwardDirections.Add(forward);
        }

        gameObject.transform.position = startPos;
        gameObject.transform.rotation = Quaternion.identity;

        target.transform.position = new Vector3(Random.Range(1 - bound, bound - 1), 0.5f, Random.Range(1 - bound, bound - 1));
        //target.transform.position = new Vector3(bound - 1, 0.5f, bound - 1);
    }

    public override void CollectObservations()
    {
        // Agent only remains on the 2D XZ plane
        AddVectorObs(target.transform.position.x);
        AddVectorObs(target.transform.position.z);

        //add past positions
        for(int i = 0; i < history; i++)
        {
            AddVectorObs(pastPositions[i].x);
            AddVectorObs(pastPositions[i].z);
        }

        AddVectorObs(gameObject.transform.position.x);
        AddVectorObs(gameObject.transform.position.z);

        for (int i = 0; i < history; i++)
        {
            AddVectorObs(pastForwardDirections[i].x);
            AddVectorObs(pastForwardDirections[i].z);
        }

        AddVectorObs(forward.x);
        AddVectorObs(forward.z);

        AddVectorObs(forwardSpeed);
        AddVectorObs(rotSpeed);
    }

    public override void AgentReset()
    {
        step = 0;
        forwardSpeed = 0f;
        forwardAcc = 0f;
        rotSpeed = 0f;
        rotAcc = 0f;

        pastForwardDirections.Clear();
        pastPositions.Clear();

        forward = new Vector3(0f, 0f, 1f);

        for (int i = 0; i < history; i++)
        {
            pastPositions.Add(startPos);
            pastForwardDirections.Add(forward);
        }

        gameObject.transform.position = startPos;
        gameObject.transform.rotation = Quaternion.identity;
        
        if(episode > numEpisodeToRepeat)
        {
            episode = 0;
            target.transform.position = new Vector3(Random.Range(1 - bound, bound - 1), 0.5f, Random.Range(1 - bound, bound - 1));
        } 
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
            gameObject.transform.position += forward.normalized * Mathf.Clamp(act[0], 0f, 1f);

            float rotAngle = 90.0f * Mathf.Clamp(act[1], -1f, 1f); // opposite ends, reachable from [-90, 90] rotation 

            gameObject.transform.Rotate(0f, rotAngle, 0f);
            forward = Quaternion.AngleAxis(rotAngle, Vector3.up) * forward;

        }
        else
        {
            int action = (int)act[0];
            //Debug.Log(action);
            if (action == 0) // accelerate forward
            {
                forwardAcc += 0.01f;
            }
            if (action == 1) // accelerate backward
            {
                forwardAcc -= 0.01f;
            }
            else if (action == 2) // accelerate rotation right
            {
                rotAcc += 0.01f;
            }
            else if (action == 3) // accelerate rotation left
            {
                rotAcc -= 0.01f;
            }
            else if (action == 4)
            {
                // do nothing
            }
            if (action == 0 || action == 1)
            {
                forwardSpeed += forwardAcc;
            }
            if (action == 2 || action == 3)
            {
                rotSpeed += rotAcc;
            }

            forwardSpeed = Mathf.Clamp(forwardSpeed, minForwardSpeed, maxForwardSpeed);
            rotSpeed = Mathf.Clamp(rotSpeed, -maxRotSpeed, maxRotSpeed);

            gameObject.transform.position += forward.normalized * forwardSpeed;
            gameObject.transform.Rotate(Vector3.up, rotSpeed);
            forward = Quaternion.AngleAxis(rotSpeed, Vector3.up) * forward;
        }

        pastForwardDirections.RemoveAt(0);
        pastForwardDirections.Add(forward);
        pastPositions.RemoveAt(0);
        pastPositions.Add(gameObject.transform.position);

        float A = gameObject.transform.position.x - target.transform.position.x;
        float B = gameObject.transform.position.z - target.transform.position.z;
        float distCurr = Mathf.Sqrt(A*A + B*B);
        float distGained = DistGained();

        //Debug.Log("Average Dist Gained " + distGained);

        if (distCurr < 1.0f)
        {
            Debug.Log("Reached Target!");
            AddReward(1.0f);
            episode++;
            Done();
        }
        else if (gameObject.transform.position.x < -bound || gameObject.transform.position.x > bound ||
            gameObject.transform.position.z < -bound || gameObject.transform.position.z > bound)
        {
            //Debug.Log("Went out of Arena!");
            AddReward(-0.5f);
            episode++;
            Done();

            //Vector3 tmp = gameObject.transform.position;

            //if (tmp.x < -bound)
            //{
            //    tmp.x = -bound;
            //}
            //if (tmp.z < -bound)
            //{   
            //    tmp.z = -bound;
            //}   
            //if (tmp.x > bound)
            //{   
            //    tmp.x = bound;
            //}   
            //if (tmp.z > bound)
            //{   
            //    tmp.z = bound;
            //}

            //gameObject.transform.position = tmp;
        }
        else if (distGained > 0)
        {
            AddReward(distGained);
        }

        AddReward(-0.01f);
        
    }

}
