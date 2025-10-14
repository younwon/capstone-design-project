using TMPro;
using UnityEngine;

public class CountDown : MonoBehaviour
{
    [Header("Refs")]
    public TextMeshProUGUI timerText;
    public PlayerDeath player;

    [Header("Best Record UI")]
    public TextMeshProUGUI bestValueText; // 000 M

    [Header("Time")]
    [Tooltip("기본 제한 시간(초) — 리셋/시작 시 이 값으로 복원됩니다.")]
    [SerializeField] float defaultSeconds = 60f;

    [HideInInspector] public float timeRemaining; // 현재 남은 시간(런타임)
    bool countdownStarted = false;   // ‘플레이어가 움직이기 시작했는가’
    bool countdownEnded = false;     // 시간 종료 여부

    void Awake()
    {
        // 씬 진입 시 내부 상태 초기화
        timeRemaining = Mathf.Max(0f, defaultSeconds);
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
        // ★ 게임이 실제로 시작되기 전에는 타이머 로직 자체를 아예 동작시키지 않음
        if (!StartGate.GameStarted) return;

        // 아직 스타트 플래그가 없다면 입력 감지로 스타트
        if (!countdownStarted && PlayerStartedMoving())
            countdownStarted = true;

        if (!countdownStarted || countdownEnded) return;

        // 타임 감소
        timeRemaining -= Time.deltaTime;
        if (timeRemaining < 0f) timeRemaining = 0f;

        if (timerText)
        {
            timerText.text = timeRemaining.ToString("F2");
            if (timeRemaining < 10f) timerText.color = Color.red;
        }

        // 0이 되면 사망 처리
        if (timeRemaining <= 0f)
        {
            countdownEnded = true;

            // ★ 여기 추가: 표시 스코어 얼리고 기록 확정
            if (ScoreSystem_Speed.I) ScoreSystem_Speed.I.FreezeScoreAndCommit();

            if (player) player.Die("Countdown Ended");
        }
    }

    bool PlayerStartedMoving()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool jumpPressed = Input.GetKeyDown(KeyCode.Space);
        return Mathf.Abs(horizontal) > 0f || Mathf.Abs(vertical) > 0f || jumpPressed;
    }

    void UpdateBestRecordUI()
    {
        if (bestValueText && ScoreSystem.I != null)
        {
            var s = ScoreSystem.I;
            bestValueText.text = "MAX " + $"{s.BestEverScore:0} M";
        }
    }

    // ===== 외부에서 호출(타이틀/시작 시 안전 리셋) =====

    /// <summary>
    /// 타이틀로 들어갈 때 호출: 타이머/플래그를 기본값으로 초기화하고 UI도 갱신.
    /// </summary>
    public void PrepareForTitle()
    {
        timeRemaining = Mathf.Max(0f, defaultSeconds);
        countdownStarted = false;
        countdownEnded = false;

        if (timerText)
        {
            timerText.color = Color.white;
            timerText.text = timeRemaining.ToString("F2");
        }
    }

    /// <summary>
    /// 게임 시작 직전에 호출: 항상 ‘풀타임’에서 출발하도록 보장.
    /// (타이틀-시작 입력 타이밍에 0으로 내려가 있던 케이스도 방지)
    /// </summary>
    public void PrepareForRun()
    {
        timeRemaining = Mathf.Max(0f, defaultSeconds);
        countdownStarted = false;   // 움직이면 다시 true로 전환됨
        countdownEnded = false;

        if (timerText)
        {
            timerText.color = Color.white;
            timerText.text = timeRemaining.ToString("F2");
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            // 에디터에서 기본값 바꾸면 미리 표기 반영
            if (timerText) timerText.text = Mathf.Max(0f, defaultSeconds).ToString("F2");
        }
    }
#endif
}
