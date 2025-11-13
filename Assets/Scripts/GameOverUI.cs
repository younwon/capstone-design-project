using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Audio;

public class GameOverUI : MonoBehaviour
{
    public static bool InputLocked { get; private set; } = false;
    public static bool IsShown { get; private set; } = false;
    public static System.Action OnShown;

    [Header("Refs")]
    [SerializeField] CanvasGroup cg;
    [SerializeField] Button btnRestart;
    [SerializeField] Button btnHome;

    [Header("Texts")]
    [SerializeField] TMP_Text thisValueText;   // ex) "123 m"
    [SerializeField] TMP_Text bestValueText;   // ex) "456 m"
    [SerializeField] TMP_Text thisTimeText;    // ex) "00:45"
    [SerializeField] TMP_Text bestTimeText;    // ex) "00:30"

    [Header("New Record Visuals")]
    [SerializeField] GameObject newBadge;             // NEW 배지
    [SerializeField] TMP_Text newRecordBanner;        // "NEW RECORD!"
    [SerializeField] Image flashOverlay;              // 흰 플래시(알파 0 시작)

    [Header("SFX (UI 공용 소스)")]
    [SerializeField] AudioSource sfxSource;           // Pitch=1 권장, Output=SFX 그룹
    [SerializeField] AudioClip sfxNewRecord;          // 신기록 효과음(선택)

    [Header("SFX: Tick / Confirm / GameOver")]
    [SerializeField] AudioClip sfxTick;               // 숫자 카운트 '틱'
    [SerializeField] AudioClip sfxConfirm;            // 카운트 완료 확정
    [SerializeField] AudioClip sfxGameOverIntro;      // 패널 등장 시 1회

    [SerializeField, Tooltip("틱 사운드 간격(초). 0이면 프레임마다 시도(권장: 0.04~0.10)")]
    float tickInterval = 0.05f;

    [Header("Flash Tuning")]
    [SerializeField, Range(0f, 1f)] float flashMaxAlpha = 0.9f;
    [SerializeField] float flashDuration = 0.6f;

    [Header("CountUp Tuning")]
    [SerializeField] float countThisDuration = 0.6f; // This Run 카운트업 시간
    [SerializeField] float countBestDuration = 0.6f; // Best 카운트업 시간
    [SerializeField] bool easeOut = true;

    [Header("Title/Home Options")]
    [SerializeField] GameObject titlePanel;           // 같은 씬 타이틀 UI (선택)
    [SerializeField] string titleSceneName = "Title"; // 별도 씬 이름(선택)

    [Header("Timing")]
    [SerializeField, Tooltip("게임오버 후 UI가 뜨기까지의 딜레이(초, 실제시간)")]
    float uiDelay = 0.8f;

    [Header("Show 시 숨길 오브젝트들")]
    [SerializeField] GameObject[] hideWhenShown;

    [Header("BGM control (Mixer ducking)")]
    [SerializeField, Tooltip("게임오버 시작 시 BGM을 이 시간에 걸쳐 -∞dB로 페이드아웃")]
    float bgmFadeOutTime = 0.35f;
    [SerializeField, Tooltip("연출이 모두 끝난 후 BGM을 duck 상태로 복귀시키는 페이드인 시간")]
    float bgmFadeInTime = 0.35f;

    // 내부 상태
    readonly List<GameObject> hiddenList = new List<GameObject>();
    bool suppressRestoreHUD = false;

    // 캐시(게임오버 시점 값)
    int cachedThisRun = 0;
    int cachedBest = 0;
    string cachedThisTime = "00:00";
    string cachedBestTime = "00:00";
    bool cachedIsNew = false;

    // Mixer 연동
    AudioMixer _mixer;
    string _bgmParam = "BGM_dB";
    string _lpParam = "BGM_LP_Hz";
    Coroutine _bgmCo;

    void Awake()
    {
        HideImmediate();

        SimpleGameOver.OnGameOver += OnGameOverSimple;

        if (btnRestart) btnRestart.onClick.AddListener(OnClickRestart);
        if (btnHome) btnHome.onClick.AddListener(OnClickHome);

        if (newRecordBanner) newRecordBanner.gameObject.SetActive(false);
        if (flashOverlay) flashOverlay.color = new Color(1, 1, 1, 0);

        // Mixer 참조
        if (PauseAudioDucker.I)
        {
            _mixer = PauseAudioDucker.I.mixer;
            _bgmParam = PauseAudioDucker.I.bgmVolumeParam;
            _lpParam = PauseAudioDucker.I.bgmLowpassParam;
        }
    }

    void OnDestroy()
    {
        SimpleGameOver.OnGameOver -= OnGameOverSimple;
        if (btnRestart) btnRestart.onClick.RemoveListener(OnClickRestart);
        if (btnHome) btnHome.onClick.RemoveListener(OnClickHome);

        if (!suppressRestoreHUD) RestoreHUD();

        InputLocked = false;
        IsShown = false;
    }

    // ========= 게임오버 신호 =========
    void OnGameOverSimple(string reason)
    {
        if (ScoreSystem.I)
        {
            var s = ScoreSystem.I;
            cachedThisRun = s.SessionMaxScore;
            cachedBest = s.BestEverScore;
            cachedThisTime = ScoreSystem.FormatMMSS(s.ThisRunTime);
            cachedBestTime = ScoreSystem.FormatMMSS(s.BestEverTime);
            cachedIsNew = s.IsNewRecord;
        }
        else
        {
            cachedThisRun = cachedBest = 0;
            cachedThisTime = cachedBestTime = "00:00";
            cachedIsNew = false;
        }

        // 입력 잠금
        InputLocked = true;

        // 1) BGM 페이드아웃(무음) — 사용자 슬라이더 값은 그대로 유지
        if (_mixer)
        {
            if (_bgmCo != null) StopCoroutine(_bgmCo);
            _bgmCo = StartCoroutine(CoFadeBgmDbTo(-80f, bgmFadeOutTime)); // -∞dB 근사
        }

        // 2) UI 지연 후 표시
        StartCoroutine(ShowAfterDelayRealtime());
    }

    IEnumerator ShowAfterDelayRealtime()
    {
        float t = 0f;
        while (t < uiDelay)
        {
            t += Time.unscaledDeltaTime;   // 실제 시간 기준
            yield return null;
        }

        // UI 표시 + 이 타이밍에 정지
        Show();
        Time.timeScale = 0f;

        // [FIX] 패널이 보이는 즉시 숫자 공백으로 초기화(깜빡임 방지)
        PreShowResetUI();

        // 3) 게임오버 인트로 SFX 1회 재생 → 끝날 때까지 대기
        if (sfxSource && sfxGameOverIntro)
        {
            sfxSource.pitch = 1f; // 절대 속도 변조 금지
            sfxSource.PlayOneShot(sfxGameOverIntro, 1f);
            yield return new WaitForSecondsRealtime(sfxGameOverIntro.length);
        }

        // 4) 시퀀스 실행: 카운트업 → (신기록이면) 연출
        yield return StartCoroutine(Sequence_CountUp_ThenFX());

        // 5) 모든 연출이 끝난 뒤 BGM을 "duck 상태"로 부드럽게 복귀
        if (PauseAudioDucker.I)
        {
            float prevDb = GetMixerDb(_bgmParam, -80f);
            float targetDb = PauseAudioDucker.I.pausedBgmDb;   // 예: -8dB
            if (_bgmCo != null) StopCoroutine(_bgmCo);
            _bgmCo = StartCoroutine(CoFadeBgmDb(prevDb, targetDb, bgmFadeInTime));

            float targetHz = PauseAudioDucker.I.pausedLpHz;    // 예: 1000Hz
            SetMixerFloatSmooth(_lpParam, targetHz, bgmFadeInTime);
        }
    }

    // 숫자·배지 사전 초기화(Show 직후 호출)
    void PreShowResetUI()
    {
        if (thisValueText) thisValueText.text = "";
        if (bestValueText) bestValueText.text = "";
        if (newBadge) newBadge.SetActive(false);
        if (newRecordBanner) newRecordBanner.gameObject.SetActive(false);

        // 시간은 미리 노출하고 싶으면 주석 해제
        // if (thisTimeText) thisTimeText.text = cachedThisTime;
        // if (bestTimeText) bestTimeText.text = cachedBestTime;
    }

    IEnumerator Sequence_CountUp_ThenFX()
    {
        // 시간 텍스트 채우기
        if (thisTimeText) thisTimeText.text = cachedThisTime;
        if (bestTimeText) bestTimeText.text = cachedBestTime;

        if (newBadge) newBadge.SetActive(false);
        if (newRecordBanner) newRecordBanner.gameObject.SetActive(false);

        if (bestValueText) bestValueText.text = ""; // This Run 끝날 때까지 빈 칸

        if (thisValueText)
            yield return CountUpNumber(thisValueText, 0, cachedThisRun, countThisDuration);

        if (bestValueText)
            yield return CountUpNumber(bestValueText, 0, cachedBest, countBestDuration);

        if (cachedIsNew)
        {
            if (newBadge) newBadge.SetActive(true);
            PlayNewRecordFX(); // 내부에서 sfxNewRecord 재생
        }
    }

    // ========= 숫자 카운트업(단위 m) + Tick/Confirm SFX =========
    IEnumerator CountUpNumber(TMP_Text target, int from, int to, float duration)
    {
        if (!target) yield break;

        if (duration <= 0.0001f)
        {
            target.text = $"{to} m";
            if (sfxSource && sfxConfirm)
            {
                sfxSource.pitch = 1f;
                sfxSource.PlayOneShot(sfxConfirm, 1f);
            }
            yield break;
        }

        float t = 0f;
        float tickTimer = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;  // 정지 상태에서도 진행
            float k = Mathf.Clamp01(t / duration);
            if (easeOut) k = EaseOutQuad(k);

            int value = Mathf.RoundToInt(Mathf.Lerp(from, to, k));
            target.text = $"{value} m";

            // Tick: 속도 고정(피치=1), 간격 기반 재생
            if (sfxSource && sfxTick)
            {
                if (tickInterval <= 0f)
                {
                    sfxSource.pitch = 1f;
                    sfxSource.PlayOneShot(sfxTick, 1f);
                }
                else
                {
                    tickTimer += Time.unscaledDeltaTime;
                    if (tickTimer >= tickInterval)
                    {
                        tickTimer = 0f;
                        sfxSource.pitch = 1f;
                        sfxSource.PlayOneShot(sfxTick, 1f);
                    }
                }
            }
            yield return null;
        }

        target.text = $"{to} m";

        // 완료음
        if (sfxSource && sfxConfirm)
        {
            sfxSource.pitch = 1f;
            sfxSource.PlayOneShot(sfxConfirm, 1f);
        }
    }

    // ========= 표시/숨김 =========
    void Show()
    {
        hiddenList.Clear();
        if (hideWhenShown != null)
        {
            foreach (var go in hideWhenShown)
            {
                if (go && go.activeSelf)
                {
                    go.SetActive(false);
                    hiddenList.Add(go);
                }
            }
        }

        if (!cg) return;
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        IsShown = true;
        OnShown?.Invoke();
    }

    void HideImmediate()
    {
        if (!cg) return;
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        IsShown = false;
    }

    void RestoreHUD()
    {
        foreach (var go in hiddenList)
            if (go) go.SetActive(true);
        hiddenList.Clear();
    }

    // ========= 버튼 =========
    public void OnClickRestart()
    {
        Time.timeScale = 1f;
        InputLocked = false;
        suppressRestoreHUD = true;
        var s = SceneManager.GetActiveScene();
        SceneManager.LoadScene(s.buildIndex);
    }

    public void OnClickHome()
    {
        Time.timeScale = 1f;
        InputLocked = false;
        suppressRestoreHUD = true;

        if (titlePanel)
        {
            HideImmediate();
            if (!titlePanel.activeSelf) titlePanel.SetActive(true);
            return;
        }

        if (!string.IsNullOrEmpty(titleSceneName))
        {
            SceneManager.LoadScene(titleSceneName);
            return;
        }

        Debug.LogWarning("[GameOverUI] TitlePanel/Scene 설정이 없어 현재 씬을 재시작합니다.");
        var sc = SceneManager.GetActiveScene();
        SceneManager.LoadScene(sc.buildIndex);
    }

    // ========= 신기록 연출 =========
    void PlayNewRecordFX()
    {
        if (flashOverlay) StartCoroutine(FlashWhite(flashOverlay, flashMaxAlpha, flashDuration));
        if (newRecordBanner) StartCoroutine(BannerBlinkLoop(newRecordBanner));

        if (sfxSource && sfxNewRecord)
        {
            sfxSource.pitch = 1f;
            sfxSource.PlayOneShot(sfxNewRecord, 1f);
        }
    }

    IEnumerator FlashWhite(Image img, float maxAlpha, float dur)
    {
        Color c = img.color;
        float up = dur * 0.4f;
        float down = dur - up;

        // up
        float t = 0f;
        while (t < up)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / up);
            c.a = Mathf.Lerp(0f, maxAlpha, k);
            img.color = c;
            yield return null;
        }
        // down
        t = 0f;
        while (t < down)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / down);
            c.a = Mathf.Lerp(maxAlpha, 0f, k);
            img.color = c;
            yield return null;
        }
        c.a = 0f; img.color = c;
    }

    IEnumerator BannerBlinkLoop(TMP_Text banner)
    {
        banner.gameObject.SetActive(true);
        float speed = 5f;
        Color baseC = banner.color;
        while (IsShown)
        {
            float a = Mathf.Lerp(0.35f, 1f, (Mathf.Sin(Time.unscaledTime * speed) + 1f) * 0.5f);
            banner.color = new Color(baseC.r, baseC.g, baseC.b, a);
            yield return null;
        }
        banner.color = new Color(baseC.r, baseC.g, baseC.b, 1f);
        banner.gameObject.SetActive(false);
    }

    // ========= Mixer helpers =========
    IEnumerator CoFadeBgmDbTo(float toDb, float time)
    {
        float fromDb = GetMixerDb(_bgmParam, toDb);
        yield return StartCoroutine(CoFadeBgmDb(fromDb, toDb, time));
    }

    IEnumerator CoFadeBgmDb(float fromDb, float toDb, float time)
    {
        if (_mixer == null || time <= 0f)
        {
            SetMixerDb(_bgmParam, toDb);
            yield break;
        }

        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime; // 정지 상태에서도 페이드
            float k = Mathf.Clamp01(t / time);
            float cur = Mathf.Lerp(fromDb, toDb, k);
            SetMixerDb(_bgmParam, cur);
            yield return null;
        }
        SetMixerDb(_bgmParam, toDb);
    }

    float GetMixerDb(string param, float fallback)
    {
        if (_mixer != null && _mixer.GetFloat(param, out float v)) return v;
        return fallback;
    }
    void SetMixerDb(string param, float v)
    {
        if (_mixer != null) _mixer.SetFloat(param, v);
    }

    void SetMixerFloatSmooth(string param, float to, float time)
    {
        if (_mixer == null) return;
        StartCoroutine(CoSetFloatSmooth(param, to, time));
    }
    IEnumerator CoSetFloatSmooth(string param, float to, float time)
    {
        if (time <= 0f)
        {
            _mixer.SetFloat(param, to);
            yield break;
        }
        _mixer.GetFloat(param, out float from);
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / time);
            float cur = Mathf.Lerp(from, to, k);
            _mixer.SetFloat(param, cur);
            yield return null;
        }
        _mixer.SetFloat(param, to);
    }

    // ========= Easing =========
    float EaseOutQuad(float x) => 1f - (1f - x) * (1f - x);
}
