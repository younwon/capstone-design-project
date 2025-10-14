// PauseController.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using TMPro;

public class PauseController : MonoBehaviour
{
    [Header("Panel & Buttons")]
    [SerializeField] GameObject pausePanel;
    [SerializeField] Button btnResume;
    [SerializeField] Button btnSettings;
    [SerializeField] Button btnRestart;
    [SerializeField] Button btnHome;
    [SerializeField] Button btnPauseIcon;

    [Header("Settings Panel")]
    [SerializeField] GameObject settingsPanel;

    [Header("Labels")]
    [SerializeField] TMP_Text txtCurrent;   // CURRENT
    [SerializeField] TMP_Text txtRunBest;   // THIS RUN (스피드런에선 숨김)
    [SerializeField] TMP_Text txtBest;      // BEST

    [Header("Score Sources (자동 감지)")]
    [SerializeField] ScoreSystem score;             // 일반 모드
    [SerializeField] ScoreSystem_Speed speedScore;  // 스피드런 모드

    [Header("Popups")]
    [SerializeField] GameObject popupRestart;
    [SerializeField] Button btnRestart_No;
    [SerializeField] Button btnRestart_Yes;

    [SerializeField] GameObject popupHome;
    [SerializeField] Button btnHome_No;
    [SerializeField] Button btnHome_Yes;

    [Header("Optional Hooks")]
    [SerializeField] UnityEvent onHomeConfirmed;
    [SerializeField] UnityEvent onRestartConfirmed;

    float prevTimeScale = 1f;
    bool isPaused = false;

    void Awake()
    {
        // 점수 소스 자동 할당 (비어있으면 씬에서 찾아옴)
        if (!score) score = ScoreSystem.I;
        if (!speedScore) speedScore = FindObjectOfType<ScoreSystem_Speed>();

        if (pausePanel) pausePanel.SetActive(false);
        if (popupRestart) popupRestart.SetActive(false);
        if (popupHome) popupHome.SetActive(false);

        if (btnResume) btnResume.onClick.AddListener(ClosePause);
        if (btnSettings) btnSettings.onClick.AddListener(OpenSettingsFromPause);
        if (btnPauseIcon) btnPauseIcon.onClick.AddListener(TogglePause);

        if (btnRestart) btnRestart.onClick.AddListener(OpenRestartPopup);
        if (btnRestart_No) btnRestart_No.onClick.AddListener(() =>
        {
            if (popupRestart) popupRestart.SetActive(false);
        });
        if (btnRestart_Yes) btnRestart_Yes.onClick.AddListener(ConfirmRestart);

        if (btnHome) btnHome.onClick.AddListener(OpenHomePopup);
        if (btnHome_No) btnHome_No.onClick.AddListener(() =>
        {
            if (popupHome) popupHome.SetActive(false);
        });
        if (btnHome_Yes) btnHome_Yes.onClick.AddListener(ConfirmHome);
    }

    void Update()
    {
        // 게임오버 중에는 ESC 무시
        if (GameOverUI.InputLocked || GameOverUI.IsShown) return;

        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    // ---------- Pause Toggle ----------
    public void TogglePause()
    {
        if (GameOverUI.InputLocked || GameOverUI.IsShown) return;

        if (isPaused) ClosePause();
        else OpenPause();
    }

    public void OpenPause()
    {
        if (GameOverUI.InputLocked || GameOverUI.IsShown) return;
        if (isPaused) return;

        RefreshStatsFromScore();  // 열기 전에 최신 값으로 갱신

        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        isPaused = true;

        if (btnPauseIcon) btnPauseIcon.gameObject.SetActive(false);
        if (pausePanel) pausePanel.SetActive(true);
    }

    public void ClosePause()
    {
        if (!isPaused) return;

        if (popupRestart && popupRestart.activeSelf) popupRestart.SetActive(false);
        if (popupHome && popupHome.activeSelf) popupHome.SetActive(false);
        if (settingsPanel && settingsPanel.activeSelf) settingsPanel.SetActive(false);

        if (pausePanel) pausePanel.SetActive(false);
        if (btnPauseIcon) btnPauseIcon.gameObject.SetActive(true);

        Time.timeScale = prevTimeScale;
        isPaused = false;
    }

    void OpenSettingsFromPause()
    {
        if (settingsPanel) settingsPanel.SetActive(true); // 일시정지 상태 유지
    }

    // ---------- Popups ----------
    void OpenRestartPopup()
    {
        if (popupHome) popupHome.SetActive(false);
        if (popupRestart) popupRestart.SetActive(true);
    }

    void OpenHomePopup()
    {
        if (popupRestart) popupRestart.SetActive(false);
        if (popupHome) popupHome.SetActive(true);
    }

    // ---------- Confirm Actions ----------
    void ConfirmRestart()
    {
        onRestartConfirmed?.Invoke();

        // 다음 씬 로드시 타이틀 건너뛰고 즉시 시작
        StartGate.SkipTitleOnce = true;

        string scene = SceneManager.GetActiveScene().name;
        Time.timeScale = 1f;
        SceneManager.LoadScene(scene);
    }

    void ConfirmHome()
    {
        onHomeConfirmed?.Invoke();

        // 비활성까지 포함해서 TitleUIController 탐색
        var title = Object.FindObjectOfType<TitleUIController>(true);
        if (!title)
        {
            Debug.LogWarning("[PauseController] TitleUIController를 찾지 못했습니다. 안전 복귀.");
            ClosePause();
            return;
        }

        // 열린 UI 및 팝업 닫기
        if (pausePanel && pausePanel.activeSelf) pausePanel.SetActive(false);
        if (settingsPanel && settingsPanel.activeSelf) settingsPanel.SetActive(false);
        if (popupHome && popupHome.activeSelf) popupHome.SetActive(false);
        if (popupRestart && popupRestart.activeSelf) popupRestart.SetActive(false);

        // 타이틀로 복귀 (타이틀에서 Time.timeScale=0 처리)
        Time.timeScale = 1f;
        isPaused = false;
        if (btnPauseIcon) btnPauseIcon.gameObject.SetActive(true);

        title.ReturnToTitle();
    }

    // ---------- Stats UI ----------
    public void RefreshStatsFromScore()
    {
        // 스피드런 모드가 있으면 우선
        if (speedScore != null && speedScore.isActiveAndEnabled)
        {
            int cur = speedScore.DisplayScore;   // 표시용(프리즈 반영)
            int best = speedScore.BestEverScore;

            if (txtCurrent) txtCurrent.text = $"{cur} m";
            if (txtBest) txtBest.text = $"{best} m";

            // 스피드런에선 "THIS RUN" 행 사용 안 함 → 숨김
            if (txtRunBest && txtRunBest.gameObject.activeSelf)
                txtRunBest.gameObject.SetActive(false);

            return;
        }

        // 일반 모드(기존 동작 유지)
        if (txtRunBest && !txtRunBest.gameObject.activeSelf)
            txtRunBest.gameObject.SetActive(true);

        int curN = (score != null) ? score.CurrentScore : 0;
        int runN = (score != null) ? score.SessionMaxScore : 0;
        int bestN = (score != null) ? score.BestEverScore : 0;

        if (txtCurrent) txtCurrent.text = $"{curN} m";
        if (txtRunBest) txtRunBest.text = $"{runN} m";
        if (txtBest) txtBest.text = $"{bestN} m";
    }
}




