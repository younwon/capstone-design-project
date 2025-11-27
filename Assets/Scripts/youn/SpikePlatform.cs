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
    public float activeDuration = 0.5f;
    [Tooltip("가시가 들어간 후, 다시 나오기까지의 대기 시간")]
    public float inactiveDuration = 3.0f;

    // [추가] 랜덤 시간차 설정
    [Header("랜덤 설정")]
    [Tooltip("체크하면 가시가 나오는 시작 타이밍을 랜덤하게 섞습니다.")]
    public bool randomizeStartTime = true;
    [Tooltip("추가될 랜덤 시간의 최대값 (초). 예: 1.0이면 0~1.0초 사이의 시간이 딜레이에 추가됨")]
    public float randomDelayMax = 1.0f;

    [Header("SFX")]
    [Tooltip("가시가 올라올 때 재생할 소리")]
    public AudioClip spikeUpSfx;
    [Tooltip("가시가 내려갈 때 재생할 소리")]
    public AudioClip spikeDownSfx;
    [Tooltip("카메라와 이 거리 이내에 있을 때만 SFX를 재생합니다.")]
    public float sfxMaxDistance = 15f;

    private Collider2D parentCollider;
    private bool lastState;
    private Vector3 hiddenPos;
    private Vector3 shownPos;
    private Coroutine runningCycle = null;
    private Collider2D[] spikeColliders;

    // 카메라 위치 캐시 (SFX 거리 제한용)
    private Transform camTransform;

    void Awake()
    {
        parentCollider = GetComponent<Collider2D>();

        // 카메라 캐시
        if (Camera.main != null)
            camTransform = Camera.main.transform;

        if (spikeChildObject != null)
        {
            Vector3 initialPos = spikeChildObject.transform.localPosition;
            hiddenPos = initialPos;
            shownPos = new Vector3(initialPos.x, initialPos.y + riseAmount, initialPos.z);

            spikeColliders = spikeChildObject.GetComponentsInChildren<Collider2D>();

            // 게임 시작 시, 자식 오브젝트를 확실히 꺼진 상태(안 보이는 상태)로 초기화
            spikeChildObject.SetActive(false);
        }
        lastState = false;
    }

    // 보여주셨던 초기 코드의 핵심 로직을 여기에 통합
    void Update()
    {
        bool isParentColliderActive = parentCollider.enabled;

        // 부모 발판의 활성화/비활성화 상태가 변경될 때만 작동
        if (isParentColliderActive != lastState)
        {
            // 부모의 상태와 똑같이 자식 오브젝트 전체를 켜거나 끕니다.
            if (spikeChildObject != null)
            {
                spikeChildObject.SetActive(isParentColliderActive);
            }

            // 활성화되면 반복 애니메이션 사이클 시작
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

        // 랜덤 딜레이 적용
        float startDelay = initialDelay;
        if (randomizeStartTime)
        {
            startDelay += Random.Range(0f, randomDelayMax);
        }

        yield return new WaitForSeconds(startDelay);

        while (true)
        {
            // 올라오기 (위험, 콜라이더 활성화)
            yield return StartCoroutine(AnimateSpike(true));
            yield return new WaitForSeconds(activeDuration);

            // 내려가기 (안전, 콜라이더 비활성화)
            yield return StartCoroutine(AnimateSpike(false));
            yield return new WaitForSeconds(inactiveDuration);
        }
    }

    private IEnumerator AnimateSpike(bool show)
    {
        if (spikeChildObject == null) yield break;

        // 애니메이션 시작/끝에 Collider만 제어
        SetSpikeColliders(show);

        // ★ 여기서 SFX 한 번만 재생
        PlaySpikeMoveSfx(show);

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

    // === SFX 재생 로직 (올라갈 때/내려갈 때 한 번씩) ===
    void PlaySpikeMoveSfx(bool goingUp)
    {
        if (AudioManager.I == null) return;

        // 색이 비활성화된 상태면 소리 안 남
        if (parentCollider != null && !parentCollider.enabled)
            return;

        // 카메라와 너무 멀면 소리 안 남 (화면 아래 멀리 있을 때)
        if (camTransform != null)
        {
            float dy = Mathf.Abs(transform.position.y - camTransform.position.y);
            if (dy > sfxMaxDistance) return;
        }

        AudioClip clip = goingUp ? spikeUpSfx : spikeDownSfx;
        if (clip == null) return;

        // 전역 SFX라서 AudioManager 통해 재생 → SFX 볼륨/설정창에 따라감
        AudioManager.I.PlaySFX(clip);
    }
}
