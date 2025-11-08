using UnityEngine;
using UnityEngine.EventSystems;

public class TitleStartToInGame : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("시작 시 끌 타이틀 패널(전체 패널 추천)")]
    public GameObject titlePanelToHide;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (StartGate.GameStarted) return;               // 이미 시작 상태면 무시

        StartGate.EnableGameplay();                       // 게임플레이 허용(타임스케일 1)
        if (BgmController.I) BgmController.I.PlayInGame();// 인게임 BGM
        if (titlePanelToHide) titlePanelToHide.SetActive(false);
    }
}
