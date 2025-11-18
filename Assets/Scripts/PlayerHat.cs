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
                if (_instance == null)
                {
                    // 씬에 없으면 새로 생성하지 않고 null 리턴 (플레이어 없는 씬 고려)
                    return null;
                }
            }
            return _instance;
        }
    }

    [Header("Components")]
    public SpriteRenderer spriteRenderer; // 플레이어의 스프라이트 렌더러
    public Animator playerAnimator;       // 플레이어의 애니메이터

    [Header("Defaults")]
    // 아이템을 벗었을 때 돌아갈 기본 애니메이션 컨트롤러
    public RuntimeAnimatorController defaultAnimatorController;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 컴포넌트 자동 할당 (없을 경우)
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (playerAnimator == null) playerAnimator = GetComponent<Animator>();

        // 시작 시 기본 컨트롤러 저장 (나중에 벗기 기능을 위해)
        if (playerAnimator != null && defaultAnimatorController == null)
        {
            defaultAnimatorController = playerAnimator.runtimeAnimatorController;
        }
    }

    private void Start()
    {
        // ShopManager가 준비되었으면 데이터 요청 (기존 로직)
        if (ShopManager.Instance != null && !string.IsNullOrEmpty(ShopManager.Instance.equippedHatName))
        {
            // ShopManager의 DeliverEquippedHatData가 호출되기를 기다리거나,
            // 여기서 직접 요청할 수도 있습니다. (기존 흐름 유지)
        }
    }

    // 아이템 장착 (스프라이트 + 애니메이션 변경)
    public void EquipHat(ShopItemData itemData)
    {
        if (itemData == null) return;

        // 1. 스프라이트 변경 (기존 기능)
        if (itemData.playerAccessorySprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = itemData.playerAccessorySprite;
        }

        // 2. 애니메이션 변경 (새 기능)
        if (itemData.animatorOverride != null && playerAnimator != null)
        {
            playerAnimator.runtimeAnimatorController = itemData.animatorOverride;
        }
        // 만약 아이템에 애니메이션은 없고 스프라이트만 있다면 기본 애니메이션 유지
        else if (playerAnimator != null)
        {
            // 필요하다면 여기서 defaultAnimatorController로 되돌릴 수도 있음
            // playerAnimator.runtimeAnimatorController = defaultAnimatorController;
        }

        Debug.Log($"[PlayerHat] {itemData.itemName} 장착 완료 (애니메이션 교체 포함)");
    }

    // ★★★ 아이템 벗기 (기본 상태로 복구) ★★★
    public void UnequipHat()
    {
        // 1. 스프라이트 초기화 (필요시 null 혹은 기본 스프라이트로)
        // spriteRenderer.sprite = defaultSprite; // 기본 스프라이트가 있다면

        // 2. 애니메이션 초기화 (원래 컨트롤러로 복구)
        if (playerAnimator != null && defaultAnimatorController != null)
        {
            playerAnimator.runtimeAnimatorController = defaultAnimatorController;
        }

        Debug.Log("[PlayerHat] 아이템 해제 완료");
    }
}