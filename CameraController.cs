using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject MoveTo = null;
    public GameObject LookAt = null;
    public float MoveSpeed = 4;
    public float LookSpeed = 320;
    private float RecallCameraY = 0; 

    private void Start()
    {
        //the RecallCameraY is so the camera doesn't flip with the car and go under the ground by accident
        RecallCameraY = Mathf.Abs(MoveTo.transform.position.y - LookAt.transform.position.y);
    }
    private void LateUpdate()
    {
        //the new position, but keep the camera at original y so it doesn't flip with the car
        float moveTargetY = LookAt.transform.position.y + RecallCameraY;
        Vector3 cameraMovePos = new Vector3(MoveTo.transform.position.x, moveTargetY, MoveTo.transform.position.z);
        this.transform.position = Vector3.Lerp(this.transform.position, cameraMovePos, MoveSpeed * Time.deltaTime);

        //rotation
        Quaternion rotTarget = Quaternion.LookRotation(LookAt.transform.position - this.transform.position);
        this.transform.rotation = Quaternion.RotateTowards(this.transform.rotation, rotTarget, LookSpeed * Time.deltaTime);

    }
}
