using UnityEngine;
using System.Collections.Generic;

public class AchievementManager : MonoBehaviour
{
    private static AchievementManager _instance;

    public static AchievementManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // 씬에서 인스턴스를 찾습니다.
                _instance = FindObjectOfType<AchievementManager>();

                if (_instance == null)
                {
                    Debug.LogError("[AchievementManager] 씬에 AchievementManager가 없습니다!");
                }
            }
            return _instance;
        }
    }
    // 이 업적 관리자가 알고 있어야 할 모든 업적 ID 목록
    public List<string> allAchievementIds = new List<string>();

    // 아이콘 검색을 위해 이 매니저가 직접 아이템 DB를 가지고 있어야 합니다.
    [Header("아이템 데이터베이스")]
    public List<ShopItemData> allShopItems;

    // 씬에 있는 UI의 참조를 저장할 변수
    private AchievementNotificationUI notificationUI;

    // 업적 달성 상태 (메모리)
    private Dictionary<string, bool> achievementStatus = new Dictionary<string, bool>();
    private const string ACHIEVEMENT_PREFS_PREFIX = "ACHIEVEMENT_";

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAchievements(); // 게임 시작 시 저장된 업적 불러오기
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 씬에 있는 AchievementNotificationUI가 스스로를 등록하는 함수
    public void RegisterNotificationUI(AchievementNotificationUI ui)
    {
        notificationUI = ui;
        Debug.Log("[AchievementManager] 알림 UI가 성공적으로 등록되었습니다.");
    }

    // 업적 달성 시 호출되는 메인 함수
    public void UnlockAchievement(string achievementId)
    {
        // 이미 달성했다면 중복 실행 방지
        if (IsAchievementUnlocked(achievementId)) return;

        achievementStatus[achievementId] = true;
        SaveAchievement(achievementId);
        Debug.Log($"업적 달성: {achievementId}");

        // --- 알림 UI 호출 로직 ---

        // 1. 텍스트 준비
        string notificationText = $"YOU GOT!\n'{achievementId}'";

        // 2. 아이콘 검색
        Sprite iconSprite = null;
        if (allShopItems != null)
        {
            foreach (var item in allShopItems)
            {
                if (item.requiredAchievementId == achievementId)
                {
                    iconSprite = item.itemIcon;
                    break;
                }
            }
        }

        // 3. UI 띄우기
        if (notificationUI != null)
        {
            // 등록된 UI의 Show 함수를 호출
            notificationUI.Show(notificationText, iconSprite);
        }
        else
        {
            Debug.LogError("[AchievementManager] 알림 UI가 등록되지 않았습니다! 씬에 AchievementNotificationUI가 있는지 확인하세요.");
        }

        // --- 알림 로직 끝 ---

        // 상점 UI 갱신 (상점이 열려있을 경우를 대비)
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.UpdateAllItemButtons();
        }
    }

    // 업적 달성 여부 확인
    public bool IsAchievementUnlocked(string achievementId)
    {
        if (string.IsNullOrEmpty(achievementId))
        {
            return true; // ID가 없으면 항상 해금 상태
        }
        return achievementStatus.ContainsKey(achievementId) && achievementStatus[achievementId];
    }

    // 업적 저장
    private void SaveAchievement(string achievementId)
    {
        PlayerPrefs.SetInt(ACHIEVEMENT_PREFS_PREFIX + achievementId, 1);
        PlayerPrefs.Save();
    }

    // 게임 시작 시 업적 로드
    private void LoadAchievements()
    {
        achievementStatus.Clear();
        foreach (string id in allAchievementIds)
        {
            if (PlayerPrefs.GetInt(ACHIEVEMENT_PREFS_PREFIX + id, 0) == 1)
            {
                achievementStatus[id] = true;
            }
            else
            {
                achievementStatus[id] = false;
            }
        }
    }

    // 업적 초기화 (디버그용)
    [ContextMenu("모든 업적 초기화 (PlayerPrefs)")]
    public void ClearAllAchievements()
    {
        foreach (string id in allAchievementIds)
        {
            PlayerPrefs.DeleteKey(ACHIEVEMENT_PREFS_PREFIX + id);
        }
        PlayerPrefs.Save();
        achievementStatus.Clear();
        LoadAchievements(); // 딕셔너리도 초기화

        Debug.Log("[AchievementManager] 모든 업적 기록이 초기화되었습니다.");

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.UpdateAllItemButtons();
        }
    }
}