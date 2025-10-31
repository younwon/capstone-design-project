using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecordSlotUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] TMP_Text txtM;
    [SerializeField] TMP_Text txtDate;
    [SerializeField] TMP_Text txtTime;
    [SerializeField] Image badge;      // 왼쪽 아이콘
    [SerializeField] TMP_Text rankNum; // 4위부터 숫자

    [Header("Optional Sprites")]
    [SerializeField] Sprite goldSprite;   // 1위
    [SerializeField] Sprite silverSprite; // 2위
    [SerializeField] Sprite bronzeSprite; // 3위

    public void Setup(int rank, int meters, float seconds, string localTimestamp)
    {
        if (txtM) txtM.text = $"{meters} M";
        if (txtDate) txtDate.text = localTimestamp;       // "yy.MM.dd  HH:mm" 형태(공백 두 칸)
        if (txtTime) txtTime.text = $"({ScoreSystem.FormatMMSS(seconds)})";

        bool top3 = rank <= 3;

        if (badge)
        {
            badge.enabled = true;
            if (top3 && (goldSprite || silverSprite || bronzeSprite))
            {
                badge.color = Color.white;
                badge.sprite = rank == 1 ? goldSprite
                              : rank == 2 ? silverSprite
                              : bronzeSprite;
            }
            else
            {
                // 탑3 아니면 아이콘 흐리게 or 완전 숨김
                badge.color = top3 ? Color.white : new Color(1, 1, 1, 0.0f);
            }
        }

        if (rankNum)
        {
            rankNum.gameObject.SetActive(!top3);
            if (!top3) rankNum.text = rank.ToString();
        }
    }
}



