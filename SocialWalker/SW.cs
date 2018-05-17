using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public struct SW
{
    public Vector3 pos;
    public Vector3 forward;
    public Vector3 target;
    public float speed;
    public float acc;
    public float radius;
    public float targetRadius;
    const float maxSpeed = 0.1f;
    public void init(Vector3 x_, Vector3 target_)
    {
        pos = x_;
        forward = new Vector3(0f, 0f, 1f);
        target = target_;
        speed = 0.0f;
        acc = 0.0f;
        radius = 1.0f;
        targetRadius = 0.5f;
        //maxSpeed = 0.2f;
        //minSpeed = 0.001f;
    }

    // accelerate in the direction of forward velocity
    public void walkForward(){ 
        acc += 0.01f;
        move();
    }

    public void turnRight(){
        // Since our plane is the XZ plane, we take the cross product of the forward direction with the up direction (0, 1, 0)
        // to get the right direction 
        forward = Quaternion.AngleAxis(1.0f, Vector3.up) * forward;
        move();
    }

    public void turnLeft(){
        // Since our plane is the XZ plane, we take the cross product of the forward direction with the up direction (0, 1, 0)
        // to get the right direction 
        forward = Quaternion.AngleAxis(-1.0f, Vector3.up) * forward;
        move();
    }

    public void Brake(){
        acc -= 0.01f;
        move();
    }

    public void move(){
        speed = speed + acc;
        
        if(speed < 0.0f){
            acc = 0.0f;
            speed = 0.0f;
        }

        if(speed > maxSpeed){
            speed = maxSpeed;
        }

        //Debug.Log("Accelerating Forward");
        pos = pos + forward * speed; // unit time
    }

    public bool targetReached(){
        Vector3 d = target - pos;
        if(d.magnitude < targetRadius){
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

    public bool isCollidingWith(SW sw)
    {
        Vector3 d = pos - sw.pos;
        //Debug.Log("distance between agents " + d.magnitude);
        if(sw.radius > d.magnitude)
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