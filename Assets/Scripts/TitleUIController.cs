// Assets/Scripts/Title/TitleUIController.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TitleUIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject titlePanel;
    [SerializeField] GameObject inGameUI;
    [SerializeField] SegmentSpawner segmentSpawner;

    [Header("Tap Anywhere Button")]
    [SerializeField] Button startCatcher;
    [SerializeField] CanvasGroup startCatcherGroup;

    [Header("Extra Buttons")]
    [SerializeField] Button btnRanking;
    [SerializeField] GameObject rankingPanel;

    [Header("Optional refs for clean reset")]
    [SerializeField] Transform playerStartLocation;
    [SerializeField] Transform mainCamera;
    [SerializeField] PlayerController2D player;
    [SerializeField] LavaRiser lava;
    [SerializeField] LogoAnimator logoAnimator;

    [Header("Scenes")]
    [SerializeField] string colorClimbScene = "ColorClimb";
    [SerializeField] string speedRunScene = "SpeedRun";

    [Header("SFX")]
    [SerializeField] AudioClip startGameSfx;

    [Header("BGM Clips (AudioManager 사용)")]
    [SerializeField] AudioClip titleBgmClip;
    [SerializeField] AudioClip inGameBgmClip;
    [SerializeField, Range(0f, 3f)] float bgmFadeTime = 0.8f;

    Vector3 _camStartPos;


    void Awake()
    {
        if (!startCatcher && transform.Find("StartCatcher"))
            startCatcher = transform.Find("StartCatcher").GetComponent<Button>();

        if (!startCatcherGroup && startCatcher)
            startCatcherGroup = startCatcher.GetComponent<CanvasGroup>();

        if (!titlePanel) titlePanel = gameObject;
        if (!mainCamera && Camera.main) mainCamera = Camera.main.transform;
        if (!player) player = FindObjectOfType<PlayerController2D>(true);
        if (!lava) lava = FindObjectOfType<LavaRiser>(true);

        if (mainCamera) _camStartPos = mainCamera.position;

        if (!startCatcher)
        {
            var sc = transform.Find("StartCatcher");
            if (sc) startCatcher = sc.GetComponent<Button>();
        }
        if (startCatcher)
        {
            startCatcher.onClick.RemoveAllListeners();
            startCatcher.onClick.AddListener(BeginGame);
        }

        if (btnRanking)
            btnRanking.onClick.AddListener(OpenRankingPanel);
    }


    void OnEnable()
    {
        if (StartGate.SkipTitleOnce)
        {
            StartGate.SkipTitleOnce = false;
            ForceStartImmediately();
            return;
        }

        OpenTitleState();
    }


    void Update()
    {
        if (!titlePanel || !titlePanel.activeInHierarchy) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            BeginGame();
            return;
        }

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            var touch = Input.GetTouch(0);
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                return;

            BeginGame();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            BeginGame();
        }
    }


    // ====================================================================
    //  Title State
    // ====================================================================
    public void OpenTitleState()
    {
        StartGate.DisableGameplay();

        if (inGameUI && inGameUI.activeSelf) inGameUI.SetActive(false);

        if (segmentSpawner)
        {
            segmentSpawner.enabled = false;
            segmentSpawner.ClearAllSegments();
        }

        ReturnToTitle_ResetPositions();

        if (titlePanel && !titlePanel.activeSelf) titlePanel.SetActive(true);
        ArmStartCatcher();

        if (logoAnimator)
            logoAnimator.RestartLoop();

        var cd = FindObjectOfType<CountDown>(true);
        if (cd) cd.PrepareForTitle();

        // ★ 타이틀 BGM (Fade)
        PlayTitleBgm_Fade();

        // 타이틀 버튼들도 여기서 다시 훅
        if (GlobalButtonSFX.I) GlobalButtonSFX.I.Refresh();
    }


    // ====================================================================
    //  Begin Game (Tap to Start)
    // ====================================================================
    public void BeginGame()
    {
        if (StartGate.GameStarted) return;

        Time.timeScale = 1f;

        if (AudioManager.I && startGameSfx)
            AudioManager.I.PlaySFX(startGameSfx);

        // ★ 인게임 BGM (Fade)
        PlayInGameBgm_Fade();

        if (inGameUI && !inGameUI.activeSelf) inGameUI.SetActive(true);

        if (segmentSpawner)
        {
            segmentSpawner.enabled = true;
            segmentSpawner.ResetAndFill();
        }

        if (titlePanel && titlePanel.activeSelf) titlePanel.SetActive(false);

        StartGate.EnableGameplay();

        if (startCatcherGroup)
        {
            startCatcherGroup.interactable = false;
            startCatcherGroup.blocksRaycasts = false;
        }

        var cd = FindObjectOfType<CountDown>(true);
        if (cd) cd.PrepareForRun();
    }


    // ====================================================================
    //  Force Start (Skip Title)
    // ====================================================================
    public void ForceStartImmediately()
    {
        PlayInGameBgm_Fade();

        if (inGameUI && !inGameUI.activeSelf) inGameUI.SetActive(true);

        if (segmentSpawner)
        {
            segmentSpawner.enabled = true;
            segmentSpawner.ResetAndFill();
        }

        if (titlePanel && titlePanel.activeSelf) titlePanel.SetActive(false);

        StartGate.EnableGameplay();
    }


    public void ReturnToTitle()
    {
        OpenTitleState();
        ArmStartCatcher();
    }


    // ====================================================================
    //  Reset Positions
    // ====================================================================
    void ReturnToTitle_ResetPositions()
    {
        var score = ScoreSystem.I;

        if (!player) player = FindObjectOfType<PlayerController2D>(true);
        if (player)
        {
            if (playerStartLocation)
                player.transform.position = playerStartLocation.position;

            var rb = player.GetComponent<Rigidbody2D>();
            if (rb) rb.velocity = Vector2.zero;
        }

        if (!mainCamera && Camera.main) mainCamera = Camera.main.transform;
        if (mainCamera) mainCamera.position = _camStartPos;

        if (!lava) lava = FindObjectOfType<LavaRiser>(true);
        if (lava) lava.TryResetToStart();

        if (score != null)
        {
            score.ResetRun();
            score.ClearSessionPeak();
        }
    }


    // ====================================================================
    //  Mode Change
    // ====================================================================
    public void OnClickModeChange()
    {
        bool gotoSpeedRun = !ModePrefs.IsSpeedRun;
        ModePrefs.IsSpeedRun = gotoSpeedRun;

        string target = gotoSpeedRun ? speedRunScene : colorClimbScene;

        if (SceneTracker.Instance != null)
        {
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            SceneTracker.Instance.SetPreviousScene(currentSceneName);
        }

        FadeRouter.Load(target, skipTitleOnce: false);
    }


    // ====================================================================
    //  Start Catcher
    // ====================================================================
    public void ArmStartCatcher()
    {
        if (!startCatcher) return;

        startCatcher.enabled = true;
        startCatcher.interactable = true;

        var g = startCatcher.targetGraphic;
        if (g) g.raycastTarget = true;

        var cg = startCatcher.GetComponentInParent<CanvasGroup>();
        if (cg)
        {
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
    }


    void OpenRankingPanel()
    {
        if (rankingPanel)
            rankingPanel.SetActive(true);
    }


    // ====================================================================
    //  BGM Helper (Fade 적용)
    // ====================================================================
    void PlayTitleBgm_Fade()
    {
        if (AudioManager.I && titleBgmClip)
            AudioManager.I.PlayBGM_Fade(titleBgmClip, bgmFadeTime);
    }

    void PlayInGameBgm_Fade()
    {
        if (AudioManager.I && inGameBgmClip)
            AudioManager.I.PlayBGM_Fade(inGameBgmClip, bgmFadeTime);
    }
}


