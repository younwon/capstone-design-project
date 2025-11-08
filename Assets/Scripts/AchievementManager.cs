using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance;

    [Header("Achievement IDs")]
    public List<string> allAchievementIds = new List<string>();

    // 업적 달성 상태
    private readonly Dictionary<string, bool> achievementStatus = new Dictionary<string, bool>();
    private const string ACHIEVEMENT_PREFS_PREFIX = "ACHIEVEMENT_";

    [Header("Notification (Prefab mode)")]
    public GameObject notificationPrefab;      // 프리팹 알림 방식
    public Transform notificationParent;       // 알림 부모(Canvas)
    public float displayDuration = 3f;         // 표시 시간

    [Header("Notification (Inline UI mode)")]
    [Tooltip("AchievementNotificationUI 컴포넌트를 직접 등록하면, 프리팹 대신 이 UI로 표시합니다.")]
    public AchievementNotificationUI inlineUI; // 등록식 알림 UI(옵션)

    private readonly Queue<string> achievementQueue = new Queue<string>();
    private bool isDisplaying = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAchievements();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 씬 로드 시, 프리팹 모드에서 쓸 부모 Canvas 자동 획득
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (notificationParent == null) // 이미 다른 씬에서 설정돼 있으면 유지
        {
            GameObject canvasObj = GameObject.Find("NotificationCanvas");
            if (canvasObj != null)
            {
                notificationParent = canvasObj.transform;
                Debug.Log("[AchievementManager] Found NotificationCanvas.");
            }
            else
            {
                Debug.LogWarning("[AchievementManager] 'NotificationCanvas' not found in scene.");
            }
        }
    }

    // [ADD] AchievementNotificationUI가 자신을 등록할 때 호출
    public void RegisterNotificationUI(AchievementNotificationUI ui)
    {
        inlineUI = ui;
        if (inlineUI && inlineUI.gameObject.activeSelf)
            inlineUI.gameObject.SetActive(false); // 시작 시 숨김
        Debug.Log("[AchievementManager] Notification UI registered.");
    }

    // 업적 해제 여부 조회(조건 ID 비었으면 true)
    public bool IsAchievementUnlocked(string achievementId)
    {
        if (string.IsNullOrEmpty(achievementId)) return true;
        return achievementStatus.ContainsKey(achievementId) && achievementStatus[achievementId];
    }

    // 업적 달성 처리
    public void UnlockAchievement(string achievementId)
    {
        if (IsAchievementUnlocked(achievementId)) return;

        achievementStatus[achievementId] = true;
        SaveAchievement(achievementId);
        Debug.Log($"[Achievement] Unlocked: {achievementId}");

        achievementQueue.Enqueue(achievementId);
        if (!isDisplaying) StartCoroutine(ProcessQueue());

        // 상점 버튼 새로 고침(있을 때)
        if (ShopManager.Instance != null)
            ShopManager.Instance.UpdateAllItemButtons();

        // 로컬 함수: 큐 처리 코루틴
        IEnumerator ProcessQueue()
        {
            isDisplaying = true;

            // 표시 준비가 될 때까지 대기
            while (inlineUI == null && notificationParent == null && notificationPrefab == null)
            {
                Debug.LogWarning("[AchievementManager] Waiting for UI (inlineUI or notificationParent/prefab)...");
                yield return null;
            }

            while (achievementQueue.Count > 0)
            {
                string id = achievementQueue.Dequeue();

                // 아이콘(선택) 탐색: ShopManager에서 해당 업적에 연결된 아이템 아이콘을 찾아본다
                Sprite iconSprite = null;
                if (ShopManager.Instance != null && ShopManager.Instance.shopItems != null)
                {
                    foreach (var item in ShopManager.Instance.shopItems)
                    {
                        if (item != null && item.requiredAchievementId == id)
                        {
                            iconSprite = item.itemIcon;
                            break;
                        }
                    }
                }

                // 1) Inline UI가 등록되어 있으면 그것으로 표시
                if (inlineUI != null)
                {
                    inlineUI.Show($"YOU GOT!\n'{id}'", iconSprite);
                    yield return new WaitForSeconds(displayDuration);
                }
                // 2) 프리팹 모드로 표시
                else if (notificationPrefab != null && notificationParent != null)
                {
                    GameObject go = Instantiate(notificationPrefab, notificationParent);

                    // 텍스트
                    var text = go.GetComponentInChildren<TextMeshProUGUI>();
                    if (text) text.text = $"YOU GOT!\n'{id}'";

                    // 아이콘
                    Image iconImage = null;
                    var t = go.transform.Find("ItemIcon");
                    if (t) iconImage = t.GetComponent<Image>();

                    if (iconImage != null)
                    {
                        if (iconSprite != null)
                        {
                            iconImage.sprite = iconSprite;
                            iconImage.gameObject.SetActive(true);
                        }
                        else iconImage.gameObject.SetActive(false);
                    }

                    yield return new WaitForSeconds(displayDuration);
                    Destroy(go);
                    yield return new WaitForSeconds(0.2f);
                }
                else
                {
                    Debug.LogWarning($"[AchievementManager] No UI to display achievement '{id}'.");
                }
            }

            isDisplaying = false;
        }
    }

    // 저장/로드
    void SaveAchievement(string achievementId)
    {
        PlayerPrefs.SetInt(ACHIEVEMENT_PREFS_PREFIX + achievementId, 1);
        PlayerPrefs.Save();
    }

    void LoadAchievements()
    {
        // 필요 시 allAchievementIds를 돌며 PlayerPrefs 값을 채우는 로직로 확장 가능
        // 여기서는 간단히 시작 상태를 초기화만 합니다.
        achievementStatus.Clear();
        foreach (var id in allAchievementIds)
        {
            if (PlayerPrefs.GetInt(ACHIEVEMENT_PREFS_PREFIX + id, 0) == 1)
                achievementStatus[id] = true;
        }
    }

    [ContextMenu("모든 업적 초기화")]
    public void ClearAllAchievements()
    {
        foreach (string id in allAchievementIds)
            PlayerPrefs.DeleteKey(ACHIEVEMENT_PREFS_PREFIX + id);

        achievementStatus.Clear();
        PlayerPrefs.Save();

        Debug.Log("[AchievementManager] Cleared all achievements.");

        if (ShopManager.Instance != null)
            ShopManager.Instance.UpdateAllItemButtons();
    }
}
