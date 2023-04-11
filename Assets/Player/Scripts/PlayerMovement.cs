using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float forwardAcceleration, forwardDeceleration, forwardSpeed,
        backwardAcceleration, backwardDeceleration, backwardSpeed,
        sidewaysAcceleration, sidewaysDeceleration, sidewaysSpeed,
        horizontalRotationSpeed;
    
    public float jumpHeight;

    public float playerGravity;
    [Range(0, 0.5f)]
    public float groundCheckDistance;
    [Range(0, 1.5708f)]
    public float groundCheckSideAngle;
    public bool grounded;
    public LayerMask gcLayerMask;

    private Vector3 inputs;
    private bool jumpInput;
    private int gcJumpFrameDelay = 0;

    private float forwardVelocity, sidewaysVelocity, verticalVelocity;
    private Vector3 localVelocity;
    private float lastYPosition;

    private float groundCheckSideHeight, groundCheckSideWidth;
    private Vector3[] gcOrigins, gcDirections;
    private float jumpVelocity;

    private Rigidbody rb;
    private Transform cameraTransform;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cameraTransform = GetComponent<Rigidbody>().gameObject.transform;
    }

    void Start()
    {
        forwardVelocity = sidewaysVelocity = 0;
        lastYPosition = transform.position.y;
        jumpVelocity = Mathf.Sqrt(2*playerGravity*jumpHeight);
        setGCSideValues(groundCheckSideAngle);
        grounded = false;
        FindObjectOfType<PauseMenuController>().playerSettingsApply += applySettings;
    }


    void Update()
    {
        /* VV Uncomment to edit ground check rays at runtime VV */
        // setGCSideValues(groundCheckSideAngle);
        debugGroundCheckRays();

        #region Movement Inputs
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        inputs = new Vector3(horizontal, 0f, vertical);
        #endregion

        #region Jump Input
        if(Input.GetButton("Jump"))
        {
            jumpInput = true;
        }
        #endregion

        #region Rotation
        float horizontalMouse = Input.GetAxis("Mouse X");
        float horizontalRotation = horizontalMouse * horizontalRotationSpeed * Time.deltaTime;
        transform.Rotate(0f, horizontalRotation, 0f);
        #endregion
    }

    void FixedUpdate()
    {
        #region Grounding Check / Ground Snap
        bool wasGrounded = grounded;
        if(gcJumpFrameDelay > 0)
        {
            gcJumpFrameDelay--;
        }
        else
        {
            checkIsGrounded();
        }
        if(!grounded)
        {
            if(wasGrounded && downhillCheck(groundCheckDistance*3f))
            {
                verticalVelocity = -5f;
                grounded = true;
            }
            else
            {
                if(verticalVelocity >= 0 || (transform.position.y - lastYPosition) / (verticalVelocity*Time.deltaTime) >= 0.5f)
                {
                    verticalVelocity -= playerGravity * Time.deltaTime;
                }
            }
        }
        else
        {
            verticalVelocity = 0f;
            if(jumpInput)
            {
                verticalVelocity = jumpVelocity;
                grounded = false;
                gcJumpFrameDelay = 2;
            }
        }
        #endregion

        #region Forward Movement Calculations
        if(inputs.z < 0)
        {
            forwardVelocity -= backwardAcceleration * Time.fixedDeltaTime;
        }
        else if(inputs.z > 0)
        {
            forwardVelocity += forwardAcceleration * Time.fixedDeltaTime;
        }
        else if(forwardVelocity != 0)
        {
            float preForwardVelocity = forwardVelocity;
            forwardVelocity += (forwardVelocity > 0 ? -forwardDeceleration : backwardDeceleration) * Time.fixedDeltaTime;
            if((preForwardVelocity > 0 && forwardVelocity < 0) || (preForwardVelocity > 0 && forwardVelocity < 0)) forwardVelocity = 0f;
        }
        if(forwardVelocity > forwardSpeed) forwardVelocity = forwardSpeed;
        else if(forwardVelocity < -backwardSpeed) forwardVelocity = -backwardSpeed;
        #endregion
        
        #region Sideways Movement Calculations
        if(inputs.x < 0)
        {
            sidewaysVelocity -= sidewaysAcceleration * Time.fixedDeltaTime;
        }
        else if(inputs.x > 0)
        {
            sidewaysVelocity += sidewaysAcceleration * Time.fixedDeltaTime;
        }
        else if(localVelocity.x != 0)
        {
            float preSidewaysVelocity = sidewaysVelocity;
            sidewaysVelocity += (sidewaysVelocity > 0 ? -forwardDeceleration : backwardDeceleration) * Time.fixedDeltaTime;
            if((preSidewaysVelocity > 0 && sidewaysVelocity < 0) || (preSidewaysVelocity > 0 && sidewaysVelocity < 0)) sidewaysVelocity = 0f;
        }
        if(sidewaysVelocity > sidewaysSpeed) sidewaysVelocity = sidewaysSpeed;
        else if(sidewaysVelocity < -sidewaysSpeed) sidewaysVelocity = -sidewaysSpeed;
        #endregion

        //Debug.Log("local velocity" + localVelocity);
        localVelocity = new Vector3(sidewaysVelocity, 0f, forwardVelocity);

        rb.velocity = transform.rotation * localVelocity + Vector3.up * verticalVelocity;
        lastYPosition = transform.position.y;
        jumpInput = false;
    }

    private void checkIsGrounded()
    {
        for(int i = 0; i < gcOrigins.Length; i++)
        {
            RaycastHit groundHit;
            Vector3 origin = transform.position + transform.rotation * gcOrigins[i];
            Vector3 direction = transform.rotation * gcDirections[i];
            if(Physics.Raycast(origin - direction*0.1f, direction, out groundHit, groundCheckDistance + 0.1f, gcLayerMask))
            {
                grounded = true;
                return;
            }
        }
        grounded = false;
    }
    private bool downhillCheck(float dist)
    {
        Vector3 origin = transform.position + transform.rotation * gcOrigins[0];
        Vector3 direction = transform.rotation * gcDirections[0];
        return Physics.Raycast(origin - direction*0.1f, direction, dist + 0.1f, gcLayerMask);
    }

    private void setGCSideValues(float angle)
    {
        gcOrigins = new Vector3[5];
        gcDirections = new Vector3[5];

        gcOrigins[0] = Vector3.zero;
        gcDirections[0] = Vector3.down;

        gcOrigins[1] = new Vector3(Mathf.Sin(angle), (1-Mathf.Cos(angle)), 0f) * 0.5f;
        gcDirections[1] = gcOrigins[1] * 2f - Vector3.up;

        gcOrigins[2] = new Vector3(-Mathf.Sin(angle), (1-Mathf.Cos(angle)), 0f) * 0.5f;
        gcDirections[2] = gcOrigins[2] * 2f - Vector3.up;

        gcOrigins[3] = new Vector3(0f, (1-Mathf.Cos(angle)), Mathf.Sin(angle)) * 0.5f;
        gcDirections[3] = gcOrigins[3] * 2f - Vector3.up;

        gcOrigins[4] = new Vector3(0f, (1-Mathf.Cos(angle)), -Mathf.Sin(angle)) * 0.5f;
        gcDirections[4] = gcOrigins[4] * 2f - Vector3.up;
    }
    private void debugGroundCheckRays()
    {
        Vector3 gcCenter = transform.position+Vector3.up * 0.5f;
        for(int i = 0; i < gcOrigins.Length; i++)
        {
            Vector3 gcStart = transform.position + transform.rotation * gcOrigins[i];
            Debug.DrawRay(gcStart, transform.rotation * gcDirections[i] * groundCheckDistance, Color.red);
        }
    }

    private void applySettings(params int[] settings)
    {
        horizontalRotationSpeed = settings[0];
        GetComponentInChildren<CameraControl>().verticalRotationSpeed = settings[0];
    }
}
