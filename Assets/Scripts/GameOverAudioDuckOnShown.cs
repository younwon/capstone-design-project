using UnityEngine;
using UnityEngine.SceneManagement;

[AddComponentMenu("Audio/GameOver Audio Duck (listen GameOverUI.OnShown)")]
public class GameOverAudioDuckOnShown : MonoBehaviour
{
    [Header("Options")]
    [Tooltip("씬 넘어갈 때(재시작/홈) 즉시 원복")]
    public bool forceRestoreOnSceneLoaded = true;

    void OnEnable()
    {
        // GameOver 패널이 '표시'될 때 호출되는 이벤트에 연결
        GameOverUI.OnShown += HandleGameOverShown;

        // 씬 전환 시(재시작/홈) 덕 상태가 남지 않도록 안전 복구
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        GameOverUI.OnShown -= HandleGameOverShown;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void HandleGameOverShown()
    {
        // 게임오버 표시 시 먹먹한 효과 적용
        if (PauseAudioDucker.I != null)
            PauseAudioDucker.I.ToPaused();   // unscaled로 부드럽게 전환
    }

    void OnSceneLoaded(Scene sc, LoadSceneMode mode)
    {
        if (!forceRestoreOnSceneLoaded) return;
        if (PauseAudioDucker.I != null)
            PauseAudioDucker.I.ForceNormalNow(); // 재시작/타이틀 진입 직후 즉시 원복
    }
}
