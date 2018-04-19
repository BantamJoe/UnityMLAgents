using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SocialWalker
{
    public Vector3 pos;
    public Vector3 vel;
    public Vector3 acc;
    public Vector3 forward;
    public Vector3 target;
    public float radius;

    float maxSpeed;
    float minSpeed;

    public void init(Vector3 x_, Vector3 target_)
    {
        pos = x_;
        vel = new Vector3(0f, 0f, 0f);
        acc = new Vector3(0f, 0f, 0f);
        forward = new Vector3(0f, 0f, 1f);
        target = target_;
        radius = 1.0f;
        maxSpeed = 0.2f;
        minSpeed = 0.001f;
    }

    private void restrictSpeed(){
        if(vel.magnitude > maxSpeed){
            vel = vel.normalized * maxSpeed;
        }
        if(vel.magnitude < minSpeed){
            vel = vel.normalized * minSpeed;
        }
    }

    // accelerate in the direction of forward velocity
    public void accelerateForward(float inc){ 
        //Debug.Log("Accelerating Forward");
        acc = acc + forward * inc;
        vel = vel + acc; // unit time

        restrictSpeed();

        pos = pos + vel; // unit time
        if(vel.magnitude > 0.0001f){
            forward = vel.normalized;
        }
    }

    public void accelerateRight(float inc){
        // Since our plane is the XZ plane, we take the cross product of the forward direction with the up direction (0, 1, 0)
        // to get the right direction 
        Vector3 right = Vector3.Cross(new Vector3(0f, 1f, 0f), forward).normalized;
        acc = acc + right * inc;
        vel = vel + acc;

        restrictSpeed();

        pos = pos + vel;
        if(vel.magnitude > 0.0001f){
            forward = vel.normalized;
        }
    }

    public void maintainSpeed(){
        acc = new Vector3(0f, 0f, 0f);
        pos = pos + vel;
        if(vel.magnitude > 0.0001f){
            forward = vel.normalized;
        }
    }

    public bool targetReached(){
        Vector3 d = target - pos;
        if(d.magnitude < 1.0f){
            return true;
        }
        return false;
    }

    public bool withinBounds(Vector3 minB, Vector3 maxB){
        if(pos.x < minB.x || pos.x > maxB.x || pos.z < minB.z || pos.z > maxB.z){
            return false;
        }
        return true;
    }

    public bool isCollidingWith(SocialWalker sw)
    {
        Vector3 d = pos - sw.pos;
        //Debug.Log("distance between agents " + d.magnitude);
        if(radius + sw.radius > d.magnitude)
        {
            return true;
        }
        return false;
    }

    // 1 if forward is exactly facing the target
    // -1 if forward is facing exactly opposite the target
    public float cosineOrientation(){
        var A = target - pos;
        var B = forward;
        return Vector3.Dot(A, B) / (A.magnitude * B.magnitude);
    }
}

public class SocialWalkerCrowd : MonoBehaviour
{
    public GameObject socialWalkerBrain_;
    public GameObject walkerPrefab_;
    public GameObject targetPrefab_;
    public int numAgents_;
    private int numAgentsTmp_;
    private List<SocialWalker> agents_;
    private List<GameObject> walkerAgents_;
    private List<GameObject> targetAgents_;
    private float bound_ = 10f;

    void Start()
    {
        agents_ = new List<SocialWalker>();
        walkerAgents_ = new List<GameObject>();
        targetAgents_ = new List<GameObject>();
    }

    void Update()
    {
        if(numAgentsTmp_ != numAgents_)
        {
            numAgentsTmp_ = numAgents_;
            Debug.Log("Num agents " + numAgents_);
            ResetAgents();
        }
    }

    void ResetAgents()
    {
        agents_.Clear();

        foreach(var wa in walkerAgents_){
            Destroy(wa);
        }

        walkerAgents_.Clear();

        foreach(var tar in targetAgents_){
            Destroy(tar);
        }

        targetAgents_.Clear();

        for(int i = 0; i < numAgents_; i++)
        {
            float hue = (float)i / numAgents_;
            Color newColor = Color.HSVToRGB(hue, 1f, 1f);
            Color newColorDark = Color.HSVToRGB(hue, 1f, 0.5f);

            Vector3 pos = new Vector3(Random.Range(1 - bound_, bound_ - 1), 0.5f, Random.Range(1 - bound_, bound_ - 1));
            Vector3 tar = new Vector3(Random.Range(1 - bound_, bound_ - 1), 0.5f, Random.Range(1 - bound_, bound_ - 1));

            SocialWalker S = new SocialWalker();
            S.init(pos, tar);
            agents_.Add(S);

            GameObject walkerClone = Instantiate(walkerPrefab_, pos , Quaternion.identity);
            walkerClone.GetComponent<MeshRenderer>().material.color = newColor;

            GameObject targetClone = Instantiate(targetPrefab_, tar, Quaternion.identity);
            targetClone.GetComponent<MeshRenderer>().material.color = newColorDark;

            walkerClone.GetComponent<SocialWalkerAgent>().GiveBrain(socialWalkerBrain_.GetComponent<Brain>());
            walkerClone.GetComponent<SocialWalkerAgent>().sw_id = i;
            walkerClone.GetComponent<SocialWalkerAgent>().target_ = targetClone;

            walkerAgents_.Add(walkerClone);
            targetAgents_.Add(targetClone);

        }
    }

    public void getAgent(ref SocialWalker agent, int id)
    {
        agent = agents_[id];
    }

    public void setAgent(ref SocialWalker agent, int id)
    {
        agents_[id] = agent;
    }

    public bool doesCollide(int id){
        for(int i = 0; i < numAgents_; i++){
            if(i == id){
                continue;
            }
            if(agents_[id].isCollidingWith(agents_[i])){
                return true;
            }
        }
        return false;
    }
}