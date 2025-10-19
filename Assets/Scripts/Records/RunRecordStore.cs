using System;
using System.Collections.Generic;
using UnityEngine;

/// 랭킹(히스토리) 저장/로드 전용 컴포넌트
/// - 모드별(클래식/스피드)로 분리 저장
/// - PlayerPrefs(JSON) 사용
/// - 최대 N개 보관, 오래된 것 자동 제거
public class RunRecordStore : MonoBehaviour
{
    public static RunRecordStore I { get; private set; }

    // 표시 개수는 전시 상황에 맞게 조절(예: 20)
    [Header("Capacity")]
    [Range(5, 200)] public int maxEntries = 50;

    const string KEY_CLASSIC = "RUNREC_CLASSIC_V1";
    const string KEY_SPEED = "RUNREC_SPEED_V1";

    [Serializable]
    public struct RunRecord
    {
        // 기록 확정 시각 (표시용)
        public string timestamp;     // ex) "2025.10.19-14:18"
        public int meters;           // 그 판의 '기록' (클래식: 세션 최대, 스피드: 종료 시점 현재값)
        public float seconds;        // 그 판 소요시간(초)
    }

    [Serializable]
    class RecordList { public List<RunRecord> list = new(); }

    RecordList _classic = new();
    RecordList _speed = new();

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        LoadAll();
    }

    // ---------- Public API ----------

    public IReadOnlyList<RunRecord> GetClassic() => _classic.list;
    public IReadOnlyList<RunRecord> GetSpeed() => _speed.list;

    /// 클래식 모드에 기록 추가
    public void AddClassic(int meters, float seconds, DateTime when)
    {
        AddInternal(_classic, KEY_CLASSIC, meters, seconds, when);
    }

    /// 스피드런 모드에 기록 추가
    public void AddSpeed(int meters, float seconds, DateTime when)
    {
        AddInternal(_speed, KEY_SPEED, meters, seconds, when);
    }

    /// 전부 삭제(에디터용: 전시 초기화 등) — F10로도 가능
    public void ClearAll()
    {
        _classic.list.Clear();
        _speed.list.Clear();
        PlayerPrefs.DeleteKey(KEY_CLASSIC);
        PlayerPrefs.DeleteKey(KEY_SPEED);
        PlayerPrefs.Save();
        Debug.Log("[RunRecordStore] 모든 기록 삭제 완료");
    }

#if UNITY_EDITOR
    void Update()
    {
        // 에디터에서 F10 → 전체 기록 삭제(전시 리셋용)
        if (Input.GetKeyDown(KeyCode.F10))
            ClearAll();
    }
#endif

    // ---------- Internal ----------

    void AddInternal(RecordList target, string key, int meters, float seconds, DateTime when)
    {
        var rec = new RunRecord
        {
            timestamp = when.ToString("yyyy.MM.dd-HH:mm"),
            meters = Mathf.Max(0, meters),
            seconds = Mathf.Max(0f, seconds)
        };

        target.list.Add(rec);

        // 정렬: meters 큰 순서(내림차순). 같으면 seconds 작은 순(더 빨리 클리어한 게 위).
        target.list.Sort((a, b) =>
        {
            int byMeters = b.meters.CompareTo(a.meters);
            if (byMeters != 0) return byMeters;
            return a.seconds.CompareTo(b.seconds);
        });

        // 초과분 제거
        if (target.list.Count > maxEntries)
            target.list.RemoveRange(maxEntries, target.list.Count - maxEntries);

        SaveList(key, target);
    }

    void LoadAll()
    {
        _classic = LoadList(KEY_CLASSIC);
        _speed = LoadList(KEY_SPEED);
    }

    RecordList LoadList(string key)
    {
        try
        {
            var json = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(json)) return new RecordList();
            return JsonUtility.FromJson<RecordList>(json) ?? new RecordList();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[RunRecordStore] Load 실패({key}): {e.Message}");
            return new RecordList();
        }
    }

    void SaveList(string key, RecordList list)
    {
        try
        {
            var json = JsonUtility.ToJson(list);
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[RunRecordStore] Save 실패({key}): {e.Message}");
        }
    }
}
