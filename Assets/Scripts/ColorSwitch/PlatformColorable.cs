using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public class PlatformColorable : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] BlockColor color = BlockColor.Green; // 이 플랫폼의 고유 색

    [Header("Sprites")]
    [Tooltip("활성 상태에서 쓸 스프라이트")]
    [SerializeField] Sprite activeSprite;
    [Tooltip("비활성 상태에서 쓸 스프라이트")]
    [SerializeField] Sprite inactiveSprite;

    Collider2D col;
    SpriteRenderer sr;
    bool subscribed;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        col.isTrigger = false; // 밟히는 발판이므로 트리거 금지
    }

    // ✅ Start에서 구독: 매니저가 준비된 뒤에 구독/적용
    void Start()
    {
        SubscribeAndApply();
    }

    void OnDisable()
    {
        if (subscribed && ColorActivationManager.I != null)
        {
            ColorActivationManager.I.OnColorChanged -= Apply;
            subscribed = false;
        }
    }

    void SubscribeAndApply()
    {
        var mgr = ColorActivationManager.I;
        if (mgr == null)
        {
            // 매니저가 아직 생성 전이면 다음 프레임에 재시도
            Invoke(nameof(SubscribeAndApply), 0f);
            return;
        }

        mgr.OnColorChanged += Apply;
        subscribed = true;

        // 현재 활성 색 즉시 반영
        Apply(mgr.ActiveColor);
    }

    void Apply(BlockColor active)
    {
        bool on = (active == color);

        // 충돌: 활성 색일 때만 밟힘
        col.enabled = on;

        // 스프라이트 교체
        if (sr != null)
        {
            if (on && activeSprite != null) sr.sprite = activeSprite;
            else if (!on && inactiveSprite != null) sr.sprite = inactiveSprite;
        }
    }

#if UNITY_EDITOR
    // 에디터에서 미리보기용
    void OnValidate()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (!Application.isPlaying && sr != null && activeSprite != null)
            sr.sprite = activeSprite;
    }
#endif
}

