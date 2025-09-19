using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SnapToGrid : MonoBehaviour
{
    public bool snapContinuously = false; // 체크하면 항상 스냅, 해제하면 필요할 때만
    public int laneOverride = -1;         // -1이면 현재 x기준 레인으로 스냅

    void Update()
    {
        if (snapContinuously) DoSnap();
    }

    [ContextMenu("Snap To Grid Now")]
    public void DoSnap()
    {
        // GridManager 찾기
        var gm = GridManager.I ? GridManager.I : FindObjectOfType<GridManager>();
        if (gm == null) return;

        var p = transform.position;

        // 레인 결정
        int lane = (laneOverride >= 0) ? Mathf.Clamp(laneOverride, 0, gm.lanes - 1)
                                       : gm.WorldToLane(p.x);

        float x = gm.GetLaneCenter(lane);
        float y = Mathf.Round(p.y / gm.cellSize) * gm.cellSize;

        transform.position = new Vector3(x, y, p.z);
    }
}

