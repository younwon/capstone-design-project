using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I;

    [Header("Mixer (Optional)")]
    [Tooltip("필요하면 AudioMixer를 연결하세요")]
    [SerializeField] private AudioMixer audioMixer;
    [Tooltip("BGM 출력 그룹")]
    [SerializeField] private AudioMixerGroup bgmGroup;
    [Tooltip("SFX 출력 그룹")]
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
    [SerializeField, Tooltip("전역 페이드 기본 시간(초)")]
    private float defaultFadeTime = 1.5f;

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
        ApplyVolumes();
    }

    void Reset()
    {
        // 에디터에서 컴포넌트 붙일 때 자동 세팅 편의
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

    // ===== BGM =====

    // 단순 재생(크로스페이드는 BgmController에서 처리)
    public void PlayBGM(AudioClip clip, bool loop = true, float volume = 1f)
    {
        if (!clip) return;
        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.volume = Mathf.Clamp01(volume) * MixedBgmVolume;
        bgmSource.pitch = 1f;
        bgmSource.Play();
    }

    public void StopBGM() => bgmSource.Stop();

    // 옵션: 부드럽게 페이드아웃 후 정지
    public void StopBGM(float fadeTime)
    {
        if (!bgmSource.isPlaying) return;
        StartCoroutine(IEFadeOutAndStop(bgmSource, Mathf.Max(0f, fadeTime)));
    }

    // ===== SFX (원샷) =====

    // [호환 API] 기존/수정 스크립트에서 사용하는 기본 시그니처
    public void PlaySFX(AudioClip clip)
    {
        if (!clip || !sfxSource) return;
        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(clip, MixedSfxVolume);
    }

    // [호환 API] 피치 오버로드 (점프 랜덤 피치 등)
    public void PlaySFX(AudioClip clip, float pitch)
    {
        if (!clip || !sfxSource) return;
        float oldPitch = sfxSource.pitch;
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, MixedSfxVolume);
        sfxSource.pitch = oldPitch;
    }

    // 위치 기반(필요 시)
    public void PlaySFXAt(AudioClip clip, Vector3 worldPos, float pitch = 1f)
    {
        if (!clip) return;
        var temp = new GameObject($"SFX_{clip.name}");
        temp.transform.position = worldPos;
        var src = temp.AddComponent<AudioSource>();
        src.clip = clip;
        src.pitch = pitch;
        src.volume = MixedSfxVolume;
        src.spatialBlend = 1f; // 3D
        if (sfxGroup) src.outputAudioMixerGroup = sfxGroup;
        src.Play();
        Destroy(temp, clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch)));
    }

    // ===== 루프 유틸 =====
    // 외부 오브젝트(예: Lava, 바람 등)의 AudioSource를 재사용하도록 설계
    // 사용 예) AudioManager.I.PlayLoop(boilClip, ref lavaSource, vol, pitch);
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
        if (!target.isPlaying) target.Play();
    }

    // 부드럽게 끄기
    public void StopWithFade(AudioSource src, float fadeTime)
    {
        if (src && src.isPlaying)
            StartCoroutine(IEFadeOutAndStop(src, Mathf.Max(0f, fadeTime)));
    }

    private IEnumerator IEFadeOutAndStop(AudioSource src, float t)
    {
        float start = src.volume;
        float elapsed = 0f;
        while (elapsed < t)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(elapsed / t);
            // 루프 소스는 sfx 볼륨과 마스터 볼륨 영향을 같이 받도록
            float mix = (src == bgmSource) ? MixedBgmVolume : MixedSfxVolume;
            src.volume = start * k * mix;
            yield return null;
        }
        src.Stop();
    }

    // ===== 전역 페이드 =====
    // 사망/게임오버 시 전체 오디오를 서서히 줄임 (BGM+SFX)
    public void FadeOutAll(float fadeTime)
    {
        StopCoroutineSafe(ref _fadeAllCo);
        _fadeAllCo = StartCoroutine(IEFadeOutAll(Mathf.Max(0f, fadeTime)));
    }

    private Coroutine _fadeAllCo;
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

        if (bgmSource) bgmSource.Stop();
        if (sfxSource) sfxSource.Stop();

        // 볼륨 복구(정지 후 재생 시 정상 레벨)
        ApplyVolumes();
    }

    private void StopCoroutineSafe(ref Coroutine co)
    {
        if (co != null)
        {
            StopCoroutine(co);
            co = null;
        }
    }

    // ===== 볼륨/믹서 =====

    public void SetMasterVolume(float v)
    {
        masterVolume = Mathf.Clamp01(v);
        ApplyVolumes();
        if (audioMixer) audioMixer.SetFloat("Master_dB", LinearToDb(masterVolume));
    }

    public void SetBGMVolume(float v)
    {
        bgmVolume = Mathf.Clamp01(v);
        ApplyVolumes();
        if (audioMixer) audioMixer.SetFloat("BGM_dB", LinearToDb(bgmVolume));
    }

    public void SetSFXVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
        ApplyVolumes();
        if (audioMixer) audioMixer.SetFloat("SFX_dB", LinearToDb(sfxVolume));
    }

    private void ApplyVolumes()
    {
        if (bgmSource) bgmSource.volume = MixedBgmVolume;
        if (sfxSource) sfxSource.volume = MixedSfxVolume;
    }

    public void SetBgm(float v) { SetBGMVolume(v); }
    public void SetSfx(float v) { SetSFXVolume(v); }

    // 선형(0~1) → dB 변환(오디오믹서 노멀라이즈용)
    private float LinearToDb(float v)
    {
        if (v <= 0.0001f) return -80f; // 무음 근사
        return Mathf.Log10(v) * 20f;
    }

    // ===== 편의 메서드 =====

    public void FadeOutAllDefault() => FadeOutAll(defaultFadeTime);

    // 전역 일시정지/해제 (원하면 사용)
    public void SetPaused(bool paused)
    {
        AudioListener.pause = paused;
        // 또는 낮은 레벨에서 볼륨 페이드로 구현하려면 여기서 코루틴 호출
    }
}