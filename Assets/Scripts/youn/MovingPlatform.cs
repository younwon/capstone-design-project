using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    // 움직일 방향을 선택할 수 있는 타입을 새로 정의합니다.
    public enum MoveDirection { Horizontal, Vertical }

    [Header("Movement Settings")]
    [Tooltip("움직일 방향을 선택하세요.")]
    public MoveDirection direction = MoveDirection.Horizontal; // 방향 선택 옵션

    [Tooltip("수평(Horizontal)으로 움직일 거리입니다.")]
    public float moveDistanceX = 4f; // X축 이동 거리

    [Tooltip("수직(Vertical)으로 움직일 거리입니다.")]
    public float moveDistanceY = 3f; // Y축 이동 거리

    public float speed = 2f;

    // [핵심 기능] 이 발판의 현재 속도를 다른 스크립트가 읽어갈 수 있도록 합니다.
    public Vector2 CurrentVelocity { get; private set; }

    private Vector3 startPosition;
    private Vector3 lastPosition;
    private static int platformCounter = 0;
    private bool startsMovingMainDirection; // 주 방향으로 먼저 움직일지 여부

    void Awake()
    {
        startsMovingMainDirection = (platformCounter % 2 == 0);
        platformCounter++;
    }

    void Start()
    {
        startPosition = transform.position;
        lastPosition = transform.position;
    }

    void FixedUpdate()
    {
        // 선택된 방향에 따라 다른 로직을 실행합니다.
        float distance = (direction == MoveDirection.Horizontal) ? moveDistanceX : moveDistanceY;

        float pingPongValue = Mathf.PingPong(Time.time * speed, distance);
        if (!startsMovingMainDirection)
        {
            pingPongValue = distance - pingPongValue;
        }

        Vector3 newPosition;
        if (direction == MoveDirection.Horizontal)
        {
            newPosition = new Vector3(startPosition.x + pingPongValue, startPosition.y, startPosition.z);
        }
        else
        {
            newPosition = new Vector3(startPosition.x, startPosition.y + pingPongValue, startPosition.z);
        }

        transform.position = newPosition;

        // [핵심 기능] 이전 프레임 위치와 현재 프레임 위치의 차이를 이용해 '속도'를 계산합니다.
        CurrentVelocity = (transform.position - lastPosition) / Time.fixedDeltaTime;
        lastPosition = transform.position;
    }
}
