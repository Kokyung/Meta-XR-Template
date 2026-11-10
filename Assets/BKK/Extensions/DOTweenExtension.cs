using UnityEngine;

#if DOTWEEN
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
#endif

public static class DOTweenExtension
{
#if DOTWEEN
    public static void DOMoveBKK(this Transform from, ref TweenerCore<Vector3, Vector3, VectorOptions> tween,
        Vector3 toPosition, float duration, bool autoKill = false, UpdateType updateType = UpdateType.Late,
        bool speedBased = false)
    {
        if (tween == null || !tween.IsActive())
        {
            tween = from
                .DOMove(toPosition, duration, false)
                .SetUpdate(updateType)
                .Blendable()
                .SetEase(Ease.Linear)
                .SetAutoKill(autoKill)
                .SetSpeedBased(speedBased)
                .SetRecyclable();
        }
        else
        {
            lock (tween)
            {
                tween.ChangeValues(from.position, toPosition, duration).Play();
            }
        }

        if (tween.Duration() == 0) tween.Done();
    }

    public static void DORotateBKK(this Transform from, ref TweenerCore<Quaternion, Vector3, QuaternionOptions> tween,
        Vector3 toEulerAngles, float duration, RotateMode rotateMode = RotateMode.Fast, bool autoKill = false,
        UpdateType updateType = UpdateType.Late, bool speedBased = false)
    {
        if (!tween.IsActive()) // Tween이 Null이거나 활성 중이 아닐 때
        {
            tween = from
                .DORotate(toEulerAngles, duration, rotateMode)// 회전
                .SetUpdate(updateType)// 업데이트 모드
                .Blendable()// 블렌딩 가능하게
                .SetEase(Ease.Linear)// 직선 방향으로 이동
                .SetAutoKill(autoKill)// Auto Kill 여부
                .SetSpeedBased(speedBased)
                .SetRecyclable();// GC 방지를 위해 재활용 가능하게
        }
        else // Tween이 존재하거나 활성 중이면
        {
            lock (tween)// 트윈 호출이 완료될 때까지 대기
            {
                // 트윈 값을 변경 후 재생
                tween.ChangeValues(from.eulerAngles, toEulerAngles, duration).Play();
            }
        }
        
        if (tween.Duration() == 0) tween.Done();
    }

    public static void DORotateQuaternionBKK(this Transform from, ref TweenerCore<Quaternion, Quaternion, NoOptions> tween, Quaternion toQuaternion, float duration, RotateMode rotateMode
 = RotateMode.Fast, bool autoKill = false, UpdateType updateType = UpdateType.Late)
    {
        if (tween == null || !tween.IsActive())
        {
            tween = from
                .DORotateQuaternion(toQuaternion, duration)
                .SetUpdate(updateType)
                .Blendable()
                .SetEase(Ease.Linear)
                .SetAutoKill(autoKill)
                .SetRecyclable();
        }
        else
        {
            lock (tween)
            {
                tween.ChangeValues(from.rotation, toQuaternion, duration).Play();
            }
        }
        
        if (tween.Duration() == 0) tween.Done();
    }
    
    public static void DOLocalRotateBKK(this Transform from, ref TweenerCore<Quaternion, Vector3, QuaternionOptions> tween, Vector3 toEulerAngles, float duration, RotateMode rotateMode
 = RotateMode.Fast, bool autoKill = false, UpdateType updateType = UpdateType.Late)
    {
        if (tween == null || !tween.IsActive())
        {
            tween = from
                .DOLocalRotate(toEulerAngles, duration, rotateMode)
                .SetUpdate(updateType)
                .Blendable()
                .SetEase(Ease.Linear)
                .SetAutoKill(autoKill)
                .SetRecyclable();
        }
        else
        {
            lock (tween)
            {
                tween.ChangeValues(from.eulerAngles, toEulerAngles, duration).Play();
            }
        }
        
        if (tween.Duration() == 0) tween.Done();
    }
#endif
}