using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameMode
{
    Classic = 0,   // 끝less(기존)
    SpeedRun = 1   // 60초 모드
}

[Serializable]
public class RecordEntry
{
    // 저장되는 값들
    public long endedAtTicks;   // 게임오버 시각(UTC Ticks)
    public int meters;          // 기록 (m)
    public float seconds;       // 소요 시간(초)

    // ---- 헬퍼(표시용) ----
    // 게임오버 “현지 시각” 문자열: yy.MM.dd-HH:mm
    public string GetEndedLocalString()
    {
        var dtLocal = new DateTime(endedAtTicks, DateTimeKind.Utc).ToLocalTime();
        return dtLocal.ToString("yy.MM.dd-HH:mm");
    }

    // 시간 포맷: MM:SS (혹은 SS.ss 원하면 변경)
    public string GetTimeMMSS()
    {
        int s = Mathf.FloorToInt(Mathf.Max(0f, seconds));
        int mm = s / 60;
        int ss = s % 60;
        return $"{mm:00}:{ss:00}";
    }
}

// JsonUtility는 루트 List를 못 다루므로 래퍼가 필요
[Serializable]
public class RecordsRoot
{
    public List<RecordEntry> classic = new();
    public List<RecordEntry> speed = new();
}
