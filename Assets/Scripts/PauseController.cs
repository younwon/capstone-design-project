using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using TMPro;

public class PauseController : MonoBehaviour
{
    [Header("Panel & Buttons")]
    [SerializeField] GameObject pausePanel;     // PausePanel 루트
    [SerializeField] Button btnResume;          // Resume
    [SerializeField] Button btnSettings;        // Settings
    [SerializeField] Button btnRestart;         // Restart (팝업 열기)
    [SerializeField] Button btnHome;            // Home (팝업 열기)
    [SerializeField] Button btnPauseIcon;       // 우상단 일시정지 아이콘 버튼

    [Header("Settings Panel")]
    [SerializeField] GameObject settingsPanel;  // 기존 SettingsPanel

    [Header("Labels (숫자 전용 텍스트)")]
    [SerializeField] TMP_Text txtCurrent;       // CURRENT 값만(예: "123 m")
    [SerializeField] TMP_Text txtRunBest;       // THIS RUN
    [SerializeField] TMP_Text txtBest;          // BEST

    [Header("Score Source")]
    [SerializeField] ScoreSystem score;         // 드래그 또는 자동 할당

    [Header("Popups")]
    [SerializeField] GameObject popupRestart;
    [SerializeField] Button btnRestart_No;
    [SerializeField] Button btnRestart_Yes;

    [SerializeField] GameObject popupHome;
    [SerializeField] Button btnHome_No;
    [SerializeField] Button btnHome_Yes;

    [Header("Title (choose one later)")]
    [Tooltip("별도 씬 전환을 사용할 경우 씬 이름. 비워두면 같은 씬 UI(titlePanel) 방식을 우선 사용.")]
    [SerializeField] string titleSceneName = "";     // A 방식: 별도 씬
    [Tooltip("같은 씬 UI로 타이틀을 쓸 경우 연결. 지금은 비워둬도 됨.")]
    [SerializeField] GameObject titlePanel;          // B 방식: 같은 씬 UI

    [Header("Optional Hooks")]
    [Tooltip("Home 확정 시 호출되는 훅(예: GameManager.ResetToTitle). 선택사항.")]
    [SerializeField] UnityEvent onHomeConfirmed;
    [Tooltip("Restart 확정 시 호출되는 훅(씬 리로드 전에 상태 정리 등). 선택사항.")]
    [SerializeField] UnityEvent onRestartConfirmed;

    float prevTimeScale = 1f;
    bool isPaused = false;

    void Awake()
    {
        // ScoreSystem 자동 할당(비어 있으면 싱글턴 사용)
        if (!score) score = ScoreSystem.I;

        // 초기 비활성
        if (pausePanel) pausePanel.SetActive(false);
        if (popupRestart) popupRestart.SetActive(false);
        if (popupHome) popupHome.SetActive(false);

        // 버튼 리스너
        if (btnResume) btnResume.onClick.AddListener(ClosePause);
        if (btnSettings) btnSettings.onClick.AddListener(OpenSettingsFromPause);
        if (btnPauseIcon) btnPauseIcon.onClick.AddListener(TogglePause);

        if (btnRestart) btnRestart.onClick.AddListener(OpenRestartPopup);
        if (btnRestart_No) btnRestart_No.onClick.AddListener(() => { if (popupRestart) popupRestart.SetActive(false); });
        if (btnRestart_Yes) btnRestart_Yes.onClick.AddListener(ConfirmRestart);

        if (btnHome) btnHome.onClick.AddListener(OpenHomePopup);
        if (btnHome_No) btnHome_No.onClick.AddListener(() => { if (popupHome) popupHome.SetActive(false); });
        if (btnHome_Yes) btnHome_Yes.onClick.AddListener(ConfirmHome);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    // ---------- Pause Toggle ----------
    public void TogglePause()
    {
        if (isPaused) ClosePause();
        else OpenPause();
    }

    public void OpenPause()
    {
        if (isPaused) return;

        // (중요) 정지 걸기 전에 최신 수치 스냅샷 → UI 업데이트
        RefreshStatsFromScore();

        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;           // 게임 정지
        isPaused = true;

        if (btnPauseIcon) btnPauseIcon.gameObject.SetActive(false);
        if (pausePanel) pausePanel.SetActive(true);
    }

    public void ClosePause()
    {
        if (!isPaused) return;

        // 열린 UI/팝업 정리
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
        if (settingsPanel) settingsPanel.SetActive(true); // 일시정지 유지
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
        onRestartConfirmed?.Invoke(); // 필요하면 여기서 score.ResetRun()도 호출 가능

        // 같은 씬 구조여도 리로드가 깔끔(모든 상태 초기화)
        string scene = SceneManager.GetActiveScene().name;
        Time.timeScale = 1f;
        SceneManager.LoadScene(scene);
    }

    void ConfirmHome()
    {
        onHomeConfirmed?.Invoke(); // B 방식이면 여기서 GameManager.ResetToTitle 등 연결

        Time.timeScale = 1f;

        // 1) 같은 씬 UI(B 방식) 우선
        if (titlePanel)
        {
            if (pausePanel && pausePanel.activeSelf) pausePanel.SetActive(false);
            if (settingsPanel && settingsPanel.activeSelf) settingsPanel.SetActive(false);
            if (popupHome && popupHome.activeSelf) popupHome.SetActive(false);
            if (popupRestart && popupRestart.activeSelf) popupRestart.SetActive(false);

            titlePanel.SetActive(true);              // 타이틀 UI 열기
            if (btnPauseIcon) btnPauseIcon.gameObject.SetActive(true);
            isPaused = false;
            return;
        }

        // 2) 별도 씬(A 방식)
        if (!string.IsNullOrEmpty(titleSceneName))
        {
            SceneManager.LoadScene(titleSceneName);
            return;
        }

        // 3) 둘 다 없으면 안전복귀
        Debug.LogWarning("[PauseController] TitlePanel/TitleScene이 설정되지 않았습니다. 일시정지를 해제합니다.");
        ClosePause();
    }

    // ---------- Stats UI ----------
    public void RefreshStatsFromScore()
    {
        // score가 없거나 아직 초기화 전이면 0으로 표시
        int cur = (score != null) ? score.CurrentScore : 0;
        int run = (score != null) ? score.SessionMaxScore : 0;
        int best = (score != null) ? score.BestEverScore : 0;

        if (txtCurrent) txtCurrent.text = $"{cur} m";
        if (txtRunBest) txtRunBest.text = $"{run} m";
        if (txtBest) txtBest.text = $"{best} m";
    }
}


