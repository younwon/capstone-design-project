// SceneRecordsIntent.cs
public static class SceneRecordsIntent
{
    // 다음 씬 진입 시 랭킹을 자동으로 열어야 하는가?
    public static bool OpenOnLoad = false;

    // 다음 씬에서 열 모드(그 씬의 랭킹 패널이 열릴 때 어떤 모드로 열지)
    public static GameMode TargetMode = GameMode.Classic;

    public static void Request(GameMode mode)
    {
        OpenOnLoad = true;
        TargetMode = mode;
    }

    // 사용 후 반드시 false로 내려 재발동 방지
    public static void Consume() => OpenOnLoad = false;
}

