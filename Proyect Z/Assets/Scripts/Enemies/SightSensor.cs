using UnityEngine;

[RequireComponent(typeof(LineRenderer))]

public class SightSensor : MonoBehaviour
{
    [Header("Vision Parameters")]
    public float viewDistance = 15f;
    [Range(0, 180)]
    public float viewAngle = 90f;

    [Header("Layers")]
    public LayerMask targetMask;     // Player
    public LayerMask obstacleMask;   // Paredes, props, etc.

    [Header("Debug")]
    public bool drawGizmos = true;

    [Header("Runtime Debug")]
    public bool drawRuntimeVision = true;
    [Range(5, 60)]
    public int visionResolution = 30;

    public Color idleColor = Color.yellow;
    public Color detectedColor = Color.red;

    private LineRenderer lineRenderer;
    private bool targetVisible;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;
    }

    private void Update()
    {
        if (!drawRuntimeVision)
        {
            lineRenderer.enabled = false;
            return;
        }

        DrawVisionCone(lineRenderer, viewDistance, viewAngle, visionResolution);
    }

    void DrawVisionCone(LineRenderer lr, float viewDistance, float viewAngle, int segments)
    {
        lr.useWorldSpace = true;
        lr.loop = false;

        int pointCount = segments + 3;
        lr.positionCount = pointCount;

        Vector3 origin = transform.position;
        lr.SetPosition(0, origin);

        float halfAngle = viewAngle * 0.5f;
        float angleStep = viewAngle / segments;

        // Borde izquierdo
        Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * transform.forward;
        lr.SetPosition(1, origin + leftDir * viewDistance);

        // Arco
        for (int i = 0; i <= segments; i++)
        {
            float angle = -halfAngle + angleStep * i;
            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;
            lr.SetPosition(i + 2, origin + dir * viewDistance);
        }

        // Cerrar el cono volviendo al origen
        lr.SetPosition(pointCount - 1, origin);
    }


    /// <summary>
    /// Devuelve true si el objetivo esta dentro del campo visual y sin obstaculos
    /// </summary>
    public bool CanSeeTarget(Transform target)
    {
        Vector3 dirToTarget = target.position - transform.position;
        float distance = dirToTarget.magnitude;

        // Distancia
        if (distance > viewDistance)
            return false;

        // Ángulo
        float angle = Vector3.Angle(transform.forward, dirToTarget);
        if (angle > viewAngle * 0.5f)
            return false;

        // Obstáculos
        if (Physics.Raycast(
            transform.position,
            dirToTarget.normalized,
            distance,
            obstacleMask))
        {
            return false;
        }

        targetVisible = true;
        return true;
    }

    // DEBUG VISUAL
    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        Vector3 left = Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + left * viewDistance);
        Gizmos.DrawLine(transform.position, transform.position + right * viewDistance);
    }
}
