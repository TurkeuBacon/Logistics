using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public float verticalRotationSpeed;
    public int minRotation, maxRotation;

    private float verticalRotation;
    private const int ROTATION_OFFSET = 90;
    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        verticalRotation = transform.rotation.eulerAngles.x + ROTATION_OFFSET;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 rotation = transform.rotation.eulerAngles;
        float verticalMouse = -Input.GetAxis("Mouse Y");
        verticalRotation += verticalMouse * verticalRotationSpeed * Time.deltaTime;
        verticalRotation = Mathf.Clamp(verticalRotation, minRotation + ROTATION_OFFSET, maxRotation + ROTATION_OFFSET);
        //Debug.Log("Vertical Rotation: " + verticalRotation + " (" + (minRotation + ROTATION_OFFSET) +  ", " + (maxRotation + ROTATION_OFFSET) + ")");

        transform.rotation = Quaternion.Euler(new Vector3(verticalRotation - ROTATION_OFFSET, rotation.y, rotation.z));
    }
}
