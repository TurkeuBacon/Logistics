using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float forwardAcceleration, forwardDeceleration, forwardSpeed,
        backwardAcceleration, backwardDeceleration, backwardSpeed,
        horizontalRotationSpeed;

    private Vector3 inputs;
    private Vector3 localVelocity;
    private Rigidbody rb;
    private Transform cameraTransform;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cameraTransform = GetComponent<Rigidbody>().gameObject.transform;
    }


    void Update()
    {
        #region Movement Inputs
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        inputs = new Vector3(horizontal, 0f, vertical);
        #endregion

        #region Rotation
        float horizontalMouse = Input.GetAxis("Mouse X");
        float horizontalRotation = horizontalMouse * horizontalRotationSpeed * Time.deltaTime;
        transform.Rotate(0f, horizontalRotation, 0f);
        #endregion
    }

    void FixedUpdate()
    {
        if(localVelocity.z <= forwardSpeed && localVelocity.z >= -backwardSpeed)
        {
            if(inputs.z < 0)
            {
                localVelocity += Vector3.forward * -backwardAcceleration * Time.fixedDeltaTime;
            }
            else if(inputs.z > 0)
            {
                localVelocity += Vector3.forward * forwardAcceleration * Time.fixedDeltaTime;
            }
            else if(localVelocity.z != 0)
            {
                Vector3 preLocalVelocity = localVelocity;
                localVelocity += Vector3.forward * (localVelocity.z > 0 ? -forwardDeceleration : backwardDeceleration) * Time.fixedDeltaTime;
                if((preLocalVelocity.z > 0 && localVelocity.z < 0) || (preLocalVelocity.z > 0 && localVelocity.z < 0)) localVelocity.z = 0f;
            }
        }
        if(localVelocity.z > forwardSpeed) localVelocity.z = forwardSpeed;
        else if(localVelocity.z < -backwardSpeed) localVelocity.z = -backwardSpeed;
        //Debug.Log("local velocity" + localVelocity);

        rb.velocity = transform.rotation * localVelocity;
    }
}
