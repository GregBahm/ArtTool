using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaintbrushScript : MonoBehaviour 
{
    public Paintable Mothership;

    public float MoveThreshold = .1f;

    private Vector3 lastPaintPoint;

    public bool Painting;

    private void Start()
    {
        lastPaintPoint = transform.position;
    }

    private void Update()
    {
        Vector3 currentPos = transform.position;
        float dist = (lastPaintPoint - currentPos).magnitude;
        //if(dist > MoveThreshold)
        //{
            lastPaintPoint = transform.position;
            
            Mothership.AddPoint(Mothership.transform.InverseTransformPoint(currentPos), !Painting);
        //}
    }
}
