using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 같은 스프라이트를 4방향으로 1~2px 오프셋 복제해서 외곽선을 만든다.
[DisallowMultipleComponent]
public class SpriteOutline2D : MonoBehaviour
{
    public Color outlineColor = new Color(0f, 0f, 0f, 0.65f);
    [Range(1, 4)] public int thicknessPx = 2;
    public int sortingOrderOffset = -1; // 본체보다 뒤에서 그리기

    SpriteRenderer baseSR;
    SpriteRenderer[] copies;
    float ppu = 100f;

    void Awake()
    {
        baseSR = GetComponent<SpriteRenderer>();
        if (!baseSR) { enabled = false; return; }
        if (baseSR.sprite) ppu = baseSR.sprite.pixelsPerUnit;
        Build();
    }

    void LateUpdate()
    {
        if (!baseSR) return;
        // 스프라이트/플립/색상 동기화
        foreach (var c in copies)
        {
            if (!c) continue;
            c.sprite = baseSR.sprite;
            c.flipX = baseSR.flipX;
            c.flipY = baseSR.flipY;
            c.sortingLayerID = baseSR.sortingLayerID;
            c.sortingOrder = baseSR.sortingOrder + sortingOrderOffset;
            c.enabled = baseSR.enabled;
        }
    }

    void Build()
    {
        DestroyCopies();
        copies = new SpriteRenderer[4];
        Vector3[] offs = {
            new Vector3( 1,  0, 0),
            new Vector3(-1,  0, 0),
            new Vector3( 0,  1, 0),
            new Vector3( 0, -1, 0),
        };
        for (int i = 0; i < 4; i++)
        {
            var go = new GameObject("OutlinePart");
            go.transform.SetParent(transform, false);
            float off = thicknessPx / ppu;
            go.transform.localPosition = offs[i] * off;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = baseSR.sprite;
            sr.material = baseSR.sharedMaterial;
            sr.color = outlineColor;
            sr.sortingLayerID = baseSR.sortingLayerID;
            sr.sortingOrder = baseSR.sortingOrder + sortingOrderOffset;
            copies[i] = sr;
        }
    }

    void OnDisable() { DestroyCopies(); }
    void OnDestroy() { DestroyCopies(); }

    void DestroyCopies()
    {
        if (copies == null) return;
        for (int i = 0; i < copies.Length; i++)
            if (copies[i]) DestroyImmediate(copies[i].gameObject);
        copies = null;
    }
}

