using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BlockFaller : MonoBehaviour
{
    public static BlockFaller Active;

    [Header("Speed")]
    public float dropSpeed = 1.1f;         // 기본 낙하 속도(스포너가 덮어씀)
    public float softDropMultiplier = 3f;   // S/↓ 홀드 시 가속 배수

    [Header("Grid Register")]
    public bool isSolid = true;
    public CellKind kind = CellKind.OtherSolid;

    public bool landed { get; private set; }

    Rigidbody2D rb;
    GridManager gm;
    bool softDropping;

    void Awake()
    {
        // 런타임에 SnapToGrid 같은 배치용 스크립트가 붙어 있으면 끔
        foreach (var m in GetComponents<MonoBehaviour>())
            if (m && m.GetType().Name == "SnapToGrid") m.enabled = false;

        rb = GetComponent<Rigidbody2D>();
        gm = GridManager.I ? GridManager.I : FindObjectOfType<GridManager>();
        if (!gm) { Debug.LogError("GridManager가 필요합니다."); enabled = false; return; }

        // 동적 바디 + 중력 0, 우리가 속도 지정
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = true;
        rb.simulated = true;

        if (dropSpeed <= 0f) dropSpeed = 1.1f;

        // 시작 레인 정렬
        int lane = Mathf.Clamp(gm.WorldToLane(transform.position.x), 0, gm.lanes - 1);
        LockXToLane(lane);
    }

    public void SetActive(bool on)
    {
        if (on) Active = this;
        else if (Active == this) Active = null;
    }

    void Update()
    {
        if (landed) return;

        // 입력은 액티브 블럭만 받음
        if (Active == this)
        {
            // 레인 이동: Q/E, 숫자키 1~lanes
            int lane = gm.WorldToLane(transform.position.x);
            if (Input.GetKeyDown(KeyCode.Q)) lane--;
            if (Input.GetKeyDown(KeyCode.E)) lane++;
            for (int i = 0; i < gm.lanes && i < 10; i++)
            {
                KeyCode k = (i == 9) ? KeyCode.Alpha0 : (KeyCode)((int)KeyCode.Alpha1 + i);
                if (Input.GetKeyDown(k)) { lane = i; break; }
            }
            lane = Mathf.Clamp(lane, 0, gm.lanes - 1);
            LockXToLane(lane);

            // ⬇ 소프트 드롭: S 또는 ↓ 키 홀드 중?
            softDropping = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        }
    }

    void FixedUpdate()
    {
        if (landed) return;

        float v = dropSpeed * (softDropping ? softDropMultiplier : 1f);
        rb.velocity = new Vector2(0f, -v);
    }

    void LockXToLane(int lane)
    {
        float x = gm.GetLaneCenter(lane);
        rb.position = new Vector2(x, rb.position.y);
        transform.position = new Vector3(x, transform.position.y, transform.position.z);
    }

    void OnCollisionEnter2D(Collision2D c) { TryLand(c); }
    void OnCollisionStay2D(Collision2D c) { TryLand(c); }

    void TryLand(Collision2D c)
    {
        if (landed) return;

        bool topHit = false;
        for (int i = 0; i < c.contactCount; i++)
            if (c.GetContact(i).normal.y > 0.5f) { topHit = true; break; }
        if (!topHit) return;

        if (c.collider.CompareTag("Ground") || c.collider.CompareTag("Block"))
            Land();
    }

    void Land()
    {
        landed = true;
        rb.velocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;

        // 그리드 스냅 + 등록
        Vector2 snapped = gm.SnapToGrid(transform.position);
        transform.position = new Vector3(snapped.x, snapped.y, transform.position.z);

        int laneIdx = gm.WorldToLane(transform.position.x);
        int rowIdx = gm.WorldYToRow(transform.position.y);
        gm.RegisterBlockAt(laneIdx, rowIdx, gameObject, isSolid, kind);

        SetActive(false);
    }
}






