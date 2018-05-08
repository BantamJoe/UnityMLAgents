using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SWCrowd : MonoBehaviour
{
    public bool imitationLearning;
    public GameObject SocialWalkerBrain_;
    public GameObject TeacherBrain_;
    public GameObject StudentBrain_;
    public GameObject walkerPrefab_;
    public GameObject targetPrefab_;
    public int numAgents_;
    private int numAgentsTmp_;
    private List<SW> agents_;
    private List<bool> agentActive_;
    private List<GameObject> walkerAgents_;
    private List<GameObject> targetAgents_;
    public Vector3 minBound_ = new Vector3(-10f, 0f, -10f);
    public Vector3 maxBound_ = new Vector3(10f, 0f, 10f);

    public Vector3 areaMinBound_ = new Vector3(-100f, 0f, -100f);
    public Vector3 areaMaxBound_ = new Vector3(100f, 0f, 100f);
    public Text info_;
    public bool allDone_;
    //private List<Vector3> debugDirections_ = new List<Vector3>();

    void Start()
    {
        allDone_ = false;
        agents_ = new List<SW>();
        agentActive_ = new List<bool>();
        walkerAgents_ = new List<GameObject>();
        targetAgents_ = new List<GameObject>();
        InvokeRepeating("UpdateInfo", 2.0f, 0.5f);
    }

    void Update()
    {
        if(numAgentsTmp_ != numAgents_)
        {
            numAgentsTmp_ = numAgents_;
            //Debug.Log("Num agents " + numAgents_);
            //info_.text = "Num agents " + numAgents_;
            ResetAgents();
        }
    }

    // void OnDrawGizmos(){
    //     for(int i = 0; i < debugDirections_.Count; i++){
    //         Gizmos.DrawLine(agents_[0].pos, agents_[0].pos + debugDirections_[i]);
    //     }
    // }

    void UpdateInfo(){
        //info_.text = "Num agents " + numAgents_;
        info_.text = "";
        for(int i = 0; i < numAgents_; i++){
            const int obsSpaceSize = 10;
            const float agentFOV = 60.0f;
            const float agentSensorLength = 30.0f;
            var sensorDataObs = getSensorsObstacle(i, obsSpaceSize, agentFOV, agentSensorLength);
            float signedAngle = Vector3.SignedAngle(agents_[i].forward, agents_[i].target - agents_[i].pos, Vector3.up);
            int dir = signedAngle > 0.0f ? 1 : 0;
            float d = 1.0f / (agents_[i].target - agents_[i].pos).sqrMagnitude;
            info_.text += "\n Agent " + i + " D = " + d + " A = " + agents_[i].cosineOrientation().ToString("F3") + "(" + dir + ")\n[[";
            for(int j = 0; j < sensorDataObs.Count; j++){
                info_.text += sensorDataObs[j].ToString("F2") + "|";
            }
            info_.text += "]]";
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

            Vector3 pos = new Vector3(Random.Range(minBound_.x, maxBound_.x), 0.5f, Random.Range(minBound_.z, maxBound_.z));
            Vector3 tar = new Vector3(Random.Range(minBound_.x, maxBound_.x), 0.5f, Random.Range(minBound_.z, maxBound_.z));

            SW S = new SW();
            S.init(pos, tar);
            agents_.Add(S);

            GameObject walkerClone = Instantiate(walkerPrefab_, pos , Quaternion.identity);
            walkerClone.GetComponent<MeshRenderer>().material.color = newColor;

            GameObject targetClone = Instantiate(targetPrefab_, tar, Quaternion.identity);
            targetClone.GetComponent<MeshRenderer>().material.color = newColorDark;

            if(imitationLearning){
                if(i == 0){
                    walkerClone.GetComponent<SWAgent>().GiveBrain(TeacherBrain_.GetComponent<Brain>());
                } else {
                    walkerClone.GetComponent<SWAgent>().GiveBrain(StudentBrain_.GetComponent<Brain>());
                }
            } else {
                walkerClone.GetComponent<SWAgent>().GiveBrain(SocialWalkerBrain_.GetComponent<Brain>());
            }

            
            walkerClone.GetComponent<SWAgent>().sw_id = i;
            walkerClone.GetComponent<SWAgent>().target_ = targetClone;

            walkerAgents_.Add(walkerClone);
            targetAgents_.Add(targetClone);
            agentActive_.Add(true);

        }
    }

    public void getAgent(ref SW agent, int id)
    {
        agent = agents_[id];
    }

    public void setAgent(ref SW agent, int id)
    {
        agents_[id] = agent;
    }

    public void setAgentActiveStatus(int id, bool status){
        //Debug.Log("Status updated called by " + id + " with " + status);
        agentActive_[id] = status;
        int numDone = 0;
        for(int i = 0; i < numAgents_; i++){
            if(!agentActive_[i]){
                numDone++;
            }
        }
        //Debug.Log("Num Done " + numDone);
        if(allDone_ && numDone == 0){
            //Debug.Log("All Done set to FALSE");
            allDone_ = false;
        }
        if(!allDone_ && numDone == numAgents_){
            //Debug.Log("All Done set to TRUE");
            allDone_ = true;
            for(int i = 0; i < numAgents_; i++){
                 walkerAgents_[i].SetActive(true);
                 targetAgents_[i].SetActive(true);
            }
        }
    }

    public bool doesCollide(int id){
        for(int i = 0; i < numAgents_; i++){
            if(i == id){
                continue;
            }
            if(!agentActive_[i]){
                continue;
            }
            if(agents_[id].isCollidingWith(agents_[i])){
                return true;
            }
        }
        return false;
    }

    public List<float> getSensorsTarget(int id, int numSensors, float FOVDegrees, float sensorLength){

        List<float> ret = new List<float>();
        Vector3 forward = agents_[id].forward;
        
        // //debug
        // if(id == 0){
        //     debugDirections_.Clear();
        // }
        // //debug

        float incAngle = FOVDegrees / numSensors;
        float startAngle = (incAngle - FOVDegrees) / 2;

        for(int i = 0; i < numSensors; i++){
            float targetDist = 20000.0f;
            float value = 0.0f;
            float angle = startAngle + i * incAngle;
            Vector3 rayDir =  Quaternion.AngleAxis(angle, Vector3.up) * forward;

            // //debug
            // if(id == 0){
            //     debugDirections_.Add(rayDir);
            // }
            // //debug


            //check if ray intersects target
            targetDist = Mathf.Min(targetDist, intersectRaySphere(agents_[id].pos, rayDir, agents_[id].target, agents_[id].targetRadius));

            if(targetDist < 20000.0f){
                value = sensorLength / (targetDist + 1);
            }
            
            ret.Add(value);

            // //debug
            // if(id == 0){
            //     debugDirections_.Add(value * rayDir);
            // }
            // //debug

        }

        return ret;
    }

    public List<float> getSensorsObstacle(int id, int numSensors, float FOVDegrees, float sensorLength){

        List<float> ret = new List<float>();
        Vector3 forward = agents_[id].forward;

        //Debug.Log(ret.Count + " " + numSensors);

        float incAngle = FOVDegrees / numSensors;
        float startAngle = (incAngle - FOVDegrees) / 2;

        for(int i = 0; i < numSensors; i++){
            float obstacleDist = 20000.0f;
            float value = 0.0f;
            float angle = startAngle + i * incAngle;
            Vector3 rayDir =  Quaternion.AngleAxis(angle, Vector3.up) * forward;

            for(int j = 0; j < numAgents_; j++){
                if(j == id){
                    continue;
                }
                if(!agentActive_[j]){
                    continue;
                }
                // check if ray intersects another agent
                // own radius used, as agent self-determines how far they should be
                obstacleDist = Mathf.Min(obstacleDist, intersectRaySphere(agents_[id].pos, rayDir, agents_[j].pos, agents_[id].radius)); 
            }

            //check if colliding with 
            obstacleDist = Mathf.Min(obstacleDist, intersectRayLineSegment(agents_[id].pos, rayDir, 
                    areaMinBound_, new Vector3(areaMinBound_.x, 0, areaMaxBound_.z))); 
            obstacleDist = Mathf.Min(obstacleDist, intersectRayLineSegment(agents_[id].pos, rayDir, 
                    areaMinBound_, new Vector3(areaMaxBound_.x, 0, areaMinBound_.z)));
            obstacleDist = Mathf.Min(obstacleDist, intersectRayLineSegment(agents_[id].pos, rayDir, 
                    areaMaxBound_, new Vector3(areaMinBound_.x, 0, areaMaxBound_.z))); 
            obstacleDist = Mathf.Min(obstacleDist, intersectRayLineSegment(agents_[id].pos, rayDir, 
                    areaMaxBound_, new Vector3(areaMaxBound_.x, 0, areaMinBound_.z)));
                     
            if(value < 20000.0f){
                value = sensorLength / (1 + obstacleDist); 
            }
            
            ret.Add(value);
        }

        return ret;
    }

    public float intersectRaySphere(Vector3 origin, Vector3 direction, Vector3 center, float radius){
        float lambda = 20000.0f;
        // solving for ax^2 + bx + c = 0
        float a = direction.sqrMagnitude;
        float b = -2.0f * Vector3.Dot(center - origin, direction);
        float c = (center - origin).sqrMagnitude - radius * radius;

        float discr = b*b - 4*a*c;
        if(discr > 0){
            lambda = (-b - Mathf.Sqrt(discr)) / (2*a);
            if(lambda < 0){
                lambda = (-b + Mathf.Sqrt(discr)) / (2*a);
                if(lambda < 0){
                   return 20000.0f;
                }
            }
        }

        return lambda;
    }

    public float intersectRayLineSegment(Vector3 origin, Vector3 direction, Vector3 p1, Vector3 p2){
        float lambda = 20000.0f;
        float dir1 = Vector3.Dot(Vector3.up, Vector3.Cross(p1 - origin, direction));
        float dir2 = Vector3.Dot(Vector3.up, Vector3.Cross(p2 - origin, direction));
        
        if(dir1 < 0.000001f){
            lambda = (p1 - origin).magnitude;
        }
        if(dir2 < 0.000001f){
            lambda = (p2 - origin).magnitude;
        }
        if(dir1 * dir2 < 0){
            return (Vector3.Lerp(p1, p2, Mathf.Abs(dir1) / (Mathf.Abs(dir1) + Mathf.Abs(dir2))) - origin).magnitude;
        }
        return lambda;
    }

}