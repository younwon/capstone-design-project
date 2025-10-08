using System.Collections.Generic;
using UnityEngine;

public class CloudSpawner : MonoBehaviour
{
    public Transform cam;

    [Header("Cloud Sprites")]
    public Sprite[] cloudSprites;

    [Header("Counts & Ranges")]
    public int targetCount = 12;
    public Vector2 yRange = new Vector2(-3f, 8f);
    public Vector2 speedRange = new Vector2(0.2f, 0.6f);
    public Vector2 scaleRange = new Vector2(0.6f, 1.2f);
    public float bandWidthMul = 2.0f;

    [Header("Fade density by height (toward space)")]
    public float densityFalloffStart = 70f;
    public float densityFalloffEnd = 95f;

    [Header("Render (always behind gameplay)")]
    public string sortingLayerName = "BackgroundNear";
    public int sortingOrder = -100;
    public Color minTint = new Color(1f, 1f, 1f, 0.55f);
    public Color maxTint = new Color(1f, 1f, 1f, 0.85f);

    [Header("Center de-emphasis")]
    public bool centerFade = true;
    public Vector2 centerOffset = Vector2.zero;
    public float centerRadius = 2.6f;
    public float centerFeather = 1.2f;
    [Range(0f, 1f)] public float centerAlphaInside = 0.35f;

    class Cloud { public Transform t; public float speed; public SpriteRenderer sr; public float baseA; }
    readonly List<Cloud> pool = new();

    void Start()
    {
        if (!cam) cam = Camera.main ? Camera.main.transform : null;
        Refill();
    }

    void Update()
    {
        if (!cam) return;

        // 타이틀 대기 중에는 unscaledDeltaTime 사용
        float dt = StartGate.GameStarted ? Time.deltaTime : Time.unscaledDeltaTime;

        float halfH = Camera.main.orthographicSize;
        float halfW = halfH * Camera.main.aspect;
        float bandHalf = halfW * bandWidthMul;

        float h = cam.position.y;
        float k = Mathf.InverseLerp(densityFalloffEnd, densityFalloffStart, h);
        int want = Mathf.RoundToInt(targetCount * Mathf.Clamp01(k));
        if (want != pool.Count) Refill(want);

        Vector2 cam2 = cam.position;
        cam2 += centerOffset;

        foreach (var c in pool)
        {
            c.t.Translate(Vector3.right * c.speed * dt, Space.World);

            float dx = c.t.position.x - cam.position.x;
            if (dx > bandHalf) c.t.position += Vector3.left * (bandHalf * 2f);
            if (dx < -bandHalf) c.t.position += Vector3.right * (bandHalf * 2f);

            float yMin = cam.position.y + yRange.x;
            float yMax = cam.position.y + yRange.y;
            var p = c.t.position; p.y = Mathf.Clamp(p.y, yMin, yMax); c.t.position = p;

            if (centerFade && c.sr)
            {
                float d = Vector2.Distance(new Vector2(c.t.position.x, c.t.position.y), cam2);
                float t = Mathf.InverseLerp(centerRadius, centerRadius + centerFeather, d);
                float centerFactor = Mathf.Lerp(centerAlphaInside, 1f, t);

                var col = c.sr.color;
                col.a = c.baseA * centerFactor;
                c.sr.color = col;
            }
        }
    }

    void Refill(int count = -1)
    {
        foreach (var c in pool) if (c.t) Destroy(c.t.gameObject);
        pool.Clear();

        if (count < 0) count = targetCount;
        if (cloudSprites == null || cloudSprites.Length == 0) return;

        float halfH = Camera.main.orthographicSize;
        float halfW = halfH * Camera.main.aspect;
        float bandHalf = halfW * bandWidthMul;

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Cloud");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = cloudSprites[Random.Range(0, cloudSprites.Length)];
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = sortingOrder;
            sr.color = Color.Lerp(minTint, maxTint, Random.value);
            sr.flipX = Random.value < 0.5f;

            float s = Random.Range(scaleRange.x, scaleRange.y);
            go.transform.localScale = new Vector3(s, s, 1f);

            float x = (cam ? cam.position.x : 0f) + Random.Range(-bandHalf, bandHalf);
            float y = (cam ? cam.position.y : 0f) + Random.Range(yRange.x, yRange.y);
            go.transform.position = new Vector3(x, y, 10f);

            go.transform.SetParent(transform);
            pool.Add(new Cloud { t = go.transform, speed = Random.Range(speedRange.x, speedRange.y), sr = sr, baseA = sr.color.a });
        }
    }
}




