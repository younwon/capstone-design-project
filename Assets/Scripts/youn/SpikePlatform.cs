using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 이 스크립트가 상태를 감지하려면, 같은 오브젝트에 Collider2D가 반드시 필요합니다.
[RequireComponent(typeof(Collider2D))]
public class SpikePlatform : MonoBehaviour
{
    [Header("오브젝트 연결")]
    public GameObject spikeChildObject;

    [Header("움직임 설정")]
    [Tooltip("기존 Y위치에서 얼마나 위로 튀어나올지 결정합니다.")]
    public float riseAmount = 0.3f;
    [Tooltip("가시가 움직이는 속도")]
    public float moveSpeed = 5f;

    [Header("타이밍 설정")]
    [Tooltip("발판 활성화 후, 가시가 처음 올라오기까지의 딜레이")]
    public float initialDelay = 1.0f;
    [Tooltip("가시가 튀어나와 있는 시간")]
    public float activeDuration = 1.0f;
    [Tooltip("가시가 들어간 후, 다시 나오기까지의 대기 시간")]
    public float inactiveDuration = 2.0f;

    private Collider2D parentCollider;
    private bool lastState;
    private Vector3 hiddenPos;
    private Vector3 shownPos;
    private Coroutine runningCycle = null;
    private Collider2D[] spikeColliders;

    void Awake()
    {
        parentCollider = GetComponent<Collider2D>();
        if (spikeChildObject != null)
        {
            Vector3 initialPos = spikeChildObject.transform.localPosition;
            hiddenPos = initialPos;
            shownPos = new Vector3(initialPos.x, initialPos.y + riseAmount, initialPos.z);

            spikeColliders = spikeChildObject.GetComponentsInChildren<Collider2D>();

            // [수정] 게임 시작 시, 자식 오브젝트를 확실히 꺼진 상태(안 보이는 상태)로 초기화합니다.
            spikeChildObject.SetActive(false);
        }
        lastState = false;
    }

    // [수정] 보여주셨던 초기 코드의 핵심 로직을 여기에 통합했습니다.
    void Update()
    {
        bool isParentColliderActive = parentCollider.enabled;

        // 부모 발판의 활성화/비활성화 상태가 변경될 때만 작동합니다.
        if (isParentColliderActive != lastState)
        {
            // 부모의 상태와 똑같이 자식 오브젝트 전체를 켜거나 끕니다.
            if (spikeChildObject != null)
            {
                spikeChildObject.SetActive(isParentColliderActive);
            }

            // 만약 '활성화'된 것이라면, 반복 애니메이션 사이클을 시작합니다.
            if (isParentColliderActive)
            {
                // 안전을 위해 이전 코루틴을 멈추고 새로 시작
                if (runningCycle != null) StopCoroutine(runningCycle);
                runningCycle = StartCoroutine(RepeatingSpikeCycle());
            }

            lastState = isParentColliderActive;
        }
    }

    private IEnumerator RepeatingSpikeCycle()
    {
        // 시작 시 가시를 숨겨진 위치에 두고, 콜라이더를 끕니다.
        spikeChildObject.transform.localPosition = hiddenPos;
        SetSpikeColliders(false);

        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            yield return StartCoroutine(AnimateSpike(true)); // 올라갈 때 Collider 켜기
            yield return new WaitForSeconds(activeDuration);
            yield return StartCoroutine(AnimateSpike(false)); // 내려갈 때 Collider 끄기
            yield return new WaitForSeconds(inactiveDuration);
        }
    }

    private IEnumerator AnimateSpike(bool show)
    {
        if (spikeChildObject == null) yield break;

        // 애니메이션 시작/끝에 Collider만 제어합니다 (SetActive는 Update에서 담당)
        SetSpikeColliders(show);

        Vector3 startPos = spikeChildObject.transform.localPosition;
        Vector3 endPos = show ? shownPos : hiddenPos;

        float journey = 0f;
        float duration = Vector3.Distance(startPos, endPos) / moveSpeed;
        if (duration <= 0) duration = 0.1f;

        while (journey < duration)
        {
            journey += Time.deltaTime;
            spikeChildObject.transform.localPosition = Vector3.Lerp(startPos, endPos, journey / duration);
            yield return null;
        }

        spikeChildObject.transform.localPosition = endPos;
    }

    private void SetSpikeColliders(bool enabled)
    {
        if (spikeColliders == null) return;
        foreach (var col in spikeColliders)
        {
            col.enabled = enabled;
        }
    }
}