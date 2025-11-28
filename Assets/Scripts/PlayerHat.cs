using UnityEngine;

public class PlayerHat : MonoBehaviour
{
    // 싱글톤
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

    [Header("▼ 인스펙터에서 직접 연결하세요 (중요!) ▼")]
    // 1. 캐릭터 본체 (PlayerSprite 오브젝트)의 Animator를 여기에 연결
    public Animator targetAnimator;

    // 2. 모자가 표시될 위치 (HatImage 오브젝트)의 SpriteRenderer를 여기에 연결
    public SpriteRenderer hatImageRenderer;

    // ★★★ [추가됨] 사망 스크립트 연결 (해당 씬에 맞는 것만 연결) ★★★
    [Header("사망 스크립트 연결")]
    public PlayerDeath playerDeathColorClimb;       // ColorClimb 씬용
    public PlayerDeath_SpeedRun playerDeathSpeedRun; // SpeedRun 씬용

    [Header("기본 설정")]
    // 아이템 해제 시 돌아갈 기본 애니메이터 (Project 창의 PlayerAC 파일 등)
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

        // 시작할 때 기본 애니메이터가 비어있으면, 현재 연결된 애니메이터를 기본값으로 저장
        if (targetAnimator != null && defaultAnimator == null)
        {
            defaultAnimator = targetAnimator.runtimeAnimatorController;
        }
    }

    // 게임 시작 시 저장된 데이터 불러오기
    private void Start()
    {
        // 1. 저장된 모자 이름 확인
        string savedHatName = PlayerPrefs.GetString("EquippedHatName", "");

        // 2. 저장된 게 있고, 샵 매니저가 있다면 데이터 요청하여 장착
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

        // 1. 애니메이터 교체 (연결된 PlayerSprite의 Animator가 바뀜)
        if (targetAnimator != null && itemData.specificAnimator != null)
        {
            targetAnimator.runtimeAnimatorController = itemData.specificAnimator;
        }

        // 2. 모자 스프라이트 표시
        if (hatImageRenderer != null && itemData.playerAccessorySprite != null)
        {
            hatImageRenderer.sprite = itemData.playerAccessorySprite;
            hatImageRenderer.gameObject.SetActive(true);
        }

        // ★★★ [추가됨] 3. 사망 스프라이트 교체 ★★★
        if (itemData.playerDeathSprite != null)
        {
            // ColorClimb용 스크립트가 있다면 교체
            if (playerDeathColorClimb != null)
                playerDeathColorClimb.SetDeathSprite(itemData.playerDeathSprite);

            // SpeedRun용 스크립트가 있다면 교체
            if (playerDeathSpeedRun != null)
                playerDeathSpeedRun.SetDeathSprite(itemData.playerDeathSprite);
        }
        else
        {
            // 아이템에 데스 스프라이트가 없다면 기본값으로 리셋
            if (playerDeathColorClimb != null) playerDeathColorClimb.ResetDeathSprite();
            if (playerDeathSpeedRun != null) playerDeathSpeedRun.ResetDeathSprite();
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

        // ★★★ [추가됨] 3. 사망 스프라이트 초기화 ★★★
        if (playerDeathColorClimb != null) playerDeathColorClimb.ResetDeathSprite();
        if (playerDeathSpeedRun != null) playerDeathSpeedRun.ResetDeathSprite();
    }

    // 현재 장착 여부 확인 (ShopItemUI에서 사용)
    public bool IsEquipped(ShopItemData itemData)
    {
        if (ShopManager.Instance != null)
        {
            return ShopManager.Instance.equippedHatName == itemData.itemName;
        }
        return false;
    }
}