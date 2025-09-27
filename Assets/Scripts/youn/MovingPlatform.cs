using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("이동할 거리입니다. 생성된 위치를 기준으로 이 값만큼 움직입니다.")]
    public float moveDistance = 3f;
    public float speed = 2f;

    // 업그레이드된 부분 1: 모든 움직이는 발판이 공유하는 '출생신고 카운터'
    private static int platformCounter = 0;

    private Vector3 startPosition;
    private bool startsMovingRight; // 이 발판이 오른쪽으로 먼저 움직일지 여부

    void Awake()
    {
        // Awake는 Start보다 먼저 실행됩니다.
        // 이 발판이 몇 번째로 생성된 움직이는 발판인지 확인합니다.
        // 카운터가 짝수(0, 2, 4...)면 오른쪽, 홀수(1, 3, 5...)면 왼쪽으로 출발하도록 정합니다.
        startsMovingRight = (platformCounter % 2 == 0);

        // 다음 발판을 위해 카운터를 1 증가시킵니다.
        platformCounter++;
    }

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        float pingPongValue = Mathf.PingPong(Time.time * speed, moveDistance);

        // 왼쪽으로 먼저 움직여야 한다면, 움직임 값을 뒤집어줍니다.
        if (!startsMovingRight)
        {
            pingPongValue = moveDistance - pingPongValue;
        }

        transform.position = new Vector3(startPosition.x + pingPongValue, startPosition.y, startPosition.z);
    }
}
