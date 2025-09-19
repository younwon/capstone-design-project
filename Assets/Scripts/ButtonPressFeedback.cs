using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Targets")]
    public RectTransform scaleTarget;          // 비워두면 자신
    public TextMeshProUGUI label;              // 눌릴 때 색 바꿀 텍스트(선택)

    [Header("Press Style")]
    [Range(0.8f, 1f)] public float pressedScale = 0.94f;
    public float tweenTime = 0.1f;             // 0.08~0.12 추천
    public Color pressedTextColor = new Color(0.85f, 0.85f, 0.85f, 1f); // 살짝 어둡게

    Vector3 _scaleNormal = Vector3.one;
    Color _labelNormalColor = Color.white;
    bool _tweening;

    void Awake()
    {
        if (!scaleTarget) scaleTarget = transform as RectTransform;
        if (label) _labelNormalColor = label.color;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(TweenScaleAndColor(_scaleNormal, Vector3.one * pressedScale,
                                          label ? _labelNormalColor : (Color)default,
                                          label ? pressedTextColor : (Color)default));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ReturnToNormal();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 누른 채로 밖으로 나가도 복귀
        ReturnToNormal();
    }

    void ReturnToNormal()
    {
        StopAllCoroutines();
        StartCoroutine(TweenScaleAndColor(scaleTarget.localScale, _scaleNormal,
                                          label ? label.color : (Color)default,
                                          label ? _labelNormalColor : (Color)default));
    }

    System.Collections.IEnumerator TweenScaleAndColor(Vector3 fromS, Vector3 toS, Color fromC, Color toC)
    {
        float t = 0f;
        while (t < tweenTime)
        {
            t += Time.unscaledDeltaTime;                       // 게임오버 일시정지 대비
            float k = Mathf.Clamp01(t / tweenTime);
            scaleTarget.localScale = Vector3.Lerp(fromS, toS, k);
            if (label) label.color = Color.Lerp(fromC, toC, k);
            yield return null;
        }
        scaleTarget.localScale = toS;
        if (label) label.color = toC;
    }

    void OnDisable()
    {
        // 비활성화되면 원래대로 복구
        if (scaleTarget) scaleTarget.localScale = _scaleNormal;
        if (label) label.color = _labelNormalColor;
    }
}

