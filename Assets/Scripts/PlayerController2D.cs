// /Scripts/Player/PlayerController2D.cs
// 통합본: 이동/점프(코요테/버퍼) + 움직이는 발판 속도 반영 + 점프 SFX + 컨트롤 잠금 API
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("External")]
    public LavaRiser lavaRiser;

    [Header("Control")]
    public bool canControl = true;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;

    [Header("Ground Check")]
    public float groundProbeOffset = 0.02f;
    public float groundProbeRadius = 0.4f;

    [Header("Jump Assist")]
    public bool enableCoyote = true;
    public float coyoteTime = 0.1f;
    public bool enableJumpBuffer = true;
    public float jumpBufferTime = 0.1f;

    [Header("SFX")]
    public AudioClip jumpClip;

    Rigidbody2D rb;
    Collider2D col;
    Animator anim;
    SpriteRenderer sr;

    bool isGrounded;
    float coyoteTimer;
    float jumpBufferTimer;
    bool _hasStarted = false;

    Vector2 platformVelocity = Vector2.zero;
    MovingPlatform currentPlatform = null;

    void Awake()
    {
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
        if (PlayerStartedMoving() && lavaRiser != null) lavaRiser.StartLava();

        if (Input.GetButtonDown("Jump")) jumpBufferTimer = jumpBufferTime;
        if (enableJumpBuffer && jumpBufferTimer > 0f) jumpBufferTimer -= Time.unscaledDeltaTime;
        if (!isGrounded && enableCoyote && coyoteTimer > 0f) coyoteTimer -= Time.unscaledDeltaTime;

        float inputX = canControl ? Input.GetAxisRaw("Horizontal") : 0f;
        HandleSpriteFacing(inputX);

        if (anim)
        {
            bool movingOnGround = Mathf.Abs(inputX) > 0.01f && isGrounded;
            anim.SetBool("IsMoving", movingOnGround);
            anim.SetBool("IsGrounded", isGrounded);
        }

        TryConsumeJump(inputX);
    }

    void FixedUpdate()
    {
        UpdateGroundAndPlatformVelocity();

        float inputX = canControl ? Input.GetAxisRaw("Horizontal") : 0f;

        float finalVelX = inputX * moveSpeed;
        float finalVelY = rb.velocity.y;

        if (isGrounded) finalVelX += platformVelocity.x;
        if (isGrounded && platformVelocity.y < 0f) finalVelY = platformVelocity.y;

        rb.velocity = new Vector2(finalVelX, finalVelY);
    }

    void UpdateGroundAndPlatformVelocity()
    {
        var b = col.bounds;
        Vector3 foot = b.center + Vector3.down * (b.extents.y + groundProbeOffset);

        Collider2D[] hits = Physics2D.OverlapCircleAll(foot, groundProbeRadius);

        bool groundedNow = false;
        platformVelocity = Vector2.zero;
        currentPlatform = null;

        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (!h || h.attachedRigidbody == rb) continue;
            if (h.CompareTag("Ground") || h.CompareTag("Block"))
            {
                groundedNow = true;
                var mp = h.GetComponent<MovingPlatform>() ?? h.GetComponentInParent<MovingPlatform>();
                if (mp)
                {
                    currentPlatform = mp;
                    platformVelocity = mp.CurrentVelocity;
                }
                break;
            }
        }

        if (groundedNow)
        {
            if (!isGrounded) coyoteTimer = coyoteTime;
            isGrounded = true;
            coyoteTimer = coyoteTime;
        }
        else
        {
            if (isGrounded) coyoteTimer = coyoteTime;
            isGrounded = false;
            currentPlatform = null;
            platformVelocity = Vector2.zero;
        }
    }

    void TryConsumeJump(float inputX)
    {
        bool canJumpNow = isGrounded || (enableCoyote && coyoteTimer > 0f);
        bool wantsJump = Input.GetButtonDown("Jump") || (enableJumpBuffer && jumpBufferTimer > 0f);
        if (!(canControl && wantsJump && canJumpNow)) return;

        jumpBufferTimer = 0f;
        coyoteTimer = 0f;

        rb.velocity = new Vector2(rb.velocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        if (anim) anim.SetTrigger("Jump");
        isGrounded = false;

        if (AudioManager.I && jumpClip)
        {
            float pitch = Random.Range(0.95f, 1.05f);
            AudioManager.I.PlaySFX(jumpClip, pitch);
        }
    }

    void HandleSpriteFacing(float inputX)
    {
        if (!sr) return;
        if (Mathf.Abs(inputX) > 0.01f) sr.flipX = inputX < 0f;
    }

    bool PlayerStartedMoving()
    {
        if (_hasStarted) return false;
        float x = Input.GetAxisRaw("Horizontal");
        bool jumpPressed = Input.GetButtonDown("Jump");
        bool started = Mathf.Abs(x) > 0.01f || jumpPressed || Mathf.Abs(rb.velocity.x) > 0.01f;
        if (started) _hasStarted = true;
        return started;
    }

    // 외력/피해 등으로 일정 시간 입력 잠금
    Coroutine _disableCo;
    public void DisableControlsFor(float seconds, bool zeroHorizontal = true)
    {
        if (_disableCo != null) StopCoroutine(_disableCo);
        _disableCo = StartCoroutine(CoDisableControls(seconds, zeroHorizontal));
    }
    IEnumerator CoDisableControls(float seconds, bool zeroHorizontal)
    {
        canControl = false;
        if (zeroHorizontal)
        {
            var v = rb.velocity; v.x = 0f; rb.velocity = v;
        }
        float t = 0f;
        while (t < seconds) { t += Time.deltaTime; yield return null; }
        canControl = true; _disableCo = null;
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
