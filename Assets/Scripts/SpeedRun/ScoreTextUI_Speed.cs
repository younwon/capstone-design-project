using UnityEngine;
using TMPro;
using System.Globalization;

public class ScoreTextUI_Speed : MonoBehaviour
{
    [Header("Refs")]
    public ScoreSystem_Speed score;           // 스피드런 전용
    public TextMeshProUGUI text;

    [Header("Format")]
    [Range(0.5f, 1.5f)] public float mScale = 0.85f;
    public bool useThousandsSeparator = false;

    int lastShown = -1;

    void Reset() { text = GetComponent<TextMeshProUGUI>(); }

    void LateUpdate()
    {
        if (!score || !text) return;

        int cur = Mathf.Max(0, score.DisplayScore);  // ★ 표시값 사용 (프리즈됨)
        if (cur == lastShown) return;

        string number = useThousandsSeparator
            ? cur.ToString("#,0", CultureInfo.InvariantCulture)
            : cur.ToString();

        text.richText = true;
        text.text = $"{number} <size={Mathf.RoundToInt(text.fontSize * mScale)}>M</size>";
        lastShown = cur;
    }
}
