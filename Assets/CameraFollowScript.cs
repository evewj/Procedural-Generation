using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowScript : MonoBehaviour
{
    public Transform targetObject;
    public float smoothFactor = 0.5f;
    public bool lookAtTarget = true;
    public float cameraBehindDistance = 5f;
    public float cameraAboveDistance = 2f;
    

    // Start is called before the first frame update
    void Start()
    {

    }

    void LateUpdate()
    {
        Vector3 newPosition = targetObject.transform.position - targetObject.transform.forward * cameraBehindDistance + targetObject.transform.up * cameraAboveDistance;
        transform.position = Vector3.Slerp(transform.position, newPosition, smoothFactor);
        if (lookAtTarget)
        {
            transform.LookAt(targetObject);
        }        
    }
}
