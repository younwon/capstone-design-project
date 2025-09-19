using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class BlockSpring : MonoBehaviour
{
    [Header("Tuning")]
    public float springForce = 18f;       // 튀어오르는 힘
    public float pressDuration = 0.12f;   // 눌린 연출 유지 시간
    public bool requireTopHit = true;     // 위에서 밟을 때만 작동

    [Header("Sprites")]
    public Sprite idleSprite;             // 펴진 스프라이트
    public Sprite pressedSprite;          // 눌린 스프라이트

    private SpriteRenderer sr;
    private bool pressing;
    private bool cooldown;                // 다중 트리거 방지

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>(); // 자식 Sprite 오브젝트
    }

    void OnCollisionEnter2D(Collision2D c) { TryBounce(c); }
    void OnCollisionStay2D(Collision2D c) { TryBounce(c); }

    void TryBounce(Collision2D c)
    {
        if (cooldown) return;

        // 플레이어인지 확인
        var otherRb = c.rigidbody;
        if (otherRb == null) return;
        var player = otherRb.GetComponent<PlayerController2D>();
        if (player == null) return;

        // ===== '위에서 밟음' 판정 =====
        if (requireTopHit && !IsFromTop(c)) return;

        // 반발력 적용
        otherRb.velocity = new Vector2(otherRb.velocity.x, 0f);
        otherRb.AddForce(Vector2.up * springForce, ForceMode2D.Impulse);

        // 점프 애니(선택)
        var anim = otherRb.GetComponentInChildren<Animator>();
        if (anim)
        {
            anim.SetBool("IsGrounded", false);
            anim.SetTrigger("Jump");
        }

        // 눌림 연출 + 쿨다운
        if (!pressing) StartCoroutine(PressVisual());
        StartCoroutine(BounceCooldown());
    }

    bool IsFromTop(Collision2D c)
    {
        // 1) 상대 속도가 '아래로'이어야 함 (스프링은 보통 Static)
        if (c.relativeVelocity.y > -0.05f) return false;

        // 2) 접촉 노멀의 위쪽 성분이 조금이라도 있으면 OK (느슨하게 0.1)
        for (int i = 0; i < c.contactCount; i++)
        {
            if (c.GetContact(i).normal.y > 0.1f) return true;
        }

        // 3) 최후의 보정: 플레이어 '발(bottom)'이 스프링 '윗면(top)'보다 위에 있으면 OK
        float playerBottom = c.collider.bounds.min.y;
        float springTop = GetComponent<Collider2D>().bounds.max.y - 0.01f;
        return playerBottom >= springTop;
    }

    IEnumerator PressVisual()
    {
        pressing = true;
        if (sr && pressedSprite) sr.sprite = pressedSprite;
        yield return new WaitForSeconds(pressDuration);
        if (sr && idleSprite) sr.sprite = idleSprite;
        pressing = false;
    }

    IEnumerator BounceCooldown()
    {
        cooldown = true;
        yield return new WaitForSeconds(0.05f); // 너무 연속으로 튀는 것 방지
        cooldown = false;
    }
}


