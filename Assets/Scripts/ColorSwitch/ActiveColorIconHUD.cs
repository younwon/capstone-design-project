using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActiveColorIconHUD : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Image fillImage; // HUD_ColorFill를 드래그해 넣기

    [Header("옵션 A: 색별 스프라이트 교체(에셋 4개)")]
    public Sprite spriteGreen;
    public Sprite spriteYellow;
    public Sprite spriteBlue;
    public Sprite spriteOrange;

    [Header("옵션 B: 단일 스프라이트 + 색상 틴트(에셋 1개)")]
    public bool useTint = true; // B방식(틴트) 기본. A방식 쓰면 false로.
    public Color tintGreen = new Color(0.35f, 0.9f, 0.45f);
    public Color tintYellow = new Color(0.98f, 0.9f, 0.30f);
    public Color tintBlue = new Color(0.35f, 0.6f, 1.0f);
    public Color tintOrange = new Color(1.0f, 0.55f, 0.2f);

    BlockColor _last;        // 마지막으로 적용한 색
    bool _hasInit;           // 초기화 여부

    void OnEnable()
    {
        TrySubscribe();
        ForceApplyNow();     // 시작 시 1회 강제 적용
    }

    void OnDisable()
    {
        if (ColorActivationManager.I != null)
            ColorActivationManager.I.OnColorChanged -= Apply;
        _hasInit = false;
    }

    void Update()
    {
        // ✅ 안전판: 이벤트가 못받아도 매 프레임 확인해서 바뀌면 적용
        var mgr = ColorActivationManager.I;
        if (!mgr || !fillImage) return;

        if (!_hasInit || mgr.ActiveColor != _last)
            Apply(mgr.ActiveColor);
    }

    void TrySubscribe()
    {
        var mgr = ColorActivationManager.I;
        if (mgr != null)
        {
            // 중복 구독 방지 위해 먼저 떼고 다시 건다
            mgr.OnColorChanged -= Apply;
            mgr.OnColorChanged += Apply;
        }
    }

    void ForceApplyNow()
    {
        var mgr = ColorActivationManager.I;
        if (mgr != null) Apply(mgr.ActiveColor);
    }

    void Apply(BlockColor c)
    {
        if (!fillImage) return;

        if (useTint)
        {
            fillImage.color = c switch
            {
                BlockColor.Green => tintGreen,
                BlockColor.Yellow => tintYellow,
                BlockColor.Blue => tintBlue,
                BlockColor.Orange => tintOrange,
                _ => Color.white
            };
        }
        else
        {
            fillImage.color = Color.white; // 스프라이트 색 왜곡 방지
            fillImage.sprite = c switch
            {
                BlockColor.Green => spriteGreen,
                BlockColor.Yellow => spriteYellow,
                BlockColor.Blue => spriteBlue,
                BlockColor.Orange => spriteOrange,
                _ => fillImage.sprite
            };
        }

        _last = c;
        _hasInit = true;
        // Debug.Log($"[HUD] ActiveColor = {c}");
    }
}


