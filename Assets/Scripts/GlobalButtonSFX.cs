// Assets/Scripts/Audio/GlobalButtonSFX.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전체의 모든 Button에 기본 클릭 SFX를 자동으로 바인딩.
/// - UIButtonSFX(특수 버튼용)가 붙은 버튼은 자동 바인딩에서 제외.
/// - 씬 로드/프리팹 생성 등으로 나중에 생기는 버튼도 주기적으로 재바인딩.
/// - TitleUIController에서 GlobalButtonSFX.I.Refresh() 호출 가능.
/// </summary>
public class GlobalButtonSFX : MonoBehaviour
{
    public static GlobalButtonSFX I { get; private set; }

    [Header("Default Click SFX")]
    [SerializeField] private AudioClip defaultClickClip;

    [Header("Options")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Tooltip("런타임 중 새로 생긴 버튼을 다시 탐색하는 주기(초). 0이면 재탐색 안 함")]
    [SerializeField] private float rebindInterval = 0.5f;

    // 이미 바인딩한 버튼 캐시
    private readonly HashSet<Button> boundButtons = new HashSet<Button>();

    // 코루틴 핸들
    private Coroutine rebindCo;

    void Awake()
    {
        if (I && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        // 씬 로드 될 때마다 한번씩 리바인딩
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        Refresh();

        if (rebindInterval > 0f)
            rebindCo = StartCoroutine(RebindRoutine());
    }

    void OnDestroy()
    {
        if (I == this) I = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (rebindCo != null)
            StopCoroutine(rebindCo);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬 바뀌면 캐시 비우고 새로 바인딩
        boundButtons.Clear();
        Refresh();
    }

    IEnumerator RebindRoutine()
    {
        var wait = new WaitForSecondsRealtime(rebindInterval);
        while (true)
        {
            Refresh();
            yield return wait;
        }
    }

    /// <summary>
    /// 현재 로드된 씬들에 존재하는 모든 버튼을 찾아 기본 클릭음을 바인딩.
    /// (이미 바인딩된 버튼은 스킵)
    /// </summary>
    public void Refresh()
    {
        // 비활성 포함 전부 찾기(프리팹 에셋도 섞여서 나오므로 필터 필요)
        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();

        foreach (var b in buttons)
        {
            if (!b) continue;

            // 로드된 씬 오브젝트만 통과(프리팹/에셋 제외)
            var scn = b.gameObject.scene;
            if (!scn.IsValid() || !scn.isLoaded) continue;

            // 이미 바인딩한 버튼이면 스킵
            if (boundButtons.Contains(b)) continue;

            // 특수 버튼(UIButtonSFX 붙음)은 기본 클릭음 자동 바인딩 제외
            if (b.GetComponent<UIButtonSFX>() != null) continue;

            // 리스너 추가
            boundButtons.Add(b);
            b.onClick.AddListener(() =>
            {
                if (AudioManager.I && defaultClickClip)
                    AudioManager.I.PlaySFX(defaultClickClip);
            });
        }
    }
}


