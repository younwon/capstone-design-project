using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 'Player' 태그를 가진 오브젝트와 충돌했는지 확인
        if (other.CompareTag("Player"))
        {
            // CoinManager 스크립트에 코인 획득을 알림
            CoinManager.Instance.AddCoin(1);

            // 코인 오브젝트를 파괴하여 사라지게 함
            Destroy(gameObject);
        }
    }
}