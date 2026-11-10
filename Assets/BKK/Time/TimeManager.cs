using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 시간 관련 기능을 모아놓은 매니저입니다.
///
/// [ 참고 ]
/// SlowMotion: https://www.youtube.com/watch?v=0VGosgaoTsw
/// </summary>
public class TimeManager : Singleton<TimeManager>
{
    public float currentTimeScale => Time.timeScale;

    [SerializeField, Range(0.0f, 3.0f)] private float defaultTimeScale = 1.0f;

    private float resultTimeScale = 1f;

    private float beforeFixedDeltaTime;
    
    private bool slowMotionActivated = false;

    private HashSet<TimeObject> timeObjects = new HashSet<TimeObject>();

    private UnityEvent onSlowMotionStart = new UnityEvent();
    private UnityEvent onSlowMotionStop = new UnityEvent();

    public UnityEvent OnSlowMotionStart => onSlowMotionStart;
    public UnityEvent OnSlowMotionStop => onSlowMotionStop;

    #region MonoBehaviour

    private void Awake()
    {
        ChangeTimeScale(defaultTimeScale);
    }

    private void OnValidate()
    {
        ChangeTimeScale(defaultTimeScale);
    }

    private void FixedUpdate()
    {
        ApplyTimeScale();
    }

    private void Reset()
    {
        ChangeTimeScale(defaultTimeScale);
    }

    #endregion

    #region Rewind

    public void AddTimeObject(TimeObject timeObject)
    {
        timeObjects.Add(timeObject);
    }
    
    public void RemoveTimeObject(TimeObject timeObject)
    {
        timeObjects.Remove(timeObject);
    }

    public void StartRewind()
    {
        foreach (var timeObject in timeObjects)
        {
            timeObject.StartRewind();
        }
    }
    
    public void StopRewind()
    {
        foreach (var timeObject in timeObjects)
        {
            timeObject.StopRewind();
        }
    }

    #endregion

    #region TimeScale

    private void ApplyTimeScale()
    {
        ClampTimeScale();
        Time.timeScale = resultTimeScale;
    }

    private void ClampTimeScale()
    {
        resultTimeScale = Mathf.Clamp(resultTimeScale, 0, defaultTimeScale);
    }

    public void ChangeTimeScale(float timeScale)
    {
        resultTimeScale = timeScale;
    }

    public void AddTimeScale(float timeScale)
    {
        resultTimeScale += timeScale;
    }
    
    public void RemoveTimeScale(float timeScale)
    {
        resultTimeScale -= timeScale;
    }

    #endregion
    
    #region Slow Motion

    public async void DoSlowMotion(float timeScale, float dampTime)
    {
        if (slowMotionActivated) return;

        slowMotionActivated = true;
        
        resultTimeScale = timeScale;
        
        beforeFixedDeltaTime = Time.fixedDeltaTime;
        Time.fixedDeltaTime = timeScale * 0.02f;

        do
        {
            if (dampTime != 0) resultTimeScale += (defaultTimeScale / dampTime) * Time.unscaledDeltaTime;
            else break;
            
            await UniTask.Yield();
        } while (resultTimeScale < defaultTimeScale);
        
        Time.fixedDeltaTime = beforeFixedDeltaTime;
        slowMotionActivated = false;
    }
    
    public async void StartSlowMotion(float timeScale, float dampTime)
    {
        if (slowMotionActivated) return;
        
        slowMotionActivated = true;
        
        beforeFixedDeltaTime = Time.fixedDeltaTime;
        
        Time.fixedDeltaTime = timeScale * 0.02f;
        
        onSlowMotionStart?.Invoke();
        
        do
        {
            if (dampTime != 0) resultTimeScale -= (1f / dampTime) * Time.unscaledDeltaTime;
            else break;
            
            await UniTask.Yield();
        } while (resultTimeScale > timeScale);

        resultTimeScale = timeScale;
    }

    public async void StopSlowMotion(float dampTime)
    {
        if (!slowMotionActivated) return;
        
        onSlowMotionStop?.Invoke();

        do
        {
            if (dampTime != 0) resultTimeScale += (1f / dampTime) * Time.unscaledDeltaTime;
            else break;
            
            await UniTask.Yield();
        } while (resultTimeScale < defaultTimeScale);

        resultTimeScale = defaultTimeScale;
        Time.fixedDeltaTime = beforeFixedDeltaTime;
        slowMotionActivated = false;
    }

    #endregion
}

public interface ITimeSpeedAdjustable
{
    public void AdjustTimeSpeed(float speed);
}

