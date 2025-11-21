using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I;

    [Header("Mixer (Optional)")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup bgmGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Volumes")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Defaults")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private float defaultFadeTime = 1.5f;

    // SettingsUIController와 키 통일 (중요!)
    const string KEY_BGM = "SET_BGM";
    const string KEY_SFX = "SET_SFX";
    const string KEY_MUTEALL = "SET_MUTEALL";

    // 외부에서 참조 가능
    public AudioMixerGroup SfxGroup => sfxGroup;
    public AudioMixerGroup BgmGroup => bgmGroup;

    void Awake()
    {
        if (I && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

        EnsureSourcesExist();

        // ★ 게임 시작 시 저장된 설정 즉시 반영
        LoadSavedVolumes();
        ApplyVolumes();
        ApplyMixerDbSafe();
    }

    void Reset()
    {
        EnsureSourcesExist();
    }

    private void EnsureSourcesExist()
    {
        if (!bgmSource)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            if (bgmGroup) bgmSource.outputAudioMixerGroup = bgmGroup;
        }
        if (!sfxSource)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            if (sfxGroup) sfxSource.outputAudioMixerGroup = sfxGroup;
        }
    }

    private float MixedBgmVolume => Mathf.Clamp01(masterVolume * bgmVolume);
    private float MixedSfxVolume => Mathf.Clamp01(masterVolume * sfxVolume);

    // ================================================================
    //  저장값 로드/적용
    // ================================================================
    void LoadSavedVolumes()
    {
        float savedBgm = PlayerPrefs.GetFloat(KEY_BGM, bgmVolume);
        float savedSfx = PlayerPrefs.GetFloat(KEY_SFX, sfxVolume);
        bool muteAll = PlayerPrefs.GetInt(KEY_MUTEALL, 0) == 1;

        if (muteAll)
        {
            bgmVolume = 0f;
            sfxVolume = 0f;
        }
        else
        {
            bgmVolume = Mathf.Clamp01(savedBgm);
            sfxVolume = Mathf.Clamp01(savedSfx);
        }
    }

    void ApplyMixerDbSafe()
    {
        // ★ audioMixer가 할당 안됐으면 절대 접근하지 않음
        if (audioMixer == null) return;

        audioMixer.SetFloat("Master_dB", LinearToDb(masterVolume));
        audioMixer.SetFloat("BGM_dB", LinearToDb(bgmVolume));
        audioMixer.SetFloat("SFX_dB", LinearToDb(sfxVolume));
    }

    // ================================================================
    //  BGM 기본 재생
    // ================================================================
    public void PlayBGM(AudioClip clip, bool loop = true, float volume = 1f, bool restartIfSame = false)
    {
        if (!clip) return;

        if (!restartIfSame && bgmSource.clip == clip && bgmSource.isPlaying)
            return;

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.volume = Mathf.Clamp01(volume) * MixedBgmVolume;
        bgmSource.pitch = 1f;
        bgmSource.Play();
    }

    public void StopBGM() => bgmSource.Stop();

    public void StopBGM(float fadeTime)
    {
        if (!bgmSource.isPlaying) return;
        StartCoroutine(IEFadeOutAndStop(bgmSource, Mathf.Max(0f, fadeTime)));
    }

    // ================================================================
    //  부드러운 페이드 전환 BGM
    // ================================================================
    public void PlayBGM_Fade(AudioClip clip, float fadeTime = 0.8f, bool restartIfSame = false)
    {
        if (!clip || !bgmSource) return;

        if (!restartIfSame && bgmSource.clip == clip && bgmSource.isPlaying)
            return;

        StartCoroutine(IEPlayBgmFade(clip, Mathf.Max(0f, fadeTime)));
    }

    private IEnumerator IEPlayBgmFade(AudioClip newClip, float t)
    {
        if (t <= 0.001f)
        {
            bgmSource.clip = newClip;
            bgmSource.volume = MixedBgmVolume;
            bgmSource.loop = true;
            bgmSource.Play();
            yield break;
        }

        float half = t * 0.5f;
        float elapsed = 0f;
        float startVol = bgmSource.volume;

        // 1) Fade Out
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / half);
            bgmSource.volume = Mathf.Lerp(startVol, 0f, k);
            yield return null;
        }

        // 2) Change Clip
        bgmSource.clip = newClip;
        bgmSource.loop = true;
        bgmSource.Play();

        // 3) Fade In
        elapsed = 0f;
        float targetVol = MixedBgmVolume;

        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / half);
            bgmSource.volume = Mathf.Lerp(0f, targetVol, k);
            yield return null;
        }

        bgmSource.volume = targetVol;
    }

    // ================================================================
    //  SFX
    // ================================================================
    public void PlaySFX(AudioClip clip)
    {
        if (!clip || !sfxSource) return;

        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(clip, MixedSfxVolume);
    }

    public void PlaySFX(AudioClip clip, float pitch)
    {
        if (!clip || !sfxSource) return;

        float old = sfxSource.pitch;
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, MixedSfxVolume);
        sfxSource.pitch = old;
    }

    public void PlaySFXAt(AudioClip clip, Vector3 pos, float pitch = 1f)
    {
        if (!clip) return;

        var go = new GameObject("SFX_" + clip.name);
        go.transform.position = pos;
        var src = go.AddComponent<AudioSource>();

        src.clip = clip;
        src.pitch = pitch;
        src.volume = MixedSfxVolume;
        src.spatialBlend = 1f;

        if (sfxGroup) src.outputAudioMixerGroup = sfxGroup;

        src.Play();
        Destroy(go, clip.length / Mathf.Max(0.1f, Mathf.Abs(pitch)));
    }

    // ================================================================
    // 루프형 SFX
    // ================================================================
    public void PlayLoop(AudioClip clip, ref AudioSource target, float volume = 1f, float pitch = 1f, bool spatial = false)
    {
        if (!clip)
        {
            if (target && target.isPlaying) target.Stop();
            return;
        }

        if (!target)
        {
            target = gameObject.AddComponent<AudioSource>();
            target.loop = true;
            target.playOnAwake = false;
            if (sfxGroup) target.outputAudioMixerGroup = sfxGroup;
        }

        target.clip = clip;
        target.volume = Mathf.Clamp01(volume) * MixedSfxVolume;
        target.pitch = pitch;
        target.spatialBlend = spatial ? 1f : 0f;

        if (!target.isPlaying)
            target.Play();
    }

    // ================================================================
    // 페이드 아웃 후 정지
    // ================================================================
    private IEnumerator IEFadeOutAndStop(AudioSource src, float t)
    {
        float start = src.volume;
        float elapsed = 0f;

        while (elapsed < t)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(elapsed / t);

            float mix = (src == bgmSource) ? MixedBgmVolume : MixedSfxVolume;
            src.volume = start * k * mix;

            yield return null;
        }

        src.Stop();
    }

    // ================================================================
    // 전역 페이드
    // ================================================================
    private Coroutine _fadeAllCo;

    public void FadeOutAll(float fadeTime)
    {
        if (_fadeAllCo != null) StopCoroutine(_fadeAllCo);
        _fadeAllCo = StartCoroutine(IEFadeOutAll(fadeTime));
    }

    private IEnumerator IEFadeOutAll(float t)
    {
        float bgmStart = bgmSource ? bgmSource.volume : 0f;
        float sfxStart = sfxSource ? sfxSource.volume : 0f;

        float elapsed = 0f;
        while (elapsed < t)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(elapsed / t);

            if (bgmSource) bgmSource.volume = bgmStart * k;
            if (sfxSource) sfxSource.volume = sfxStart * k;

            yield return null;
        }

        bgmSource?.Stop();
        sfxSource?.Stop();

        ApplyVolumes();
    }

    // ================================================================
    // 볼륨 관련
    // ================================================================
    public void SetMasterVolume(float v)
    {
        masterVolume = Mathf.Clamp01(v);
        ApplyVolumes();
        if (audioMixer != null)
            audioMixer.SetFloat("Master_dB", LinearToDb(masterVolume));
    }

    public void SetBGMVolume(float v)
    {
        bgmVolume = Mathf.Clamp01(v);
        ApplyVolumes();
        if (audioMixer != null)
            audioMixer.SetFloat("BGM_dB", LinearToDb(bgmVolume));
    }

    public void SetSFXVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
        ApplyVolumes();
        if (audioMixer != null)
            audioMixer.SetFloat("SFX_dB", LinearToDb(sfxVolume));
    }

    private void ApplyVolumes()
    {
        if (bgmSource) bgmSource.volume = MixedBgmVolume;
        if (sfxSource) sfxSource.volume = MixedSfxVolume;
    }

    private float LinearToDb(float v)
    {
        if (v <= 0.0001f) return -80f;
        return Mathf.Log10(v) * 20f;
    }
}

