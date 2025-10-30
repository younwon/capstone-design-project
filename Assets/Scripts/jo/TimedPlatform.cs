using System.Collections;
using UnityEngine;

public class TimedPlatform : MonoBehaviour
{
    [Header("Platform Settings")]
    public float activeDuration = 10f;
    public float blinkDuration = 5f;
    public float blinkInterval = 0.3f;

    private Renderer platformRenderer;
    private Collider2D platformCollider;
    private Coroutine activeCoroutine;

    void Awake()
    {
        platformRenderer = GetComponent<Renderer>();
        platformCollider = GetComponent<Collider2D>();

        // 처음에는 꺼진 상태로 시작
        SetPlatformActive(false);
    }

    public void ActivatePlatform()
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }

        activeCoroutine = StartCoroutine(ActivatePlatformRoutine());
    }

    private IEnumerator ActivatePlatformRoutine()
    {
        SetPlatformActive(true);

        // 깜빡이기 전까지 대기
        yield return new WaitForSeconds(activeDuration - blinkDuration);

        // 깜빡이는 구간
        float blinkTime = 0f;
        while (blinkTime < blinkDuration)
        {
            platformRenderer.enabled = !platformRenderer.enabled;
            yield return new WaitForSeconds(blinkInterval);
            blinkTime += blinkInterval;
        }

        SetPlatformActive(false);
        activeCoroutine = null;

        // 완전히 꺼질 때 오브젝트 제거 (선택 사항)
        Destroy(gameObject);
    }

    private void SetPlatformActive(bool isActive)
    {
        if (platformRenderer != null)
            platformRenderer.enabled = isActive;

        if (platformCollider != null)
            platformCollider.enabled = isActive;
    }
}