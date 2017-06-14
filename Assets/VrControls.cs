using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VrControls : MonoBehaviour
{
    public PaintbrushScript PaintBrush;
    public Transform MeshTransform;

    private void Start()
    {
        PaintBrush.enabled = true;   
    }

	void Update ()
    {
        bool leftTrigger = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
        bool leftHand = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch);
        PaintBrush.Painting = leftTrigger;
        if(leftHand)
        {
            MeshTransform.parent = PaintBrush.transform;
        }
        else
        {
            MeshTransform.parent = null;
        }
    }
}
