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

    [Header("카메라 설정")]
    public Color shopDefaultColor = new Color(0.1f, 0.2f, 0.4f, 1f);
    public Color colorClimbShopColor = new Color(0.8f, 0.2f, 0.2f, 1f);
    public Color speedRunShopColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    private Camera mainCamera;

    private List<ShopItemUI> allShopItemUIs = new List<ShopItemUI>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // 저장된 장착 아이템 불러오기
            equippedHatName = PlayerPrefs.GetString("EquippedHatName", "");
            // 상점 아이템 리스트에 아이템이 하나라도 있다면
            if (shopItems.Count > 0)
            {
                // 첫 번째 아이템(0번)의 구매 기록을 확인하고, 없으면 구매 처리(1)합니다.
                if (PlayerPrefs.GetInt(shopItems[0].itemName, 0) == 0)
                {
                    PlayerPrefs.SetInt(shopItems[0].itemName, 1);

                    // (선택 사항) 만약 현재 장착 중인 아이템이 없다면, 기본 캐릭터를 장착시킵니다.
                    if (string.IsNullOrEmpty(equippedHatName))
                    {
                        equippedHatName = shopItems[0].itemName;
                        PlayerPrefs.SetString("EquippedHatName", equippedHatName);
                    }

                    PlayerPrefs.Save();
                }
            }

            SceneManager.sceneLoaded += OnSceneLoaded;

            if (Camera.main != null)
            {
                mainCamera = Camera.main;
            }
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
        if (mainCamera == null && Camera.main != null)
        {
            mainCamera = Camera.main;
        }

        // 상점 씬으로 돌아왔을 때 UI를 다시 로드
        if (scene.name == "Shop")
        {
            string cameFromScene = "";

            // SceneTracker에서 이전 씬 이름을 가져와 확인합니다.
            if (SceneTracker.Instance != null)
            {
                cameFromScene = SceneTracker.Instance.PreviousSceneName;
                Debug.Log($"상점 씬에 진입했습니다. 이전 씬은 {cameFromScene}입니다.");
            }
            if (mainCamera != null)
            {
                Color targetColor = shopDefaultColor; // 기본색으로 시작

                if (cameFromScene == "SpeedRun")
                {
                    targetColor = speedRunShopColor;
                }
                else if (cameFromScene == "ColorClimb")
                {
                    targetColor = colorClimbShopColor;
                }

                // 선택된 색상으로 카메라 배경색을 변경
                mainCamera.backgroundColor = targetColor;
            }

            // ... (기존 상점 초기화 로직: shopContentParent 찾기, ClearAllShopItems 등) ...
            GameObject contentObject = GameObject.Find("Content");
            if (contentObject != null)
            {
                shopContentParent = contentObject.transform;
                ClearAllShopItems();
                PopulateShop();
                UpdateAllItemButtons();
            }
        }
        // 플레이 씬으로 돌아갔을 때 배경색 복구 로직 (참고용)
        else if (scene.name == "SpeedRun" || scene.name == "ColorClimb")
        {
            DeliverEquippedHatData();

        }
    }
    private void ClearAllShopItems()
    {
        if (shopContentParent != null)
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
        if (shopContentParent == null) return;

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
        if (allShopItemUIs.Count == 0) return; // 빈 리스트일 때 오류 방지

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