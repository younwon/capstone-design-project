using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

[AddComponentMenu("Audio/Pause Audio Ducker")]
public class PauseAudioDucker : MonoBehaviour
{
    public static PauseAudioDucker I;

    [Header("Mixer & Params")]
    [Tooltip("GameMixer 에셋. 비워두면 MixerVolumeController.I.mixer를 자동 사용")]
    public AudioMixer mixer;
    [Tooltip("BGM 볼륨 파라미터 이름(Exposed)")]
    public string bgmVolumeParam = "BGM_dB";
    [Tooltip("BGM Lowpass 컷오프 파라미터 이름(Exposed)")]
    public string bgmLowpassParam = "BGM_LP_Hz";

    [Header("Targets (Normal / Paused)")]
    [Tooltip("정상 상태 BGM 볼륨(dB)")] public float normalBgmDb = 0f;
    [Tooltip("정상 상태 Lowpass(Hz)")] public float normalLpHz = 22000f;
    [Tooltip("일시정지 BGM 볼륨(dB)")] public float pausedBgmDb = -8f;
    [Tooltip("일시정지 Lowpass(Hz)")] public float pausedLpHz = 1000f;

    [Header("Transition")]
    [Range(0.05f, 2f)] public float fadeTime = 0.3f;

    Coroutine _co;

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        if (!mixer && MixerVolumeController.I) mixer = MixerVolumeController.I.mixer;
    }

    public void ToPaused()
    {
        if (!mixer) return;
        StartBlend(pausedBgmDb, pausedLpHz);
    }

    public void ToNormal()
    {
        if (!mixer) return;
        StartBlend(normalBgmDb, normalLpHz);
    }

    // 즉시 복구(페이드 없이 바로)
    public void ForceNormalNow()
    {
        if (!mixer) return;
        if (_co != null) StopCoroutine(_co);
        mixer.SetFloat(bgmVolumeParam, normalBgmDb);
        mixer.SetFloat(bgmLowpassParam, normalLpHz);
        _co = null;
    }

    void StartBlend(float toDb, float toHz)
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoBlend(toDb, toHz));
    }

    IEnumerator CoBlend(float toDb, float toHz)
    {
        float t = 0f;
        mixer.GetFloat(bgmVolumeParam, out float fromDb);
        mixer.GetFloat(bgmLowpassParam, out float fromHz);

        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime; // 일시정지 중에도 전환
            float k = Mathf.Clamp01(t / fadeTime);
            mixer.SetFloat(bgmVolumeParam, Mathf.Lerp(fromDb, toDb, k));
            mixer.SetFloat(bgmLowpassParam, Mathf.Lerp(fromHz, toHz, k));
            yield return null;
        }
        mixer.SetFloat(bgmVolumeParam, toDb);
        mixer.SetFloat(bgmLowpassParam, toHz);
        _co = null;
    }
}
