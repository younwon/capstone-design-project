using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ColorActivationManager : MonoBehaviour
{
    public static ColorActivationManager I;

    // 시작 기본색 (원하면 Green 그대로)
    public BlockColor ActiveColor { get; private set; } = BlockColor.Green;

    // 색이 바뀔 때 알림 이벤트 (나중에 플랫폼이 이걸 구독)
    public event Action<BlockColor> OnColorChanged;

    [Header("Switch")]
    [Tooltip("연타 방지용 쿨다운(초). 너무 낮추면 난이도 급상승")]
    [SerializeField] float switchCooldown = 0.12f;
    float _cd;

    void Awake()
    {
        I = this;   // 어디서든 ColorActivationManager.I 로 접근 가능
    }

    void Update()
    {
        if (_cd > 0f) { _cd -= Time.deltaTime; return; }

        // 숫자키 1~4
        if (Input.GetKeyDown(KeyCode.Alpha1)) Set(BlockColor.Green);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) Set(BlockColor.Yellow);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) Set(BlockColor.Blue);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) Set(BlockColor.Orange);

        // 보조키 Q W E R (선택사항: 손가락 편한 키)
        if (Input.GetKeyDown(KeyCode.Q)) Set(BlockColor.Green);
        else if (Input.GetKeyDown(KeyCode.W)) Set(BlockColor.Yellow);
        else if (Input.GetKeyDown(KeyCode.E)) Set(BlockColor.Blue);
        else if (Input.GetKeyDown(KeyCode.R)) Set(BlockColor.Orange);
    }

    public void Set(BlockColor c)
    {
        if (ActiveColor == c) return;
        ActiveColor = c;
        _cd = switchCooldown;
        OnColorChanged?.Invoke(c);  // 구독자들에게 알림
        // 나중에 디버그 로그가 필요하면 아래 주석 해제
        Debug.Log($"ActiveColor -> {c}");
    }
}

