using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathFunctionTester : MonoBehaviour 
{
    public Transform Point1;
    public Transform Point2;
    public Transform CapsuleThis;
    public Transform Lalala;

    public float Distance;

    public bool WithinBounds;

    void Start () 
    {
		
	}
	
	void Update () 
    {
        //Distance = DistanceTo(Point1.position, Point2.position, Lalala.position);
        WithinBounds = IsWithinSegmentBounds(Point1.position, Point2.position, Lalala.position);
    }

    internal float DistanceTo(Vector3 edgeVertA, Vector3 edgeVertB, Vector3 point)
    {
        Vector3 normal = (edgeVertA - edgeVertB).normalized;
        Vector3 vect = point - edgeVertA;
        Vector3 projectedPoint = Vector3.Project(vect, normal) + edgeVertA;

        float distTo0 = (edgeVertA - point).magnitude;
        float distTo1 = (edgeVertB - point).magnitude;

        float length = (edgeVertA - edgeVertB).magnitude;

        if (distTo0 > length)
        {
            projectedPoint = edgeVertB;
        }
        if (distTo1 > length)
        {
            projectedPoint = edgeVertA;
        }


        CapsuleThis.position = projectedPoint;
        return (point - projectedPoint).magnitude;
    }

    private bool IsWithinSegmentBounds(Vector3 edgeVertA, Vector3 edgeVertB, Vector3 point)
    {
        Vector3 normal = (edgeVertA - edgeVertB).normalized;
        Vector3 vect = point - edgeVertA;
        Vector3 projectedPoint = Vector3.Project(vect, normal) + edgeVertA;
        float length = (edgeVertA - edgeVertB).magnitude;

        float distTo0 = (edgeVertA - projectedPoint).magnitude;
        float distTo1 = (edgeVertB - projectedPoint).magnitude;

        CapsuleThis.position = projectedPoint;

        return (distTo0 < length && distTo1 < length);
    }
}
