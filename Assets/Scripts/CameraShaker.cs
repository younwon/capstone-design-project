using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// 간단 카메라 쉐이크. 같은 오브젝트의 transform.localPosition에
/// '오프셋'만 더해주므로, 다른 카메라 추적 스크립트와 함께 써도 안전.
public class CameraShaker : MonoBehaviour
{
    [Header("Shake Shape")]
    public float amplitude = 0.15f;   // 최대 흔들림 크기(유닛)
    public float frequency = 18f;     // 흔들림 속도(Hz)

    [Header("Trauma (one-shot)")]
    public float traumaDecay = 1.2f;  // 초당 감소량

    [Header("Continuous (외부에서 연속 제어)")]
    [Range(0f, 1f)] public float external = 0f; // 0~1, 외부가 넣는 상시 세기

    float trauma;            // 0~1
    Vector3 currentOffset;   // 이번 프레임에 더해진 오프셋

    public void AddTrauma(float t) => trauma = Mathf.Clamp01(trauma + t);
    public void SetExternal(float v) => external = Mathf.Clamp01(v);

    void LateUpdate()
    {
        // 다른 추적 스크립트가 먼저 위치를 정한 뒤, 오프셋만 '추가'한다.
        transform.localPosition -= currentOffset;

        float t = Mathf.Clamp01(trauma);
        // 제곱해서 작은 값은 더 작게, 큰 값은 급격히 커지게
        float traumaShake = t * t;
        float shake = (traumaShake + external) * amplitude;

        if (shake > 0f)
        {
            float nx = (Mathf.PerlinNoise(Time.time * frequency, 0f) - 0.5f) * 2f;
            float ny = (Mathf.PerlinNoise(0f, Time.time * frequency) - 0.5f) * 2f;
            currentOffset = new Vector3(nx, ny, 0f) * shake;
        }
        else currentOffset = Vector3.zero;

        transform.localPosition += currentOffset;

        // 트라우마는 자연감소
        trauma = Mathf.Clamp01(trauma - traumaDecay * Time.deltaTime);
    }
}

