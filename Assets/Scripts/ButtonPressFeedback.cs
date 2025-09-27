using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Targets")]
    public RectTransform scaleTarget;          // 비우면 자신
    public TextMeshProUGUI label;              // 눌릴 때 색 바꿀 텍스트(선택)
    public Image spriteTarget;                 // 스프라이트 바꿀 대상(선택, 비우면 GetComponent<Image>())

    [Header("Press Style")]
    [Range(0.8f, 1f)] public float pressedScale = 0.94f;
    public float tweenTime = 0.1f;             // 0.08~0.12 추천
    public Color pressedTextColor = new Color(0.85f, 0.85f, 0.85f, 1f);

    [Header("Sprite Swap (Optional)")]
    public Sprite pressedSprite;               // 지정하면 다운 시 스왑
    private Sprite _normalSprite;

    Vector3 _scaleNormal;
    Color _labelNormalColor = Color.white;
    Coroutine _tweenCo;

    void Awake()
    {
        if (!scaleTarget) scaleTarget = transform as RectTransform;
        if (!spriteTarget) spriteTarget = GetComponent<Image>();

        _scaleNormal = scaleTarget ? scaleTarget.localScale : Vector3.one;
        if (label) _labelNormalColor = label.color;
        if (spriteTarget) _normalSprite = spriteTarget.sprite;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        StartTween(_scaleNormal, _scaleNormal * pressedScale,
                   label ? label.color : default,
                   label ? pressedTextColor : default);

        // 스프라이트 스왑
        if (spriteTarget && pressedSprite) spriteTarget.sprite = pressedSprite;
    }

    public void OnPointerUp(PointerEventData eventData) => ReturnToNormal();
    public void OnPointerExit(PointerEventData eventData) => ReturnToNormal();

    void ReturnToNormal()
    {
        StartTween(scaleTarget ? scaleTarget.localScale : Vector3.one, _scaleNormal,
                   label ? label.color : default,
                   label ? _labelNormalColor : default);

        if (spriteTarget && pressedSprite && _normalSprite)
            spriteTarget.sprite = _normalSprite;
    }

    void StartTween(Vector3 fromS, Vector3 toS, Color fromC, Color toC)
    {
        if (_tweenCo != null) StopCoroutine(_tweenCo);
        _tweenCo = StartCoroutine(TweenScaleAndColor(fromS, toS, fromC, toC));
    }

    System.Collections.IEnumerator TweenScaleAndColor(Vector3 fromS, Vector3 toS, Color fromC, Color toC)
    {
        float t = 0f;
        while (t < tweenTime)
        {
            t += Time.unscaledDeltaTime; // 일시정지 중에도 동작
            float k = tweenTime <= 0f ? 1f : Mathf.Clamp01(t / tweenTime);

            if (scaleTarget) scaleTarget.localScale = Vector3.Lerp(fromS, toS, k);
            if (label) label.color = Color.Lerp(fromC, toC, k);
            yield return null;
        }
        if (scaleTarget) scaleTarget.localScale = toS;
        if (label) label.color = toC;
        _tweenCo = null;
    }

    void OnDisable()
    {
        // 비활성화되면 원래대로 복구
        if (scaleTarget) scaleTarget.localScale = _scaleNormal;
        if (label) label.color = _labelNormalColor;
        if (spriteTarget && pressedSprite && _normalSprite) spriteTarget.sprite = _normalSprite;
    }
}
