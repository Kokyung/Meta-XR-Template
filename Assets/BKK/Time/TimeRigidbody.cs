using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeRigidbody : TimeObject
{
    protected override void Record()
    {
        
    }

    protected override void Rewind()
    {
        
    }
}

[System.Serializable]
public class TimeRigidbodyRecordData
{
    public TimeTransform transform;
    public Vector3 velocity;
    public Vector3 angularVelocity;
}