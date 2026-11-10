using UnityEngine;

public static class PhysicsUtility
{
    private static readonly RaycastHitDistanceComparer distanceComparer =
        new RaycastHitDistanceComparer();

    public static int RaycastAllSorted(Ray ray, RaycastHit[] hits, float distance, LayerMask layerMask,
        bool sphereCast = false, float radius = 1f)
    {
        int hitCount = 0;
        if (sphereCast) hitCount = UnityEngine.Physics.SphereCastNonAlloc(ray, radius, hits, distance, layerMask);
        else hitCount = UnityEngine.Physics.RaycastNonAlloc(ray, hits, distance, layerMask);
        if (hitCount > 0) System.Array.Sort(hits, 0, hitCount, distanceComparer);
        return hitCount;
    }

    public class RaycastHitDistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
    {
        public int Compare(RaycastHit a, RaycastHit b)
        {
            return a.distance.CompareTo(b.distance);
        }
    }

    public static void GetJointSpringDamper(ref float spring, ref float damper, Rigidbody rigidbody,
        float positionFrequency, float positionDampingRatio)
    {
        // 감쇠 조화 진동자에 작용하는 힘을 구하는 방정식(예: 용수철)
        // F = (-k * x) - (c * v)
        // 여기서 k와 c값을 찾아야함.
        // k: positionSpring(강성)
        // c: positionDamper(감쇠)
        // m: 물체의 질량
        // x: 목표 위치와의 거리
        // v: 속도
        // frequency: 반응 속도
        // Omega: 고유 진동수 = Sqrt(k / m) = 2 * PI * frequency (두 식은 똑같이 고유 진동수가 도출되지만 후자는 frequency 조절이 됨)
        // k = Pow(Omega) * m (상단 Omega 두 식을 이용해서 k 공식 도출)
        // zeta: 떨림 정도(dampingRatio)
        // c = 2 * zeta * Sqrt(k * m) = 2 * zeta * m * omega (Omega = Sqrt(k / m) 식을 이용하여 도출)
        var omega = 2f * Mathf.PI * positionFrequency;
        spring = rigidbody.mass * omega * omega;
        damper = 2f * positionDampingRatio * rigidbody.mass * omega;
    }

    public static bool IsScriptMode()
    {
        return UnityEngine.Physics.simulationMode == SimulationMode.Script;
    }
}
