using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    public string equippedHatName = "";

    public Transform shopContentParent;
    public GameObject shopItemUIPrefab;
    public List<ShopItemData> shopItems;

    private List<ShopItemUI> allShopItemUIs = new List<ShopItemUI>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 상점 씬으로 돌아왔을 때 UI를 다시 로드
        if (scene.name == "Shop") // "상점 씬 이름"을 실제 씬 이름으로 변경
        {
            // ★★★ 새로운 씬에서 shopContentParent를 다시 찾습니다. ★★★
            GameObject contentObject = GameObject.Find("Content");
            if (contentObject != null)
            {
                shopContentParent = contentObject.transform;
                ClearAllShopItems();
                PopulateShop();
                UpdateAllItemButtons();
            }
        }
        // 플레이 씬으로 돌아갔을 때 데이터 전달
        else if (scene.name == "SpeedRun") // "플레이 씬 이름"을 실제 씬 이름으로 변경
        {
            DeliverEquippedHatData();
        }
        else if (scene.name == "ColorClimb") // "플레이 씬 이름"을 실제 씬 이름으로 변경
        {
            DeliverEquippedHatData();
        }
    }

    private void Start()
    {
        // Start()에서는 이제 상점 UI를 초기화하지 않습니다.
        // OnSceneLoaded() 함수에서 처리합니다.
    }

    private void ClearAllShopItems()
    {
        if (shopContentParent != null) // ★★★ null 체크 추가
        {
            foreach (Transform child in shopContentParent)
            {
                Destroy(child.gameObject);
            }
        }
        allShopItemUIs.Clear();
    }

    private void PopulateShop()
    {
        if (shopContentParent == null) return; // ★★★ null 체크 추가

        foreach (ShopItemData itemData in shopItems)
        {
            GameObject itemUI = Instantiate(shopItemUIPrefab, shopContentParent);
            ShopItemUI shopItemUI = itemUI.GetComponent<ShopItemUI>();
            if (shopItemUI != null)
            {
                shopItemUI.SetData(itemData);
                allShopItemUIs.Add(shopItemUI);
            }
        }
    }

    public void UpdateAllItemButtons()
    {
        if (allShopItemUIs.Count == 0) return; // ★★★ 빈 리스트일 때 오류 방지

        foreach (ShopItemUI itemUI in allShopItemUIs)
        {
            itemUI.UpdateButtonState();
        }
    }

    private void DeliverEquippedHatData()
    {
        if (PlayerHat.Instance != null && !string.IsNullOrEmpty(equippedHatName))
        {
            ShopItemData hatData = FindHatDataByName(equippedHatName);
            if (hatData != null)
            {
                PlayerHat.Instance.EquipHat(hatData);
            }
        }
    }

    public ShopItemData FindHatDataByName(string hatName)
    {
        foreach (var item in shopItems)
        {
            if (item.itemName == hatName)
            {
                return item;
            }
        }
        return null;
    }
    [ContextMenu("Clear All Purchased Items")]
    public void ClearAllPurchasedItems()
    {
        foreach (ShopItemData itemData in shopItems)
        {
            // 각 아이템의 구매 기록을 PlayerPrefs에서 삭제
            PlayerPrefs.DeleteKey(itemData.itemName);
        }
        PlayerPrefs.Save();

        Debug.Log("[ShopManager] 모든 아이템 구매 기록 초기화 완료.");

        // 아이템 상태를 초기화 후, 모든 버튼의 상태를 업데이트
        equippedHatName = "";

        // PlayerHat이 존재하면 모자를 벗김
        if (PlayerHat.Instance != null)
        {
            PlayerHat.Instance.UnequipHat();
        }

        // 상점 UI의 모든 버튼 상태를 갱신
        UpdateAllItemButtons();
    }
}