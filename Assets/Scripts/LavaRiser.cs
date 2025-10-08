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

    // ---------- ★ NEW : 보조 트리거(플레이어 움직임 감지) ----------
    [Header("Start Trigger (optional)")]
    [Tooltip("게임 시작 후 플레이어가 움직이거나 점프 입력이 들어오면 자동으로 StartLava()를 1회 호출합니다.")]
    [SerializeField] bool autoStartWhenPlayerMoves = true;
    [SerializeField, Tooltip("속도 임계값(이 이상이면 '움직임'으로 간주)")]
    float moveSpeedThreshold = 0.02f;

    PlayerController2D player;       // 자동 탐색
    Rigidbody2D playerRb;            // 자동 탐색
    // -------------------------------------------------------------

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

        // ★ NEW : 플레이어 캐시 시도
        CachePlayerIfNeeded();
    }

    void Start()
    {
        startTime = Time.time;
        PlaceStartBelowScreen();
        ResizeAndLayout();
    }

    void Update()
    {
        // ★ NEW : 아직 시작 전이라면, 플레이어 움직임으로 한 번 시작 시도
        if (!lavaStarted && autoStartWhenPlayerMoves && StartGate.GameStarted)
        {
            TryAutoStartFromPlayerMotion();
        }

        if (!lavaStarted) return; // 신호 전에는 상승 안 함

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

    // ==== 외부에서 타이틀 귀환 시 호출될 수 있는 리셋용 메서드 ====
    /// <summary>
    /// 용암을 정지시키고, 카메라 하단 기준 startBelowScreenUnits 만큼 아래에 재배치.
    /// 타이틀 복귀 시 호출되어도 안전합니다.
    /// </summary>
    public void ResetToStart()
    {
        // 상승 중지
        lavaStarted = false;

        // 타이머 초기화(가속/서지 계산 초기화)
        startTime = Time.time;

        // 기준 카메라 보정
        if (!cam) cam = Camera.main ? Camera.main.transform : null;

        // 시작 위치로 내리고 폭/콜라이더 재설정
        PlaceStartBelowScreen();
        ResizeAndLayout();

        // 마지막 피격 쿨타임도 초기화(안전)
        lastHit.Clear();
    }

    /// <summary>
    /// 외부에서 수동으로 시작/정지 제어가 필요할 때 사용.
    /// </summary>
    public void SetStarted(bool started)
    {
        if (started) StartLava();
        else lavaStarted = false;
    }

    public void StartLava()
    {
        lavaStarted = true;
        startTime = Time.time;
    }

    // ==== 내부 구현 ====

    void PlaceStartBelowScreen()
    {
        var c = Camera.main;
        if (!c || !cam) return;

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
            if (c)
            {
                float bodyH = c.orthographicSize * 2f * bodyHeightMul;
                bodySR.size = new Vector2(width, bodyH);
                bodySR.transform.localPosition = new Vector3(0f, -bodyH * 0.5f, 0f);
            }
        }

        // Cap은 표면에 얇게 놓기
        if (capSR)
        {
            capSR.size = new Vector2(width, capHeight);
            capSR.transform.localPosition = new Vector3(0f, capYOffset, 0f);
        }

        // 충돌 영역: 표면 바로 아래 얇게
        if (col)
        {
            col.isTrigger = true;
            col.size = new Vector2(width, Mathf.Max(0.6f, capHeight + 0.2f));
            col.offset = new Vector2(0f, -col.size.y * 0.5f + capYOffset + 0.05f);
        }
    }

    // 타이틀 복귀 시 용암 표면을 카메라 하단 밑으로 다시 내린다.
    public bool TryResetToStart()
    {
        // 참조 보정
        if (!cam) cam = Camera.main ? Camera.main.transform : null;
        if (!grid) grid = GridManager.I ? GridManager.I : FindObjectOfType<GridManager>();
        if (!cam || !Camera.main) return false;

        var c = Camera.main;
        float halfH = c.orthographicSize;
        float camBottom = cam.position.y - halfH;

        // “표면” 위치 재설정
        Vector3 p = transform.position;
        p.y = camBottom - startBelowScreenUnits;   // 화면 하단에서 지정 거리만큼 아래
        if (grid) p.x = grid.originX;              // 가로 정렬(그리드 기준)
        transform.position = p;

        // 상태 리셋
        lavaStarted = false;   // 타이틀에서는 멈춰있게
        startTime = Time.time;

        // 렌더/콜라이더 다시 맞춤
        ResizeAndLayout();
        return true;
    }

    float GetPlayWidth()
    {
        if (grid != null) { var (l, r) = grid.GetPlayBoundsX(); return r - l; }
        var c = Camera.main;
        return c ? c.orthographicSize * 2f * c.aspect : 10f;
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

    // ---------- ★ NEW : 보조 트리거 구현 ----------
    void CachePlayerIfNeeded()
    {
        if (!player) player = Object.FindObjectOfType<PlayerController2D>(true);
        if (player && !playerRb) playerRb = player.GetComponent<Rigidbody2D>();
    }

    void TryAutoStartFromPlayerMotion()
    {
        CachePlayerIfNeeded();
        if (!player && !playerRb) return;

        // 입력 또는 실제 속도로 판단(둘 중 하나라도 감지되면 시작)
        bool inputMove = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f || Input.GetButtonDown("Jump");
        bool rbMove = false;
        if (playerRb)
        {
            var v = playerRb.velocity;
            rbMove = Mathf.Abs(v.x) > moveSpeedThreshold || Mathf.Abs(v.y) > moveSpeedThreshold;
        }

        if (inputMove || rbMove)
        {
            StartLava(); // 한 번만 호출됨(lavaStarted=true로 전환)
        }
    }
    // ------------------------------------------------
}



