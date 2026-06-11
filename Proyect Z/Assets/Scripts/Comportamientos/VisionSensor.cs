using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class VisionSensor : MonoBehaviour
{
    [Header("Vision")]
    public float viewDistance = 15f;
    [Range(0, 180)] public float viewAngle = 90f;
    [Range(6, 60)] public int resolution = 30;

    [Header("Layers / Tags")]
    public LayerMask obstacleMask;         // bloqueadores (walls)
    public string targetTag = "Player";

    [Header("Debug")]
    public bool drawRuntime = true;
    public Color idleColor = Color.yellow;
    public Color detectedColor = Color.red;

    LineRenderer lr;
    bool targetVisible;

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = false;
        lr.positionCount = resolution + 3;
    }

    private void Update()
    {
        if (drawRuntime) DrawCone();
        else lr.enabled = false;
    }

    void DrawCone()
    {
        lr.enabled = true;
        Vector3 origin = transform.position;
        lr.SetPosition(0, origin);

        float half = viewAngle * 0.5f;
        float step = viewAngle / resolution;

        Vector3 leftDir = Quaternion.Euler(0, -half, 0) * transform.forward;
        lr.SetPosition(1, origin + leftDir * viewDistance);

        for (int i = 0; i <= resolution; i++)
        {
            float angle = -half + step * i;
            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;
            lr.SetPosition(i + 2, origin + dir * viewDistance);
        }

        lr.SetPosition(resolution + 2, origin);

        lr.startColor = lr.endColor = (targetVisible ? detectedColor : idleColor);
    }

    /// <summary>
    /// True si el transform con tag targetTag está dentro de cono y sin obstaculos
    /// </summary>
    public bool CanSeePlayer(out Transform playerTransform)
    {
        playerTransform = null;
        GameObject player = GameObject.FindGameObjectWithTag(targetTag);
        if (player == null) return false;

        Vector3 dir = player.transform.position - transform.position;
        float dist = dir.magnitude;
        if (dist > viewDistance) return false;

        float angle = Vector3.Angle(transform.forward, dir);
        if (angle > viewAngle * 0.5f) return false;

        // raycast hacia el jugador (levanta desde ojos ligeramente)
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 dirNorm = dir.normalized;
        if (Physics.Raycast(origin, dirNorm, out RaycastHit hit, dist, obstacleMask))
        {
            // si el primer hit no es el jugador, está bloqueado
            if (hit.collider.gameObject != player) return false;
        }

        playerTransform = player.transform;
        return true;
    }

    // helper
    public bool CanSeePlayerSimple(Transform player)
    {
        if (player == null) return false;
        Vector3 dir = player.position - transform.position;
        float dist = dir.magnitude;
        if (dist > viewDistance) return false;
        float angle = Vector3.Angle(transform.forward, dir);
        if (angle > viewAngle * 0.5f) return false;
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dist, obstacleMask))
        {
            if (hit.collider.transform != player) return false;
        }
        return true;
    }
}
