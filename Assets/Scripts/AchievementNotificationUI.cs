using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// 이 스크립트를 1단계에서 만든 'AchievementNotification' 패널에 붙입니다.
public class AchievementNotificationUI : MonoBehaviour
{
    // 1. 인스펙터에서 연결할 UI 요소들
    public Image itemIconImage;
    public TextMeshProUGUI achievementText;
    public float displayDuration = 3f; // 표시 시간

    // 2. 이 UI를 'AchievementManager'에 등록
    private void Awake()
    {
        // 'Instance' 프로퍼티가 호출되면, AchievementManager가 아직 Awake() 전이라도
        // 스스로를 찾아 _instance에 할당하므로 null이 아닙니다.
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.RegisterNotificationUI(this);
        }
        else
        {
            Debug.LogError("[AchievementNotificationUI] AchievementManager 인스턴스를 찾을 수 없습니다.");
        }

        // 시작 시 비활성화
        gameObject.SetActive(false);
    }

    // 3. 알림을 표시하는 공용 함수
    public void Show(string text, Sprite icon)
    {
        // 텍스트와 아이콘 설정
        if (achievementText != null)
        {
            achievementText.text = text;
        }

        if (itemIconImage != null)
        {
            if (icon != null)
            {
                itemIconImage.sprite = icon;
                itemIconImage.gameObject.SetActive(true);
            }
            else
            {
                // 아이콘이 없으면 이미지 숨기기
                itemIconImage.gameObject.SetActive(false);
            }
        }

        // UI 활성화
        gameObject.SetActive(true);

        // 이전에 실행 중이던 숨기기 코루틴이 있다면 중지 (연속 달성 시)
        StopAllCoroutines();
        // N초 후에 자동으로 숨기는 코루틴 시작
        StartCoroutine(HideAfterDelay());
    }

    // 4. N초 후 숨기기
    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        gameObject.SetActive(false);
    }
}