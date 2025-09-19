using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AutoTiledBackdrop : MonoBehaviour
{
    public Transform cam;

    [Header("Follow")]
    public bool lockToCamera = true;     // ← 이걸 켜면 항상 카메라 정중앙에 고정
    [Range(0f, 1f)] public float parallax = 1.0f; // lockToCamera가 꺼진 경우에만 사용
    public Vector2 offset;

    [Header("Fill Size (always cover screen)")]
    public float widthMul = 2.4f;
    public float heightMul = 2.4f;

    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr.drawMode != SpriteDrawMode.Tiled) sr.drawMode = SpriteDrawMode.Tiled;
        if (!cam) cam = Camera.main ? Camera.main.transform : null;
    }

    void LateUpdate()
    {
        if (!cam) { cam = Camera.main ? Camera.main.transform : null; if (!cam) return; }

        // 위치
        Vector3 p = cam.position;
        Vector3 target = lockToCamera
            ? new Vector3(p.x + offset.x, p.y + offset.y, transform.position.z)
            : new Vector3(p.x * parallax + offset.x, p.y * parallax + offset.y, transform.position.z);

        transform.position = target;

        // 화면보다 항상 크게 타일 사이즈 갱신
        var c = Camera.main;
        if (c)
        {
            float halfH = c.orthographicSize;
            float halfW = halfH * c.aspect;
            sr.size = new Vector2(halfW * 2f * widthMul, halfH * 2f * heightMul);
        }
    }
}


