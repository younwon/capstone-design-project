using TMPro;
using UnityEngine;

public class CountDown : MonoBehaviour
{
    [Header("Refs")]
    public TextMeshProUGUI timerText;                 // 상단 타이머 텍스트
    public PlayerDeath playerClassic;                 // (옵션) 클래식용 PlayerDeath
    public PlayerDeath_SpeedRun speedRunDeath;        // (옵션) 스피드런 전용 PlayerDeath

    [Header("Best Record UI (옵션)")]
    public TextMeshProUGUI bestValueText;             // "MAX 000 M" 같은 표시 (있으면 갱신)

    [Header("Time")]
    [Tooltip("제한 시간(초). 타이틀/런 시작시 이 값으로 복원됩니다.")]
    [SerializeField] float defaultSeconds = 60f;

    // 외부에서 읽기 좋은 프로퍼티
    public float InitialSeconds => Mathf.Max(0f, defaultSeconds);
    public float ElapsedSeconds => Mathf.Clamp(InitialSeconds - timeRemaining, 0f, InitialSeconds);

    [HideInInspector] public float timeRemaining;     // 현재 남은 시간
    bool countdownStarted = false;                    // 실제로 플레이어가 움직여서 카운트다운이 시작됐는가
    bool countdownEnded = false;                      // 0에 도달했는가

    void Awake()
    {
        // 레퍼런스 자동 탐색(비어 있을 때만)
        if (!playerClassic) playerClassic = FindObjectOfType<PlayerDeath>(true);
        if (!speedRunDeath) speedRunDeath = FindObjectOfType<PlayerDeath_SpeedRun>(true);

        // 런타임 상태 초기화
        timeRemaining = InitialSeconds;
        countdownStarted = false;
        countdownEnded = false;
    }

    void Start()
    {
        UpdateBestRecordUI();

        if (timerText)
        {
            timerText.color = Color.white;
            timerText.text = timeRemaining.ToString("F2");
        }
    }

    void Update()
    {
        // 게임이 아직 시작되지 않았다면 아무 것도 하지 않음(타이틀 상태)
        if (!StartGate.GameStarted) return;

        // 첫 입력(이동/점프) 시 카운트다운 시작
        if (!countdownStarted && PlayerStartedMoving())
            countdownStarted = true;

        if (!countdownStarted || countdownEnded) return;

        // 남은 시간 감소
        timeRemaining -= Time.deltaTime;
        if (timeRemaining < 0f) timeRemaining = 0f;

        // UI 갱신
        if (timerText)
        {
            timerText.text = timeRemaining.ToString("F2");
            if (timeRemaining < 10f) timerText.color = Color.red;
        }

        // 0이 되면 타임업 처리
        if (timeRemaining <= 0f)
        {
            countdownEnded = true;

            // 스피드런: 표시 스코어 프리즈 + 기록 확정
            if (ScoreSystem_Speed.I) ScoreSystem_Speed.I.FreezeScoreAndCommit();

            // 스피드런 전용 죽음 연출이 있으면 그걸 먼저 사용
            if (speedRunDeath != null)
            {
                speedRunDeath.Die("TimeUp");
            }
            else
            {
                // 백업 경로: 클래식용 PlayerDeath 또는 즉시 GameOver
                if (playerClassic) playerClassic.Die("Countdown Ended");
                else SimpleGameOver.TryGameOver("Countdown Ended");
            }
        }
    }

    // -------- 타이틀/런 진입시 안전 초기화 --------
    public void PrepareForTitle()
    {
        timeRemaining = InitialSeconds;
        countdownStarted = false;
        countdownEnded = false;

        if (timerText)
        {
            timerText.color = Color.white;
            timerText.text = timeRemaining.ToString("F2");
        }
    }

    public void PrepareForRun()
    {
        timeRemaining = InitialSeconds;
        countdownStarted = false;   // 움직이면 true로 전환
        countdownEnded = false;

        if (timerText)
        {
            timerText.color = Color.white;
            timerText.text = timeRemaining.ToString("F2");
        }
    }

    // -------- 보조 --------
    bool PlayerStartedMoving()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool jump = Input.GetKeyDown(KeyCode.Space);
        return Mathf.Abs(horizontal) > 0f || Mathf.Abs(vertical) > 0f || jump;
    }

    void UpdateBestRecordUI()
    {
        if (!bestValueText) return;

        // 스피드런 최고기록 우선 표시, 없으면 클래식 표시(선택)
        if (ScoreSystem_Speed.I != null)
        {
            bestValueText.text = "MAX " + $"{ScoreSystem_Speed.I.BestEverScore:0} M";
        }
        else if (ScoreSystem.I != null)
        {
            bestValueText.text = "MAX " + $"{ScoreSystem.I.BestEverScore:0} M";
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying && timerText)
            timerText.text = Mathf.Max(0f, defaultSeconds).ToString("F2");
    }
#endif
}


