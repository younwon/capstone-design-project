// LavaProximityFX.cs 교체/업데이트
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LavaProximityFX : MonoBehaviour
{
    [Header("Refs")]
    public LavaRiser lava;
    public Transform cam;
    public Image screenTint;
    public CameraShaker shaker;

    [Header("Distance (world units)")]
    public float startBelow = 0.6f;
    public float fullAt = 0.05f;

    [Header("Visual Fade")]
    [Range(0f, 1f)] public float maxAlpha = 0.12f;
    public float visualFadeSpeed = 8f;
    [Range(0f, 1f)] public float shakeAtFull = 0.22f;

    [Header("Audio")]
    public AudioClip lavaBoilLoop;
    public float audioLerpSpeed = 10f;
    [Range(-0.5f, 0.5f)] public float audioLead = 0.18f;
    public float minPitch = 0.92f;
    public float maxPitch = 1.10f;

    [Header("Audible Gate")]
    [Range(0f, 1f)] public float audibleEnter = 0.02f;
    [Range(0f, 1f)] public float audibleExit = 0.00f;

    [Header("Fade Out on End")]
    public float endFadeOutTime = 1.5f;

    AudioSource lavaSource;
    Camera _camComp;
    bool _audible = false;
    Coroutine _exitFadeCo;

    void Awake()
    {
        if (!cam && Camera.main) cam = Camera.main.transform;
        if (!_camComp && Camera.main) _camComp = Camera.main;

        if (!lava) lava = FindObjectOfType<LavaRiser>();
        if (!shaker && Camera.main) shaker = Camera.main.GetComponent<CameraShaker>();

        if (!TryGetComponent(out lavaSource))
            lavaSource = gameObject.AddComponent<AudioSource>();

        lavaSource.loop = true;
        lavaSource.playOnAwake = false;
        lavaSource.spatialBlend = 0f;
        lavaSource.volume = 0f;
        lavaSource.pitch = minPitch;
        lavaSource.clip = lavaBoilLoop;

        // ★ 추가 — SFC 설정 반영되도록 믹서 그룹 적용
        if (AudioManager.I && AudioManager.I.SfxGroup)
            lavaSource.outputAudioMixerGroup = AudioManager.I.SfxGroup;

        if (audibleExit > audibleEnter) audibleExit = audibleEnter * 0.5f;
    }


    void OnEnable()
    {
        SimpleGameOver.OnGameOver += HandleGameOver;
        PlayerDeath.OnPlayerDied += HandlePlayerDied;
        GameOverUI.OnShown += HandleGameOverUIShown;   // [ADD]
    }

    void OnDisable()
    {
        SimpleGameOver.OnGameOver -= HandleGameOver;
        PlayerDeath.OnPlayerDied -= HandlePlayerDied;
        GameOverUI.OnShown -= HandleGameOverUIShown;   // [ADD]
    }

    void LateUpdate()
    {
        if (!lava || !cam || !_camComp || !screenTint) return;

        float camBottom = cam.position.y - _camComp.orthographicSize;
        float dist = lava.transform.position.y - camBottom;

        float tVisual = Mathf.InverseLerp(-startBelow, fullAt, dist);
        tVisual = Mathf.Clamp01(tVisual);

        float tAudio = Mathf.Clamp01(tVisual + audioLead);

        // Visual
        var c = screenTint.color;
        float targetA = tVisual * maxAlpha;
        c.a = Mathf.MoveTowards(c.a, targetA, visualFadeSpeed * Time.deltaTime);
        screenTint.color = c;

        if (shaker) shaker.SetExternal(tVisual * shakeAtFull);

        // Gate
        HandleAudibleGate(tVisual);

        // Audio
        if (_audible && lavaSource && lavaBoilLoop)
        {
            if (!lavaSource.isPlaying) lavaSource.Play();

            float targetVol = tAudio;

            float mix = 1f;
            if (AudioManager.I)
                mix = Mathf.Clamp01(AudioManager.I.masterVolume * AudioManager.I.sfxVolume);

            float targetPitch = Mathf.Lerp(minPitch, maxPitch, tAudio);
            lavaSource.volume = Mathf.Lerp(lavaSource.volume, targetVol, Time.deltaTime * audioLerpSpeed);
            lavaSource.pitch = Mathf.Lerp(lavaSource.pitch, targetPitch, Time.deltaTime * audioLerpSpeed);
        }
    }

    void HandleAudibleGate(float tVisual)
    {
        if (!lavaSource) return;

        if (!_audible && tVisual >= audibleEnter)
        {
            _audible = true;
            if (_exitFadeCo != null) { StopCoroutine(_exitFadeCo); _exitFadeCo = null; }
            lavaSource.volume = 0f;
            lavaSource.pitch = minPitch;
            // 재생은 LateUpdate에서 처리
            return;
        }

        if (_audible && tVisual <= audibleExit)
        {
            _audible = false;
            if (_exitFadeCo != null) StopCoroutine(_exitFadeCo);
            _exitFadeCo = StartCoroutine(CoFadeOutAndStop(0.6f));
        }
    }

    // 게임오버/사망/게임오버UI 표시 모두 페이드아웃으로 통합
    void HandlePlayerDied(Transform _) => StartEndFade();
    void HandleGameOver(string reason) => StartEndFade();
    void HandleGameOverUIShown() => StartEndFade(); // [ADD]

    void StartEndFade()
    {
        if (!lavaSource) return;
        _audible = false;
        if (_exitFadeCo != null) StopCoroutine(_exitFadeCo);
        _exitFadeCo = StartCoroutine(CoFadeOutAndStop(endFadeOutTime));
    }

    IEnumerator CoFadeOutAndStop(float time)
    {
        if (!lavaSource) yield break;
        float start = lavaSource.volume;
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime; // 정지 중에도 빠지게
            lavaSource.volume = Mathf.Lerp(start, 0f, t / time);
            yield return null;
        }
        lavaSource.volume = 0f;
        lavaSource.Stop();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (maxAlpha < 0f) maxAlpha = 0f;
        if (minPitch < 0.1f) minPitch = 0.1f;
        if (maxPitch < minPitch) maxPitch = minPitch;
        if (audibleExit > audibleEnter) audibleExit = audibleEnter * 0.5f;
        if (!_camComp && Camera.main) _camComp = Camera.main;
    }
#endif
}
