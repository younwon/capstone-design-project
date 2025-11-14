using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AspectRatioFixer : MonoBehaviour
{
    // 원하는 비율 (9:16)
    public float targetAspectWidth = 9.0f;
    public float targetAspectHeight = 16.0f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        UpdateAspectRatio();
    }

    // 창 크기 조절 시에도 대응
    void Update()
    {
        UpdateAspectRatio();
    }

    public void UpdateAspectRatio()
    {
        float targetRatio = targetAspectWidth / targetAspectHeight;
        float currentRatio = (float)Screen.width / (float)Screen.height;
        float scaleHeight = currentRatio / targetRatio;

        Rect rect = new Rect(0, 0, 1, 1);

        if (scaleHeight < 1.0f) // 레터박스 (위아래 검은 바)
        {
            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;
        }
        else // 필러박스 (좌우 검은 바) - 지금 필요한 것!
        {
            float scaleWidth = 1.0f / scaleHeight;
            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;
        }

        cam.rect = rect; // 메인 카메라의 렌더링 영역을 계산된 사각형으로 강제함
    }
}