using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartsUI : MonoBehaviour
{
    [Header("Slots (왼쪽→오른쪽)")]
    public Image[] hearts;           // Heart_1, Heart_2, Heart_3 드래그

    [Header("Sprites (선택)")]
    public Sprite fullHeart;         // Kenney UI Pack의 꽉 찬 하트
    public Sprite emptyHeart;        // (있으면) 빈 하트/아웃라인

    [Header("Colors (빈하트 스프라이트 없을 때)")]
    public Color fullColor = Color.white;
    public Color emptyColor = new Color(1f, 1f, 1f, 0.35f);

    public void Set(int current, int max)
    {
        if (hearts == null) return;

        for (int i = 0; i < hearts.Length; i++)
        {
            bool slotActive = i < max;
            if (!hearts[i]) continue;

            hearts[i].gameObject.SetActive(slotActive);
            if (!slotActive) continue;

            bool filled = i < current;

            if (fullHeart && emptyHeart)
                hearts[i].sprite = filled ? fullHeart : emptyHeart;

            hearts[i].color = filled ? fullColor : emptyColor;
            hearts[i].preserveAspect = true;
        }
    }
}

