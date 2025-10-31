using System;
using System.Collections.Generic;
using UnityEngine;

public class RunRecordStore : MonoBehaviour
{
    public static RunRecordStore I { get; private set; }

    [Header("Capacity")]
    [Range(5, 200)] public int maxEntries = 50;

    const string KEY_CLASSIC = "RUNREC_CLASSIC_V1";
    const string KEY_SPEED = "RUNREC_SPEED_V1";

    [Serializable]
    public struct RunRecord
    {
        public string timestamp; // "yyyy.MM.dd-HH:mm"
        public int meters;
        public float seconds;
    }
    [Serializable] class RecordList { public List<RunRecord> list = new(); }

    RecordList _classic = new();
    RecordList _speed = new();

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        LoadAll();
    }

    public IReadOnlyList<RunRecord> GetClassic() => _classic.list;
    public IReadOnlyList<RunRecord> GetSpeed() => _speed.list;

    public void AddClassic(int meters, float seconds, DateTime when)
        => AddInternal(_classic, KEY_CLASSIC, meters, seconds, when);

    public void AddSpeed(int meters, float seconds, DateTime when)
        => AddInternal(_speed, KEY_SPEED, meters, seconds, when);

    public void ClearAll()
    {
        _classic.list.Clear(); _speed.list.Clear();
        PlayerPrefs.DeleteKey(KEY_CLASSIC);
        PlayerPrefs.DeleteKey(KEY_SPEED);
        PlayerPrefs.Save();
    }

    void AddInternal(RecordList tgt, string key, int m, float s, DateTime when)
    {
        var rec = new RunRecord
        {
            timestamp = when.ToString("yyyy.MM.dd-HH:mm"),
            meters = Mathf.Max(0, m),
            seconds = Mathf.Max(0f, s)
        };
        tgt.list.Add(rec);
        tgt.list.Sort((a, b) => {
            int byM = b.meters.CompareTo(a.meters);
            return byM != 0 ? byM : a.seconds.CompareTo(b.seconds);
        });
        if (tgt.list.Count > maxEntries)
            tgt.list.RemoveRange(maxEntries, tgt.list.Count - maxEntries);
        SaveList(key, tgt);
    }

    void LoadAll() { _classic = LoadList(KEY_CLASSIC); _speed = LoadList(KEY_SPEED); }

    RecordList LoadList(string key)
    {
        try
        {
            var json = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(json)) return new RecordList();
            return JsonUtility.FromJson<RecordList>(json) ?? new RecordList();
        }
        catch { return new RecordList(); }
    }
    void SaveList(string key, RecordList list)
    {
        var json = JsonUtility.ToJson(list);
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
    }
}
