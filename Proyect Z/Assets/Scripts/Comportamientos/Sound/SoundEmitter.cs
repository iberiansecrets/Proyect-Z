using UnityEngine;
using System.Collections.Generic;

public class SoundEmitter : MonoBehaviour
{
    [Header("Sound Configuration")]
    public List<SoundData> sounds = new();
    private Dictionary<SoundType, SoundData> soundMap;

    [Header("Debug")]
    public bool drawDebug = true;
    private SoundData lastEmittedProfile;
    private bool hasEmittedSound = false;

    private void Awake()
    {
        soundMap = new Dictionary<SoundType, SoundData>();
        foreach (var s in sounds)
            soundMap[s.type] = s;
    }

    public void EmitSound(SoundType type)
    {
        if (!soundMap.TryGetValue(type, out SoundData data))
        {
            Debug.LogWarning($"No sound data for {type}");
            return;
        }

        SoundData profile = soundMap[type];
        lastEmittedProfile = profile;
        hasEmittedSound = true;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            data.radius
        );

        foreach (var col in hits)
        {
            SoundSensor sensor = col.GetComponent<SoundSensor>();
            if (sensor != null)
            {
                sensor.NotifySound(
                    transform.position,
                    data.intensity
                );
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawDebug || !hasEmittedSound)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lastEmittedProfile.radius);
    }
}
