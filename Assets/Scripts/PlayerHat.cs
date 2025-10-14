using UnityEngine;

public class PlayerHat : MonoBehaviour
{
    public static PlayerHat Instance;

    public SpriteRenderer hatImage;
    private ShopItemData equippedHat = null;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (ShopManager.Instance != null)
        {
            string equippedHatName = ShopManager.Instance.equippedHatName;
            if (!string.IsNullOrEmpty(equippedHatName))
            {
                // ★★★ 이름으로 ShopItemData를 찾습니다.
                ShopItemData hatData = ShopManager.Instance.FindHatDataByName(equippedHatName);
                if (hatData != null)
                {
                    EquipHat(hatData);
                }
            }
        }
    }

    public void EquipHat(ShopItemData itemData)
    {
        if (hatImage == null)
        {
            Debug.LogError("EquipHat 실패: 'Hat Image' 필드가 null입니다. 유니티 에디터에서 연결해주세요.");
            return;
        }
        if (itemData != null && itemData.playerAccessorySprite != null)
        {
            hatImage.sprite = itemData.playerAccessorySprite;
            hatImage.gameObject.SetActive(true);
            equippedHat = itemData;
        }
        else
        {
            UnequipHat();
        }
    }

    public void UnequipHat()
    {
        if (hatImage != null)
        {
            hatImage.gameObject.SetActive(false);
            equippedHat = null;
        }
    }

    public bool IsEquipped(ShopItemData itemData)
    {
        return equippedHat == itemData;
    }
}