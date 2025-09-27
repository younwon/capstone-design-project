using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

/// 게임오버 패널 컨트롤러 (씬 전환/타이틀 패널 둘 다 지원)
public class GameOverUI : MonoBehaviour
{
    // ====== 전역 잠금 플래그 ======
    /// 다른 스크립트(예: PauseController)에서 참고해서 입력 무시하도록 사용
    public static bool InputLocked { get; private set; } = false;
    public static bool IsShown { get; private set; } = false;

    [Header("Refs")]
    [SerializeField] CanvasGroup cg;
    [SerializeField] Button btnRestart;
    [SerializeField] Button btnHome;        // 타이틀로(씬 or 패널)

    // 라벨은 씬 배치대로 두고, 값 표기만 연결해 쓰면 됨
    [Header("Values")]
    [SerializeField] TMP_Text thisValueText;  // ex) "200 m"
    [SerializeField] TMP_Text bestValueText;  // ex) "300 m"
    [SerializeField] TMP_Text thisTimeText;   // ex) "00:45"
    [SerializeField] TMP_Text bestTimeText;   // ex) "00:30"
    [SerializeField] GameObject newBadge;     // "NEW RECORD!" 배지(없어도 OK)

    [Header("Where to go Home")]
    [Tooltip("타이틀 UI 패널(같은 씬에서 열기). 지정되면 이것이 우선, 없으면 Scene 로딩 사용.")]
    [SerializeField] GameObject titlePanel;   // Title 패널 루트 (옵션)
    [Tooltip("타이틀 씬 이름 (titlePanel이 없을 때만 사용)")]
    [SerializeField] string titleSceneName = "TitleScene";

    [Header("Timing")]
    [SerializeField, Tooltip("게임오버 후 UI가 뜨기까지의 딜레이(초, 실제 시간 기준)")]
    float uiDelay = 0.8f;

    [Header("Hide these when shown")]
    [SerializeField] GameObject[] hideWhenShown;   // 게임오버 때 숨길 HUD/오브젝트들

    readonly List<GameObject> hiddenList = new List<GameObject>();
    bool suppressRestoreHUD = false; // 홈/재시작 시 HUD 복구 생략

    void Awake()
    {
        HideImmediate();

        SimpleGameOver.OnGameOver += OnGameOverSimple;

        if (btnRestart) btnRestart.onClick.AddListener(OnClickRestart);
        if (btnHome) btnHome.onClick.AddListener(OnClickHome);
    }

    void OnDestroy()
    {
        SimpleGameOver.OnGameOver -= OnGameOverSimple;

        if (btnRestart) btnRestart.onClick.RemoveListener(OnClickRestart);
        if (btnHome) btnHome.onClick.RemoveListener(OnClickHome);

        if (!suppressRestoreHUD) RestoreHUD();

        // 혹시 파괴 시점에 잠금이 남지 않도록
        InputLocked = false;
        IsShown = false;
    }

    // ====== 게임오버 신호 수신 ======
    void OnGameOverSimple(string reason)
    {
        // 1) 점수/시간 먼저 채워두기
        if (ScoreSystem.I)
        {
            var s = ScoreSystem.I;
            if (thisValueText) thisValueText.text = $"{s.SessionMaxScore} m";
            if (bestValueText) bestValueText.text = $"{s.BestEverScore} m";
            if (thisTimeText) thisTimeText.text = ScoreSystem.FormatMMSS(s.ThisRunTime);
            if (bestTimeText) bestTimeText.text = ScoreSystem.FormatMMSS(s.BestEverTime);
            if (newBadge) newBadge.SetActive(s.IsNewRecord);
        }

        // 2) 입력 즉시 잠금 (ESC로 Pause 못 열게)
        InputLocked = true;

        // 3) 실제 시간 기준 딜레이 후 표시 + 그 순간에 게임 정지
        StartCoroutine(ShowAfterDelayRealtime());
    }

    IEnumerator ShowAfterDelayRealtime()
    {
        float t = 0f;
        while (t < uiDelay)
        {
            t += Time.unscaledDeltaTime;   // 실제 시간 기준
            yield return null;
        }
        Show();                 // UI 표시
        Time.timeScale = 0f;    // UI 뜨는 타이밍에 정지
    }

    // ====== 표시/숨김 ======
    void Show()
    {
        // HUD 숨기기
        hiddenList.Clear();
        if (hideWhenShown != null)
        {
            foreach (var go in hideWhenShown)
            {
                if (go && go.activeSelf)
                {
                    go.SetActive(false);
                    hiddenList.Add(go);
                }
            }
        }

        if (!cg) return;
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        IsShown = true;
    }

    void HideImmediate()
    {
        if (!cg) return;
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        IsShown = false;
    }

    void RestoreHUD()
    {
        foreach (var go in hiddenList)
            if (go) go.SetActive(true);
        hiddenList.Clear();
    }

    // ====== 버튼 ======
    public void OnClickRestart()
    {
        // 재시작: 시간/입력 잠금 해제 → 씬 리로드
        Time.timeScale = 1f;
        InputLocked = false;
        suppressRestoreHUD = true; // HUD는 씬 리로드로 정리
        var s = SceneManager.GetActiveScene();
        SceneManager.LoadScene(s.buildIndex);
    }

    public void OnClickHome()
    {
        // 홈: 시간/입력 잠금 해제 후
        Time.timeScale = 1f;
        InputLocked = false;
        suppressRestoreHUD = true; // 타이틀로 가면 HUD는 그대로 숨긴 상태 유지

        // 1) 같은 씬의 타이틀 패널 사용
        if (titlePanel)
        {
            // 자신은 닫고 타이틀 패널 오픈
            HideImmediate();
            if (!titlePanel.activeSelf) titlePanel.SetActive(true);
            return;
        }

        // 2) 타이틀 씬 전환
        if (!TryLoadSceneByName(titleSceneName))
        {
            // 타이틀 씬이 빌드세팅에 없으면 안전하게 재시작
            Debug.LogWarning("[GameOverUI] TitleScene 미등록 → 현재 씬 재시작");
            var s = SceneManager.GetActiveScene();
            SceneManager.LoadScene(s.buildIndex);
        }
    }

    bool TryLoadSceneByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = Path.GetFileNameWithoutExtension(path);
            if (name == sceneName)
            {
                SceneManager.LoadScene(sceneName);
                return true;
            }
        }
        return false;
    }
}



