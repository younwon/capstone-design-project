using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneHistory : MonoBehaviour
{
    public static SceneHistory I { get; private set; }

    [Tooltip("이전 씬이 비어있을 때 사용할 기본 씬(예: Title)")]
    public string fallbackScene = "Title";

    public string LastSceneName { get; private set; } = "";

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject); // 루트 오브젝트에 붙어 있어야 경고 없음
    }

    /// 현재 씬을 기록하고 target으로 이동 (페이드 라우터가 있으면 사용)
    public void RememberAndLoad(string target, bool useFade = true)
    {
        LastSceneName = SceneManager.GetActiveScene().name;
        if (useFade && FadeRouter.I) FadeRouter.Load(target);
        else SceneManager.LoadScene(target);
    }

    /// 이전 씬이 있으면 그리로, 없으면 fallback으로
    public void LoadPreviousOrFallback(bool useFade = true)
    {
        var dest = string.IsNullOrEmpty(LastSceneName) ? fallbackScene : LastSceneName;
        if (useFade && FadeRouter.I) FadeRouter.Load(dest);
        else SceneManager.LoadScene(dest);
    }
}
