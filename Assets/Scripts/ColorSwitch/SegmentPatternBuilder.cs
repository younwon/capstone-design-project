using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct LaneRowFine
{
    public int lane;         // 0 ~ lanes-1 (시각 배치용)
    public int rowOffset;    // 1,2,3… (세그먼트 내부 행)
    public float yFine;      // 셀 크기 기준 미세 보정 (예: -0.5f)
    public int colorGroup;   // 같은 그룹끼리 동일색 강제(>0일 때)
}

public class SegmentPatternBuilder : MonoBehaviour
{
    [Header("Palette (필수)")]
    public PlatformPalette palette;

    [Header("패턴 포인트들")]
    public LaneRowFine[] points = new LaneRowFine[] {
        new LaneRowFine{ lane=0, rowOffset=1, yFine=0f, colorGroup=0 },
        new LaneRowFine{ lane=2, rowOffset=2, yFine=0f, colorGroup=0 },
        new LaneRowFine{ lane=4, rowOffset=3, yFine=0f, colorGroup=0 },
        new LaneRowFine{ lane=6, rowOffset=4, yFine=0f, colorGroup=0 },
        new LaneRowFine{ lane=4, rowOffset=5, yFine=0f, colorGroup=0 },
        new LaneRowFine{ lane=2, rowOffset=6, yFine=0f, colorGroup=0 },
    };

    [Header("Same-lane Extra Gap")]
    [Tooltip("같은 레인에서 이 행 차이 이내면 같은 색 금지(세그먼트 경계까지 더 강하게 막기용)")]
    public int sameLaneExtraGap = 0;

    [Header("Lane Offset (전체 이동)")]
    public int laneOffset = 0;

    [Header("색 규칙(기존)")]
    public int minRowGapForSameColor = 2; // 같은 레인, 이 행차 이내면 금지
    public int globalMaxStreak = 2;       // 같은 색 연속 최대 허용(2면 3연속 금지)

    [Header("빌드 옵션")]
    public bool clearOldChildrenOnBuild = true;

    [Header("시각 레인 중앙 보정 (셀 단위)")]
    [Tooltip("이 값으로 ‘보이는 위치’를 미세 보정 (프리팹마다 달라도 됨)")]
    public float halfCellBias = 0f;       // 예: Basic=-0.5, A=0

    // ▼ 규칙판단(논리 레인)용 – 모든 프리팹이 ‘같은 기준’을 쓰도록 고정
    [Header("논리 레인 옵션")]
    [Tooltip("규칙 판정(머리박기/연속색 등)에 논리 레인(공통 바이어스)을 사용")]
    public bool useLogicLaneIndex = true;
    [Tooltip("논리 레인의 중앙 보정(셀 단위). 보통 0을 권장(전 프리팹 공통)")]
    public float logicHalfBias = 0f;      // 권장 0

    [Header("추가 금지 규칙(겹침 방지)")]
    public float minWorldDXForSameColor = 0.6f; // 셀단위
    public float minWorldDYForSameColor = 1.1f; // 셀단위
    public bool useProximityBan = true;

    [Header("그룹 강제 규칙")]
    public bool forceSameColorInGroup = true;
    public bool useColorGroupUnique = false;   // 구형(보통 꺼두기)
    public bool groupOverridesOtherBans = true;

    // Grid
    float cellSize = 1f;
    int lanes = 8;
    float originX = 0f;

    void Awake()
    {
        var gm = GridManager.I ? GridManager.I : FindObjectOfType<GridManager>();
        if (gm) { cellSize = gm.cellSize; lanes = gm.lanes; originX = gm.originX; }
    }

    public int PatternTopOffset
    {
        get
        {
            int top = 0;
            for (int i = 0; i < points.Length; i++)
                if (points[i].rowOffset > top) top = points[i].rowOffset;
            return top;
        }
    }

    struct SpawnedInfo { public Vector2 pos; public BlockColor color; public int group; public int lane; public int row; }
    readonly List<SpawnedInfo> spawned = new List<SpawnedInfo>();
    readonly Dictionary<int, BlockColor> _groupColor = new Dictionary<int, BlockColor>();

    public int BuildAtBaseRow(int baseRow, ref ColorRuleState state)
    {
        if (!palette || points == null || points.Length == 0) return baseRow;

        if (clearOldChildrenOnBuild)
        {
#if UNITY_EDITOR
            var del = new List<Transform>();
            foreach (Transform c in transform) if (!c.name.StartsWith("P")) del.Add(c);
            foreach (var t in del) DestroyImmediate(t.gameObject);
#else
            foreach (Transform c in transform) if (!c.name.StartsWith("P")) Destroy(c.gameObject);
#endif
        }

        spawned.Clear();
        _groupColor.Clear();

        int maxRow = baseRow;

        for (int i = 0; i < points.Length; i++)
        {
            // 시각 배치용 lane(halfCellBias 적용) + 오프셋
            int visLane = Mathf.Clamp(points[i].lane + laneOffset, 0, lanes - 1);
            int row = baseRow + Mathf.Max(0, points[i].rowOffset);
            float yFineWorld = points[i].yFine * cellSize;
            int group = points[i].colorGroup;

            // 월드 좌표(보이는 위치)
            float x = GetLaneCenter(visLane, halfCellBias);
            float y = row * cellSize + yFineWorld;
            Vector2 wpos = new Vector2(x, y);

            // ▲ 규칙 판정에 사용할 "논리 레인" 인덱스 계산 (logicHalfBias 사용)
            int logicLane = GetLogicLaneIndexFromX(x);

            BlockColor pick;

            // 1) 그룹 고정색 우선
            if (forceSameColorInGroup && group > 0 && _groupColor.TryGetValue(group, out var fixedColor))
            {
                pick = fixedColor;
            }
            else
            {
                var banned = new HashSet<BlockColor>();

                // (A) 같은 '논리 레인'에서 너무 가까운 행이면 금지 (머리 박기)
                if (state.hasLaneHistory[logicLane])
                {
                    int d = Mathf.Abs(row - state.lastRowPerLane[logicLane]);
                    int needGap = Mathf.Max(minRowGapForSameColor, sameLaneExtraGap);
                    if (d <= needGap)
                        banned.Add(state.lastColorPerLane[logicLane]);
                }

                // (B) 전역 3연속 금지
                if (globalMaxStreak > 0 && state.hasGlobal && state.globalStreak >= globalMaxStreak)
                    banned.Add(state.globalColor);

                // (C) 구형 그룹 유니크(보통 사용 안 함)
                if (useColorGroupUnique && group > 0)
                {
                    for (int k = 0; k < spawned.Count; k++)
                        if (spawned[k].group == group)
                            banned.Add(spawned[k].color);
                }

                // (D) 근접 금지
                if (useProximityBan)
                {
                    float dxTh = Mathf.Abs(minWorldDXForSameColor * cellSize);
                    float dyTh = Mathf.Abs(minWorldDYForSameColor * cellSize);
                    for (int k = 0; k < spawned.Count; k++)
                    {
                        var d = spawned[k];
                        if (Mathf.Abs(d.pos.x - wpos.x) < dxTh && Mathf.Abs(d.pos.y - wpos.y) < dyTh)
                            banned.Add(d.color);
                    }
                }

                // 후보 색 선택
                var all = palette.AllColors;
                var candidates = new List<BlockColor>(all.Count);
                foreach (var c in all) if (!banned.Contains(c)) candidates.Add(c);
                pick = (candidates.Count == 0)
                    ? all[Random.Range(0, all.Count)]
                    : candidates[Random.Range(0, candidates.Count)];

                if (forceSameColorInGroup && group > 0)
                    _groupColor[group] = pick;
            }

            // 생성(시각 위치로 배치)
            var prefab = palette.GetPrefab(pick);
            if (prefab)
            {
                var go = Instantiate(prefab, new Vector3(x, y, 0f), Quaternion.identity, transform);
                go.name = $"Plat_{pick}_#{i}";
            }

            // 상태 갱신은 반드시 "논리 레인" 기준
            state.hasLaneHistory[logicLane] = true;
            state.lastRowPerLane[logicLane] = row;
            state.lastColorPerLane[logicLane] = pick;

            if (!state.hasGlobal)
            {
                state.hasGlobal = true;
                state.globalColor = pick;
                state.globalStreak = 1;
            }
            else
            {
                state.globalStreak = pick.Equals(state.globalColor) ? state.globalStreak + 1 : 1;
                state.globalColor = pick;
            }

            spawned.Add(new SpawnedInfo { pos = wpos, color = pick, group = group, lane = logicLane, row = row });
            if (row > maxRow) maxRow = row;
        }

        return maxRow;
    }

    // 보이는 위치 계산 (프리팹별 halfCellBias 사용)
    float GetLaneCenter(int lane, float halfBias)
    {
        float left = originX - (lanes * cellSize) * 0.5f;
        float bias = (lanes % 2 == 0) ? halfBias * cellSize : 0f;
        return left + (lane + 0.5f) * cellSize + bias;
    }

    // 월드 X로부터 “논리 레인 인덱스”를 계산 (logicHalfBias 사용, 모든 프리팹 공통 기준)
    int GetLogicLaneIndexFromX(float worldX)
    {
        if (!useLogicLaneIndex) return Mathf.Clamp(Mathf.RoundToInt((worldX - (originX - (lanes * cellSize) * 0.5f)) / cellSize - 0.5f), 0, lanes - 1);

        float left = originX - (lanes * cellSize) * 0.5f;
        float bias = (lanes % 2 == 0) ? logicHalfBias * cellSize : 0f;
        float t = (worldX - (left + bias)) / cellSize - 0.5f;
        int idx = Mathf.RoundToInt(t);
        return Mathf.Clamp(idx, 0, lanes - 1);
    }
}


