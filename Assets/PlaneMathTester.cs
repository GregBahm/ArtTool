using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneMathTester : MonoBehaviour 
{
    public float Output;

	void Update () 
    {
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        Output = plane.GetDistanceToPoint(transform.position);
	}
}
