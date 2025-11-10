using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverRecorder : MonoBehaviour
{
    public enum ModeSource { AutoFromScene, ForceClassic, ForceSpeedRun }

    [Header("Mode")]
    public ModeSource modeSource = ModeSource.AutoFromScene;
    [Tooltip("씬 이름에 이 키워드가 포함되면 SpeedRun으로 간주 (Auto 모드일 때만)")]
    public string speedSceneKeyword = "Speed"; // 예: "SpeedRun", "Speed"

    [Header("Optional Refs (자동 탐색됨)")]
    public ScoreSystem classicScore;         // Classic 모드용
    public ScoreSystem_Speed speedScore;     // SpeedRun 모드용
    public CountDown countDown;              // SpeedRun 타이머(있으면 시간 계산에 사용)

    [Header("Debug")]
    [SerializeField] bool debugLogs = true;

    bool _subscribed = false;

    void Awake()
    {
        if (!classicScore) classicScore = FindObjectOfType<ScoreSystem>(includeInactive: true);
        if (!speedScore) speedScore = FindObjectOfType<ScoreSystem_Speed>(includeInactive: true);
        if (!countDown) countDown = FindObjectOfType<CountDown>(includeInactive: true);
    }

    void OnEnable()
    {
        if (!_subscribed)
        {
            SimpleGameOver.OnGameOver += OnGameOver;
            _subscribed = true;
            if (debugLogs) Debug.Log("[GameOverRecorder] Subscribed to SimpleGameOver.");
        }
    }

    void OnDisable()
    {
        if (_subscribed)
        {
            SimpleGameOver.OnGameOver -= OnGameOver;
            _subscribed = false;
            if (debugLogs) Debug.Log("[GameOverRecorder] Unsubscribed from SimpleGameOver.");
        }
    }

    void OnGameOver(string reason)
    {
        var mode = ResolveMode();

        int meters = 0;
        float seconds = 0f;

        if (mode == GameMode.Classic)
        {
            var s = classicScore ?? ScoreSystem.I;
            if (s != null)
            {
                meters = Mathf.Max(0, s.SessionMaxScore);
                seconds = Mathf.Max(0f, s.ThisRunTime);
            }
        }
        else // SpeedRun
        {
            var ss = speedScore ?? FindObjectOfType<ScoreSystem_Speed>(true);
            if (ss != null) meters = Mathf.Max(0, ss.CurrentScore);

            // CountDown 공개 필드만 안전하게 사용 (initialTime이 있으면 우선 사용)
            float elapsedByCountdown = -1f;
            if (countDown == null) countDown = FindObjectOfType<CountDown>(true);
            if (countDown != null)
            {
                // CountDown에 public float initialTime 게터/프로퍼티가 있다면 사용
                try
                {
                    // 리플렉션 없이, 존재한다고 가정한 프로퍼티를 컴파일러가 알 수 없으므로
                    // 여기서는 안전 폴백만 사용. (필요 시 CountDown에 public float InitialTime 게터 추가)
                    // elapsedByCountdown = Mathf.Max(0f, countDown.initialTime - countDown.timeRemaining);

                    // 폴백: ScoreSystem_Speed의 ThisRunTime 사용
                    elapsedByCountdown = -1f;
                }
                catch { elapsedByCountdown = -1f; }
            }

            if (elapsedByCountdown >= 0f)
            {
                seconds = elapsedByCountdown;
            }
            else
            {
                // 최종 폴백: 스코어 시스템의 런타임(우리가 이미 갱신해 둠)
                if (ss != null) seconds = Mathf.Max(0f, ss.ThisRunTime);
            }
        }

        // ===== 저장 =====
        bool saved = SaveRecord(mode, meters, seconds);
        if (debugLogs)
        {
            string storeName = (RunRecordStore.I != null) ? "RunRecordStore" :
                               (RecordsDBAvailable() ? "RecordsDB" : "NO_STORE");
            Debug.Log($"[GameOverRecorder] Save {(saved ? "OK" : "FAIL")} | Mode={mode} | Height={meters}m | Time={seconds:0.00}s | Store={storeName} | Reason={reason}");
        }
    }

    GameMode ResolveMode()
    {
        switch (modeSource)
        {
            case ModeSource.ForceClassic: return GameMode.Classic;
            case ModeSource.ForceSpeedRun: return GameMode.SpeedRun;
            default:
                string s = SceneManager.GetActiveScene().name;
                return (s.IndexOf(speedSceneKeyword, StringComparison.OrdinalIgnoreCase) >= 0)
                        ? GameMode.SpeedRun : GameMode.Classic;
        }
    }

    bool SaveRecord(GameMode mode, int meters, float seconds)
    {
        // 1) RunRecordStore가 있으면 먼저 시도
        if (RunRecordStore.I != null)
        {
            object store = RunRecordStore.I;
            var t = store.GetType();

            // 우리가 지원하는 후보 시그니처들
            // A: AddRecord(GameMode,int,float,DateTime)
            var mi = t.GetMethod("AddRecord", new Type[] { typeof(GameMode), typeof(int), typeof(float), typeof(DateTime) });
            if (mi != null) { mi.Invoke(store, new object[] { mode, meters, seconds, DateTime.UtcNow }); return true; }

            // B: Add(GameMode,int,float,DateTime)
            mi = t.GetMethod("Add", new Type[] { typeof(GameMode), typeof(int), typeof(float), typeof(DateTime) });
            if (mi != null) { mi.Invoke(store, new object[] { mode, meters, seconds, DateTime.UtcNow }); return true; }

            // C: AddRecord(GameMode,int,float)
            mi = t.GetMethod("AddRecord", new Type[] { typeof(GameMode), typeof(int), typeof(float) });
            if (mi != null) { mi.Invoke(store, new object[] { mode, meters, seconds }); return true; }

            // D: Add(GameMode,int,float)
            mi = t.GetMethod("Add", new Type[] { typeof(GameMode), typeof(int), typeof(float) });
            if (mi != null) { mi.Invoke(store, new object[] { mode, meters, seconds }); return true; }

            Debug.LogWarning("[GameOverRecorder] RunRecordStore는 발견했지만 Add/AddRecord 시그니처를 찾지 못했습니다.");
        }

        // 2) RecordsDB 폴백
        if (RecordsDBAvailable())
        {
            // 너의 RecordsDB 시그니처에 맞춰 호출 (필요 시 여기서도 위처럼 후보 검색 가능)
            RecordsDB.I.AddRecord(mode, meters, seconds, DateTime.UtcNow);
            return true;
        }

        Debug.LogWarning("[GameOverRecorder] No record store found (RunRecordStore/RecordsDB 둘 다 없음).");
        return false;
    }

    bool RecordsDBAvailable()
        => (RecordsDB.I != null); // 싱글톤만 확인 (Type.GetType 체크는 불필요)
}


