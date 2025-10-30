using UnityEngine;
using System.Collections.Generic;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance;
    public List<string> allAchievementIds = new List<string> { /* 다른 업적 ID들 추가 */ };

    // 업적 ID와 달성 여부를 저장하는 딕셔너리
    private Dictionary<string, bool> achievementStatus = new Dictionary<string, bool>();
    private const string ACHIEVEMENT_PREFS_PREFIX = "ACHIEVEMENT_";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAchievements(); // 게임 시작 시 저장된 업적 불러오기
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 특정 업적을 달성 처리하는 함수
    public void UnlockAchievement(string achievementId)
    {
        if (!IsAchievementUnlocked(achievementId))
        {
            achievementStatus[achievementId] = true;
            SaveAchievement(achievementId);
            Debug.Log($"업적 달성: {achievementId}");

            // 업적 달성 후 상점 UI를 새로고침해야 할 수 있으므로,
            // 현재 씬이 상점이라면 ShopManager의 버튼 업데이트 함수를 호출
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.UpdateAllItemButtons();
            }
        }
    }

    // 특정 업적의 달성 여부를 확인하는 함수
    public bool IsAchievementUnlocked(string achievementId)
    {
        // achievementId가 null이거나 비어있으면, 조건이 없는 것이므로 항상 true 반환
        if (string.IsNullOrEmpty(achievementId))
        {
            return true;
        }

        return achievementStatus.ContainsKey(achievementId) && achievementStatus[achievementId];
    }

    // PlayerPrefs에 특정 업적 상태를 저장
    private void SaveAchievement(string achievementId)
    {
        PlayerPrefs.SetInt(ACHIEVEMENT_PREFS_PREFIX + achievementId, 1);
        PlayerPrefs.Save();
    }

    // PlayerPrefs에서 모든 업적 상태를 불러와 딕셔너리에 저장
    // 이 예제에서는 모든 가능한 업적 ID를 미리 알 수 없으므로,
    // 저장된 값들만 불러옵니다. 실제 게임에서는 모든 업적 목록을 기반으로 로드할 수 있습니다.
    private void LoadAchievements()
    {
        // 이 부분은 게임에 존재하는 모든 업적 ID 리스트를 기반으로 개선할 수 있습니다.
        // 예를 들어, ScriptableObject에 모든 업적 ID를 정의해두고 순회하는 방식입니다.
        // 지금은 단순화를 위해 저장된 키를 기반으로만 로드합니다.
    }


    [ContextMenu("모든 업적 초기화")]
    [ContextMenu("모든 업적 초기화 (PlayerPrefs)")]
    public void ClearAllAchievements()
    {
        // 1. Inspector에 등록된 모든 업적 ID를 순회하며 PlayerPrefs에서 삭제
        foreach (string id in allAchievementIds)
        {
            PlayerPrefs.DeleteKey(ACHIEVEMENT_PREFS_PREFIX + id);
        }

        // 2. 현재 게임 메모리에 로드된 업적 상태도 초기화
        achievementStatus.Clear();

        // 3. 변경 사항을 디스크에 저장
        PlayerPrefs.Save();

        Debug.Log("[AchievementManager] 모든 업적 기록이 초기화되었습니다.");

        // 4. (중요) 상점 UI를 즉시 갱신하여 잠금 상태로 되돌림
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.UpdateAllItemButtons();
        }
    }
}