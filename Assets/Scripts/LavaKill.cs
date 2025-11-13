using UnityEngine;

[AddComponentMenu("Hazard/Lava Kill")]
public class LavaKill : MonoBehaviour
{
    [Tooltip("플레이어로 인식할 태그")]
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other || !other.CompareTag(playerTag)) return;

        var death = other.GetComponent<PlayerDeath>();
        if (death == null || death.IsDead) return;

        // ★ 용암 접촉 순간 A 사운드
        other.GetComponent<DeathSfxController>()?.OnLavaHit();

        // 실제 죽음 처리
        death.Die("Lava");
    }
}
