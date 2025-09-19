using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowVertical : MonoBehaviour
{
    public Transform target;          // Player
    public float topOffset = 2.0f;    // 플레이어 머리 위로 얼마나 여유
    public float deadZoneHalf = 1.5f; // 카메라가 움직이지 않는 세로 여유(절반 높이)
    public float smoothTime = 0.2f;   // 따라오는 부드러움

    float yVel;
    float baseX, baseZ;

    void Awake()
    {
        baseX = transform.position.x; // 가로 고정
        baseZ = transform.position.z; // -10 고정
    }

    void LateUpdate()
    {
        if (!target) return;

        // 플레이어 기준 ‘관심 지점’ (머리 위로 약간)
        float focusY = target.position.y + topOffset;

        // 현재 카메라 중심 주변의 소프트존(움직이지 않는 구간)
        float camY = transform.position.y;
        float zoneTop = camY + deadZoneHalf;
        float zoneBot = camY - deadZoneHalf;

        // 관심 지점이 존을 벗어났을 때만 목표 위치 변경
        float desiredY = camY;
        if (focusY > zoneTop) desiredY = focusY - deadZoneHalf; // 위로 벗어남
        else if (focusY < zoneBot) desiredY = focusY + deadZoneHalf; // 아래로 벗어남

        // 부드럽게 이동 (위/아래 모두)
        float newY = Mathf.SmoothDamp(camY, desiredY, ref yVel, smoothTime);
        transform.position = new Vector3(baseX, newY, baseZ);
    }
}


