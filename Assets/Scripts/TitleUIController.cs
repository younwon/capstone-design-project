// Assets/Scripts/Title/TitleUIController.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TitleUIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject titlePanel;        // TitlePanel 루트(자기 자신 가능)
    [SerializeField] GameObject inGameUI;          // HUD/일시정지 등 묶음
    [SerializeField] SegmentSpawner segmentSpawner;

    [Header("Tap Anywhere Button")]
    [SerializeField] Button startCatcher;          // 전체화면 투명 버튼
    [SerializeField] CanvasGroup startCatcherGroup;

    [Header("Extra Buttons")]
    [SerializeField] Button btnRanking;          // 노란 버튼
    [SerializeField] GameObject rankingPanel;    // RecordsPanel_Classic (비활성 시작)

    [Header("Optional refs for clean reset")]
    [SerializeField] Transform playerStartLocation;
    [SerializeField] Transform mainCamera;         // 비우면 카메라 자동
    [SerializeField] PlayerController2D player;    // 비우면 자동
    [SerializeField] LavaRiser lava;               // 비우면 자동
    [SerializeField] LogoAnimator logoAnimator;    // 로고 애니메이터(선택)

    [Header("Scenes")]
    [SerializeField] string colorClimbScene = "ColorClimb";     // 기존 모드 씬 이름
    [SerializeField] string speedRunScene = "SpeedRun";         // 타임어택 씬 이름

    [Header("BGM Clips (AudioManager 사용)")]
    [SerializeField] AudioClip titleBgmClip;      // 타이틀에서 재생할 BGM
    [SerializeField] AudioClip inGameBgmClip;     // 인게임에서 재생할 BGM

    // 내부 캐시(카메라 시작 위치)
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
        // 재시작(씬 리로드) 직후 타이틀 스킵?
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
        // 타이틀이 떠 있을 때만 처리
        if (!titlePanel || !titlePanel.activeInHierarchy) return;

        // 1) 마우스 클릭(에디터/PC 테스트용)
        if (Input.GetMouseButtonDown(0))
        {
            // UI(버튼 등) 위에서 클릭했으면 시작 금지
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            BeginGame();
            return;
        }

        // 2) 터치(모바일)
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            var touch = Input.GetTouch(0);
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                return;

            BeginGame();
            return;
        }

        // 3) 키보드로도 시작 (Space/Enter)
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            BeginGame();
        }
    }

    // -------- 상태 전환 --------
    public void OpenTitleState()
    {
        // 시간 멈춤, 게임중 아님
        StartGate.DisableGameplay();

        // 인게임 UI 끄기
        if (inGameUI && inGameUI.activeSelf) inGameUI.SetActive(false);

        // 스포너 끄고 깨끗이 비우기
        if (segmentSpawner)
        {
            segmentSpawner.enabled = false;
            segmentSpawner.ClearAllSegments();
        }

        // 플레이어/카메라/용암/스코어도 시작 위치로(가능한 경우)
        ReturnToTitle_ResetPositions();

        // 타이틀 보이기
        if (titlePanel && !titlePanel.activeSelf) titlePanel.SetActive(true);
        ArmStartCatcher();

        // 로고 애니메이션 재시작
        if (logoAnimator) logoAnimator.RestartLoop();

        var cd = FindObjectOfType<CountDown>(true);
        if (cd) cd.PrepareForTitle();

        // ==== BGM: 타이틀용 재생 ====
        PlayTitleBgm();
    }

    public void BeginGame()
    {
        // 이미 시작 상태면 무시
        if (StartGate.GameStarted) return;

        // 시간 정지 해제
        Time.timeScale = 1f;

        // ==== BGM: 인게임용 재생 ====
        PlayInGameBgm();

        // 인게임 UI 켜기
        if (inGameUI && !inGameUI.activeSelf) inGameUI.SetActive(true);

        // 스포너 리셋 + 활성화
        if (segmentSpawner)
        {
            segmentSpawner.enabled = true;
            segmentSpawner.ResetAndFill();
        }

        // 타이틀 패널 끄기
        if (titlePanel && titlePanel.activeSelf) titlePanel.SetActive(false);

        // 게임 시작 상태로 변경
        StartGate.EnableGameplay();

        // StartCatcher 클릭 막기
        if (startCatcherGroup)
        {
            startCatcherGroup.interactable = false;
            startCatcherGroup.blocksRaycasts = false;
        }

        var cd = FindObjectOfType<CountDown>(true);
        if (cd) cd.PrepareForRun();
    }

    // 재시작(타이틀 스킵)용: 바로 인게임으로
    public void ForceStartImmediately()
    {
        PlayInGameBgm();

        if (inGameUI && !inGameUI.activeSelf) inGameUI.SetActive(true);

        if (segmentSpawner)
        {
            segmentSpawner.enabled = true;
            segmentSpawner.ResetAndFill();
        }

        if (titlePanel && titlePanel.activeSelf) titlePanel.SetActive(false);
        StartGate.EnableGameplay();
    }

    // Pause → Home에서 호출
    public void ReturnToTitle()
    {
        OpenTitleState();
        ArmStartCatcher();
    }

    // 실제 위치/상태 리셋 처리
    void ReturnToTitle_ResetPositions()
    {
        var score = ScoreSystem.I;

        // (a) 플레이어
        if (!player) player = FindObjectOfType<PlayerController2D>(true);
        if (player)
        {
            if (playerStartLocation)
                player.transform.position = playerStartLocation.position;

            var rb = player.GetComponent<Rigidbody2D>();
            if (rb) rb.velocity = Vector2.zero;
        }

        // (b) 카메라
        if (!mainCamera && Camera.main) mainCamera = Camera.main.transform;
        if (mainCamera) mainCamera.position = _camStartPos;

        // (c) 용암
        if (!lava) lava = FindObjectOfType<LavaRiser>(true);
        if (lava) lava.TryResetToStart();

        // (d) 스코어(세션 값 정리)
        if (score != null)
        {
            score.ResetRun();
            score.ClearSessionPeak();
        }
    }

    // 모드 전환 버튼
    public void OnClickModeChange()
    {
        bool gotoSpeedRun = !ModePrefs.IsSpeedRun;
        ModePrefs.IsSpeedRun = gotoSpeedRun;

        string target = gotoSpeedRun ? speedRunScene : colorClimbScene;

        if (SceneTracker.Instance != null)
        {
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            SceneTracker.Instance.SetPreviousScene(currentSceneName);
            Debug.Log($"[TitleUIController] 이전 씬 저장 완료: {currentSceneName}");
        }
        else
        {
            Debug.LogError("[TitleUIController] SceneTracker 인스턴스를 찾을 수 없습니다! 메인 씬에 SceneTracker 오브젝트가 있는지 확인하세요.");
        }

        FadeRouter.Load(target, skipTitleOnce: false);
    }

    // StartCatcher 활성화
    public void ArmStartCatcher()
    {
        if (!startCatcher) return;

        startCatcher.enabled = true;
        startCatcher.interactable = true;

        var g = startCatcher.targetGraphic;
        if (g) g.raycastTarget = true;

        var cg = startCatcher.GetComponentInParent<CanvasGroup>();
        if (cg) { cg.interactable = true; cg.blocksRaycasts = true; }
    }

    void OpenRankingPanel()
    {
        if (rankingPanel)
            rankingPanel.SetActive(true);
    }

    // ==== BGM 헬퍼 ====
    void PlayTitleBgm()
    {
        if (AudioManager.I && titleBgmClip)
            AudioManager.I.PlayBGM(titleBgmClip, loop: true);
    }

    void PlayInGameBgm()
    {
        if (AudioManager.I && inGameBgmClip)
            AudioManager.I.PlayBGM(inGameBgmClip, loop: true);
    }
}


