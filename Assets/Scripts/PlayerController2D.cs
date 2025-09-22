using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// 좌우 이동 + 점프(코요테/버퍼 지원). Collider2D 종류(박스/캡슐 등)와 무관하게 동작.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;

    [Header("Ground Check")]
    [Tooltip("발 아래로 살짝 내린 거리")]
    public float groundProbeOffset = 0.02f;
    [Tooltip("발끝 원형 검출 반경")]
    public float groundProbeRadius = 0.4f;   // 캡슐 사용시 0.1 정도 권장

    [Header("Jump Assist")]
    public bool enableCoyote = true;
    public float coyoteTime = 0.1f;
    public bool enableJumpBuffer = true;
    public float jumpBufferTime = 0.1f;

    Rigidbody2D rb;
    Collider2D col;            // ★ Collider2D로 변경 (Box 전제 제거)
    Animator anim;
    SpriteRenderer sr;

    bool isGrounded;
    float coyoteTimer;
    float jumpBufferTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();   // ★ 어떤 2D 콜라이더든 OK
        anim = GetComponentInChildren<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();

        rb.gravityScale = Mathf.Max(1f, rb.gravityScale);
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Update()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        rb.velocity = new Vector2(inputX * moveSpeed, rb.velocity.y);

        if (sr && Mathf.Abs(inputX) > 0.01f) sr.flipX = inputX < 0f;

        if (Input.GetButtonDown("Jump")) jumpBufferTimer = jumpBufferTime;

        if (anim)
        {
            bool movingOnGround = Mathf.Abs(inputX) > 0.01f && isGrounded;
            anim.SetBool("IsMoving", movingOnGround);
            anim.SetBool("IsGrounded", isGrounded);
        }

        if (enableJumpBuffer && jumpBufferTimer > 0f) jumpBufferTimer -= Time.unscaledDeltaTime;
        if (!isGrounded && enableCoyote && coyoteTimer > 0f) coyoteTimer -= Time.unscaledDeltaTime;

        TryConsumeJump();
    }

    void FixedUpdate()
    {
        // ★ Collider2D의 Bounds로 발 위치 계산 (박스/캡슐 모두 지원)
        var b = col.bounds;
        Vector3 foot = b.center + Vector3.down * (b.extents.y + groundProbeOffset);

        // 작은 원으로 바닥 감지
        Collider2D[] hits = Physics2D.OverlapCircleAll(foot, groundProbeRadius);

        bool groundedNow = false;
        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (!h || h.attachedRigidbody == rb) continue; // 자기 자신 제외
            if (h.CompareTag("Ground") || h.CompareTag("Block"))
            {
                groundedNow = true;
                break;
            }
        }

        if (groundedNow)
        {
            isGrounded = true;
            coyoteTimer = coyoteTime; // 바닥에 있으면 리필
        }
        else
        {
            if (isGrounded) coyoteTimer = coyoteTime; // 막 떨어진 순간 코요테 시작
            isGrounded = false;
        }
    }

    void TryConsumeJump()
    {
        bool canJumpNow = isGrounded || (enableCoyote && coyoteTimer > 0f);
        bool wantsJump = Input.GetButtonDown("Jump") ||
                         (enableJumpBuffer && jumpBufferTimer > 0f);

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

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var c = GetComponent<Collider2D>();
        if (!c) return;
        var b = c.bounds;
        Vector3 foot = b.center + Vector3.down * (b.extents.y + groundProbeOffset);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(foot, groundProbeRadius);
    }
#endif
}
