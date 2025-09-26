using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I;

    [Header("Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    public float BgmVolume { get; private set; }
    public float SfxVolume { get; private set; }

    const string KEY_BGM = "SET_BGM";
    const string KEY_SFX = "SET_SFX";

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        BgmVolume = PlayerPrefs.GetFloat(KEY_BGM, 0.8f);
        SfxVolume = PlayerPrefs.GetFloat(KEY_SFX, 0.8f);
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        if (bgmSource) bgmSource.volume = BgmVolume;
        if (sfxSource) sfxSource.volume = SfxVolume;
    }

    public void SetBgm(float v)
    {
        BgmVolume = Mathf.Clamp01(v);
        ApplyVolumes();
        PlayerPrefs.SetFloat(KEY_BGM, BgmVolume);
    }

    public void SetSfx(float v)
    {
        SfxVolume = Mathf.Clamp01(v);
        ApplyVolumes();
        PlayerPrefs.SetFloat(KEY_SFX, SfxVolume);
    }

    public void PlaySfx(AudioClip clip, float vol = 1f)
    {
        if (!clip || !sfxSource) return;
        sfxSource.PlayOneShot(clip, SfxVolume * vol);
    }

    public void PlayBgm(AudioClip clip, bool loop = true)
    {
        if (!clip || !bgmSource) return;
        bgmSource.loop = loop;
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void StopBgm()
    {
        if (bgmSource) bgmSource.Stop();
    }
}

