using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 그리드 좌우에 보이지 않는 벽(BoxCollider2D) 두 개를 자동 배치/리사이즈.
/// - 카메라를 따라 위아래로 충분히 덮어서, 플레이어가 좌우로 못 빠지게 막음
/// - 벽은 Tag "Wall" 사용(바닥 판정에 안 걸리도록)
/// </summary>
public class SideWallsAuto : MonoBehaviour
{
    public GridManager grid;
    public Transform cam;

    [Header("Wall Shape")]
    public float thickness = 0.3f;    // 벽 두께(유닛)
    public float margin = 0.02f;   // 그리드 바깥쪽 여유
    public float heightMul = 2.4f;    // 화면 높이의 몇 배로 벽을 키울지

    [Header("Layer/Tag")]
    public string wallTag = "Wall";   // Ground와 구분 (플레이어 바닥판정 제외)
    public string wallLayerName = "Default";

    BoxCollider2D leftCol, rightCol;

    void Awake()
    {
        if (!grid) grid = GridManager.I ? GridManager.I : FindObjectOfType<GridManager>();
        if (!cam) cam = Camera.main ? Camera.main.transform : null;

        // 왼/오 자식 생성
        leftCol = CreateChild("Wall_Left");
        rightCol = CreateChild("Wall_Right");
    }

    BoxCollider2D CreateChild(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.tag = wallTag;
        if (LayerMask.NameToLayer(wallLayerName) != -1) go.layer = LayerMask.NameToLayer(wallLayerName);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = false;                 // 실제로 막히게
        return col;
    }

    void LateUpdate()
    {
        if (!grid || !cam) return;

        var (leftX, rightX) = grid.GetPlayBoundsX();

        float camHalfH = Camera.main.orthographicSize;
        float height = camHalfH * 2f * heightMul;

        // 왼쪽/오른쪽 벽 x 위치
        float leftPosX = leftX - thickness * 0.5f - margin;
        float rightPosX = rightX + thickness * 0.5f + margin;

        // 카메라 y를 따라다니며 충분히 긴 벽 유지
        Vector3 center = new Vector3(0f, cam.position.y, 0f);

        leftCol.transform.position = new Vector3(leftPosX, center.y, 0f);
        rightCol.transform.position = new Vector3(rightPosX, center.y, 0f);

        leftCol.size = new Vector2(thickness, height);
        rightCol.size = new Vector2(thickness, height);
    }
}

