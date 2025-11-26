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

    public PlayerDeath playerDeath;
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
        if (playerDeath == null)
        {
            playerDeath = GetComponentInParent<PlayerDeath>(); // 보통 같은 오브젝트나 부모에 있음
        }
    }
    private void OnEnable()
    {
        // 잠시 대기 후 장착 시도 (씬 로드 타이밍 이슈 방지)
        Invoke(nameof(ForceEquip), 0.1f);
    }
    private void Start()
    {
        ForceEquip();
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
    private void ForceEquip()
    {
        string hatToWear = "";

        // 1. ShopManager가 살아있다면 거기서 현재 장착 이름을 가져옴 (가장 정확)
        if (ShopManager.Instance != null && !string.IsNullOrEmpty(ShopManager.Instance.equippedHatName))
        {
            hatToWear = ShopManager.Instance.equippedHatName;
        }
        // 2. 없다면 저장된 데이터(PlayerPrefs)에서 가져옴
        else
        {
            hatToWear = PlayerPrefs.GetString("EquippedHatName", "");
        }

        // 3. 이름이 있다면 장착 시도
        if (!string.IsNullOrEmpty(hatToWear) && ShopManager.Instance != null)
        {
            ShopItemData hatData = ShopManager.Instance.FindHatDataByName(hatToWear);
            if (hatData != null)
            {
                EquipHat(hatData);
                Debug.Log($"[PlayerHat] {hatToWear} 장착 성공 (씬: {gameObject.scene.name})");
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
        if (playerDeath != null)
        {
            // 아이템 데이터에 사망 스프라이트가 있으면 교체, 없으면 기본값 유지
            if (itemData.playerDeathSprite != null)
            {
                playerDeath.SetDeathSprite(itemData.playerDeathSprite);
            }
            else
            {
                // 아이템은 꼈지만 전용 사망 이미지가 없다면 기본 사망 이미지 사용
                playerDeath.ResetDeathSprite();
            }
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
        if (playerDeath != null)
        {
            playerDeath.ResetDeathSprite();
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