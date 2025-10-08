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

    void OnEnable()
    {
        if (!targetImage) targetImage = GetComponent<Image>();
        if (logoSprites != null && logoSprites.Length > 0)
        {
            loop = StartCoroutine(CycleLogos_Unscaled());
        }
    }

    void OnDisable()
    {
        if (loop != null) StopCoroutine(loop);
        loop = null;
    }

    public void RestartLoop()
    {
        if (loop != null) StopCoroutine(loop);
        loop = StartCoroutine(CycleLogos_Unscaled());
    }

    IEnumerator CycleLogos_Unscaled()
    {
        int index = 0;
        while (true)
        {
            if (targetImage) targetImage.sprite = logoSprites[index];
            StartCoroutine(FlashEffect_Unscaled());

            yield return new WaitForSecondsRealtime(changeInterval);

            index = (index + 1) % logoSprites.Length;
        }
    }

    IEnumerator FlashEffect_Unscaled()
    {
        Vector3 original = transform.localScale;
        Vector3 target = original * flashScale;
        float half = flashDuration * 0.5f;
        float t = 0f;

        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / half);
            transform.localScale = Vector3.Lerp(original, target, k);
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / half);
            transform.localScale = Vector3.Lerp(target, original, k);
            yield return null;
        }

        transform.localScale = original;
    }
}

