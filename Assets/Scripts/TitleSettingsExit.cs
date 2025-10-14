using UnityEngine;

public class TitleSettingsExit : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GameObject settingsPanel;      // 이미 만들어둔 Settings 패널
    [SerializeField] CanvasGroup startCatcherGroup; // TitleUIController에 있던 StartCatcherGroup (없으면 비워도 OK)
    [SerializeField] GameObject exitPopup;          // (선택) 종료 확인 팝업 (없으면 비워도 OK)

    // --- Settings ---
    public void OpenSettings()
    {
        // 타이틀에서 '탭해서 시작'을 막기 위해 레이캐스트 잠시 OFF
        if (startCatcherGroup) startCatcherGroup.blocksRaycasts = false;

        if (settingsPanel) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel) settingsPanel.SetActive(false);

        // 다시 '탭해서 시작' 받도록 ON
        if (startCatcherGroup) startCatcherGroup.blocksRaycasts = true;
    }

    // --- Exit ---
    public void OpenExitPopup()
    {
        // 팝업이 있으면 열고, 없으면 바로 종료 처리
        if (exitPopup)
        {
            if (startCatcherGroup) startCatcherGroup.blocksRaycasts = false;
            exitPopup.SetActive(true);
        }
        else
        {
            QuitGame();
        }
    }

    public void CancelExit()
    {
        if (exitPopup) exitPopup.SetActive(false);
        if (startCatcherGroup) startCatcherGroup.blocksRaycasts = true;
    }

    public void ConfirmExit()
    {
        QuitGame();
    }

    // 실제 종료
    void QuitGame()
    {
#if UNITY_EDITOR
        // 에디터에선 게임 모드만 종료
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

