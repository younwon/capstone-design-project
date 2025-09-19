using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class GameOverUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] CanvasGroup cg;
    [SerializeField] Button btnRestart;
    [SerializeField] Button btnTitleX;

    // 라벨(제목)들 – 씬에 이미 써둔 텍스트라면 건드리지 않아도 됨
    [SerializeField] TextMeshProUGUI bestText;       // "- BEST HEIGHT -"
    [SerializeField] TextMeshProUGUI thisText;       // "- THIS HEIGHT -"

    // 숫자(커다란 값) 전용
    [SerializeField] TextMeshProUGUI bestValueText;  // "000 M"
    [SerializeField] TextMeshProUGUI thisValueText;  // "000 M"

    // 시간 표기
    [SerializeField] TextMeshProUGUI bestTimeText;   // "time : 00m 00s"
    [SerializeField] TextMeshProUGUI thisTimeText;   // "time : 00m 00s"

    [SerializeField] GameObject newBadge;

    [Header("Optional")]
    [SerializeField] string titleSceneName = "TitleScene";

    [Header("Timing")]
    [SerializeField] float uiDelay = 0.8f;

    [Header("Hide these when shown")]
    [SerializeField] GameObject[] hideWhenShown;

    readonly List<GameObject> hiddenList = new List<GameObject>();
    bool suppressRestoreHUD = false; // 씬 전환/재시작 시 HUD 복구 생략

    void Awake()
    {
        HideImmediate();

        SimpleGameOver.OnGameOver += OnGameOverSimple;
        if (btnRestart) btnRestart.onClick.AddListener(OnClickRestart);
        if (btnTitleX) btnTitleX.onClick.AddListener(OnClickTitle);
    }

    void OnDestroy()
    {
        SimpleGameOver.OnGameOver -= OnGameOverSimple;
        if (btnRestart) btnRestart.onClick.RemoveListener(OnClickRestart);
        if (btnTitleX) btnTitleX.onClick.RemoveListener(OnClickTitle);

        if (!suppressRestoreHUD) RestoreHUD();
    }

    void OnGameOverSimple(string reason)
    {
        // ScoreSystem.CommitBestIfHigher()가 먼저 호출되어 있어야 함
        if (ScoreSystem.I)
        {
            var s = ScoreSystem.I;

            // 값(숫자) 채우기 — 여기만 핵심적으로 바뀜
            if (bestValueText) bestValueText.text = $"{s.BestEverScore:000} M";
            if (thisValueText) thisValueText.text = $"{s.SessionMaxScore:000} M";

            // 시간
            if (bestTimeText) bestTimeText.text = $"time : {ScoreSystem.FormatMMSS(s.BestEverTime)}";
            if (thisTimeText) thisTimeText.text = $"time : {ScoreSystem.FormatMMSS(s.ThisRunTime)}";

            // NEW 배지
            if (newBadge) newBadge.SetActive(s.IsNewRecord);
        }

        StartCoroutine(ShowAfterDelay());
    }

    IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSeconds(uiDelay);
        Show();
    }

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
    }

    void HideImmediate()
    {
        if (!cg) return;
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    void RestoreHUD()
    {
        foreach (var go in hiddenList)
            if (go) go.SetActive(true);
        hiddenList.Clear();
    }

    // ===== Buttons =====
    public void OnClickRestart()
    {
        suppressRestoreHUD = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnClickTitle()
    {
        suppressRestoreHUD = true;
        if (!TryLoadSceneByName(titleSceneName))
        {
            Debug.LogWarning("[GameOverUI] TitleScene 미등록 → 현재 씬 재시작");
            OnClickRestart();
        }
    }

    bool TryLoadSceneByName(string sceneName)
    {
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




