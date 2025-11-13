using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct LaneRowFine
{
    public int lane;
    public int rowOffset;
    public float yFine;
    public int colorGroup;
    public int paletteIndex;

    [Tooltip("체크 해제 시, 이 포인트는 절대 스프링 블록으로 생성되지 않습니다.")]
    public bool allowSpring; // 스프링 허용 여부 변수 추가
}

public class SegmentPatternBuilder : MonoBehaviour
{
    [Header("Palettes (여기에 여러 종류의 팔레트를 등록)")]
    public List<PlatformPalette> palettes = new List<PlatformPalette>();

    [Header("Coin Spawn Settings (Optional)")]
    [Tooltip("블록 위에 생성할 코인 프리팹")]
    public GameObject coinPrefab;
    [Tooltip("코인이 생성될 확률 (%)")]
    [Range(0f, 100f)] public float coinSpawnChance = 25f; // 기본 25%
    [Tooltip("블록 위치로부터 코인이 생성될 Y축 오프셋")]
    public float coinYOffset = 1f;
    // --- 추가 끝 ---

    [Header("패턴 포인트들")]
    public LaneRowFine[] points = new LaneRowFine[] {
        new LaneRowFine{ lane=0, rowOffset=1, yFine=0f, colorGroup=0, allowSpring=true },
        new LaneRowFine{ lane=2, rowOffset=2, yFine=0f, colorGroup=0, allowSpring=true },
        new LaneRowFine{ lane=4, rowOffset=3, yFine=0f, colorGroup=0, allowSpring=true },
        new LaneRowFine{ lane=6, rowOffset=4, yFine=0f, colorGroup=0, allowSpring=true },
        new LaneRowFine{ lane=4, rowOffset=5, yFine=0f, colorGroup=0, allowSpring=true },
        new LaneRowFine{ lane=2, rowOffset=6, yFine=0f, colorGroup=0, allowSpring=true },
    };

    [Header("Same-lane Extra Gap")]
    [Tooltip("같은 레인에서 이 행 차이 이내면 같은 색 금지(세그먼트 경계까지 더 강하게 막기용)")]
    public int sameLaneExtraGap = 0;
    [Header("Lane Offset (전체 이동)")]
    public int laneOffset = 0;
    [Header("색 규칙(기존)")]
    public int minRowGapForSameColor = 2;
    public int globalMaxStreak = 2;
    [Header("빌드 옵션")]
    public bool clearOldChildrenOnBuild = true;
    [Header("시각 레인 중앙 보정 (셀 단위)")]
    [Tooltip("이 값으로 ‘보이는 위치’를 미세 보정 (프리팹마다 달라도 됨)")]
    public float halfCellBias = 0f;
    [Header("논리 레인 옵션")]
    [Tooltip("규칙 판정(머리박기/연속색 등)에 논리 레인(공통 바이어스)을 사용")]
    public bool useLogicLaneIndex = true;
    [Tooltip("논리 레인의 중앙 보정(셀 단위). 보통 0을 권장(전 프리팹 공통)")]
    public float logicHalfBias = 0f;
    [Header("추가 금지 규칙(겹침 방지)")]
    public float minWorldDXForSameColor = 0.6f;
    public float minWorldDYForSameColor = 1.1f;
    public bool useProximityBan = true;
    [Header("그룹 강제 규칙")]
    public bool forceSameColorInGroup = true;
    public bool useColorGroupUnique = false;
    public bool groupOverridesOtherBans = true;

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
        if (palettes == null || palettes.Count == 0 || points == null || points.Length == 0)
        {
            Debug.LogWarning("[SegmentPatternBuilder] Palettes 리스트가 비어있습니다.", gameObject);
            return baseRow;
        }

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
            int visLane = Mathf.Clamp(points[i].lane + laneOffset, 0, lanes - 1);
            int row = baseRow + Mathf.Max(0, points[i].rowOffset);
            float yFineWorld = points[i].yFine * cellSize;
            int group = points[i].colorGroup;
            float x = GetLaneCenter(visLane, halfCellBias);
            float y = row * cellSize + yFineWorld;
            Vector2 wpos = new Vector2(x, y);
            int logicLane = GetLogicLaneIndexFromX(x);
            BlockColor pick;

            if (forceSameColorInGroup && group > 0 && _groupColor.TryGetValue(group, out var fixedColor))
            {
                pick = fixedColor;
            }
            else
            {
                var banned = new HashSet<BlockColor>();
                if (state.hasLaneHistory[logicLane])
                {
                    int d = Mathf.Abs(row - state.lastRowPerLane[logicLane]);
                    int needGap = Mathf.Max(minRowGapForSameColor, sameLaneExtraGap);
                    if (d <= needGap)
                        banned.Add(state.lastColorPerLane[logicLane]);
                }
                if (globalMaxStreak > 0 && state.hasGlobal && state.globalStreak >= globalMaxStreak)
                    banned.Add(state.globalColor);
                if (useColorGroupUnique && group > 0)
                {
                    for (int k = 0; k < spawned.Count; k++)
                        if (spawned[k].group == group)
                            banned.Add(spawned[k].color);
                }
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

                int paletteIdx = points[i].paletteIndex;
                if (paletteIdx < 0 || paletteIdx >= palettes.Count) paletteIdx = 0;
                var all = palettes[paletteIdx].AllColors;

                var candidates = new List<BlockColor>(all.Count);
                foreach (var c in all) if (!banned.Contains(c)) candidates.Add(c);
                pick = (candidates.Count == 0)
                    ? all[Random.Range(0, all.Count)]
                    : candidates[Random.Range(0, candidates.Count)];

                if (forceSameColorInGroup && group > 0)
                    _groupColor[group] = pick;
            }

            int index = points[i].paletteIndex;
            if (index < 0 || index >= palettes.Count)
            {
                Debug.LogWarning($"잘못된 Palette Index({index})가 감지되어 0번 팔레트를 사용합니다.", gameObject);
                index = 0;
            }
            PlatformPalette paletteToUse = palettes[index];

            // [수정] 여기서 확률적으로 가시 팔레트로 바꿔치기할지 결정합니다.
            // 생성할 최종 프리팹을 담을 변수
            GameObject prefab = null;

            // --- [이 플래그 변수 추가] ---
            bool isSpringBlock = false; // 스프링 블록인지 확인하기 위함

            // 현재 처리 중인 포인트의 정보를 가져옵니다.
            var currentPoint = points[i];

            // 1. 스프링 블록 확률 체크
            // [추가된 조건] currentPoint.allowSpring가 true일 때만 확률 체크를 실행합니다.
            if (currentPoint.allowSpring && paletteToUse.springVersionAsset != null && Random.Range(0f, 100f) < paletteToUse.springChance)
            {
                // 스프링 블록이 당첨되면 프리팹을 스프링 블록으로 교체!
                prefab = paletteToUse.springVersionAsset.GetPrefab();
                isSpringBlock = true; // --- [플래그 설정] ---
            }
            // 2. 가시 블록 확률 다음으로 체크 (스프링이 아닐 경우)
            else if (paletteToUse.spikeVersionPalette != null && Random.Range(0, 100) < paletteToUse.spikeChance)
            {
                // 가시 블록이 당첨되면, 해당 색상의 가시 프리팹으로 교체!
                var spikePalette = paletteToUse.spikeVersionPalette;
                prefab = spikePalette.GetPrefab(pick); // 'pick'된 색상에 맞는 가시 블록을 가져옴
            }

            // 3. Mov2 블록 확률 다음으로 체크
            else if (paletteToUse.movingVersionPalette != null && Random.Range(0, 100) < paletteToUse.movingChance)
            {
                // Mov2 팔레트로 교체
                var movingPalette = paletteToUse.movingVersionPalette;
                // 'pick'된 색상에 맞는 움직이는 블록을 가져옴
                prefab = movingPalette.GetPrefab(pick);
            }

            // 4. [신규 추가] 움직이는 가시 블록 확률 체크
            else if (paletteToUse.movingSpikeVersionPalette != null && Random.Range(0, 100) < paletteToUse.movingSpikeChance)
            {
                var movingSpikePalette = paletteToUse.movingSpikeVersionPalette;
                prefab = movingSpikePalette.GetPrefab(pick);
            }
            // 5. [신규 추가] 움직이는 스프링 블록 확률 체크
            else if (currentPoint.allowSpring && paletteToUse.movingSpringVersionAsset != null && Random.Range(0f, 100f) < paletteToUse.movingSpringChance)
            {
                prefab = paletteToUse.movingSpringVersionAsset.GetPrefab();
                isSpringBlock = true; // 움직이는 스프링도 코인 생성 X
            }

            // 3. 스프링/가시 둘 다 당첨되지 않았으면 일반 프리팹 사용
            if (prefab == null)
            {
                prefab = paletteToUse.GetPrefab(pick);
            }

            if (prefab)
            {
                var go = Instantiate(prefab, new Vector3(x, y, 0f), Quaternion.identity, transform);
                go.name = $"Plat_{pick}_#{i}";

                // 2. [추가] 코인 생성 로직
                // 코인 프리팹이 있고, 스프링 블록이 아니며, 확률에 당첨되었을 때
                if (coinPrefab != null && !isSpringBlock && Random.Range(0f, 100f) < coinSpawnChance)
                {
                    // 코인 위치 계산 (블록 위치 + Y 오프셋)
                    Vector3 coinPosition = new Vector3(x, y + coinYOffset, 0f);

                    // 코인을 생성하고 'go.transform'(방금 만든 블록)의 자식으로 설정
                    Instantiate(coinPrefab, coinPosition, Quaternion.identity, go.transform);
                }
            }

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

    float GetLaneCenter(int lane, float halfBias)
    {
        float left = originX - (lanes * cellSize) * 0.5f;
        float bias = (lanes % 2 == 0) ? halfBias * cellSize : 0f;
        return left + (lane + 0.5f) * cellSize + bias;
    }

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