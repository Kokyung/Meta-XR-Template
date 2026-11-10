using System.Collections.Generic;
using UnityEngine;

public abstract class TimeObject : MonoBehaviour
{
    private bool isRewinding = false;
    
    private void Awake()
    {
        TimeManager.Instance.AddTimeObject(this);
    }

    private void FixedUpdate()
    {
        if (isRewinding) Rewind();
        else Record();
    }

    private void OnDestroy()
    {
        TimeManager.Instance.RemoveTimeObject(this);
    }

    protected abstract void Record();

    protected abstract void Rewind();

    public virtual void StartRewind()
    {
        isRewinding = true;
    }

    public virtual void StopRewind()
    {
        isRewinding = false;
    }

    public bool IsRewinding()
    {
        return isRewinding;
    }
}
