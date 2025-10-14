using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FadeRouter : MonoBehaviour
{
    public static FadeRouter I;

    [Header("Refs")]
    [SerializeField] Image fader;            // 전체 화면 Image (검은색)
    [SerializeField] Canvas canvas;          // 이 오브젝트의 Canvas(Overlay)

    [Header("Timing (unscaled)")]
    [SerializeField] float fadeOutTime = 0.35f;
    [SerializeField] float fadeInTime = 0.35f;

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        if (!canvas) canvas = GetComponentInChildren<Canvas>();
        if (!fader) fader = GetComponentInChildren<Image>();

        // 시작은 투명
        if (fader)
        {
            var c = fader.color; c.a = 0f; fader.color = c;
            fader.raycastTarget = false;
        }
    }

    // --- 외부 호출 ---
    public static void Load(string sceneName, bool skipTitleOnce = false)
    {
        if (skipTitleOnce) StartGate.SkipTitleOnce = true;

        if (I && I.fader) I.StartCoroutine(I.Co_Load(sceneName));
        else SceneManager.LoadScene(sceneName); // 비상 처리
    }

    IEnumerator Co_Load(string sceneName)
    {
        // Fade-Out
        if (fader) fader.raycastTarget = true;
        yield return Co_Fade(1f, fadeOutTime);

        // 실제 로드
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;
        yield return null; // 1프레임 쉬어 UI 안정화

        // Fade-In
        yield return Co_Fade(0f, fadeInTime);
        if (fader) fader.raycastTarget = false;
    }

    IEnumerator Co_Fade(float targetA, float time)
    {
        if (!fader || time <= 0f) yield break;

        float t = 0f;
        Color c = fader.color;
        float startA = c.a;

        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / time);
            c.a = Mathf.Lerp(startA, targetA, k);
            fader.color = c;
            yield return null;
        }

        c.a = targetA; fader.color = c;
    }
}

