using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public Image itemImage;
    public Button purchaseButton;
    public TextMeshProUGUI buttonText;

    // 잠금 상태를 시각적으로 보여줄 UI 요소 (자물쇠 아이콘 등)
    public GameObject lockOverlay; 

    private ShopItemData itemData;

    // 데이터 설정 및 UI 초기화
    public void SetData(ShopItemData data)
    {
        itemData = data;
        nameText.text = itemData.itemName;
        priceText.text = itemData.itemCost.ToString();
        itemImage.sprite = itemData.itemIcon;
        UpdateButtonState();
    }

    // 버튼 상태 업데이트 (가장 중요한 부분!)
    public void UpdateButtonState()
    {
        if (itemData == null) return;

        // 1. 업적 달성 여부를 먼저 확인
        bool isUnlocked = AchievementManager.Instance.IsAchievementUnlocked(itemData.requiredAchievementId);

        if (!isUnlocked)
        {
            //  업적 미달성 시: "잠김" 상태로 표시
            purchaseButton.interactable = false;
            buttonText.text = "LOCK";
            priceText.gameObject.SetActive(false); // 가격 숨기기
            if (lockOverlay != null) lockOverlay.SetActive(true);
        }
        else
        {
            //  업적 달성 시: 기존 구매/장착 로직 실행
            if (lockOverlay != null) lockOverlay.SetActive(false);
            priceText.gameObject.SetActive(true);

            bool isPurchased = PlayerPrefs.GetInt(itemData.itemName, 0) == 1;

            if (isPurchased)
            {
                // 구매 완료 상태
                purchaseButton.interactable = true;
                bool isEquipped = ShopManager.Instance.equippedHatName == itemData.itemName;
                buttonText.text = isEquipped ? "O" : "X";
                
                purchaseButton.onClick.RemoveAllListeners();
                purchaseButton.onClick.AddListener(EquipItem);
            }
            else
            {
                // 구매 가능 상태
                buttonText.text = "BUY";
                // 코인이 충분한지 확인하여 버튼 활성화/비활성화
                purchaseButton.interactable = CoinManager.Instance.GetCoinCount() >= itemData.itemCost;

                purchaseButton.onClick.RemoveAllListeners();
                purchaseButton.onClick.AddListener(TryPurchase);
            }
        }
    }

    // 아이템 구매 시도
    private void TryPurchase()
    {
        // 코인이 충분한지 다시 한번 확인
        if (CoinManager.Instance.SpendCoin(itemData.itemCost))
        {
            PlayerPrefs.SetInt(itemData.itemName, 1); // 구매 기록 저장
            PlayerPrefs.Save();
            ShopManager.Instance.UpdateAllItemButtons(); // 모든 아이템 UI 갱신
        }
    }

    // 아이템 장착
    private void EquipItem()
    {
        ShopManager.Instance.equippedHatName = itemData.itemName;
        ShopManager.Instance.UpdateAllItemButtons();
        // 실제 플레이어에게 아이템 적용하는 로직 호출
    }
}