using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverRecorder : MonoBehaviour
{
    public enum ModeSource { AutoFromScene, ForceClassic, ForceSpeedRun }

    [Header("Mode")]
    public ModeSource modeSource = ModeSource.AutoFromScene;
    public string speedSceneKeyword = "Speed";

    [Header("Optional refs (자동 탐색됨)")]
    public ScoreSystem classicScore;
    public ScoreSystem_Speed speedScore;
    public CountDown countDown;

    [Header("Debug")]
    public bool log = true;

    void Awake()
    {
        if (!classicScore) classicScore = FindObjectOfType<ScoreSystem>(true);
        if (!speedScore) speedScore = FindObjectOfType<ScoreSystem_Speed>(true);
        if (!countDown) countDown = FindObjectOfType<CountDown>(true);

        if (log) Debug.Log($"[GOR] Awake. refs → classic:{(classicScore ? "ok" : "null")}, " +
                           $"speed:{(speedScore ? "ok" : "null")}, cd:{(countDown ? "ok" : "null")}");
    }

    void OnEnable()
    {
        SimpleGameOver.OnGameOver += OnGameOver;
        if (log) Debug.Log("[GOR] OnEnable → subscribed");
    }

    void OnDisable()
    {
        SimpleGameOver.OnGameOver -= OnGameOver;
        if (log) Debug.Log("[GOR] OnDisable → unsubscribed");
    }

    void OnGameOver(string reason)
    {
        if (log) Debug.Log($"[GOR] OnGameOver ({reason})");

        // DB 인스턴스 보장(없으면 즉시 생성)
        if (RecordsDB.I == null)
        {
            var found = FindObjectOfType<RecordsDB>(true);
            if (!found)
            {
                var go = new GameObject("RecordsDB");
                go.AddComponent<RecordsDB>(); // Awake에서 DontDestroyOnLoad + Load
                if (log) Debug.Log("[GOR] RecordsDB spawned by recorder");
            }
        }
        if (RecordsDB.I == null) { Debug.LogWarning("[GOR] RecordsDB not ready"); return; }

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
            else if (log) Debug.LogWarning("[GOR] ScoreSystem (classic) missing");
        }
        else
        {
            var ss = speedScore ?? FindObjectOfType<ScoreSystem_Speed>(true);
            if (ss != null) meters = Mathf.Max(0, ss.CurrentScore);
            else if (log) Debug.LogWarning("[GOR] ScoreSystem_Speed missing");

            if (!countDown) countDown = FindObjectOfType<CountDown>(true);
            if (countDown != null)
                seconds = Mathf.Max(0f, countDown.InitialSeconds - countDown.timeRemaining);
            else if (ss != null)
                seconds = Mathf.Max(0f, ss.ThisRunTime);
        }

        if (log) Debug.Log($"[GOR] AddRecord → mode:{mode}, meters:{meters}, seconds:{seconds:0.00}");
        RecordsDB.I.AddRecord(mode, meters, seconds, DateTime.UtcNow);
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
}







