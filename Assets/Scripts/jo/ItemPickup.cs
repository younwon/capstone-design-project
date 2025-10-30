using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    // TimedPlatform 프리팹 연결
    public GameObject timedPlatformPrefab;

    // 생성 위치 (없으면 아이템 위치 사용)
    public Transform spawnPoint;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (timedPlatformPrefab != null)
            {
                Vector3 pos = spawnPoint ? spawnPoint.position : transform.position;
                GameObject go = Instantiate(timedPlatformPrefab, pos, Quaternion.identity);

                // 생성된 오브젝트에서 TimedPlatform 실행
                TimedPlatform tp = go.GetComponent<TimedPlatform>();
                if (tp != null)
                {
                    tp.ActivatePlatform();
                }
            }

            Destroy(gameObject);
        }
    }
}