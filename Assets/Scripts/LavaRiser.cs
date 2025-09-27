using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class LavaRiser : MonoBehaviour
{
    public Transform cam;
    public GridManager grid;
    public bool lavaStarted = false;

    [Header("Renderers (children)")]
    public SpriteRenderer bodySR;   // Lava/Body의 SR (Tiled)
    public SpriteRenderer capSR;    // Lava/Cap  의 SR (Tiled)
    public float capHeight = 0.35f; // 캡 두께(월드 유닛)
    public float capYOffset = 0.02f;

    [Header("Start position (top of lava)")]
    public float startBelowScreenUnits = 3.0f; // 카메라 하단에서 이만큼 더 아래서 시작(“표면” 기준)

    [Header("Width/size")]
    public float sideMargin = 0.15f;     // 좌우 여유
    public float bodyHeightMul = 2.2f;   // 화면 높이의 몇 배만큼 아래로 채움

    [Header("Speed")]
    public float baseSpeed = 0.25f;      // 초당 유닛
    public float addPerMinute = 0.12f;

    [Header("Surge")]
    public float surgeInterval = 12f;    // 매 12초
    public float surgeDuration = 2.2f;   // 2.2초 동안
    public float surgeMult = 1.7f;       // 배속

    [Header("Damage")]
    public float damageCooldown = 1.0f;
    public float bounceUpVelocity = 7.5f;

    BoxCollider2D col;
    float startTime;
    readonly Dictionary<PlayerHP, float> lastHit = new();

    void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        if (!cam) cam = Camera.main ? Camera.main.transform : null;
        if (!grid) grid = GridManager.I ? GridManager.I : FindObjectOfType<GridManager>();

        if (!bodySR || !capSR)
        {
            // 자동 탐색(이름 기준)
            bodySR = transform.Find("Body")?.GetComponent<SpriteRenderer>();
            capSR = transform.Find("Cap")?.GetComponent<SpriteRenderer>();
        }
        if (bodySR) bodySR.drawMode = SpriteDrawMode.Tiled;
        if (capSR) capSR.drawMode = SpriteDrawMode.Tiled;
    }

    void Start()
    {
        startTime = Time.time;
        PlaceStartBelowScreen();
        ResizeAndLayout();
    }

    void Update()
    {
        if (!lavaStarted) return; // 신호 전에는 상승 안 함

        ResizeAndLayout();

        if (!cam) cam = Camera.main ? Camera.main.transform : null;
        if (!cam) return;

        // 매 프레임 폭/레이아웃 보정
        ResizeAndLayout();

        // 속도(시간 경과 + 서지)
        float elapsed = Time.time - startTime;
        float speed = baseSpeed + addPerMinute * (elapsed / 60f);
        if (surgeInterval > 0f && surgeDuration > 0f)
        {
            float t = elapsed % (surgeInterval + surgeDuration);
            if (t < surgeDuration) speed *= surgeMult;
        }

        // 표면(부모)을 상승
        transform.position += Vector3.up * (speed * Time.deltaTime);
    }

    void PlaceStartBelowScreen()
    {
        var c = Camera.main;
        float halfH = c.orthographicSize;
        float camBottom = cam.position.y - halfH;

        Vector3 p = transform.position;
        p.y = camBottom - startBelowScreenUnits;     // “표면”을 화면 하단 밑으로 내림
        p.x = grid ? grid.originX : 0f;
        transform.position = p;
    }

    void ResizeAndLayout()
    {
        float width = GetPlayWidth() + sideMargin * 2f;

        // Body는 "아래로" 채우게: 크기 = 화면 높이의 몇 배, 로컬 Y = -size.y/2
        if (bodySR)
        {
            var c = Camera.main;
            float bodyH = c.orthographicSize * 2f * bodyHeightMul;
            bodySR.size = new Vector2(width, bodyH);
            bodySR.transform.localPosition = new Vector3(0f, -bodyH * 0.5f, 0f);
        }

        // Cap은 표면에 얇게 놓기
        if (capSR)
        {
            capSR.size = new Vector2(width, capHeight);
            capSR.transform.localPosition = new Vector3(0f, capYOffset, 0f);
        }

        // 충돌 영역: 표면 바로 아래 얇게
        col.isTrigger = true;
        col.size = new Vector2(width, Mathf.Max(0.6f, capHeight + 0.2f));
        col.offset = new Vector2(0f, -col.size.y * 0.5f + capYOffset + 0.05f);
    }

    float GetPlayWidth()
    {
        if (grid != null) { var (l, r) = grid.GetPlayBoundsX(); return r - l; }
        var c = Camera.main;
        return c.orthographicSize * 2f * c.aspect;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        var hp = other.GetComponentInParent<PlayerHP>();
        if (!hp) return;

        if (lastHit.TryGetValue(hp, out float last))
            if (Time.time - last < damageCooldown) return;

        hp.Damage(1);
        lastHit[hp] = Time.time;

        var rb = other.attachedRigidbody;
        if (rb && rb.velocity.y < bounceUpVelocity)
            rb.velocity = new Vector2(rb.velocity.x, bounceUpVelocity);
    }
    public void StartLava()
    {
        lavaStarted = true;
        startTime = Time.time;
    }
}


