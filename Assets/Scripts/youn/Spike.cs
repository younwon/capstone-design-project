// /Scripts/youn/Spike.cs
// 플레이어가 닿으면 넉백 + 일정 시간 조작 잠금
// - PlayerController2D에 DisableControlsFor(seconds, zeroHorizontal) 메서드가 있어야 합니다.
// - 자식 콜라이더로 닿아도 GetComponentInParent<>로 컨트롤러를 탐색합니다.

using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Spike : MonoBehaviour
{
    [Header("Knockback")]
    [Tooltip("피격 시 가해질 넉백 힘 (x는 좌/우 방향에 따라 부호가 자동 적용)")]
    public Vector2 knockbackForce = new Vector2(6f, 8f);

    [Tooltip("넉백 전에 속도를 0으로 초기화할지")]
    public bool resetVelocityBeforeKnockback = true;

    [Header("Control Lock")]
    [Tooltip("입력 잠금 유지 시간(초)")]
    public float controlDisableDuration = 0.4f;

    [Tooltip("잠금 직전에 수평속도를 0으로 만들지 (DisableControlsFor의 zeroHorizontal 인자)")]
    public bool zeroHorizontalOnLock = true;

    [Header("Optional SFX")]
    public AudioClip sfxHit;

    [Header("Detection")]
    [Tooltip("플레이어를 태그로 제한하고 싶다면 Player 태그를 사용하세요. 비활성화하면 컨트롤러 유무로 판단합니다.")]
    public bool usePlayerTag = false;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true; // 기본은 트리거 추천
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.collider);
    }

    private void HandleHit(Collider2D other)
    {
        if (!other) return;

        // 태그 제한이 켜져있다면 Player 태그만 통과
        if (usePlayerTag && !other.CompareTag("Player"))
            return;

        // 자식 콜라이더 대응: 부모에서 PlayerController2D 찾기
        var controller = other.GetComponentInParent<PlayerController2D>();
        if (!controller) return;

        var rb = controller.GetComponent<Rigidbody2D>();
        if (!rb) return;

        // 컨트롤 잠금
        controller.DisableControlsFor(controlDisableDuration, zeroHorizontalOnLock);

        // 넉백
        if (resetVelocityBeforeKnockback)
            rb.velocity = Vector2.zero;

        // 스파이크 기준 좌/우 방향 결정 (같은 x면 오른쪽으로 처리)
        float dirX = Mathf.Sign(controller.transform.position.x - transform.position.x);
        if (Mathf.Approximately(dirX, 0f)) dirX = 1f;

        Vector2 finalKnockback = new Vector2(knockbackForce.x * dirX, knockbackForce.y);
        rb.AddForce(finalKnockback, ForceMode2D.Impulse);

        // SFX
        if (AudioManager.I && sfxHit)
            AudioManager.I.PlaySFX(sfxHit);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
        var c = GetComponent<Collider2D>();
        if (c is BoxCollider2D b)
            Gizmos.DrawCube(b.bounds.center, b.bounds.size);
        else if (c is CircleCollider2D cc)
            Gizmos.DrawWireSphere(cc.bounds.center, cc.radius * 2f);
        else if (c is PolygonCollider2D || c is CapsuleCollider2D)
            Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
    }
#endif
}
