using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance;
    public List<string> allAchievementIds = new List<string> { /* 다른 업적 ID들 추가 */ };

    // 업적 ID와 달성 여부를 저장하는 딕셔너리
    private Dictionary<string, bool> achievementStatus = new Dictionary<string, bool>();
    private const string ACHIEVEMENT_PREFS_PREFIX = "ACHIEVEMENT_";

    [Header("알림 UI 설정")]
    public GameObject notificationPrefab; // 알림 UI 프리팹 (슬롯)
    public float displayDuration = 3f;  // 알림이 화면에 머무는 시간
    public Transform notificationParent; // 알림이 표시될 부모 오브젝트 (Canvas)

    private Queue<string> achievementQueue = new Queue<string>();
    private bool isDisplaying = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAchievements(); // 게임 시작 시 저장된 업적 불러오기
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void OnDestroy()
    {
        // 구독 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    // 씬이 로드될 때마다 호출되어 Canvas를 새로 찾습니다.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // "NotificationCanvas"는 실제 씬에 있는 Canvas의 이름이어야 합니다.
        GameObject canvasObj = GameObject.Find("NotificationCanvas");
        if (canvasObj != null)
        {
            notificationParent = canvasObj.transform;
            Debug.Log("[AchievementManager] 씬에서 Canvas를 찾았습니다.");
        }
        else
        {
            Debug.LogError("[AchievementManager] 씬에서 'NotificationCanvas'라는 이름의 Canvas를 찾을 수 없습니다!");
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

            achievementQueue.Enqueue(achievementId); // 큐에 추가
            if (!isDisplaying)
            {
                StartCoroutine(ProcessQueue()); // 알림 처리 시작

                // 업적 달성 후 상점 UI를 새로고침해야 할 수 있으므로,
                // 현재 씬이 상점이라면 ShopManager의 버튼 업데이트 함수를 호출
                if (ShopManager.Instance != null)
                {
                    ShopManager.Instance.UpdateAllItemButtons();
                }
            }
        }
        IEnumerator ProcessQueue()
        {
            isDisplaying = true;

            // notificationParent가 찾아질 때까지 잠시 대기 (안전장치)
            while (notificationParent == null)
            {
                Debug.LogWarning("[AchievementManager] 알림을 띄울 Canvas를 기다리는 중...");
                yield return null;
            }

            while (achievementQueue.Count > 0)
            {
                string achievementId = achievementQueue.Dequeue();

                // 이제 부모(notificationParent)가 DontDestroyOnLoad가 아니므로 오류가 발생하지 않습니다.
                GameObject notification = Instantiate(notificationPrefab, notificationParent);

                TextMeshProUGUI textComponent = notification.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = $"YOU GOT!\n'{achievementId}'";
                }

                Image iconImage = null;
                Transform iconTransform = notification.transform.Find("ItemIcon");
                if (iconTransform != null)
                {
                    iconImage = iconTransform.GetComponent<Image>();
                }

                Sprite iconSprite = null;

                // ShopManager가 준비될 때까지 대기
                if (ShopManager.Instance != null)
                {
                    if (ShopManager.Instance.shopItems != null)
                    {
                        foreach (var item in ShopManager.Instance.shopItems)
                        {
                            if (item.requiredAchievementId == achievementId)
                            {
                                iconSprite = item.itemIcon;
                                break;
                            }
                        }
                    }
                }

                if (iconImage != null && iconSprite != null)
                {
                    iconImage.sprite = iconSprite;
                    iconImage.gameObject.SetActive(true);
                }
                else if (iconImage != null)
                {
                    iconImage.gameObject.SetActive(false);
                }

                yield return new WaitForSeconds(displayDuration);
                Destroy(notification);
                yield return new WaitForSeconds(0.2f);
            }

            isDisplaying = false;
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