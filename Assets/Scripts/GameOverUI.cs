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
    [SerializeField] TMP_Text thisValueText;
    [SerializeField] TMP_Text bestValueText;
    [SerializeField] TMP_Text thisTimeText;
    [SerializeField] TMP_Text bestTimeText;

    [Header("New Record Visuals")]
    [SerializeField] GameObject newBadge;
    [SerializeField] TMP_Text newRecordBanner;
    [SerializeField] Image flashOverlay;

    [Header("SFX (optional)")]
    [SerializeField] AudioSource sfxSource;    // UI 전용 오디오 소스
    [SerializeField] AudioClip sfxNewRecord;   // 신기록 효과음(선택)

    // ==== Tick / Confirm ====
    [SerializeField] AudioClip sfxTick;
    [SerializeField] AudioClip sfxConfirm;
    [SerializeField, Tooltip("틱 사운드 간격(초). 0이면 값이 바뀔 때마다 재생")]
    float tickInterval = 0.05f;

    [Header("Button Click SFX (GameOver only)")]
    [Tooltip("게임오버 UI의 Restart/Home 클릭음")]
    [SerializeField] AudioClip sfxButtonClick;

    [Header("Flash Tuning")]
    [SerializeField, Range(0f, 1f)] float flashMaxAlpha = 0.9f;
    [SerializeField] float flashDuration = 0.6f;

    [Header("CountUp Tuning")]
    [SerializeField] float countThisDuration = 0.6f;
    [SerializeField] float countBestDuration = 0.6f;
    [SerializeField] bool easeOut = true;

    [Header("Title/Home Options")]
    [SerializeField] GameObject titlePanel;
    [SerializeField] string titleSceneName = "Title";

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

        // ====== [핵심 추가 1] sfxSource 없으면 자동 생성 ======
        if (!sfxSource)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }

        // ====== [핵심 추가 2] AudioManager의 SFX 그룹에 연결 ======
        if (AudioManager.I && AudioManager.I.SfxGroup)
        {
            sfxSource.outputAudioMixerGroup = AudioManager.I.SfxGroup;
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
        while (t < uiDelay)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

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

        if (duration <= 0.0001f)
        {
            target.text = $"{to} m";
            if (AudioManager.I && sfxConfirm) AudioManager.I.PlaySFX(sfxConfirm);
            yield break;
        }

        float t = 0f;
        float tickTimer = 0f;
        int lastValue = from - 1;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            tickTimer += Time.unscaledDeltaTime;

            float k = Mathf.Clamp01(t / duration);
            if (easeOut) k = EaseOutQuad(k);

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

    public static System.Action OnShown;

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

        if (GlobalButtonSFX.I) GlobalButtonSFX.I.Refresh();

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

    // ====== 버튼 ======
    public void OnClickRestart()
    {
        PlayButtonClickSfx();   // ★ [핵심] 여기서 무조건 재생

        Time.timeScale = 1f;
        InputLocked = false;
        suppressRestoreHUD = true;

        StartGate.SkipTitleOnce = true;

        HideImmediate();

        var s = SceneManager.GetActiveScene();
        SceneManager.LoadScene(s.buildIndex);
    }

    public void OnClickHome()
    {
        PlayButtonClickSfx();   // ★ [핵심] 여기서 무조건 재생

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

    void PlayButtonClickSfx()
    {
        if (sfxSource && sfxButtonClick)
            sfxSource.PlayOneShot(sfxButtonClick);
    }

    void PlayNewRecordFX()
    {
        if (flashOverlay) StartCoroutine(FlashWhite(flashOverlay, flashMaxAlpha, flashDuration));
        if (newRecordBanner) StartCoroutine(BannerBlinkLoop(newRecordBanner));

        if (sfxSource && sfxNewRecord)
            sfxSource.PlayOneShot(sfxNewRecord);
    }

    IEnumerator FlashWhite(Image img, float maxAlpha, float dur)
    {
        Color c = img.color;
        float up = dur * 0.4f;
        float down = dur - up;

        float t = 0f;
        while (t < up)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / up);
            c.a = Mathf.Lerp(0f, maxAlpha, k);
            img.color = c;
            yield return null;
        }

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

    float EaseOutQuad(float x) => 1f - (1f - x) * (1f - x);
}

