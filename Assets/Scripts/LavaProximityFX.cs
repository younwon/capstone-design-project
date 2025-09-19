using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// 용암이 화면 하단에 '아주 가까워질 때만' 화면 전체를 살짝 붉게 틴트하고,
/// 카메라 쉐이크 세기를 조금 올린다. (좁은 범위, 저강도)
public class LavaProximityFX : MonoBehaviour
{
    public LavaRiser lava;              // Lava(부모, LavaRiser) 드래그
    public Transform cam;               // Main Camera
    public Image screenTint;            // Canvas의 ScreenTint(Image)
    public CameraShaker shaker;         // Main Camera의 CameraShaker

    [Header("Distances (world units)")]
    public float startBelow = 0.6f;     // 하단에서 이만큼 '아래'에 있을 때부터 살짝 시작
    public float fullAt = 0.05f;    // 이 거리 이내면(거의 닿음) 100%

    [Header("Visual")]
    [Range(0f, 1f)] public float maxAlpha = 0.12f; // 최종 알파(아주 은은하게)
    public float fadeSpeed = 8f;                   // 알파 보간 속도

    [Header("Shake")]
    [Range(0f, 1f)] public float shakeAtFull = 0.22f; // 알파=1일 때 external 강도

    void Awake()
    {
        if (!cam) cam = Camera.main ? Camera.main.transform : null;
        if (!lava) lava = FindObjectOfType<LavaRiser>();
        if (!shaker && Camera.main) shaker = Camera.main.GetComponent<CameraShaker>();
    }

    void LateUpdate()
    {
        if (!lava || !cam || !screenTint) return;

        var c = Camera.main;
        float camBottom = cam.position.y - c.orthographicSize;
        float dist = lava.transform.position.y - camBottom; // +: 하단 위로, -: 아직 아래

        // -startBelow → fullAt 구간을 0→1로 변환 (가까울수록 1)
        float t01 = Mathf.InverseLerp(-startBelow, fullAt, dist);
        t01 = Mathf.Clamp01(t01);

        // 화면 틴트 알파
        var col = screenTint.color;
        float targetA = t01 * maxAlpha;
        col.a = Mathf.MoveTowards(col.a, targetA, fadeSpeed * Time.deltaTime);
        screenTint.color = col;

        // 쉐이크는 더 약하게
        if (shaker) shaker.SetExternal(t01 * shakeAtFull);
    }
}

