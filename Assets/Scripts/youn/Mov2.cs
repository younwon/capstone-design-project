using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mov2 : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("수평 이동일 경우 true, 수직 이동일 경우 false")]
    public bool moveHorizontally = true;

    [Tooltip("이동할 월드 좌표 최소값")]
    public float minPosition = -3f;

    [Tooltip("이동할 월드 좌표 최대값")]
    public float maxPosition = 3f;

    [Tooltip("이동 속도")]
    public float speed = 0.5f;

    // 현재 프레임의 속도 (다른 스크립트 참조용)
    public Vector2 CurrentVelocity { get; private set; }

    private Vector3 startPosition;
    private Vector3 lastPosition;

    // 각 플랫폼마다 다른 위상(phase)을 주기 위한 값 (0..1 범위)
    private float phaseOffset;

    void Start()
    {
        startPosition = transform.position;
        lastPosition = transform.position;

        // 생성 위치를 min~max 구간 안으로 사영(클램프)
        if (moveHorizontally)
        {
            float projectedX = Mathf.Clamp(startPosition.x, minPosition, maxPosition);
            // min->max 사이에서 0..1로 맵핑
            phaseOffset = Mathf.InverseLerp(minPosition, maxPosition, projectedX);
        }
        else
        {
            float projectedY = Mathf.Clamp(startPosition.y, minPosition, maxPosition);
            phaseOffset = Mathf.InverseLerp(minPosition, maxPosition, projectedY);
        }
        // phaseOffset은 0..1 범위. 이것을 Time에 더해서 각 플랫폼 시작 위상을 달리함.
    }

    void FixedUpdate()
    {
        // 위상(phase)을 추가해서 플랫폼마다 다른 시작 위치 갖게 함
        float t = Mathf.PingPong(Time.time * speed + phaseOffset, 1f);

        Vector3 newPosition = transform.position;

        if (moveHorizontally)
        {
            float newX = Mathf.Lerp(minPosition, maxPosition, t);
            // Y,Z는 생성 시점의 위치(또는 현재 Y) 유지 — 생성 위치를 시작위치로 유지하려면 startPosition.y 사용
            newPosition = new Vector3(newX, startPosition.y, transform.position.z);
        }
        else
        {
            float newY = Mathf.Lerp(minPosition, maxPosition, t);
            newPosition = new Vector3(transform.position.x, newY, transform.position.z);
        }

        transform.position = newPosition;

        // 속도 계산
        CurrentVelocity = (transform.position - lastPosition) / Time.fixedDeltaTime;
        lastPosition = transform.position;
    }
}