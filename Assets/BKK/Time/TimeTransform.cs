using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeTransform : TimeObject
{
    protected override void Record()
    {
        
    }

    protected override void Rewind()
    {
        
    }
}

[System.Serializable]
public class TimeTransformRecordData
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
}
