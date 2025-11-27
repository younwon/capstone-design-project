using UnityEngine;

public class ScoreSystem_Speed : MonoBehaviour
{
    public static ScoreSystem_Speed I;

    [Header("Setup")]
    public Transform player;           // Player Transform
    public float metersPerUnit = 1f;   // 1 unit = 1 m
    public float baselineY = 0f;       // y=0이 0m면 0

    [Header("Prefs Keys")]
    [SerializeField] string PREF_BEST = "SPEED_BEST";    // 스피드런 전용 키
    [SerializeField] string PREF_BEST_TIME = "SPEED_BEST_TIME"; // (원하면 시간도 기록)

    // “현재 높이”가 곧 점수
    public int CurrentScore { get; private set; }   // 실시간 현재 높이(내부)
    public int BestEverScore { get; private set; }  // 최고 기록(영구)
    public float ThisRunTime { get; private set; }  // 이번 판 경과 시간(선택)

    // UI용 표시 값(프리즈되면 이 값을 고정 표기)
    public int DisplayScore => _frozen ? _frozenScore : CurrentScore;

    public bool IsNewRecord { get; private set; }

    private bool hasUnlocked25M = false;

    // 내부
    float _runTimer;
    bool _ended;        // 게임오버 확정
    bool _frozen;       // 표시 스코어를 동결했는가
    int _frozenScore;  // 동결된 표시 값

    void Awake()
    {
        I = this;
        if (!player)
        {
            var pc = FindObjectOfType<PlayerController2D>();
            if (pc) player = pc.transform;
        }

        BestEverScore = PlayerPrefs.GetInt(PREF_BEST, 0);
        // 시간을 기록하고 싶으면 아래도 사용
        // BestEverTime  = PlayerPrefs.GetFloat(PREF_BEST_TIME, 0f);

        CurrentScore = 0;
        _runTimer = 0f;
        _ended = false;
        _frozen = false;
        _frozenScore = 0;
        IsNewRecord = false;
        ThisRunTime = 0f;
    }

    void Update()
    {
        if (!player || _ended) return;

        // 진행 중 시간 누적
        _runTimer += Time.deltaTime;

        // 현재 높이 계산(항상 갱신되지만, 화면 표시는 DisplayScore를 쓰면 안전)
        float curMeters = Mathf.Max(0f, (player.position.y - baselineY) * metersPerUnit);
        int cur = Mathf.FloorToInt(curMeters);
        if (cur != CurrentScore) CurrentScore = cur;

        if (cur >= 25 && !hasUnlocked25M)
        {
            hasUnlocked25M = true;
            AchievementManager.Instance.UnlockAchievement("SPEEDRUN 25M");
            Debug.Log("업적 달성");
        }

        // F9 키로 최고기록 초기화
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Input.GetKeyDown(KeyCode.F9))
        {
            ClearBestEver();
        }
#endif
    }

    /// <summary>
    /// 카운트다운이 0이 되는 ‘그 순간’ 호출. 표시 스코어를 얼리고 기록 확정.
    /// </summary>
    public void FreezeScoreAndCommit()
    {
        if (_frozen) return;
        _frozenScore = CurrentScore;
        _frozen = true;

        CommitBestCurrent();
    }

    /// <summary>
    /// 이번 판 기록을 확정(현재 표시값 기준). 스피드런은 “현재 높이”가 기록.
    /// </summary>
    public void CommitBestCurrent()
    {
        if (_ended) return;
        _ended = true;

        ThisRunTime = _runTimer;
        IsNewRecord = false;

        int candidate = DisplayScore; // 프리즈돼 있으면 고정값

        if (candidate > BestEverScore)
        {
            BestEverScore = candidate;
            PlayerPrefs.SetInt(PREF_BEST, BestEverScore);
            // PlayerPrefs.SetFloat(PREF_BEST_TIME, ThisRunTime); // 시간도 저장하려면 주석 해제
            PlayerPrefs.Save();
            IsNewRecord = true;
        }
    }

    /// <summary>
    /// 새 판 시작 시 호출(씬 리로드 또는 수동 리셋).
    /// </summary>
    public void ResetRun()
    {
        CurrentScore = 0;
        _frozenScore = 0;
        _frozen = false;
        _ended = false;
        _runTimer = 0f;
        ThisRunTime = 0f;
        IsNewRecord = false;
    }

    // ✅ 추가됨 : 최고기록 초기화 함수
    [ContextMenu("Reset BestEver (PlayerPrefs)")]
    public void ClearBestEver()
    {
        BestEverScore = 0;
        PlayerPrefs.DeleteKey(PREF_BEST);
        PlayerPrefs.DeleteKey(PREF_BEST_TIME);
        PlayerPrefs.Save();
        Debug.Log("[ScoreSystem_Speed] 최고기록 초기화 완료");
    }
}

