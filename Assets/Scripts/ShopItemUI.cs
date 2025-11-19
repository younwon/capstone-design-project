using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI descriptionText;
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
        descriptionText.text = itemData.itemDescription;
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
            if (priceText) priceText.gameObject.SetActive(true);

            bool isPurchased = PlayerPrefs.GetInt(itemData.itemName, 0) == 1;

            if (isPurchased)
            {
                // 구매 완료 상태
                purchaseButton.interactable = true;
                bool isEquipped = ShopManager.Instance.equippedHatName == itemData.itemName;
                buttonText.text = isEquipped ? "X" : "O";
                
                purchaseButton.onClick.RemoveAllListeners();
                purchaseButton.onClick.AddListener(EquipItem);
            }
            else
            {
                // 구매 가능 상태
                buttonText.text = "BUY";
                purchaseButton.interactable = true;
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
        // 이미 내가 장착 중이라면? -> 해제(Unequip)
        if (ShopManager.Instance.equippedHatName == itemData.itemName)
        {
            ShopManager.Instance.equippedHatName = ""; // 이름 비우기
            PlayerPrefs.SetString("EquippedHatName", "");
        }
        // 다른 걸 끼고 있거나 아무것도 안 꼈다면? -> 장착(Equip)
        else
        {
            ShopManager.Instance.equippedHatName = itemData.itemName;
            PlayerPrefs.SetString("EquippedHatName", itemData.itemName);
        }

        PlayerPrefs.Save();

        // 1. 상점 UI 전체 갱신 (O, X 표시 업데이트)
        ShopManager.Instance.UpdateAllItemButtons();

        // 2. 현재 씬에 플레이어가 있다면 즉시 외형/애니메이션 변경 적용 (미리보기)
        if (PlayerHat.Instance != null)
        {
            // ShopManager가 가지고 있는 데이터를 통해 PlayerHat 갱신
            // (ShopManager에 있는 아이템 찾기 로직 활용)
            var hatData = ShopManager.Instance.FindHatDataByName(ShopManager.Instance.equippedHatName);

            if (hatData != null)
                PlayerHat.Instance.EquipHat(hatData);
            else
                PlayerHat.Instance.UnequipHat();
        }
    }
}