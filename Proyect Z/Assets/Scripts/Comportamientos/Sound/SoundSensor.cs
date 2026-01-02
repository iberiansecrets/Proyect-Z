using UnityEngine;

public class SoundSensor : MonoBehaviour
{
    [Header("Hearing Parameters")]
    public float hearingRadius = 12f;
    public float soundMemoryTime = 3f;

    private float lastHeardTime = -Mathf.Infinity;
    private Vector3 lastHeardPosition;
    private bool hasHeardSound;

    /// <summary>
    /// Llamado por una fuente de sonido
    /// </summary>
    public void NotifySound(Vector3 soundPosition, float intensity)
    {
        Debug.Log($"[SoundSensor] Sound received at {soundPosition}");

        float distance = Vector3.Distance(transform.position, soundPosition);

        if (distance > hearingRadius * intensity)
            return;

        hasHeardSound = true;
        lastHeardTime = Time.time;
        lastHeardPosition = soundPosition;
    }

    /// <summary>
    /// ¿El enemigo recuerda un sonido reciente?
    /// </summary>
    public bool HasHeardSound()
    {
        if (!hasHeardSound)
            return false;

        if (Time.time - lastHeardTime > soundMemoryTime)
        {
            hasHeardSound = false;
            return false;
        }

        return true;
    }

    public Vector3 GetLastHeardPosition()
    {
        return lastHeardPosition;
    }
}
