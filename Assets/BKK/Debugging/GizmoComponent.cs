using UnityEngine;

public class GizmoComponent : MonoBehaviour
{
    public GizmoShape gizmoShape = GizmoShape.Sphere;
    public Color color = Color.green;
    [Range(0.00f, 1.00f)] public float radius = 0.05f;
    public Vector3 size = Vector3.one * 0.05f;
    public Mesh mesh;

    public bool wired = false;
    public bool showWhenSelected = false;

    private void OnDrawGizmos()
    {
        if(!showWhenSelected) Draw();
    }

    private void OnDrawGizmosSelected()
    {
        if(showWhenSelected) Draw();
    }

    private void Draw()
    {
        Gizmos.color = color;
        switch (gizmoShape)
        {
            case GizmoShape.Sphere:
                if (wired)
                {
                    Gizmos.DrawWireSphere(transform.position, radius);
                }
                else
                {
                    Gizmos.DrawSphere(transform.position, radius);
                }
                
                break;
            case GizmoShape.Cube:
                if (wired)
                {
                    Gizmos.DrawWireCube(transform.position, size);
                }
                else
                {
                    Gizmos.DrawCube(transform.position, size);
                }
                
                break;
            case GizmoShape.Mesh:
                if (wired)
                {
                    Gizmos.DrawWireMesh(mesh, transform.position);
                }
                else
                {
                    Gizmos.DrawMesh(mesh, transform.position);
                }
                
                break;
            default:
                break;
        }
    }
}

public enum GizmoShape
{
    Sphere,
    Cube,
    Mesh,
}
