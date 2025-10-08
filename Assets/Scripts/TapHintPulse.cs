using UnityEngine;
using TMPro;

/// Tap 힌트 텍스트를 부드럽게 깜빡이고, 살짝 커졌다가 원래대로 돌아오게 만든다.
[RequireComponent(typeof(TMP_Text))]
public class TapHintPulse : MonoBehaviour
{
    [Header("Alpha Blink")]
    [SerializeField] float blinkSpeed = 2.8f;      // 깜빡임 속도
    [SerializeField] float minAlpha = 0.35f;       // 최소 투명도
    [SerializeField] float maxAlpha = 1.0f;        // 최대 투명도

    [Header("Scale Pulse")]
    [SerializeField] float scaleAmount = 1.06f;    // 최대 스케일 배수(살짝 커짐)
    [SerializeField] float scaleSpeed = 1.8f;      // 커졌다 작아지는 속도

    TMP_Text _txt;
    Color _baseColor;
    Vector3 _baseScale;

    void Awake()
    {
        _txt = GetComponent<TMP_Text>();
        _baseColor = _txt.color;
        _baseScale = transform.localScale;
    }

    void OnEnable()
    {
        // 켜질 때 항상 초기화
        _txt.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, maxAlpha);
        transform.localScale = _baseScale;
    }

    void Update()
    {
        // 시간 멈춤 상태에서도 보이게 unscaledTime 사용
        float t = Time.unscaledTime;

        // 1) 알파 깜빡임 (sin 기반)
        float a = (Mathf.Sin(t * blinkSpeed) + 1f) * 0.5f;     // 0~1
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, a);
        _txt.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, alpha);

        // 2) 스케일 펄스
        float s = (Mathf.Sin(t * scaleSpeed) + 1f) * 0.5f;     // 0~1
        float k = Mathf.Lerp(1f, scaleAmount, s);
        transform.localScale = _baseScale * k;
    }
}

