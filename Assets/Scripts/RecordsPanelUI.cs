using System.Linq;
using TMPro;
using UnityEngine;

public class RecordsPanelUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] Transform content;          // Window/ScrollView/Viewport/Content
    [SerializeField] GameObject recordSlotPrefab;
    [SerializeField] TMP_Text titleMode;

    [Header("Options")]
    [SerializeField] int maxToShow = 10;

    [Header("Scene Names")]
    [SerializeField] string classicSceneName = "ColorClimb"; // ← 프로젝트 씬명에 맞춰 설정
    [SerializeField] string speedSceneName = "SpeedRun";   // ← 프로젝트 씬명에 맞춰 설정

    GameMode _openedMode = GameMode.Classic;

    public void OpenClassic()
    {
        _openedMode = GameMode.Classic;
        gameObject.SetActive(true);
        if (titleMode) titleMode.text = "RANKING (CLASSIC)";
        Refresh(GameMode.Classic);
    }

    public void OpenSpeed()
    {
        _openedMode = GameMode.SpeedRun;
        gameObject.SetActive(true);
        if (titleMode) titleMode.text = "RANKING (SPEED)";
        Refresh(GameMode.SpeedRun);
    }

    public void Close() => gameObject.SetActive(false);

    void ClearContent()
    {
        if (!content) return;
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }

    void Refresh(GameMode mode)
    {
        ClearContent();

        if (RecordsDB.I == null) return;
        var list = RecordsDB.I.GetTop(mode, Mathf.Max(1, maxToShow));
        if (list == null || list.Count == 0) return;

        int rank = 1;
        foreach (var rec in list)
        {
            var go = Instantiate(recordSlotPrefab, content);
            var ui = go.GetComponent<RecordSlotUI>();
            if (ui)
                ui.Setup(
                    rank,
                    rec.meters,
                    rec.seconds,
                    rec.GetEndedLocalString()
                );
            rank++;
        }
    }

    // ====== 모드 전환 버튼 (하단 버튼) ======
    public void OnClickSwitchMode()
    {
        // 현재 패널이 Classic이면 Speed 씬으로, Speed이면 Classic 씬으로
        var targetMode = (_openedMode == GameMode.Classic) ? GameMode.SpeedRun : GameMode.Classic;
        var targetScene = (targetMode == GameMode.Classic) ? classicSceneName : speedSceneName;

        // 다음 씬 진입 시 랭킹 자동 오픈을 요청
        SceneRecordsIntent.Request(targetMode);

        // 타이틀 스킵 방지(도착 씬에서 타이틀이 떠 있어야 그 위에 랭킹이 열림)
        StartGate.SkipTitleOnce = false;

        // 페이드 + 씬 로드
        FadeRouter.Load(targetScene, skipTitleOnce: false);
    }
}









