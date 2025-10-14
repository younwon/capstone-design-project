using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Test : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI buttonText;
    public int upgradeCost = 10;

    [Header("Player")]
    public Transform playerTransform; // 플레이어 트랜스폼
    public GameObject playerEffect;   // 활성화할 플레이어 이펙트 오브젝트

    private void Start()
    {
        // 게임 시작 시 버튼 텍스트 업데이트
        if (buttonText != null)
        {
            buttonText.text = $"{upgradeCost}";
        }
    }

    // 버튼 클릭 시 호출될 함수
    public void BuyButton()
    {
        // 코인 매니저가 유효한지 확인
        if (CoinManager.Instance == null)
        {
            Debug.LogError("CoinManager 인스턴스를 찾을 수 없습니다.");
            return;
        }

        // 코인 개수가 충분한지 확인
        if (CoinManager.Instance.GetCoinCount() >= upgradeCost)
        {
            // 코인 소모
            CoinManager.Instance.SpendCoin(upgradeCost);

            // 플레이어 이펙트 활성화
            if (playerEffect != null)
            {
                playerEffect.SetActive(true);
                Debug.Log("플레이어 이펙트가 활성화되었습니다.");
            }
            else
            {
                Debug.LogError("플레이어 이펙트 오브젝트가 할당되지 않았습니다.");
            }
        }
        else
        {
            Debug.LogWarning("코인이 부족합니다!");
        }
    }
}