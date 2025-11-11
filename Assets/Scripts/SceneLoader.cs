// /Scripts/System/SceneLoader.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("Scenes")]
    [Tooltip("버튼으로 고를 수 있는 대상 씬 이름들")]
    public string[] scenesToLoad;

    [Header("Navigation")]
    [Tooltip("이전 씬 기록이 없을 때 돌아갈 기본 씬")]
    public string fallbackScene = "SpeedRun";
    [Tooltip("상점 씬 이름(선택)")]
    public string shopScene = "Shop";

    [Header("Effects")]
    public bool useFade = true;
    public bool skipTitleOnceOnLoad = false;

    // ---------- Public API (버튼에 연결) ----------

    // 인덱스로 씬 이동 (현재 씬을 이전씬으로 자동 저장)
    public void LoadSceneByIndex(int index)
    {
        if (scenesToLoad == null || index < 0 || index >= scenesToLoad.Length)
        {
            Debug.LogError("[SceneLoader] 잘못된 씬 인덱스입니다.");
            return;
        }
        string target = scenesToLoad[index];
        LoadRemember(target);
    }

    // 상점으로 이동 (현재 씬을 이전씬으로 자동 저장)
    public void GoToShop()
    {
        if (string.IsNullOrEmpty(shopScene))
        {
            Debug.LogError("[SceneLoader] shopScene 이름이 비었습니다.");
            return;
        }
        LoadRemember(shopScene);
    }

    // 이전 씬으로 돌아가기 (없으면 fallback)
    public void GoBack()
    {
        if (SceneTracker.Instance != null)
        {
            SceneTracker.Instance.GoBackOr(string.IsNullOrEmpty(fallbackScene)
                ? SceneManager.GetActiveScene().name
                : fallbackScene, useFade);
            return;
        }

        // SceneTracker가 없는 비상 경로
        string target = string.IsNullOrEmpty(fallbackScene)
            ? SceneManager.GetActiveScene().name
            : fallbackScene;
        LoadDirect(target);
    }

    // ---------- 내부 유틸 ----------

    // 현재 씬을 이전씬으로 기록하고 target 로드
    void LoadRemember(string target)
    {
        if (SceneTracker.Instance != null)
        {
            SceneTracker.Instance.LoadSceneRemember(target, useFade, skipTitleOnceOnLoad);
        }
        else
        {
            // SceneTracker가 없으면 현재 씬을 로그로만 남기고 바로 로드
            Debug.LogWarning("[SceneLoader] SceneTracker가 없어 바로 로드합니다.");
            LoadDirect(target);
        }
    }

    // FadeRouter가 있으면 페이드, 없으면 즉시 로드
    void LoadDirect(string target)
    {
        if (useFade && FadeRouter.I != null)
        {
            FadeRouter.Load(target, skipTitleOnceOnLoad);
        }
        else
        {
            SceneManager.LoadScene(target);
        }
    }
}
