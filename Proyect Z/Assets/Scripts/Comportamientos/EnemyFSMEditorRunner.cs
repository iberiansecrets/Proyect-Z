using BehaviourAPI.UnityToolkit.GUIDesigner.Runtime;
using UnityEngine;

public class EnemyFSMEditorRunner : EditorBehaviourRunner
{
    [Header("Target")]
    public Transform target;

    private SightSensor sightSensor;
    private SoundSensor soundSensor;

    protected override void Init()
    {
        sightSensor = GetComponent<SightSensor>();
        soundSensor = GetComponent<SoundSensor>();

        // Auto-assign target si no se asignó en inspector
        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }

        Debug.Log($"[Runner Init] sightSensor={(sightSensor != null)}, soundSensor={(soundSensor != null)}, target={(target != null ? target.name : "NULL")}");

        base.Init(); // MUY IMPORTANTE
    }

    // ================= PERCEPCIONES =================

    public bool CanSeePlayer()
    {
        if (target == null || sightSensor == null)
        {
            Debug.Log("[Runner] CanSeePlayer = false (no target o sin sightSensor)");
            return false;
        }

        bool sees = sightSensor.CanSeeTarget(target);
        Debug.Log($"[Runner] CanSeePlayer = {sees}");
        return sees;
    }

    public bool HasHeardSound()
    {
        if (soundSensor == null)
        {
            Debug.Log("[Runner] HasHeardSound = false (sin soundSensor)");
            return false;
        }

        bool heard = soundSensor.HasHeardSound();
        Debug.Log($"[Runner] HasHeardSound = {heard}");
        return heard;
    }

    public Vector3 GetLastHeardPosition()
    {
        return soundSensor != null
            ? soundSensor.GetLastHeardPosition()
            : transform.position;
    }

    public bool AlwaysTrue_Debug()
    {
        Debug.Log("[Runner] AlwaysTrue_Debug called");
        return true;
    }
}
