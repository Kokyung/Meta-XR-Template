using UnityEngine;

public static class MathExtension
{
    /// <summary>
    /// 두 쿼터니언 간에 사인 각도 계산
    /// 출처 참고
    /// https://discussions.unity.com/t/angles-from-quaternion-vector-problem/90181/3
    /// </summary>
    /// <param name="A"></param>
    /// <param name="B"></param>
    /// <param name="axis"></param>
    /// <returns></returns>
    public static float GetSignedAngle(Quaternion A, Quaternion B, Vector3 axis)
    {
        float angle = 0f;
        Vector3 angleAxis = Vector3.zero;
        
        (B * Quaternion.Inverse(A)).ToAngleAxis(out angle, out angleAxis);
        if (Vector3.Angle(axis, angleAxis) > 90f)
        {
            angle = -angle;
        }

        return Mathf.DeltaAngle(0f, angle);
    }

    /// <summary>
    /// aloLow와 oldHigh 사이의 input을 newLow와 newHigh 사이의 수로 리매핑합니다.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="oldLow"></param>
    /// <param name="oldHigh"></param>
    /// <param name="newLow"></param>
    /// <param name="newHigh"></param>
    /// <returns></returns>
    public static float Remap(this float input, float oldLow, float oldHigh, float newLow, float newHigh)
    {
        var t = Mathf.InverseLerp(oldLow, oldHigh, input);
        return Mathf.Lerp(newLow, newHigh, t);
    }

    public static Vector3 DirectionTo(this Vector3 from, Vector3 to)
    {
        return (to - from);
    }

    public static float Distance(this Vector3 from, Vector3 to)
    {
        return (to - from).magnitude;
    }
}
