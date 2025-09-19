using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreRunMaxUI : MonoBehaviour
{
    public ScoreSystem score;
    public TextMeshProUGUI text;
    [Range(0.5f, 1.5f)] public float mScale = 0.85f;  // 'M' »ìÂ¦ ÀÛ°Ô

    int lastShown = -1;

    void Reset()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    void LateUpdate()
    {
        if (!score || !text) return;

        int runMax = Mathf.Max(0, score.SessionMaxScore);
        if (runMax == lastShown) return;

        text.richText = true;
        text.text = $"MAX {runMax} <size={Mathf.RoundToInt(text.fontSize * mScale)}>M</size>";

        lastShown = runMax;
    }
}

