// /Scripts/UI/GameOverUI_SpeedRun.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverUI_SpeedRun : MonoBehaviour
{
    public static bool InputLocked { get; private set; } = false;
    public static bool IsShown { get; private set; } = false;

    [Header("Refs")]
    [SerializeField] CanvasGroup cg;
    [SerializeField] Button btnRestart;   // 재시작(즉시 게임 다시 시작)
    [SerializeField] Button btnHome;      // 타이틀로(같은 씬 처음 상태)

    [Header("Texts")]
    [SerializeField] TMP_Text thisValueText;   // "123 m"
    [SerializeField] TMP_Text bestValueText;   // "456 m"
    [SerializeField] TMP_Text thisTimeText;    // "00:45"
    [SerializeField] TMP_Text bestTimeText;    // "--:--"

    [Header("New Record Visuals")]
    [SerializeField] GameObject newBadge;
    [SerializeField] TMP_Text newRecordBanner;
    [SerializeField] Image flashOverlay;

    [Header("SFX (optional)")]
    [Tooltip("숫자 카운트 중 '틱' 사운드")]
    [SerializeField] AudioClip sfxTick;
    [Tooltip("카운트 완료 확정 사운드")]
    [SerializeField] AudioClip sfxConfirm;

    [Tooltip("★ 신기록일 때만 재생되는 효과음 (NEW RECORD SFX)")]
    [SerializeField] AudioClip sfxBestRecord;

    [SerializeField, Tooltip("틱 사운드 간격(초). 0이면 값이 바뀔 때마다 재생")]
    float tickInterval = 0.05f;

    [Header("Flash Tuning")]
    [SerializeField, Range(0f, 1f)] float flashMaxAlpha = 0.9f;
    [SerializeField] float flashDuration = 0.6f;

    [Header("CountUp Tuning")]
    [SerializeField] float countThisDuration = 0.6f;
    [SerializeField] float countBestDuration = 0.6f;
    [SerializeField] bool easeOut = true;

    [Header("Timing")]
    [SerializeField, Tooltip("게임오버 후 UI가 뜨기까지의 딜레이(초, 실제시간)")]
    float uiDelay = 0.8f;

    [Header("Hide these when shown")]
    [SerializeField] GameObject[] hideWhenShown;

    readonly List<GameObject> hiddenList = new List<GameObject>();
    bool suppressRestoreHUD = false;

    // 캐시
    int cachedThisRun = 0;
    int cachedBest = 0;
    string cachedThisTime = "00:00";
    string cachedBestTime = "--:--";
    bool cachedIsNew = false;

    void Awake()
    {
        HideImmediate();

        SimpleGameOver.OnGameOver += OnGameOverSimple;
        if (btnRestart) btnRestart.onClick.AddListener(OnClickRestart);
        if (btnHome) btnHome.onClick.AddListener(OnClickHome);

        if (newRecordBanner) newRecordBanner.gameObject.SetActive(false);
        if (flashOverlay) flashOverlay.color = new Color(1, 1, 1, 0);
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

    void OnGameOverSimple(string reason)
    {
        var speed = ScoreSystem_Speed.I;
        var cd = FindObjectOfType<CountDown>(true);

        if (speed != null)
        {
            cachedThisRun = Mathf.Max(0, speed.DisplayScore);
            cachedBest = Mathf.Max(0, speed.BestEverScore);

            if (cd != null)
                cachedThisTime = ScoreSystem.FormatMMSS(Mathf.Max(0f, cd.InitialSeconds - cd.timeRemaining));
            else
                cachedThisTime = ScoreSystem.FormatMMSS(Mathf.Max(0f, speed.ThisRunTime));

            cachedBestTime = "--:--";

            bool computedNew = cachedThisRun > cachedBest;
            bool flagNew = speed.IsNewRecord;
            cachedIsNew = computedNew || (flagNew && cachedThisRun >= cachedBest);
        }
        else
        {
            cachedThisRun = cachedBest = 0;
            cachedThisTime = cachedBestTime = "00:00";
            cachedIsNew = false;
        }

        InputLocked = true;
        StartCoroutine(ShowAfterDelayRealtime());
    }

    IEnumerator ShowAfterDelayRealtime()
    {
        float t = 0f;
        while (t < uiDelay) { t += Time.unscaledDeltaTime; yield return null; }

        Show();
        Time.timeScale = 0f;
        StartCoroutine(Sequence_CountUp_ThenFX());
    }

    IEnumerator Sequence_CountUp_ThenFX()
    {
        if (thisTimeText) thisTimeText.text = cachedThisTime;
        if (bestTimeText) bestTimeText.text = cachedBestTime;

        if (newBadge) newBadge.SetActive(false);
        if (newRecordBanner) newRecordBanner.gameObject.SetActive(false);
        if (bestValueText) bestValueText.text = "";

        if (thisValueText) yield return CountUpNumber(thisValueText, 0, cachedThisRun, countThisDuration);
        if (bestValueText) yield return CountUpNumber(bestValueText, 0, cachedBest, countBestDuration);

        if (cachedIsNew)
        {
            if (newBadge) newBadge.SetActive(true);
            PlayNewRecordFX();
        }
    }

    // ========= [여기만 클래식 방식으로 싱크 수정] =========
    IEnumerator CountUpNumber(TMP_Text target, int from, int to, float duration)
    {
        if (!target) yield break;

        if (duration <= 0.0001f)
        {
            target.text = $"{to} m";
            if (AudioManager.I && sfxConfirm) AudioManager.I.PlaySFX(sfxConfirm);
            yield break;
        }

        float t = 0f;
        float tickTimer = 0f;

        // ★ 핵심: 이전 값 기억해서 "값이 바뀌는 순간"에만 틱
        int lastValue = from - 1;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            tickTimer += Time.unscaledDeltaTime;

            float k = Mathf.Clamp01(t / duration);
            if (easeOut) k = 1f - (1f - k) * (1f - k);

            int value = Mathf.RoundToInt(Mathf.Lerp(from, to, k));

            if (value != lastValue)
            {
                lastValue = value;
                target.text = $"{value} m";

                if (AudioManager.I && sfxTick)
                {
                    if (tickInterval <= 0f || tickTimer >= tickInterval)
                    {
                        tickTimer = 0f;
                        AudioManager.I.PlaySFX(sfxTick);
                    }
                }
            }

            yield return null;
        }

        target.text = $"{to} m";

        if (AudioManager.I && sfxConfirm)
            AudioManager.I.PlaySFX(sfxConfirm);
    }
    // =====================================================

    void Show()
    {
        hiddenList.Clear();
        if (hideWhenShown != null)
        {
            foreach (var go in hideWhenShown)
                if (go && go.activeSelf) { go.SetActive(false); hiddenList.Add(go); }
        }

        if (!cg) return;
        cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true;
        IsShown = true;

        if (GlobalButtonSFX.I) GlobalButtonSFX.I.Refresh();
    }

    void HideImmediate()
    {
        if (!cg) return;
        cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false;
        IsShown = false;
    }

    void RestoreHUD()
    {
        foreach (var go in hiddenList) if (go) go.SetActive(true);
        hiddenList.Clear();
    }

    public void OnClickRestart()
    {
        Time.timeScale = 1f;
        InputLocked = false;
        suppressRestoreHUD = true;

        StartGate.SkipTitleOnce = true;
        var s = SceneManager.GetActiveScene();
        SceneManager.LoadScene(s.buildIndex);
    }

    public void OnClickHome()
    {
        Time.timeScale = 1f;
        InputLocked = false;
        suppressRestoreHUD = true;

        StartGate.SkipTitleOnce = false;
        var sc = SceneManager.GetActiveScene();
        SceneManager.LoadScene(sc.buildIndex);
    }

    void PlayNewRecordFX()
    {
        if (flashOverlay) StartCoroutine(FlashWhite(flashOverlay, flashMaxAlpha, flashDuration));
        if (newRecordBanner) StartCoroutine(BlinkBanner(newRecordBanner));

        if (AudioManager.I && sfxBestRecord)
            AudioManager.I.PlaySFX(sfxBestRecord);
    }

    IEnumerator FlashWhite(Image img, float maxAlpha, float dur)
    {
        Color c = img.color;
        float up = dur * 0.4f, down = dur - up;

        float t = 0f;
        while (t < up)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(0f, maxAlpha, t / up);
            img.color = c;
            yield return null;
        }
        t = 0f;
        while (t < down)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(maxAlpha, 0f, t / down);
            img.color = c;
            yield return null;
        }
        c.a = 0f; img.color = c;
    }

    IEnumerator BlinkBanner(TMP_Text banner)
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
}




