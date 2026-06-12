using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Transform))]
public class NoiseEmitter : MonoBehaviour
{
    [Header("Sound Profiles")]
    public List<SoundProfile> soundProfiles = new List<SoundProfile>();

    private Dictionary<SoundType, SoundProfile> profileMap;

    [Header("Debug")]
    public bool drawLastSphere = true;
    private SoundProfile lastProfile;
    private bool hasEmitted = false;

    private void Awake()
    {
        profileMap = new Dictionary<SoundType, SoundProfile>();
        foreach (var p in soundProfiles) profileMap[p.type] = p;
    }

    public void EmitNoise(SoundType type)
    {
        if (!profileMap.TryGetValue(type, out SoundProfile profile))
        {
            Debug.LogWarning($"[NoiseEmitter] No profile for {type}");
            return;
        }

        lastProfile = profile;
        hasEmitted = true;

        Collider[] hits = Physics.OverlapSphere(transform.position, profile.radius);
        foreach (var c in hits)
        {
            // busca sensor en el mismo GO o en padres
            var sensor = c.GetComponentInParent<HearingSensor>();
            if (sensor != null) sensor.NotifySound(transform.position, profile.intensity);
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawLastSphere || !hasEmitted || lastProfile == null) return;
        Gizmos.color = new Color(1, 1, 0, 0.25f);
        Gizmos.DrawWireSphere(transform.position, lastProfile.radius);
    }
}
