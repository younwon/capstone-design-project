using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class ScoreSystem : MonoBehaviour
{
    public static ScoreSystem I;

    [Header("Setup")]
    public Transform player;          // Player Transform
    public float metersPerUnit = 1f;  // 1unit = 1m
    public float baselineY = 0f;      // 예: y=0이 0m면 0

    // 실시간 높이
    public int CurrentScore { get; private set; }

    // 이번 판 최대 높이
    public int SessionMaxScore { get; private set; }

    // 이번 판 걸린 시간(초) — 게임오버 시 확정
    public float ThisRunTime { get; private set; }

    // 누적 최고 기록 (영구 저장)
    public int BestEverScore { get; private set; }   // 최고 높이
    public float BestEverTime { get; private set; }  // 그때 걸린 시간

    // 내부
    float _runTimer;          // 진행중 누적 타이머
    bool _runEnded;           // 게임오버 후 타이머 정지
    public bool IsNewRecord { get; private set; } // 이번 게임에서 신기록 여부

    private bool hasUnlocked50M = false;
    private bool hasUnlocked100M = false;
    private bool hasUnlocked150M = false;
    void Awake()
    {
        I = this;

        BestEverScore = PlayerPrefs.GetInt("BEST_EVER", 0);
        BestEverTime = PlayerPrefs.GetFloat("BEST_EVER_TIME", 0f);

        CurrentScore = 0;
        SessionMaxScore = 0;
        _runTimer = 0f;
        _runEnded = false;
        ThisRunTime = 0f;
        IsNewRecord = false;
    }

    void Update()
    {
        if (!player || _runEnded) return;

        // 타이머(게임 진행 중일 때만)
        _runTimer += Time.deltaTime;

        // 현재 높이(m)
        float curMeters = Mathf.Max(0f, (player.position.y - baselineY) * metersPerUnit);
        int cur = Mathf.FloorToInt(curMeters);

        // 현재/세션 최대 갱신
        if (cur != CurrentScore)
            CurrentScore = cur;

        if (cur > SessionMaxScore)
            SessionMaxScore = cur;

        if (cur >= 50 && !hasUnlocked50M)
        {
            hasUnlocked50M = true;
            AchievementManager.Instance.UnlockAchievement("REACH 50M");
            Debug.Log("업적 달성");
        }
        if (cur >= 100 && !hasUnlocked100M)
        {
            hasUnlocked100M = true;
            AchievementManager.Instance.UnlockAchievement("REACH 100M");
            Debug.Log("업적 달성");
        }
        if (cur >= 150 && !hasUnlocked150M)
        {
            hasUnlocked150M = true;
            AchievementManager.Instance.UnlockAchievement("REACH 150M");
            Debug.Log("업적 달성");
        }
    }

    /// <summary>
    /// 게임 오버 순간 호출: 이번 판 시간 확정 + 최고기록 갱신 판단
    /// </summary>
    public void CommitBestIfHigher()
    {
        if (_runEnded) return; // 한 번만

        _runEnded = true;
        ThisRunTime = _runTimer; // 이번 판 최종 시간 확정
        IsNewRecord = false;

        // 갱신 규칙:
        // 1) 높이가 더 크면 무조건 신기록
        // 2) 높이가 같으면 시간이 더 짧으면 신기록
        bool higherScore = SessionMaxScore > BestEverScore;
        bool equalScoreBetterTime = (SessionMaxScore == BestEverScore) &&
                                    (BestEverTime <= 0f || ThisRunTime < BestEverTime);

        if (higherScore || equalScoreBetterTime)
        {
            BestEverScore = SessionMaxScore;
            BestEverTime = ThisRunTime;

            PlayerPrefs.SetInt("BEST_EVER", BestEverScore);
            PlayerPrefs.SetFloat("BEST_EVER_TIME", BestEverTime);
            PlayerPrefs.Save();

            IsNewRecord = true;
        }
    }

    /// <summary>
    /// 새 판 시작(씬 리스타트 시 불림)
    /// </summary>
    public void ResetRun()
    {
        CurrentScore = 0;
        SessionMaxScore = 0;
        ThisRunTime = 0f;
        _runTimer = 0f;
        _runEnded = false;
        IsNewRecord = false;
        // BestEver* 는 유지
    }

    /// <summary>
    /// 타이틀로 복귀할 때, 이번 세션 관련 임시 최고값/시간을 비워준다.
    /// </summary>
    public void ClearSessionPeak()
    {
        // 프로젝트에 있는 실제 필드명에 맞춰 수정하세요.
        // 대표적으로 아래 두 값만 0으로 초기화하면 충분합니다.
        SessionMaxScore = 0;  // 이번 세션 최고 높이
        ThisRunTime = 0f; // 이번 판 누적 시간(표시용)
    }

    /// <summary>
    /// UI에서 쓰기 좋은 포맷 도우미 (MM:SS)
    /// </summary>
    public static string FormatMMSS(float seconds)
    {
        if (seconds < 0f) seconds = 0f;

        int s = Mathf.FloorToInt(seconds);
        int mm = s / 60;
        int ss = s % 60;

        return $"{mm:00}:{ss:00}";
    }

    /// <summary>
    /// 디버그용 리셋(설정 메뉴/디버그 버튼에서 호출 가능)
    /// </summary>
    public void ClearBestEver()
    {
        BestEverScore = 0;
        BestEverTime = 0f;

        PlayerPrefs.DeleteKey("BEST_EVER");
        PlayerPrefs.DeleteKey("BEST_EVER_TIME");
        PlayerPrefs.Save();

        Debug.Log("[ScoreSystem] BEST_EVER / BEST_EVER_TIME 초기화 완료");
    }

    // ====== ⬇⬇ 추가된 부분: 에디터 편의 기능 2가지 ⬇⬇ ======

    // 1) 인스펙터 컨텍스트 메뉴로 초기화
    [ContextMenu("Reset BestEver (PlayerPrefs)")]
    public void ResetBestEverMenu()
    {
        ClearBestEver();
        Debug.Log("[ScoreSystem] (ContextMenu) 최고기록 초기화");
    }

#if UNITY_EDITOR
    // 2) 플레이 중 F9 키로 초기화
    void LateUpdate()
    {
        if (!Application.isPlaying) return;

        if (Input.GetKeyDown(KeyCode.F9))
        {
            ClearBestEver();
            Debug.Log("[ScoreSystem] (F9) 최고기록 초기화");
        }
    }
#endif
}







