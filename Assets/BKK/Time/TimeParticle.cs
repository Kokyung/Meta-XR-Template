using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeParticle : TimeObject
{
    protected override void Record()
    {
        
    }

    protected override void Rewind()
    {
        
    }
}

[System.Serializable]
public class TimeParticleRecordData
{
    public float time;
}
