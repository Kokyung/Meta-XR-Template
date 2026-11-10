using UnityEngine;

public static class CameraExtension
{
    public static bool IsInCamera(this Renderer renderer, Camera cam)
    {
        return renderer.bounds.IsInCamera(cam);
    }

    public static bool IsInCamera(this Collider collider, Camera cam)
    {
        return collider.bounds.IsInCamera(cam);
    }

    public static bool IsInCamera(this Bounds bounds, Camera cam)
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);

        return GeometryUtility.TestPlanesAABB(planes, bounds);
    }

    // 카메라 없이 체크하는 함수 제작중.
    // 아직 제대로 작동 안함.
    public static bool IsInSight(this Bounds bounds, ref Matrix4x4 matrix, Transform eye, float fov, float aspect,
        float zNear, float zFar)
    {
        // matrix = Matrix4x4.TRS(eye.position, eye.rotation, eye.lossyScale);
        matrix = Matrix4x4.Perspective(fov, aspect, zNear, zFar);
        matrix.SetTRS(eye.position, eye.rotation, eye.localScale);
        matrix = matrix.inverse;

        // Debug.Log($"P{matrix.GetPosition()} R{matrix.GetRotation()} S{matrix.GetScale()}");

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(matrix);

        return GeometryUtility.TestPlanesAABB(planes, bounds);
    }

    public static void IsMoving(this Camera cam)
    {
        var buffer = cam.targetTexture.GetNativeTexturePtr();
    }
}