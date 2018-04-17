using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SocialWalker
{
    public Vector3 x;
    public Vector3 v;
    public Vector3 a;

    public void init(Vector3 x_, Vector3 v_, Vector3 a_)
    {
        x = x_;
        v = v_;
        a = a_;
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
    private float bound = 10f;

    void Start()
    {
        
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
        for(int i = 0; i < numAgents_; i++)
        {
            float hue = (float)i / numAgents_;
            Color newColor = Color.HSVToRGB(hue, 1f, 1f);
            Color newColorDark = Color.HSVToRGB(hue, 1f, 0.5f);
            
            GameObject walkerClone = Instantiate(walkerPrefab_, new Vector3(Random.Range(1 - bound, bound - 1), 0.5f, Random.Range(1 - bound, bound - 1)), Quaternion.identity);
            walkerClone.GetComponent<MeshRenderer>().material.color = newColor;

            GameObject targetClone = Instantiate(targetPrefab_, new Vector3(Random.Range(1 - bound, bound - 1), 0.5f, Random.Range(1 - bound, bound - 1)), Quaternion.identity);
            targetClone.GetComponent<MeshRenderer>().material.color = newColorDark;

            //Brain brainClone = Instantiate(socialWalkerBrain_.GetComponent<Brain>());
            //walkerClone.GetComponent<SocialWalkerAgent>().brain = brainClone;
            walkerClone.GetComponent<SocialWalkerAgent>().brain = socialWalkerBrain_.GetComponent<Brain>();
            walkerClone.GetComponent<SocialWalkerAgent>().target = targetClone;
        }
    }

}