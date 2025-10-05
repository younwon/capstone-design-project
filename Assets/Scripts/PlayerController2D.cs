using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController2D : MonoBehaviour
{
    // --- 기존 변수들은 그대로 ---
    public LavaRiser lavaRiser;
    bool _hasStarted = false;
    [SerializeField] float startInputThreshold = 0.01f;
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;
    [Header("Ground Check (2-Point)")]
    public float footSeparation = 0.2f;
    public float groundProbeOffset = 0.02f;
    public float groundProbeRadius = 0.1f;
    [Header("Jump Assist")]
    public bool enableCoyote = true;
    public float coyoteTime = 0.1f;
    public bool enableJumpBuffer = true;
    public float jumpBufferTime = 0.1f;

    Rigidbody2D rb;
    Collider2D col;
    Animator anim;
    SpriteRenderer sr;

    bool isGrounded;
    float coyoteTimer;
    float jumpBufferTimer;

    // [추가된 변수]
    private bool canControl = true;

    void Awake()
    {
        // ... Awake 함수는 변경 없음 ...
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        anim = GetComponentInChildren<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();
        rb.gravityScale = Mathf.Max(1f, rb.gravityScale);
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Update()
    {
        // ✅ [수정] canControl이 true일 때만 입력 처리
        if (canControl)
        {
            if (PlayerStartedMoving() && lavaRiser != null)
            {
                lavaRiser.StartLava();
            }
            float inputX = Input.GetAxisRaw("Horizontal");
            rb.velocity = new Vector2(inputX * moveSpeed, rb.velocity.y);
            if (Input.GetButtonDown("Jump")) jumpBufferTimer = jumpBufferTime;
        }

        // 시각적 처리는 canControl과 무관하게 처리
        HandleSpriteFacing();
        HandleAnimation();

        if (enableJumpBuffer && jumpBufferTimer > 0f) jumpBufferTimer -= Time.unscaledDeltaTime;
        if (!isGrounded && coyoteTimer > 0f) coyoteTimer -= Time.unscaledDeltaTime;
        TryConsumeJump();
    }

    void HandleSpriteFacing()
    {
        if (sr && Mathf.Abs(rb.velocity.x) > 0.1f)
        {
            sr.flipX = rb.velocity.x < 0f;
        }
    }

    void HandleAnimation()
    {
        if (anim)
        {
            bool movingOnGround = Mathf.Abs(rb.velocity.x) > 0.1f && isGrounded;
            anim.SetBool("IsMoving", movingOnGround);
            anim.SetBool("IsGrounded", isGrounded);
        }
    }

    // [추가된 함수] 외부에서 호출하여 잠시 조작을 막는 기능
    public void DisableControlsFor(float duration)
    {
        StartCoroutine(DisableControlsRoutine(duration));
    }

    private IEnumerator DisableControlsRoutine(float duration)
    {
        canControl = false;
        yield return new WaitForSeconds(duration);
        canControl = true;
    }

    // --- 나머지 함수들은 그대로 ---
    void FixedUpdate()
    {
        bool groundedNow = false;
        var b = col.bounds;
        Vector3 bottomCenter = b.center + Vector3.down * (b.extents.y + groundProbeOffset);
        Vector3 footLeft = bottomCenter + Vector3.left * footSeparation;
        Vector3 footRight = bottomCenter + Vector3.right * footSeparation;
        if (IsGroundedOn(Physics2D.OverlapCircleAll(footLeft, groundProbeRadius)))
        {
            groundedNow = true;
        }
        else if (IsGroundedOn(Physics2D.OverlapCircleAll(footRight, groundProbeRadius)))
        {
            groundedNow = true;
        }
        if (groundedNow)
        {
            isGrounded = true;
            coyoteTimer = coyoteTime;
        }
        else
        {
            if (isGrounded) coyoteTimer = coyoteTime;
            isGrounded = false;
        }
    }

    bool IsGroundedOn(Collider2D[] hits)
    {
        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (!h || h.attachedRigidbody == rb) continue;
            if (h.CompareTag("Ground") || h.CompareTag("Block")) return true;
        }
        return false;
    }

    void TryConsumeJump()
    {
        bool canJumpNow = isGrounded || (enableCoyote && coyoteTimer > 0f);
        bool wantsJump = (canControl && Input.GetButtonDown("Jump")) || (enableJumpBuffer && jumpBufferTimer > 0f);
        if (canJumpNow && wantsJump)
        {
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            rb.velocity = new Vector2(rb.velocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            if (anim) anim.SetTrigger("Jump");
            isGrounded = false;
        }
    }

    bool PlayerStartedMoving()
    {
        if (_hasStarted) return false;
        float x = Input.GetAxisRaw("Horizontal");
        bool jumpPressed = Input.GetButtonDown("Jump");
        bool started = Mathf.Abs(x) > startInputThreshold || jumpPressed || Mathf.Abs(rb.velocity.x) > 0.01f;
        if (started) _hasStarted = true;
        return started;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var c = GetComponent<Collider2D>();
        if (!c) return;
        var b = c.bounds;
        Vector3 bottomCenter = b.center + Vector3.down * (b.extents.y + groundProbeOffset);
        Vector3 footLeft = bottomCenter + Vector3.left * footSeparation;
        Vector3 footRight = bottomCenter + Vector3.right * footSeparation;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(footLeft, groundProbeRadius);
        Gizmos.DrawWireSphere(footRight, groundProbeRadius);
    }
#endif
}