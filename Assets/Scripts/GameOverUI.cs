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
    [SerializeField] GameObject titlePanel;
    [SerializeField] string titleSceneName = "Title"; // 사용 안 해도 됨(백업)

    [Header("Timing")]
    [SerializeField] float uiDelay = 0.8f;

    [Header("Hide these when shown")]
    [SerializeField] GameObject[] hideWhenShown;

    readonly List<GameObject> hiddenList = new List<GameObject>();
    bool suppressRestoreHUD = false;

    int cachedThisRun = 0;
    int cachedBest = 0;
    string cachedThisTime = "00:00";
    string cachedBestTime = "00:00";
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

        StartGate.SkipTitleOnce = true;  // 재시작 시 타이틀 스킵
        var s = SceneManager.GetActiveScene();
        SceneManager.LoadScene(s.buildIndex);
    }

    public void OnClickHome()
    {
        Time.timeScale = 1f;
        InputLocked = false;
        suppressRestoreHUD = true;

        // 같은 씬 UI 방식: Title로 복귀
        var title = Object.FindObjectOfType<TitleUIController>();
        if (title)
        {
            HideImmediate();
            title.ReturnToTitle();
            return;
        }

        // (백업) 별도 씬이 있을 때만 사용
        if (!string.IsNullOrEmpty(titleSceneName))
        {
            SceneManager.LoadScene(titleSceneName);
            return;
        }

        // 둘 다 없으면 재시작
        var sc = SceneManager.GetActiveScene();
        SceneManager.LoadScene(sc.buildIndex);
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




