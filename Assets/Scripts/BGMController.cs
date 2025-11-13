using UnityEngine;
using System.Collections;
using UnityEngine.Audio;

public class BgmController : MonoBehaviour
{
    public static BgmController I;

    [Header("Assign two looped AudioSources")]
    public AudioSource titleSrc;                 // 타이틀 BGM
    public AudioSource inGameSrc;                // 인게임 BGM

    [Header("Route (optional)")]
    public AudioMixerGroup bgmGroup;             // 둘 다 같은 BGM 그룹으로 라우팅 권장

    [Header("Crossfade")]
    [Range(0.1f, 5f)] public float fadeTime = 1.2f;
    public bool playTitleOnAwake = true;
    public bool dontDestroy = true;

    Coroutine fadeCo;

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        if (dontDestroy) DontDestroyOnLoad(gameObject);

        SetupSource(titleSrc);
        SetupSource(inGameSrc);
    }

    void SetupSource(AudioSource s)
    {
        if (!s) return;
        s.loop = true;
        s.playOnAwake = false;
        s.spatialBlend = 0f;
        s.volume = 0f;                            // 상대 볼륨(크로스페이드용)
        if (bgmGroup) s.outputAudioMixerGroup = bgmGroup; // Mixer BGM 그룹으로 라우팅
    }

    void Start()
    {
        if (playTitleOnAwake && titleSrc && titleSrc.clip)
        {
            inGameSrc?.Stop();
            titleSrc.volume = 1f;                 // 상대 1.0
            titleSrc.Play();
        }
    }

    public void PlayTitle(bool immediate = false) => Play(titleSrc, inGameSrc, immediate);
    public void PlayInGame(bool immediate = false) => Play(inGameSrc, titleSrc, immediate);

    public void StopAll(float time = 0.6f)
    {
        if (fadeCo != null) StopCoroutine(fadeCo);
        fadeCo = StartCoroutine(CoStopAll(time));
    }

    void Play(AudioSource toOn, AudioSource toOff, bool immediate)
    {
        if (!toOn) return;

        if (immediate)
        {
            if (toOff && toOff.isPlaying) { toOff.Stop(); toOff.volume = 0f; }
            toOn.volume = 1f;
            if (!toOn.isPlaying) toOn.Play();
            return;
        }

        if (fadeCo != null) StopCoroutine(fadeCo);
        fadeCo = StartCoroutine(Crossfade(toOn, toOff));
    }

    IEnumerator Crossfade(AudioSource toOn, AudioSource toOff)
    {
        if (!toOn) yield break;
        if (!toOn.isPlaying) toOn.Play();

        float t = 0f;
        float fromOn = Mathf.Clamp01(toOn.volume);
        float fromOff = toOff ? Mathf.Clamp01(toOff.volume) : 0f;

        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;          // 일시정지 중에도 자연스럽게
            float k = Mathf.Clamp01(t / fadeTime);
            toOn.volume = Mathf.Lerp(fromOn, 1f, k);    // 상대 볼륨(최종 음량은 Mixer BGM이 결정)
            if (toOff) toOff.volume = Mathf.Lerp(fromOff, 0f, k);
            yield return null;
        }
        toOn.volume = 1f;
        if (toOff) { toOff.Stop(); toOff.volume = 0f; }
        fadeCo = null;
    }

    IEnumerator CoStopAll(float time)
    {
        float t = 0f;
        float a0 = titleSrc ? titleSrc.volume : 0f;
        float b0 = inGameSrc ? inGameSrc.volume : 0f;

        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / time);
            if (titleSrc) titleSrc.volume = Mathf.Lerp(a0, 0f, k);
            if (inGameSrc) inGameSrc.volume = Mathf.Lerp(b0, 0f, k);
            yield return null;
        }
        if (titleSrc) { titleSrc.Stop(); titleSrc.volume = 0f; }
        if (inGameSrc) { inGameSrc.Stop(); inGameSrc.volume = 0f; }
        fadeCo = null;
    }
}
