using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LogoAnimator : MonoBehaviour
{
    [Header("Images to Cycle")]
    [SerializeField] Sprite[] logoSprites; // 로고 이미지 4종
    [SerializeField] Image targetImage;    // 바뀔 대상

    [Header("Timing")]
    [SerializeField] float changeInterval = 1.8f; // 이미지 전환 주기(초)
    [SerializeField] float flashScale = 1.1f;     // 번쩍일 때 크기 배수
    [SerializeField] float flashDuration = 0.25f; // 커졌다 작아지는 총 시간(초)

    Coroutine loop;
    Coroutine flashLoop;

    // 항상 돌아와야 하는 “기본 크기”
    Vector3 baseScale;
    bool initializedBaseScale = false;

    void Awake()
    {
        // 시작 시 한 번만 기본 스케일 기억
        baseScale = transform.localScale;
        initializedBaseScale = true;
    }

    void OnEnable()
    {
        if (!targetImage) targetImage = GetComponent<Image>();

        if (!initializedBaseScale)
        {
            baseScale = transform.localScale;
            initializedBaseScale = true;
        }

        // 혹시라도 꼬여 있으면 기본 크기로 되돌려 놓기
        transform.localScale = baseScale;

        if (logoSprites != null && logoSprites.Length > 0)
        {
            loop = StartCoroutine(CycleLogos_Unscaled());
        }
    }

    void OnDisable()
    {
        if (loop != null) StopCoroutine(loop);
        if (flashLoop != null) StopCoroutine(flashLoop);
        loop = null;
        flashLoop = null;

        // 꺼질 때도 항상 원래 크기로 정리
        if (initializedBaseScale)
            transform.localScale = baseScale;
    }

    public void RestartLoop()
    {
        if (loop != null) StopCoroutine(loop);
        if (flashLoop != null) StopCoroutine(flashLoop);

        // 스케일을 확실히 리셋
        if (initializedBaseScale)
            transform.localScale = baseScale;

        loop = StartCoroutine(CycleLogos_Unscaled());
    }

    IEnumerator CycleLogos_Unscaled()
    {
        int index = 0;
        while (true)
        {
            if (targetImage) targetImage.sprite = logoSprites[index];

            // 이전 플래시가 돌고 있으면 끊고 새로 시작
            if (flashLoop != null) StopCoroutine(flashLoop);
            flashLoop = StartCoroutine(FlashEffect_Unscaled());

            yield return new WaitForSecondsRealtime(changeInterval);

            index = (index + 1) % logoSprites.Length;
        }
    }

    IEnumerator FlashEffect_Unscaled()
    {
        // 항상 baseScale 기준으로만 커졌다/작아졌다
        Vector3 start = baseScale;
        Vector3 target = baseScale * flashScale;

        float half = flashDuration * 0.5f;
        float t = 0f;

        // 커지기
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / half);
            transform.localScale = Vector3.Lerp(start, target, k);
            yield return null;
        }

        // 줄어들기
        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / half);
            transform.localScale = Vector3.Lerp(target, start, k);
            yield return null;
        }

        // 마지막에 항상 딱 baseScale로 고정
        transform.localScale = baseScale;
    }
}


