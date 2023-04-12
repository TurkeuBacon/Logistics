using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothMinTest : MonoBehaviour
{
    public int resolution;
    public AnimationCurve curve;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float dt = 1f/resolution;
        Keyframe[] points = curve.keys;
        for(float i = 0; i <= 1f; i += dt)
        {
            Keyframe point1, point2;
            for(int j = 1; j < points.Length; j++)
            {
                
            }
        }
        for(int i = 1; i < points.Length; i++)
        {
            Debug.DrawLine(new Vector3(points[i-1].time, points[i-1].value)+Vector3.up, new Vector3(points[i].time, points[i].value)+Vector3.up, Color.red);
        }
    }

    private float smoothMin(float a)
    {
        return a;
    }
    private float smoothMax(float a)
    {
        return a;
    }
    
}
