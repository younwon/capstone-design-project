using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Shop Item", menuName = "Shop/Shop Item Data")]
public class ShopItemData : ScriptableObject
{
    public string itemName;      // 아이템 이름
    public string itemDescription; // 아이템 설명
    public int itemCost;         // 아이템 가격
    public Sprite itemIcon;      // 아이템 아이콘
    public GameObject itemPrefab; // 구매 시 활성화할 오브젝트 (선택 사항)
    [Header("Player Accessory")]
    public Sprite playerAccessorySprite; // 플레이어에게 씌울 모자 스프라이트
}