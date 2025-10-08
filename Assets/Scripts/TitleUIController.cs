// TitleUIController.cs
using UnityEngine;
using UnityEngine.UI;

public class TitleUIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject titlePanel;        // TitlePanel 루트(자기 자신 가능)
    [SerializeField] GameObject inGameUI;          // HUD/일시정지 등 묶음
    [SerializeField] SegmentSpawner segmentSpawner;

    [Header("Tap Anywhere Button")]
    [SerializeField] Button startCatcher;          // 전체화면 투명 버튼

    [Header("Optional refs for clean reset")]
    [SerializeField] Transform playerStartLocation;
    [SerializeField] Transform mainCamera;         // 비우면 카메라 자동
    [SerializeField] PlayerController2D player;    // 비우면 자동
    [SerializeField] LavaRiser lava;               // 비우면 자동
    [SerializeField] LogoAnimator logoAnimator;    // 로고 애니메이터(선택)

    // 내부 캐시(카메라 시작 위치)
    Vector3 _camStartPos;

    void Awake()
    {
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
        // 타이틀이 열려 있고, 아무 키나 눌렀다면 시작
        if (titlePanel && titlePanel.activeInHierarchy)
        {
            if (Input.anyKeyDown) BeginGame();
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

        // 로고 애니메이션 재시작
        if (logoAnimator) logoAnimator.RestartLoop();
    }

    public void BeginGame()
    {
        if (StartGate.GameStarted) return;

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
    }

    // 재시작(타이틀 스킵)용: 바로 인게임으로
    public void ForceStartImmediately()
    {
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
}

