using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public abstract class Paintable : MonoBehaviour
{
    public abstract void AddPoint(Vector3 point, bool previewMode);
}
