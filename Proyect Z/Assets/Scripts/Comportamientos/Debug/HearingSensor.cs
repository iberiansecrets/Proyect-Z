using UnityEngine;

[DisallowMultipleComponent]
public class HearingSensor : MonoBehaviour
{
    [Header("Hearing Parameters")]
    public float hearingRadius = 12f; // fallback
    public float soundMemoryTime = 3f;

    [Header("Debug")]
    public bool debugLogs = false;

    float lastHeardTime = -Mathf.Infinity;
    Vector3 lastHeardPosition = Vector3.zero;
    bool hasHeardSound = false;

    // Llamado por NoiseEmitter
    public void NotifySound(Vector3 soundPosition, float intensity)
    {
        float distance = Vector3.Distance(transform.position, soundPosition);
        float effectiveRadius = Mathf.Max(0f, hearingRadius * intensity);

        if (distance > effectiveRadius)
        {
            if (debugLogs) Debug.Log($"[HearingSensor] Ignored sound: dist {distance:F2} > radius {effectiveRadius:F2}");
            return;
        }

        hasHeardSound = true;
        lastHeardTime = Time.time;
        lastHeardPosition = soundPosition;

        if (debugLogs) Debug.Log($"[HearingSensor] Heard sound at {soundPosition} dist {distance:F2}");
    }

    public bool HasHeardSound()
    {
        if (!hasHeardSound) return false;
        if (Time.time - lastHeardTime > soundMemoryTime)
        {
            hasHeardSound = false;
            return false;
        }
        return true;
    }

    public bool TryGetLastHeardPosition(out Vector3 pos)
    {
        if (!HasHeardSound()) { pos = Vector3.zero; return false; }
        pos = lastHeardPosition; return true;
    }

    public void ClearMemory()
    {
        hasHeardSound = false;
        lastHeardTime = -Mathf.Infinity;
        lastHeardPosition = Vector3.zero;
    }
}
