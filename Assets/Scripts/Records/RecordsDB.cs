using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 기기 로컬에 기록을 저장/불러오는 단일 저장소.
/// - JSON 파일: {persistentDataPath}/records.json
/// - 모드별로 목록 분리(Classic/SpeedRun)
/// - 정렬 규칙:
///   meters 내림차순 → seconds 오름차순 → endedAtTicks 오름차순
/// </summary>
public class RecordsDB : MonoBehaviour
{
    public static RecordsDB I { get; private set; }

    [Header("Keep and Show Limits")]
    [Tooltip("디스크에 최대 몇 개까지 보관할지(오래된 것부터 삭제)")]
    public int maxKeepPerMode = 500;
    [Tooltip("UI에서 기본으로 몇 개까지 보여줄지(스크롤 가능)")]
    public int defaultShowCount = 20;

    string _path;
    RecordsRoot _data = new RecordsRoot();

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        _path = Path.Combine(Application.persistentDataPath, "records.json");
        LoadFromDiskSafe();
    }

    // ========== Public API ==========

    public void AddRecord(GameMode mode, int meters, float seconds, DateTime endedUtc)
    {
        var list = GetList(mode);
        var entry = new RecordEntry
        {
            meters = Mathf.Max(0, meters),
            seconds = Mathf.Max(0f, seconds),
            endedAtTicks = endedUtc.ToUniversalTime().Ticks
        };
        list.Add(entry);

        SortList(list);
        TrimList(list, maxKeepPerMode);
        SaveToDiskSafe();
    }

    public IReadOnlyList<RecordEntry> GetAll(GameMode mode)
    {
        return GetList(mode);
    }

    public List<RecordEntry> GetTop(GameMode mode, int count)
    {
        var list = GetList(mode);
        count = Mathf.Clamp(count, 0, list.Count);
        return list.GetRange(0, count);
    }

    public void ClearAll()
    {
        _data = new RecordsRoot();
        SaveToDiskSafe();
        Debug.Log("[RecordsDB] All rankings cleared.");
    }

    public void ClearMode(GameMode mode)
    {
        if (mode == GameMode.Classic) _data.classic = new List<RecordEntry>();
        else _data.speed = new List<RecordEntry>();
        SaveToDiskSafe();
        Debug.Log($"[RecordsDB] {mode} rankings cleared.");
    }

#if UNITY_EDITOR
    // 개발/에디터 편의: F10 누르면 전체 삭제 (전시 세팅 편함)
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F10))
        {
            ClearAll();
            Debug.LogWarning("[RecordsDB] (F10) All rankings cleared.");
        }
    }
#endif

    // ========== Internal ==========

    List<RecordEntry> GetList(GameMode mode)
    {
        return (mode == GameMode.Classic) ? _data.classic : _data.speed;
    }

    void SortList(List<RecordEntry> list)
    {
        // meters desc → seconds asc → endedAt asc
        list.Sort((a, b) =>
        {
            int byMeters = b.meters.CompareTo(a.meters);
            if (byMeters != 0) return byMeters;

            int bySeconds = a.seconds.CompareTo(b.seconds);
            if (bySeconds != 0) return bySeconds;

            return a.endedAtTicks.CompareTo(b.endedAtTicks);
        });
    }

    void TrimList(List<RecordEntry> list, int max)
    {
        if (max <= 0) return;
        if (list.Count <= max) return;
        list.RemoveRange(max, list.Count - max);
    }

    void LoadFromDiskSafe()
    {
        try
        {
            if (File.Exists(_path))
            {
                string json = File.ReadAllText(_path);
                _data = JsonUtility.FromJson<RecordsRoot>(json);
                if (_data == null) _data = new RecordsRoot();
            }
            else
            {
                _data = new RecordsRoot();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[RecordsDB] Load failed, resetting. {e.Message}");
            _data = new RecordsRoot(); // 손상 시 복구
        }
    }

    void SaveToDiskSafe()
    {
        try
        {
            string json = JsonUtility.ToJson(_data, prettyPrint: true);
            File.WriteAllText(_path, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[RecordsDB] Save failed: {e.Message}");
        }
    }
}

