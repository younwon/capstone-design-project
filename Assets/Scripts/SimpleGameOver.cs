using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleGameOver : MonoBehaviour
{
    // 게임오버 알림 이벤트(사유 문자열 포함)
    public static event System.Action<string> OnGameOver;

    // 어디서든 호출
    public static void TryGameOver(string reason = "")
    {
        Debug.Log("[GameOver] " + reason);
        OnGameOver?.Invoke(reason);
    }
}

