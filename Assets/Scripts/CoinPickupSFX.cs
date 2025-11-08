// Assets/Scripts/Audio/CoinPickupSFX.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CoinPickupSFX : MonoBehaviour
{
    [Header("Clip")]
    [SerializeField] private AudioClip pickupClip;

    [Header("Volume / Pitch")]
    [Range(0f, 1f)] public float volume = 1f;
    [Tooltip("재생 시 랜덤 피치 범위")]
    public Vector2 randomPitch = new Vector2(0.98f, 1.02f);

    [Header("3D Option")]
    [Tooltip("3D로 공간감 있게 재생(필요 없으면 꺼두세요)")]
    public bool playAs3D = false;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true; // 코인은 보통 트리거
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other || !other.CompareTag("Player")) return;
        PlayNow();
        // Coin.cs에서 실제 파괴/카운트 처리는 이미 수행됩니다.
    }

    public void PlayNow()
    {
        if (!pickupClip) return;

        float pitch = (randomPitch.x == 0f && randomPitch.y == 0f)
            ? 1f
            : Random.Range(Mathf.Min(randomPitch.x, randomPitch.y), Mathf.Max(randomPitch.x, randomPitch.y));

        // AudioManager가 있으면 그것 사용
        if (AudioManager.I)
        {
            if (playAs3D)
                AudioManager.I.PlaySFXAt(pickupClip, transform.position, pitch);
            else
                AudioManager.I.PlaySFX(pickupClip, pitch);
        }
        else
        {
            // 폴백: AudioSource 없이도 한 번 재생
            AudioSource.PlayClipAtPoint(pickupClip, transform.position, volume);
        }
    }
}
