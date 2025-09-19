using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SegmentSpawner : MonoBehaviour
{
    [Header("Default (티어가 비었을 때 사용)")]
    public GameObject segmentPrefab;      // Seg_Basic 등 1개만 쓸 때

    [Header("Rows / 반복")]
    public int startBaseRow = 0;          // 첫 세그먼트 기준 행
    public int gapRows = 0;               // 세그먼트 경계 간격(0이면 딱 붙음)
    public int initialSegments = 6;       // 시작 시 몇 개 깔지

    [Header("Auto Spawn (항상 위로 버퍼 유지)")]
    public Transform follow;              // 기준 Transform(보통 Player) - 비워두면 자동 탐색
    public int aheadBufferRows = 24;      // 기준 위치 위로 최소 확보할 행 수
    public int maxSpawnPerFrame = 4;      // 한 프레임에 너무 많이 생성되는 것 방지

    [Header("Height-based Pattern Tiers (옵션)")]
    [Tooltip("현재 높이 >= MinMeters인 티어 중 가장 높은 티어 1개가 적용됩니다.")]
    public List<PatternTier> tiers = new List<PatternTier>();

    // 내부 상태
    GridManager gm;
    int currentTopRow;                     // 지금까지 쌓인 최상단 행
    ColorRuleState ruleState;              // 색 규칙(세그먼트 경계 넘어 공유)
    Transform fallbackPlayer;             // 점수시스템 없을 때 사용할 플레이어 Transform

    void Awake()
    {
        gm = GridManager.I ? GridManager.I : FindObjectOfType<GridManager>();
    }

    void Start()
    {
        // 규칙 상태 초기화
        int lanes = gm ? gm.lanes : 8;
        ruleState = ColorRuleState.Create(lanes);

        // 첫 세그먼트가 startBaseRow에서 시작하도록
        currentTopRow = startBaseRow - gapRows;

        // 기준 Transform 자동 탐색(비어있으면)
        if (!follow)
        {
            // 우선 플레이어 컨트롤러 찾기
            var pc = FindObjectOfType<PlayerController2D>();
            if (pc) fallbackPlayer = pc.transform;
        }

        // 초기 세그먼트 배치
        for (int i = 0; i < initialSegments; i++)
            SpawnNext();
    }

    void Update()
    {
        AutoEnsureAhead();
    }

    void AutoEnsureAhead()
    {
        // 기준 위치의 현재 "행" 계산
        float cell = (gm ? gm.cellSize : 1f);
        Transform t = follow ? follow : fallbackPlayer;
        float y = 0f;

        if (t) y = t.position.y;
        else if (ScoreSystem.I) // 마지막 안전망
            y = ScoreSystem.I.player ? ScoreSystem.I.player.position.y : 0f;

        int currentRow = Mathf.FloorToInt(y / cell);

        // 기준 위치 위로 버퍼가 부족하면 스폰
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

        // 어떤 프리팹을 쓸지 결정(티어 → 기본)
        GameObject prefab = PickPrefabByCurrentHeight();
        if (!prefab)
        {
            Debug.LogWarning("[SegmentSpawner] 선택 프리팹이 없습니다. tiers 또는 segmentPrefab 확인.");
            return;
        }

        var seg = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
        seg.name = $"{prefab.name}_{transform.childCount:00}";

        var builder = seg.GetComponent<SegmentPatternBuilder>();
        if (!builder)
        {
            Debug.LogError("SegmentPatternBuilder가 프리팹에 없습니다.");
            return;
        }

        // 패턴 빌드(색 규칙 state 공유)
        int newTop = builder.BuildAtBaseRow(baseRow, ref ruleState);
        currentTopRow = newTop;
    }

    // ---------- 확률 선택 ----------

    GameObject PickPrefabByCurrentHeight()
    {
        // 티어가 없으면 기존 방식
        if (tiers == null || tiers.Count == 0)
            return segmentPrefab;

        int meters = CurrentMeters();

        // 적용 가능한 티어 중 minMeters가 가장 큰 것
        PatternTier chosen = null;
        int bestMin = int.MinValue;
        foreach (var t in tiers)
        {
            if (meters >= t.minMeters && t.minMeters >= bestMin)
            {
                chosen = t;
                bestMin = t.minMeters;
            }
        }

        if (chosen == null || chosen.entries == null || chosen.entries.Count == 0)
            return segmentPrefab;

        int total = 0;
        foreach (var e in chosen.entries) total += Mathf.Max(0, e.weight);
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

    // ---------- 데이터 구조 ----------

    [System.Serializable]
    public class PatternTier
    {
        public int minMeters = 0;
        public List<PatternEntry> entries = new List<PatternEntry>();
    }

    [System.Serializable]
    public class PatternEntry
    {
        public GameObject prefab;               // Seg_Basic, Seg_A, Seg_B...
        [Range(0, 100)] public int weight = 100; // 가중치
    }
}
