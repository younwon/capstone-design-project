using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum ShopItemState { Buy, Equip, Unequip }

public class ShopItemUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public Image iconImage;
    public Button buyButton;
    public TextMeshProUGUI buttonText;

    private ShopItemData itemData;
    private ShopItemState currentState;

    private void Start()
    {
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnButtonClick);
    }

    public void SetData(ShopItemData data)
    {
        itemData = data;
        nameText.text = data.itemName;
        costText.text = data.itemCost.ToString();
        iconImage.sprite = data.itemIcon;
        UpdateButtonState();
    }

    private void OnButtonClick()
    {
        if (currentState == ShopItemState.Buy) BuyItem();
        else if (currentState == ShopItemState.Equip) EquipItem();
        else if (currentState == ShopItemState.Unequip) UnequipItem();
    }

    public void UpdateButtonState()
    {
        bool hasPurchased = PlayerPrefs.GetInt(itemData.itemName, 0) == 1;

        if (hasPurchased)
        {
            string equippedHatName = ShopManager.Instance != null ? ShopManager.Instance.equippedHatName : "";

            if (equippedHatName == itemData.itemName)
            {
                currentState = ShopItemState.Unequip;
                buttonText.text = "X";
            }
            else
            {
                currentState = ShopItemState.Equip;
                buttonText.text = "O";
            }
        }
        else
        {
            currentState = ShopItemState.Buy;
            buttonText.text = "BUY";
        }
    }

    private void BuyItem()
    {
        if (CoinManager.Instance.SpendCoin(itemData.itemCost))
        {
            PlayerPrefs.SetInt(itemData.itemName, 1);
            PlayerPrefs.Save();

            if (ShopManager.Instance != null) ShopManager.Instance.equippedHatName = itemData.itemName;

            UpdateButtonState();
            if (ShopManager.Instance != null)
            {
                // 이 함수가 상점의 모든 버튼을 갱신합니다
                ShopManager.Instance.UpdateAllItemButtons();
            }
        }
    }

    private void EquipItem()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.equippedHatName = itemData.itemName;
        }
        UpdateButtonState();
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.UpdateAllItemButtons();
        }
    }

    private void UnequipItem()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.equippedHatName = "";
        }
        UpdateButtonState();
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.UpdateAllItemButtons();
        }
    }
}