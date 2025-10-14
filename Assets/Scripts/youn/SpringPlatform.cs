using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class SpringPlatform : MonoBehaviour
{
    [Header("점프 설정")]
    public float jumpForce = 20f;       // 튀어오르는 힘
    public float jumpInterval = 0.5f;   // 점프 간격 (초)

    [Header("스프라이트 설정")]
    public Sprite idleSprite;           // 평상시 스프라이트
    public Sprite pressedSprite;        // 눌렸을 때 스프라이트
    public float pressDuration = 0.1f;  // 눌린 스프라이트 유지 시간

    private SpriteRenderer spriteRenderer;
    private bool canJump = true;        // 점프 쿨다운 확인용

    void Awake()
    {
        // 자식 오브젝트에 있는 SpriteRenderer를 찾습니다.
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    // 콜라이더에 무언가 '닿아있는 동안' 계속 호출되는 함수입니다.
    void OnCollisionStay2D(Collision2D collision)
    {
        // 1. 점프 가능한 상태이고,
        // 2. 부딪힌 오브젝트에 Rigidbody2D가 있다면
        if (canJump && collision.rigidbody != null)
        {
            Rigidbody2D otherRb = collision.rigidbody;

            // 위로 튀어오르기 전, y축 속도를 0으로 만들어 매번 일정한 높이로 튀게 합니다.
            otherRb.velocity = new Vector2(otherRb.velocity.x, 0f);

            // 위쪽으로 순간적인 힘을 가합니다.
            otherRb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            // 점프 쿨다운 및 시각 효과 코루틴을 시작합니다.
            StartCoroutine(JumpCooldown());
        }
    }

    IEnumerator JumpCooldown()
    {
        // 점프를 했으니 잠시 뛸 수 없는 상태로 변경
        canJump = false;

        // 눌린 스프라이트로 변경
        if (spriteRenderer != null && pressedSprite != null)
        {
            spriteRenderer.sprite = pressedSprite;
        }

        // 짧은 시간 동안 눌린 모습 보여주기
        yield return new WaitForSeconds(pressDuration);

        // 다시 원래 스프라이트로 복귀
        if (spriteRenderer != null && idleSprite != null)
        {
            spriteRenderer.sprite = idleSprite;
        }

        // 전체 점프 간격(jumpInterval)을 채우기 위해 나머지 시간만큼 더 대기
        float remainingTime = jumpInterval - pressDuration;
        if (remainingTime > 0)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        // 다시 점프 가능한 상태로 변경
        canJump = true;
    }
}
