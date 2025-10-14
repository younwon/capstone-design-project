using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class GameOverUI : MonoBehaviour
{
    public static bool InputLocked { get; private set; } = false;
    public static bool IsShown { get; private set; } = false;

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
    [SerializeField] GameObject newBadge;
    [SerializeField] TMP_Text newRecordBanner;
    [SerializeField] Image flashOverlay;

    [Header("SFX (optional)")]
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioClip sfxNewRecord;

    [Header("Flash Tuning")]
    [SerializeField, Range(0f, 1f)] float flashMaxAlpha = 0.9f;
    [SerializeField] float flashDuration = 0.6f;

    [Header("CountUp Tuning")]
    [SerializeField] float countThisDuration = 0.6f;
    [SerializeField] float countBestDuration = 0.6f;
    [SerializeField] bool easeOut = true;

    [Header("Title/Home Options")]
    [SerializeField] GameObject titlePanel;          // 같은 씬 타이틀 UI (있어도 이번엔 사용 X)
    [SerializeField] string titleSceneName = "Title";// 백업용(별도 씬)

    [Header("Timing")]
    [SerializeField] float uiDelay = 0.8f;

    [Header("Hide these when shown")]
    [SerializeField] GameObject[] hideWhenShown;

    [Header("Score Sources (auto)")]
    [SerializeField] ScoreSystem score;             // 일반 모드
    [SerializeField] ScoreSystem_Speed speedScore;  // 스피드런 모드

    readonly List<GameObject> hiddenList = new List<GameObject>();
    bool suppressRestoreHUD = false;

    int cachedThisRun = 0;
    int cachedBest = 0;
    string cachedThisTime = "00:00";
    string cachedBestTime = "00:00";
    bool cachedIsNew = false;
    bool cachedIsSpeedMode = false;

    void Awake()
    {
        HideImmediate();

        if (!score) score = ScoreSystem.I;
        if (!speedScore) speedScore = FindObjectOfType<ScoreSystem_Speed>();

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
        cachedIsSpeedMode = (speedScore != null && speedScore.isActiveAndEnabled);

        if (cachedIsSpeedMode)
        {
            cachedThisRun = speedScore.DisplayScore; // FreezeAtEnd 반영
            cachedBest = speedScore.BestEverScore;
            cachedIsNew = speedScore.IsNewRecord;
            cachedThisTime = "--:--";
            cachedBestTime = "--:--";
        }
        else if (score != null)
        {
            cachedThisRun = score.SessionMaxScore;
            cachedBest = score.BestEverScore;
            cachedThisTime = ScoreSystem.FormatMMSS(score.ThisRunTime);
            cachedBestTime = ScoreSystem.FormatMMSS(score.BestEverTime);
            cachedIsNew = score.IsNewRecord;
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
        if (cachedIsSpeedMode)
        {
            if (thisTimeText) thisTimeText.gameObject.SetActive(false);
            if (bestTimeText) bestTimeText.gameObject.SetActive(false);
        }
        else
        {
            if (thisTimeText) { thisTimeText.gameObject.SetActive(true); thisTimeText.text = cachedThisTime; }
            if (bestTimeText) { bestTimeText.gameObject.SetActive(true); bestTimeText.text = cachedBestTime; }
        }

        if (newBadge) newBadge.SetActive(false);
        if (newRecordBanner) newRecordBanner.gameObject.SetActive(false);

        if (bestValueText) bestValueText.text = "";

        if (thisValueText)
            yield return CountUpNumber(thisValueText, 0, cachedThisRun, countThisDuration);

        if (bestValueText)
            yield return CountUpNumber(bestValueText, 0, cachedBest, countBestDuration);

        if (cachedIsNew)
        {
            if (newBadge) newBadge.SetActive(true);
            PlayNewRecordFX();
        }
    }

    IEnumerator CountUpNumber(TMP_Text target, int from, int to, float duration)
    {
        if (!target) yield break;
        if (duration <= 0.0001f) { target.text = $"{to} m"; yield break; }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            if (easeOut) k = 1f - (1f - k) * (1f - k);

            int value = Mathf.RoundToInt(Mathf.Lerp(from, to, k));
            target.text = $"{value} m";
            yield return null;
        }
        target.text = $"{to} m";
    }

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

        StartGate.SkipTitleOnce = true;  // 재시작 시 바로 시작
        var s = SceneManager.GetActiveScene();
        SceneManager.LoadScene(s.buildIndex);
    }

    // 🔧 여기만 변경: 타이틀로는 '씬 리로드'로 돌아가 초기 상태 보장
    public void OnClickHome()
    {
        Time.timeScale = 1f;
        InputLocked = false;
        suppressRestoreHUD = true;

        // 타이틀에서 자동 시작 금지
        StartGate.SkipTitleOnce = false;

        var sc = SceneManager.GetActiveScene();
        SceneManager.LoadScene(sc.buildIndex); // 씬을 처음 상태로 리셋 → TitleUI가 타이틀 셋업
    }

    void PlayNewRecordFX()
    {
        if (flashOverlay) StartCoroutine(FlashWhite(flashOverlay, flashMaxAlpha, flashDuration));
        if (newRecordBanner) StartCoroutine(BannerBlinkLoop(newRecordBanner));
        if (sfxSource && sfxNewRecord) sfxSource.PlayOneShot(sfxNewRecord);
    }

    IEnumerator FlashWhite(Image img, float maxAlpha, float dur)
    {
        Color c = img.color;
        float up = dur * 0.4f, down = dur - up;

        float t = 0f;
        while (t < up) { t += Time.unscaledDeltaTime; c.a = Mathf.Lerp(0f, maxAlpha, t / up); img.color = c; yield return null; }
        t = 0f;
        while (t < down) { t += Time.unscaledDeltaTime; c.a = Mathf.Lerp(maxAlpha, 0f, t / down); img.color = c; yield return null; }
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
}





