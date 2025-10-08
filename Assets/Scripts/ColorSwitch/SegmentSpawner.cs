using System.Collections.Generic;
using UnityEngine;

public class SegmentSpawner : MonoBehaviour
{
    [Header("Default (티어가 비었을 때 사용)")]
    public GameObject segmentPrefab;

    [Header("Rows / 반복")]
    public int startBaseRow = 0;
    public int gapRows = 0;
    public int initialSegments = 6;

    [Header("Auto Spawn (항상 위로 버퍼 유지)")]
    public Transform follow;
    public int aheadBufferRows = 24;
    public int maxSpawnPerFrame = 4;

    [Header("Height-based Pattern Tiers (옵션)")]
    public List<PatternTier> tiers = new();

    // 내부
    GridManager gm;
    int currentTopRow;
    ColorRuleState ruleState;
    Transform fallbackPlayer;

    bool _started = false; // 자동 Update 가드

    void Awake()
    {
        gm = GridManager.I ? GridManager.I : FindObjectOfType<GridManager>();
    }

    void Start()
    {
        // ※ 이전엔 자동으로 초기 생성했음 → 이제는 타이틀에서 보이지 않게
        // BeginGame 시점(TitleUIController)에서 ResetAndFill()을 호출해 생성한다.
        if (!follow)
        {
            var pc = FindObjectOfType<PlayerController2D>();
            if (pc) fallbackPlayer = pc.transform;
        }
    }

    void Update()
    {
        if (!_started) return;
        AutoEnsureAhead();
    }

    // ----- 공개: 시작 시 호출 -----
    public void ResetAndFill()
    {
        ClearAllSegments();

        int lanes = gm ? gm.lanes : 8;
        ruleState = ColorRuleState.Create(lanes);

        currentTopRow = startBaseRow - gapRows;

        // 초기 세그먼트 배치
        for (int i = 0; i < initialSegments; i++)
            SpawnNext();

        _started = true;
    }

    public void ClearAllSegments()
    {
        // 자식(세그먼트 프리팹 루트들) 전부 삭제
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        _started = false;
        currentTopRow = startBaseRow - gapRows;
    }

    void AutoEnsureAhead()
    {
        float cell = (gm ? gm.cellSize : 1f);
        Transform t = follow ? follow : fallbackPlayer;
        float y = 0f;
        if (t) y = t.position.y;
        else if (ScoreSystem.I)
            y = ScoreSystem.I.player ? ScoreSystem.I.player.position.y : 0f;

        int currentRow = Mathf.FloorToInt(y / cell);
        int targetTopRow = currentRow + Mathf.Max(0, aheadBufferRows);
        int spawnedThisFrame = 0;

        while (currentTopRow < targetTopRow && spawnedThisFrame < maxSpawnPerFrame)
        {
            SpawnNext();
            spawnedThisFrame++;
        }
    }

    public void SpawnNext()
    {
        int baseRow = currentTopRow + gapRows;
        GameObject prefab = PickPrefabByCurrentHeight();
        if (!prefab) return;

        var seg = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
        seg.name = $"{prefab.name}_{transform.childCount:00}";

        var builder = seg.GetComponent<SegmentPatternBuilder>();
        if (!builder) { Debug.LogError("SegmentPatternBuilder가 프리팹에 없습니다."); return; }

        int newTop = builder.BuildAtBaseRow(baseRow, ref ruleState);
        currentTopRow = newTop;
    }

    // --- 이하 PickPrefabByCurrentHeight / CurrentMeters / 데이터 구조는 기존 그대로 ---
    GameObject PickPrefabByCurrentHeight()
    {
        if (tiers == null || tiers.Count == 0) return segmentPrefab;
        int meters = CurrentMeters();

        PatternTier chosen = null; int bestMin = int.MinValue;
        foreach (var t in tiers)
        {
            if (meters >= t.minMeters && t.minMeters >= bestMin) { chosen = t; bestMin = t.minMeters; }
        }
        if (chosen == null || chosen.entries == null || chosen.entries.Count == 0) return segmentPrefab;

        int total = 0; foreach (var e in chosen.entries) total += Mathf.Max(0, e.weight);
        if (total <= 0) return segmentPrefab;

        int r = Random.Range(0, total);
        foreach (var e in chosen.entries)
        {
            int w = Mathf.Max(0, e.weight);
            if (r < w) return e.prefab;
            r -= w;
        }
        return chosen.entries[chosen.entries.Count - 1].prefab;
    }

    int CurrentMeters()
    {
        if (ScoreSystem.I) return ScoreSystem.I.CurrentScore;
        Transform t = follow ? follow : fallbackPlayer;
        if (t && gm) return Mathf.FloorToInt(t.position.y / Mathf.Max(0.0001f, gm.cellSize));
        return 0;
    }

    [System.Serializable] public class PatternTier { public int minMeters = 0; public List<PatternEntry> entries = new(); }
    [System.Serializable] public class PatternEntry { public GameObject prefab; [Range(0, 100)] public int weight = 100; }
}
