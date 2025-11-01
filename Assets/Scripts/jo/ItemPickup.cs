using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Platform Settings")]
    [Tooltip("생성할 TimedPlatform 프리팹")]
    public GameObject timedPlatformPrefab;

    [Tooltip("생성할 블록 개수")]
    public int platformCount = 5;

    [Tooltip("각 블록 간의 가로 간격")]
    public float stepX = 1.5f;

    [Tooltip("각 블록 간의 세로 높이 차이")]
    public float stepY = 0.8f;

    [Tooltip("아이템 기준으로 첫 번째 블록이 얼마나 떨어져 생성될지 (가로 거리)")]
    public float startOffsetX = 1.0f;

    [Header("Block Size Settings")]
    [Tooltip("생성되는 블록의 가로 크기")]
    public float blockScaleX = 1f;

    [Tooltip("생성되는 블록의 세로 크기")]
    public float blockScaleY = 1f;

    [Tooltip("플레이어의 태그 (기본: Player)")]
    public string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            CreateStairPlatforms(other.transform);
            Destroy(gameObject); // 아이템 제거
        }
    }

    private void CreateStairPlatforms(Transform player)
    {
        if (timedPlatformPrefab == null)
        {
            Debug.LogWarning("?? TimedPlatform Prefab이 연결되지 않았습니다!");
            return;
        }

        // ? 플레이어가 바라보는 방향에 따라 계단 방향 결정
        float direction = player.localScale.x > 0 ? 1f : -1f;

        // ? 시작 위치: 아이템 위치에서 약간 옆으로 오프셋
        Vector3 startPos = transform.position + new Vector3(startOffsetX * direction, 0f, 0f);

        for (int i = 0; i < platformCount; i++)
        {
            // 계단형 위치 계산
            Vector3 spawnPos = new Vector3(
                startPos.x + (stepX * i * direction),
                startPos.y + (stepY * i),
                startPos.z
            );

            // 블록 생성
            GameObject go = Instantiate(timedPlatformPrefab, spawnPos, Quaternion.identity);

            // ? 블록 크기 조절 (스케일 적용)
            go.transform.localScale = new Vector3(blockScaleX, blockScaleY, 1f);

            // 블록 활성화 (TimedPlatform 기능 시작)
            TimedPlatform tp = go.GetComponent<TimedPlatform>();
            if (tp != null)
                tp.ActivatePlatform();
        }
    }
}
