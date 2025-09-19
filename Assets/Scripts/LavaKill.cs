using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LavaKill : MonoBehaviour
{
    [Tooltip("플레이어를 식별할 태그")]
    public string playerTag = "Player";

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        var death = other.GetComponent<PlayerDeath>();
        if (death) death.Die("Lava");
        else SimpleGameOver.TryGameOver("Lava (PlayerDeath 없음)");
    }

    // 만약 Trigger가 아니라 충돌 방식이면 아래 메서드 사용
    void OnCollisionEnter2D(Collision2D c)
    {
        var other = c.collider;
        if (!other.CompareTag(playerTag)) return;
        var death = other.GetComponent<PlayerDeath>();
        if (death) death.Die("Lava");
        else SimpleGameOver.TryGameOver("Lava (PlayerDeath 없음)");
    }
}


