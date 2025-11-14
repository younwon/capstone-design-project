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
    public float groundProbeRadius = 0.1f;

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

        // [수정] canControl이 false면 inputX가 0이 되어 키보드 입력이 막힙니다.
        float inputX = canControl ? Input.GetAxisRaw("Horizontal") : 0f;
        HandleSpriteFacing(inputX);

        if (anim)
        {
            // [수정] 넉백 중에도 걷는 애니메이션이 나오지 않도록 inputX 기준으로 변경
            bool movingOnGround = Mathf.Abs(inputX) > 0.01f && isGrounded;
            anim.SetBool("IsMoving", movingOnGround);
            anim.SetBool("IsGrounded", isGrounded);
        }

        TryConsumeJump(inputX);
    }

    //  [수정] FixedUpdate 로직 수정
    void FixedUpdate()
    {
        UpdateGroundAndPlatformVelocity();

        float finalVelX;
        float finalVelY = rb.velocity.y;

        // X축 속도 결정
        if (canControl)
        {
            // 조작이 가능할 때만 키보드 입력을 받음
            float inputX = Input.GetAxisRaw("Horizontal");
            finalVelX = inputX * moveSpeed;
            if (isGrounded) finalVelX += platformVelocity.x;
        }
        else
        {
            // 넉백 중(canControl == false)이므로, X축 속도를 건드리지 않고
            // 현재 넉백으로 받고 있는 속도를 그대로 유지합니다.
            finalVelX = rb.velocity.x;
        }

        // Y축 속도 결정 (움직이는 발판 대응)
        if (isGrounded && platformVelocity.y < 0f)
        {
            finalVelY = platformVelocity.y;
        }

        rb.velocity = new Vector2(finalVelX, finalVelY);
    }

    void UpdateGroundAndPlatformVelocity()
    {
        var b = col.bounds;
        Vector3 foot = b.center + Vector3.down * (b.extents.y + groundProbeOffset);

        Collider2D[] hits = Physics2D.OverlapCircleAll(foot, groundProbeRadius);

        bool groundedNow = false;
        platformVelocity = Vector2.zero; // 매번 속도를 0으로 초기화
        currentPlatform = null; // 매번 현재 플랫폼을 null로 초기화

        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (!h || h.attachedRigidbody == rb) continue;
            if (h.CompareTag("Ground") || h.CompareTag("Block"))
            {
                groundedNow = true;


                // ---[핵심 수정] ---
                // 1. MovingPlatform (V1) 스크립트를 먼저 찾습니다.
                var mp1 = h.GetComponent<MovingPlatform>() ?? h.GetComponentInParent<MovingPlatform>();
                if (mp1)
                {
                    currentPlatform = mp1;
                    platformVelocity = mp1.CurrentVelocity;
                }
                else
                {
                    // 2. V1이 없으면 Mov2 (V2) 스크립트를 찾습니다.
                    var mp2 = h.GetComponent<Mov2>() ?? h.GetComponentInParent<Mov2>();
                    if (mp2)
                    {
                        platformVelocity = mp2.CurrentVelocity;
                        // 참고: currentPlatform 변수는 Mov2 타입을 담을 수 없으므로 null로 유지됩니다.
                    }
                }
                // --- [수정 끝] ---

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
        // [수정] canControl이 true일 때만 점프 입력을 받습니다.
        bool wantsJump = (canControl && Input.GetButtonDown("Jump")) || (enableJumpBuffer && jumpBufferTimer > 0f);
        if (!(wantsJump && canJumpNow)) return; // canControl 조건이 wantsJump 안에 포함됨

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
        // [수정] 넉백 중에도 실제 속도에 따라 방향이 바뀌도록 수정
        if (!sr) return;
        if (Mathf.Abs(rb.velocity.x) > 0.1f) // 넉백/입력 모두 포함
        {
            sr.flipX = rb.velocity.x < 0f;
        }
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
