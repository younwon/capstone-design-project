using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform_fixed : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("움직일 방향을 선택하세요.")]
    public bool moveHorizontally = true; // true = X축, false = Y축

    [Tooltip("이동 구간의 최소값 (World 좌표 기준)")]
    public float minPosition = -3f;

    [Tooltip("이동 구간의 최대값 (World 좌표 기준)")]
    public float maxPosition = 3f;

    [Tooltip("이동 속도")]
    public float speed = 1f;

    // [핵심] 다른 스크립트가 읽을 수 있는 현재 속도
    public Vector2 CurrentVelocity { get; private set; }

    private Vector3 lastPosition;
    private bool reverseStart; // 일부 발판이 반대 방향으로 시작하도록

    private static int platformCounter = 0;

    void Awake()
    {
        reverseStart = (platformCounter % 2 == 1);
        platformCounter++;
    }

    void Start()
    {
        lastPosition = transform.position;
    }

    void FixedUpdate()
    {
        float t = Mathf.PingPong(Time.time * speed, 1f); // 0~1 반복
        if (reverseStart) t = 1f - t;

        Vector3 newPosition = transform.position;

        if (moveHorizontally)
        {
            float newX = Mathf.Lerp(minPosition, maxPosition, t);
            newPosition = new Vector3(newX, transform.position.y, transform.position.z);
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
