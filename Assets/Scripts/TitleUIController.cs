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


    [Header("Optional refs for clean reset")]
    [SerializeField] Transform playerStartLocation;
    [SerializeField] Transform mainCamera;         // 비우면 카메라 자동
    [SerializeField] PlayerController2D player;    // 비우면 자동
    [SerializeField] LavaRiser lava;               // 비우면 자동
    [SerializeField] LogoAnimator logoAnimator;    // 로고 애니메이터(선택)

    [Header("Scenes")]
    [SerializeField] string colorClimbScene = "ColorClimb";     // 기존 모드 씬 이름
    [SerializeField] string speedRunScene = "SpeedRun"; // 타임어택 씬 이름

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

        // 3) 키보드로도 시작하고 싶으면 (Space/Enter 정도만)
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

        if (titlePanel && !titlePanel.activeSelf) titlePanel.SetActive(true);
        ArmStartCatcher();

        // 로고 애니메이션 재시작
        if (logoAnimator) logoAnimator.RestartLoop();

        var cd = FindObjectOfType<CountDown>(true);
        if (cd) cd.PrepareForTitle();

        if (BgmController.I) BgmController.I.PlayTitle();
    }

    public void BeginGame()
    {
        if (StartGate.GameStarted) return;

        if (BgmController.I) BgmController.I.PlayInGame();

        // 인게임 UI 켜기
        if (inGameUI && !inGameUI.activeSelf) inGameUI.SetActive(true);

        // 스포너 리셋 + 활성화(초기 세그먼트 다시 생성)
        if (segmentSpawner)
        {
            segmentSpawner.enabled = true;
            segmentSpawner.ResetAndFill();
        }

        // 타이틀 끄고 진행 on
        if (titlePanel && titlePanel.activeSelf) titlePanel.SetActive(false);
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
        if (BgmController.I) BgmController.I.PlayInGame();

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
            score.ResetRun();        // 러닝 값 초기화(있다면)
            score.ClearSessionPeak(); // 세션 피크/시간 0으로(우리가 추가한 메서드)
        }
    }

    // TitleUIController.cs 안에 아래 메서드만 추가
    public void OnClickModeChange()
    {
        // 현재 모드 토글: ColorClimb -> SpeedRun, SpeedRun -> ColorClimb
        bool gotoSpeedRun = !ModePrefs.IsSpeedRun;
        ModePrefs.IsSpeedRun = gotoSpeedRun;

        string target = gotoSpeedRun ? speedRunScene : colorClimbScene;

        // 씬 전환 전에 현재 씬 이름을 SceneTracker에 저장하는 로직 추가
        if (SceneTracker.Instance != null)
        {
            // 현재 활성화된 씬의 이름을 가져옵니다.
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            // SceneTracker에 이전 씬 이름으로 저장합니다.
            SceneTracker.Instance.SetPreviousScene(currentSceneName);

            Debug.Log($"[TitleUIController] 이전 씬 저장 완료: {currentSceneName}");
        }
        else
        {
            Debug.LogError("[TitleUIController] SceneTracker 인스턴스를 찾을 수 없습니다! 메인 씬에 SceneTracker 오브젝트가 있는지 확인하세요.");
        }

        // 다음 씬에서 바로 시작(타이틀 스킵) + 페이드 연출
        FadeRouter.Load(target, skipTitleOnce: false);
    }

    void ArmStartCatcher()
    {
        if (!startCatcher) return;

        // 버튼 자체 활성/상호작용
        startCatcher.enabled = true;
        startCatcher.interactable = true;

        // 버튼의 그래픽(주로 Image)이 Raycast를 받도록
        var g = startCatcher.targetGraphic;
        if (g) g.raycastTarget = true;

        // 혹시 그룹으로 막아놨다면 해제
        var cg = startCatcher.GetComponentInParent<CanvasGroup>();
        if (cg) { cg.interactable = true; cg.blocksRaycasts = true; }

        // 숨겨진 차폐물(검은 페이드, 팝업 등)이 남아있다면 끄기
        // (Scene 내에서 사용하는 다른 전면 오브젝트가 있다면 여기서 확실히 Off)
    }


}

