using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Globalization;

public class ScoreTextUI : MonoBehaviour
{
    [Header("Refs")]
    public ScoreSystem score;                 // GameSystems의 ScoreSystem
    public TextMeshProUGUI text;              // ScoreText (이 컴포넌트)

    [Header("Format")]
    [Tooltip("‘M’의 상대 크기(1=같음, 0.8=작게)")]
    [Range(0.5f, 1.5f)] public float mScale = 0.85f;
    public bool useThousandsSeparator = false;

    int lastShown = -1;

    void Reset()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    void LateUpdate()
    {
        if (!score || !text) return;

        int cur = Mathf.Max(0, score.CurrentScore);
        if (cur == lastShown) return;

        string number = useThousandsSeparator
            ? cur.ToString("#,0", CultureInfo.InvariantCulture)
            : cur.ToString();

        // 리치 텍스트로 ‘M’만 살짝 작게
        text.richText = true;
        text.text = $"{number} <size={Mathf.RoundToInt(text.fontSize * mScale)}>M</size>";

        lastShown = cur;
    }
}
