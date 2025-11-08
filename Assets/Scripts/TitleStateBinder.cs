// Assets/Scripts/Title/TitleStateBinder.cs
using UnityEngine;

public class TitleStateBinder : MonoBehaviour
{
    [Tooltip("씬 시작 시 타이틀 BGM을 즉시 재생할지")]
    public bool playTitleOnEnable = true;

    void OnEnable()
    {
        // 타이틀 상태 진입
        StartGate.DisableGameplay();                    // 타임스케일 0, GameStarted=false
        if (playTitleOnEnable && BgmController.I)
            BgmController.I.PlayTitle();                // 타이틀 BGM으로 전환
    }
}
