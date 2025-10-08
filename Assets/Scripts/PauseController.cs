// PauseController.cs (확인용: Home 처리만 눈여겨봐 주세요)
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
    [SerializeField] TMP_Text txtCurrent;
    [SerializeField] TMP_Text txtRunBest;
    [SerializeField] TMP_Text txtBest;

    [Header("Score Source")]
    [SerializeField] ScoreSystem score;

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
        if (!score) score = ScoreSystem.I;

        if (pausePanel) pausePanel.SetActive(false);
        if (popupRestart) popupRestart.SetActive(false);
        if (popupHome) popupHome.SetActive(false);

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
        // 게임오버 중에는 ESC 무시
        if (GameOverUI.InputLocked || GameOverUI.IsShown) return;

        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

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

        RefreshStatsFromScore();

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
        if (settingsPanel) settingsPanel.SetActive(true);
    }

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

    // -------- 재시작 / 홈 --------
    void ConfirmRestart()
    {
        onRestartConfirmed?.Invoke();

        StartGate.SkipTitleOnce = true;       // 씬 리로드 후 바로 시작
        string scene = SceneManager.GetActiveScene().name;
        Time.timeScale = 1f;
        SceneManager.LoadScene(scene);
    }

    void ConfirmHome()
    {
        onHomeConfirmed?.Invoke();

        // 비활성 오브젝트까지 포함해서 TitleUIController 찾기!
        var title = Object.FindObjectOfType<TitleUIController>(true);
        if (!title)
        {
            Debug.LogWarning("[PauseController] TitleUIController가 없어 타이틀 복귀를 수행할 수 없습니다.");
            ClosePause();
            return;
        }

        // 열려 있는 UI/팝업 정리
        if (pausePanel && pausePanel.activeSelf) pausePanel.SetActive(false);
        if (settingsPanel && settingsPanel.activeSelf) settingsPanel.SetActive(false);
        if (popupHome && popupHome.activeSelf) popupHome.SetActive(false);
        if (popupRestart && popupRestart.activeSelf) popupRestart.SetActive(false);

        // 타이틀로 복귀(타이틀에서 TimeScale=0 처리)
        Time.timeScale = 1f;
        isPaused = false;
        if (btnPauseIcon) btnPauseIcon.gameObject.SetActive(true);

        title.ReturnToTitle();
    }

    public void RefreshStatsFromScore()
    {
        int cur = (score != null) ? score.CurrentScore : 0;
        int run = (score != null) ? score.SessionMaxScore : 0;
        int best = (score != null) ? score.BestEverScore : 0;

        if (txtCurrent) txtCurrent.text = $"{cur} m";
        if (txtRunBest) txtRunBest.text = $"{run} m";
        if (txtBest) txtBest.text = $"{best} m";
    }
}



