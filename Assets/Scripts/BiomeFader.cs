using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BiomeFader : MonoBehaviour
{
    public Transform cam;

    [Header("Heights (world Y)")]
    public float skyToSpaceStart = 72f;  // 이 높이부터 우주가 보이기 시작
    public float skyToSpaceEnd = 86f;  // 이 높이에서 우주 완전 보임

    [Header("Renderers")]
    public Renderer[] skyRenderers;      // (선택) Sky 레이어 스프라이트가 있다면 넣기
    public Renderer[] spaceRenderers;    // SpaceLayer의 SpriteRenderer 넣기

    void LateUpdate()
    {
        if (!cam) cam = Camera.main ? Camera.main.transform : null;
        if (!cam) return;

        float y = cam.position.y;

        // 0 → 1로 보간: y가 높아질수록 우주가 서서히 나타남
        float aSpace = Mathf.InverseLerp(skyToSpaceStart, skyToSpaceEnd, y);
        float aSky = 1f - aSpace;

        SetAlpha(skyRenderers, Mathf.Clamp01(aSky));
        SetAlpha(spaceRenderers, Mathf.Clamp01(aSpace));
    }

    void SetAlpha(Renderer[] rs, float a)
    {
        if (rs == null) return;
        for (int i = 0; i < rs.Length; i++)
        {
            var r = rs[i];
            if (!r) continue;

            // Sprites/Default 재질이면 런타임에 안전하게 색 알파만 바꿔도 OK
            var c = r.material.color;
            c.a = a;
            r.material.color = c;
        }
    }
}

