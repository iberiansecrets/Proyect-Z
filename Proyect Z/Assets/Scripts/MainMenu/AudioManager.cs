using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Mixers")]
    public AudioMixer mainMixer;

    private const string MASTER_PARAM = "Master";
    private const string MUSIC_PARAM = "MusicVolume";
    private const string SFX_PARAM = "SFXVolume";

    // Valores guardados
    public float masterVolume { get; private set; }
    public float musicVolume { get; private set; }
    public float sfxVolume { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadVolumes();
    }

    private float ToDecibels(float value)
    {
        // Si slider = 0 -> silencio (-80 dB)
        if (value <= 0.0001f)
            return -80f;

        return Mathf.Log10(value) * 20f;
    }

    private void LoadVolumes()
    {
        masterVolume = PlayerPrefs.GetFloat(MASTER_PARAM, 1f);
        musicVolume = PlayerPrefs.GetFloat(MUSIC_PARAM, 1f);
        sfxVolume = PlayerPrefs.GetFloat(SFX_PARAM, 1f);

        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        mainMixer.SetFloat(MASTER_PARAM, ToDecibels(masterVolume));
        mainMixer.SetFloat(MUSIC_PARAM, ToDecibels(musicVolume));
        mainMixer.SetFloat(SFX_PARAM, ToDecibels(sfxVolume));
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = value;
        mainMixer.SetFloat(MASTER_PARAM, ToDecibels(value));
        PlayerPrefs.SetFloat(MASTER_PARAM, value);
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = value;
        mainMixer.SetFloat(MUSIC_PARAM, ToDecibels(value));
        PlayerPrefs.SetFloat(MUSIC_PARAM, value);
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = value;
        mainMixer.SetFloat(SFX_PARAM, ToDecibels(value));
        PlayerPrefs.SetFloat(SFX_PARAM, value);
    }
}
