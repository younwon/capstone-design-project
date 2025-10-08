// StartGate.cs
using UnityEngine;

public static class StartGate
{
    /// 게임이 실제 진행 중인지(타이틀 대기 중이면 false)
    public static bool GameStarted { get; private set; } = false;

    /// 재시작 시 타이틀을 한 번 건너뛰기 위한 플래그(필요 시 사용)
    public static bool SkipTitleOnce { get; set; } = false;

    public static void EnableGameplay()
    {
        Time.timeScale = 1f;
        GameStarted = true;
    }

    public static void DisableGameplay()
    {
        Time.timeScale = 0f;
        GameStarted = false;
    }
}

