using UnityEngine;

[System.Serializable]
public class SoundProfile
{
    public SoundType type;
    [Min(0f)] public float radius = 6f;
    [Min(0f)] public float intensity = 1f; // multiplicador (1 = normal)
}
