// /Scripts/System/SceneTracker.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTracker : MonoBehaviour
{
    public static SceneTracker Instance;

    [Header("Options")]
    [SerializeField] bool log = true;

    // 외부에서 읽기만 가능
    public string PreviousSceneName { get; private set; } = "";

    void Awake()
    {
        if (Instance != this && Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        // 씬 전환될 때(prev -> next) prev.name을 자동으로 기록
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    void OnActiveSceneChanged(Scene prev, Scene next)
    {
        if (prev.IsValid())
        {
            PreviousSceneName = prev.name;
            if (log) Debug.Log($"[SceneTracker] 자동 기록: 이전 씬 = {PreviousSceneName} → 다음 씬 = {next.name}");
        }
    }

    /// <summary>
    /// (기존 메서드) 외부에서 수동으로 이전 씬 이름을 지정하고 싶을 때 호출.
    /// </summary>
    public void SetPreviousScene(string sceneName)
    {
        PreviousSceneName = sceneName;
        if (log) Debug.Log($"[SceneTracker] 이전 씬 이름 저장(수동): {sceneName}");
    }

    // ---------- 편의 메서드 (선택 사용) ----------

    /// <summary>
    /// 현재 활성 씬을 '이전 씬'으로 기록한 뒤 target 씬으로 전환.
    /// FadeRouter가 있으면 페이드로, 없으면 즉시 로드.
    /// </summary>
    public void LoadSceneRemember(string target, bool useFade = true, bool skipTitleOnce = false)
    {
        var cur = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(cur)) PreviousSceneName = cur;
        if (log) Debug.Log($"[SceneTracker] LoadSceneRemember → prev='{PreviousSceneName}', target='{target}'");

        if (useFade && FadeRouter.I != null) FadeRouter.Load(target, skipTitleOnce);
        else SceneManager.LoadScene(target);
    }

    /// <summary>
    /// 기록된 이전 씬으로 돌아감. 없으면 fallback으로 이동.
    /// FadeRouter가 있으면 페이드 사용.
    /// </summary>
    public void GoBackOr(string fallbackScene, bool useFade = true)
    {
        string target = string.IsNullOrEmpty(PreviousSceneName) ? fallbackScene : PreviousSceneName;
        if (log) Debug.Log($"[SceneTracker] GoBackOr → '{target}'로 이동 (fallback='{fallbackScene}')");

        if (useFade && FadeRouter.I != null) FadeRouter.Load(target);
        else SceneManager.LoadScene(target);
    }
}
