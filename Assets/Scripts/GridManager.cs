using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CellKind { Empty, Stone, Spring, OtherSolid, Lava, Water }

public class GridManager : MonoBehaviour
{
    public static GridManager I;

    [Header("Grid / Lane Settings")]
    public int lanes = 8;        // 가로 레인 수
    public float cellSize = 1f;  // 1칸 = 1유닛
    public float originX = 0f;   // 그리드 중앙 x

    // 행(row) 단위로 보관: rowIndex -> lane 배열
    struct Cell { public GameObject go; public CellKind kind; public bool solid; public int placedTick; }
    readonly Dictionary<int, Cell[]> rows = new Dictionary<int, Cell[]>();
    int tick;       // “먼저/나중” 비교용 타임스탬프

    float[] laneCenters;

    void Awake()
    {
        I = this;
        Rebuild();
    }

    void OnValidate() { Rebuild(); }

    public void Rebuild()
    {
        if (lanes <= 0) lanes = 1;
        float playWidth = lanes * cellSize;
        float left = originX - (playWidth * 0.5f);

        laneCenters = new float[lanes];
        for (int i = 0; i < lanes; i++)
            laneCenters[i] = left + (i + 0.5f) * cellSize;
    }

    // ===== 좌표/스냅 유틸 =====
    public float GetLaneCenter(int lane)
    {
        lane = Mathf.Clamp(lane, 0, lanes - 1);
        return laneCenters[lane];
    }

    public int WorldToLane(float x)
    {
        float playWidth = lanes * cellSize;
        float left = originX - (playWidth * 0.5f);
        int idx = Mathf.FloorToInt((x - left) / cellSize);
        return Mathf.Clamp(idx, 0, lanes - 1);
    }

    public int WorldYToRow(float y)
    {
        return Mathf.RoundToInt(y / cellSize);
    }

    public Vector2 SnapToGrid(Vector2 worldPos)
    {
        int lane = WorldToLane(worldPos.x);
        float x = GetLaneCenter(lane);
        float y = Mathf.Round(worldPos.y / cellSize) * cellSize;
        return new Vector2(x, y);
    }

    public Vector3 GetCellWorldPos(int lane, int row)
    {
        return new Vector3(GetLaneCenter(lane), row * cellSize, 0f);
    }

    public (float left, float right) GetPlayBoundsX()
    {
        float half = (lanes * cellSize) * 0.5f;
        return (originX - half, originX + half);
    }

    // ===== 점유 관리 =====
    Cell[] EnsureRow(int row)
    {
        if (!rows.TryGetValue(row, out var arr))
        {
            arr = new Cell[lanes];
            rows[row] = arr;
        }
        return arr;
    }

    public void RegisterBlockAt(int lane, int row, GameObject go, bool solid, CellKind kind)
    {
        var arr = EnsureRow(row);

        // 이미 점유되어 있으면 위로 한 칸 올려서 다시 등록(간단 보호)
        if (arr[lane].go != null)
        {
            row += 1;
            arr = EnsureRow(row);
        }

        arr[lane] = new Cell { go = go, kind = kind, solid = solid, placedTick = ++tick };
        rows[row] = arr;
        // (행 꽉 참/봉인 로직은 완전히 제거됨)
    }

    public bool IsOccupied(int lane, int row, bool requireSolid = false)
    {
        var arr = EnsureRow(row);
        if (arr[lane].go == null) return false;
        return requireSolid ? arr[lane].solid : true;
    }

    public void ClearCell(int lane, int row, bool destroyGO = true)
    {
        var arr = EnsureRow(row);
        if (arr[lane].go != null && destroyGO)
            Object.Destroy(arr[lane].go);
        arr[lane] = default;
        rows[row] = arr;
    }
}


