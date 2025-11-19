using UnityEngine;

public class PlayerHat : MonoBehaviour
{
    private static PlayerHat _instance;
    public static PlayerHat Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PlayerHat>();
            }
            return _instance;
        }
    }

    [Header("인스펙터에서 직접 연결")]
    // 1. 캐릭터 본체 (애니메이션이 재생되는 곳)
    public Animator targetAnimator;

    // 2. 모자가 표시될 위치
    public SpriteRenderer hatImageRenderer;

    [Header("기본 설정")]
    // 아이템 해제 시 돌아갈 기본 애니메이터 (Project 창에서 드래그)
    public RuntimeAnimatorController defaultAnimator;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 시작할 때 기본 애니메이터가 비어있으면, 현재 애니메이터를 기본값으로 저장
        if (targetAnimator != null && defaultAnimator == null)
        {
            defaultAnimator = targetAnimator.runtimeAnimatorController;
        }
    }

    private void Start()
    {
        // 1. 저장된 모자 이름 확인
        string savedHatName = PlayerPrefs.GetString("EquippedHatName", "");

        // 2. 저장된게 있고, 샵 매니저가 있다면 데이터 요청
        if (!string.IsNullOrEmpty(savedHatName) && ShopManager.Instance != null)
        {
            ShopItemData hatData = ShopManager.Instance.FindHatDataByName(savedHatName);
            if (hatData != null)
            {
                EquipHat(hatData);
            }
        }
    }

    // ★ 장착 함수
    public void EquipHat(ShopItemData itemData)
    {
        if (itemData == null) return;

        // 1. 애니메이터 교체 (PlayerSprite의 Animator가 바뀜)
        if (targetAnimator != null && itemData.specificAnimator != null)
        {
            targetAnimator.runtimeAnimatorController = itemData.specificAnimator;
            // 애니메이션 싱크가 안 맞으면 아래 줄 주석 해제
            // targetAnimator.Play("Idle", 0, 0f); 
        }

        // 2. 모자 스프라이트 표시
        if (hatImageRenderer != null && itemData.playerAccessorySprite != null)
        {
            hatImageRenderer.sprite = itemData.playerAccessorySprite;
            hatImageRenderer.gameObject.SetActive(true);
        }
    }

    // ★ 해제 함수
    public void UnequipHat()
    {
        // 1. 기본 애니메이터로 복구
        if (targetAnimator != null && defaultAnimator != null)
        {
            targetAnimator.runtimeAnimatorController = defaultAnimator;
        }

        // 2. 모자 숨기기
        if (hatImageRenderer != null)
        {
            hatImageRenderer.gameObject.SetActive(false);
        }
    }

    public bool IsEquipped(ShopItemData itemData)
    {
        if (ShopManager.Instance != null)
        {
            return ShopManager.Instance.equippedHatName == itemData.itemName;
        }
        return false;
    }
}