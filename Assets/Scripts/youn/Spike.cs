using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Spike : MonoBehaviour
{
    [Tooltip("플레이어를 튕겨낼 힘. X는 수평, Y는 수직 힘입니다.")]
    public Vector2 knockbackForce = new Vector2(5f, 7f);
    [Tooltip("연속적인 충돌을 막기 위한 쿨다운 시간(초)")]
    public float effectCooldown = 1.0f;
    // [추가된 변수]
    [Tooltip("튕겨나간 후 조작이 가능해지기까지의 시간")]
    public float controlDisableDuration = 0.5f;

    private System.Collections.Generic.Dictionary<PlayerHP, float> lastHitPlayers = new();

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void HandleCollision(Collider2D other)
    {
        PlayerHP playerHP = other.GetComponentInParent<PlayerHP>();
        if (playerHP == null) return;

        if (lastHitPlayers.TryGetValue(playerHP, out float lastHitTime))
        {
            if (Time.time - lastHitTime < effectCooldown) return;
        }
        lastHitPlayers[playerHP] = Time.time;

        playerHP.TriggerHitReaction();

        Rigidbody2D playerRb = other.attachedRigidbody;
        if (playerRb != null)
        {
            //  [추가된 부분] 플레이어 컨트롤러를 찾아 조작을 잠시 막습니다.
            var controller = other.GetComponent<PlayerController2D>();
            if (controller != null)
            {
                controller.DisableControlsFor(controlDisableDuration);
            }

            playerRb.velocity = Vector2.zero;
            float direction = Mathf.Sign(other.transform.position.x - transform.position.x);
            if (direction == 0) direction = 1;
            Vector2 finalKnockback = new Vector2(knockbackForce.x * direction, knockbackForce.y);
            playerRb.AddForce(finalKnockback, ForceMode2D.Impulse);
        }
    }

    private void OnTriggerEnter2D(Collider2D other) { HandleCollision(other); }
    private void OnTriggerStay2D(Collider2D other) { HandleCollision(other); }
}