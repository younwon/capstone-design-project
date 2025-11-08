using UnityEngine;
using System.Collections;

public class BgmController : MonoBehaviour
{
    public static BgmController I;

    [Header("Assign two looped AudioSources")]
    public AudioSource titleSrc;
    public AudioSource inGameSrc;

    [Header("Fade")]
    [Range(0.1f, 5f)] public float fadeTime = 1.2f;
    [Range(0f, 1f)] public float bgmVolume = 1f;
    public bool playTitleOnAwake = true;
    public bool dontDestroy = true;

    Coroutine fadeCo;

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        if (dontDestroy) DontDestroyOnLoad(gameObject);

        if (titleSrc) { titleSrc.loop = true; titleSrc.playOnAwake = false; titleSrc.volume = 0f; }
        if (inGameSrc) { inGameSrc.loop = true; inGameSrc.playOnAwake = false; inGameSrc.volume = 0f; }
    }

    void Start()
    {
        if (playTitleOnAwake && titleSrc && titleSrc.clip)
        {
            // 타이틀 즉시 세팅
            inGameSrc?.Stop();
            titleSrc.volume = bgmVolume;
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
            if (toOff && toOff.isPlaying) toOff.Stop();
            toOn.volume = bgmVolume;
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
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeTime);
            toOn.volume = Mathf.Lerp(fromOn, bgmVolume, k);
            if (toOff) toOff.volume = Mathf.Lerp(fromOff, 0f, k);
            yield return null;
        }

        toOn.volume = bgmVolume;
        if (toOff) { toOff.Stop(); toOff.volume = 0f; }
        fadeCo = null;
    }

    IEnumerator CoStopAll(float time)
    {
        float t = 0f;
        float a = titleSrc ? titleSrc.volume : 0f;
        float b = inGameSrc ? inGameSrc.volume : 0f;

        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / time);
            if (titleSrc) titleSrc.volume = Mathf.Lerp(a, 0f, k);
            if (inGameSrc) inGameSrc.volume = Mathf.Lerp(b, 0f, k);
            yield return null;
        }
        if (titleSrc) { titleSrc.Stop(); titleSrc.volume = 0f; }
        if (inGameSrc) { inGameSrc.Stop(); inGameSrc.volume = 0f; }
        fadeCo = null;
    }
}
