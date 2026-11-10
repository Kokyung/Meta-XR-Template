using System;
using BKK.Filter.OneEuro;
using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    public enum InterpolationMethod
    {
        None,
        Lerp,
        Slerp,
        OneEuro,
    }
    
    public enum UpdateMode
    {
        FixedUpdate,
        Update,
        LateUpdate
    }
    
    public Transform target;
    public InterpolationMethod interpolationMethod = InterpolationMethod.None;
    public UpdateMode updateMode = UpdateMode.LateUpdate;
    [Min(0)] public float lerpSpeed = 50f;
    public OneEuroFilterTransform targetFilter;

    private void Awake()
    {
        targetFilter = new OneEuroFilterTransform(new[] {target});
        targetFilter.UpdateProperties();
    }

    private void FixedUpdate()
    {
        if (updateMode != UpdateMode.FixedUpdate) return;

        UpdateMove();
    }

    private void Update()
    {
        if (updateMode != UpdateMode.Update) return;
        
        UpdateMove();
    }

    private void LateUpdate()
    {
        if (updateMode != UpdateMode.LateUpdate) return;
        
        UpdateMove();
    }

    private void UpdateMove()
    {
        if (target == null) return;

        switch (interpolationMethod)
        {
            case InterpolationMethod.None:
                LinearMove();
                break;
            case InterpolationMethod.Lerp:
                LerpMove();
                break;
            case InterpolationMethod.Slerp:
                SLerpMove();
                break;
            case InterpolationMethod.OneEuro:
                FilteringMove();
                break;
            default:
                LinearMove();
                break;
        }
    }

    private void LinearMove()
    {
        transform.position = target.position;
        transform.rotation = target.rotation;
    }

    private void LerpMove()
    {
        transform.position = Vector3.Lerp(transform.position, target.position, Time.deltaTime * lerpSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, target.rotation, Time.deltaTime * lerpSpeed);
    }
    
    private void SLerpMove()
    {
        transform.position = Vector3.Slerp(transform.position, target.position, Time.deltaTime * lerpSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, Time.deltaTime * lerpSpeed);
    }

    private void FilteringMove()
    {
        targetFilter.UpdateProperties();
        targetFilter.Filters[target.name].Filter(target, transform);
    }
}
