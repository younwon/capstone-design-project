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
    [SerializeField] GameObject newBadge;             // (선택) NEW 배지 아이콘
    [SerializeField] TMP_Text newRecordBanner;        // "NEW RECORD!" (비활성 시작)
    [SerializeField] Image flashOverlay;              // 화면 전체 흰 이미지(알파 0)

    [Header("SFX (optional)")]
    [SerializeField] AudioSource sfxSource;           // UI 전용 오디오 소스(PlayOnAwake 꺼짐)
    [SerializeField] AudioClip sfxNewRecord;          // 신기록 효과음(선택)

    [Header("Flash Tuning")]
    [SerializeField, Range(0f, 1f)] float flashMaxAlpha = 0.9f; // 플래시 세기
    [SerializeField] float flashDuration = 0.6f;                // 플래시 길이(초)

    [Header("CountUp Tuning")]
    [SerializeField] float countThisDuration = 0.6f; // This Run 카운트업 시간
    [SerializeField] float countBestDuration = 0.6f; // Best 카운트업 시간
    [SerializeField] bool easeOut = true;            // 끝으로 갈수록 천천히

    [Header("Title/Home Options")]
    [SerializeField] GameObject titlePanel;           // 같은 씬 타이틀 UI
    [SerializeField] string titleSceneName = "Title"; // 별도 씬 이름

    [Header("Timing")]
    [SerializeField, Tooltip("게임오버 후 UI가 뜨기까지의 딜레이(초, 실제시간)")]
    float uiDelay = 0.8f;

    [Header("Hide these when shown")]
    [SerializeField] GameObject[] hideWhenShown;

    readonly List<GameObject> hiddenList = new List<GameObject>();
    bool suppressRestoreHUD = false;

    // --- 캐시(게임오버 시점 값) ---
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
        if (flashOverlay) flashOverlay.color = new Color(1, 1, 1, 0); // 알파 0 시작
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

    // ====== 게임오버 신호 수신 ======
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
            t += Time.unscaledDeltaTime;   // 실제 시간 기준
            yield return null;
        }

        // UI 표시 + 이 타이밍에 정지
        Show();
        Time.timeScale = 0f;

        // 시퀀스 실행: 카운트업 → (신기록이면) 연출
        StartCoroutine(Sequence_CountUp_ThenFX());
    }

    IEnumerator Sequence_CountUp_ThenFX()
    {
        // 초기 텍스트
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
            PlayNewRecordFX();
        }
    }

    // ====== 숫자 카운트업(단위 m 고정) ======
    IEnumerator CountUpNumber(TMP_Text target, int from, int to, float duration)
    {
        if (!target) yield break;

        if (duration <= 0.0001f)
        {
            target.text = $"{to} m";
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;  // 정지 상태에서도 진행
            float k = Mathf.Clamp01(t / duration);
            if (easeOut) k = EaseOutQuad(k);

            int value = Mathf.RoundToInt(Mathf.Lerp(from, to, k));
            target.text = $"{value} m";
            yield return null;
        }
        target.text = $"{to} m";
    }

    // ====== 표시/숨김 ======
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

    // ====== 신기록 연출(플래시 + 배너 + SFX) ======
    void PlayNewRecordFX()
    {
        if (flashOverlay) StartCoroutine(FlashWhite(flashOverlay, flashMaxAlpha, flashDuration));
        if (newRecordBanner) StartCoroutine(BannerBlinkLoop(newRecordBanner));   // UI 열려있는 동안 계속 깜빡

        // (옵션) 사운드
        if (sfxSource && sfxNewRecord)
            sfxSource.PlayOneShot(sfxNewRecord);
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
        while (IsShown) // 패널이 열려 있는 동안 계속 깜빡
        {
            float a = Mathf.Lerp(0.35f, 1f, (Mathf.Sin(Time.unscaledTime * speed) + 1f) * 0.5f);
            banner.color = new Color(baseC.r, baseC.g, baseC.b, a);
            yield return null;
        }
        // 닫히면 원복
        banner.color = new Color(baseC.r, baseC.g, baseC.b, 1f);
        banner.gameObject.SetActive(false);
    }

    // ===== Easing =====
    float EaseOutQuad(float x) => 1f - (1f - x) * (1f - x);
}





