using System.Collections;
using UnityEngine;

public class RecordsLauncherOnLoad : MonoBehaviour
{
    private void Start()
    {
        // 타이틀이 뜬 상태에서 랭킹을 바로 띄우려면 1~2프레임 양보가 안정적
        StartCoroutine(Co_LaunchIfRequested());
    }

    IEnumerator Co_LaunchIfRequested()
    {
        // 혹시 페이드/초기화 동작을 위해 한 프레임 대기
        yield return null;

        if (!SceneRecordsIntent.OpenOnLoad) yield break;

        // 혹시 이전에 타이틀 스킵이 켜져 있으면 끄기(안전)
        StartGate.SkipTitleOnce = false;

        // 타이틀 보장(네 프로젝트에서 TitleUIController가 있다면 찾아서 타이틀을 확실히 켜줌)
        var title = FindObjectOfType<TitleUIController>(true);
        if (title != null)
        {
            // 타이틀이 비활성이면 켜기
            title.gameObject.SetActive(true);
            // 필요 시 타이틀 표시 함수가 있으면 호출
            // title.ReturnToTitle(); // 프로젝트에서 이미 타이틀 복귀 기능이 안정적이라면 풀어도 됨
        }

        // 랭킹 패널 열기
        var panel = FindObjectOfType<RecordsPanelUI>(true);
        if (panel != null)
        {
            if (SceneRecordsIntent.TargetMode == GameMode.Classic) panel.OpenClassic();
            else panel.OpenSpeed();
        }

        SceneRecordsIntent.Consume();
    }
}

