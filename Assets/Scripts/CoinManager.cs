using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance;

    [Header("State")]
    public int currentCoins = 0;

    // ✅ 기존 필드 유지(호환용) + 내부에선 다중 타깃 리스트로 처리
    [Header("UI (optional)")]
    public TextMeshProUGUI coinText; // 남겨두되, 실제 갱신은 리스트 사용
    readonly List<TextMeshProUGUI> _targets = new List<TextMeshProUGUI>();

    const string COIN_PREFS_KEY = "CURRENT_COINS";
    const string UI_OBJECT_NAME = "CoinText"; // 씬 내 텍스트 오브젝트 이름

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        currentCoins = PlayerPrefs.GetInt(COIN_PREFS_KEY, 0);

        // 씬 로드시 UI 타깃 재수집
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 에디터에서 최초 씬에서도 바로 표시되게 한 번 갱신
        RefreshUiTargets();
        UpdateCoinText();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ===== 씬 진입 시 UI 타깃 재바인딩 =====
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshUiTargets();
        UpdateCoinText();
#if UNITY_EDITOR
        Debug.Log("새로운 씬의 코인 텍스트 오브젝트들을 다시 바인딩했습니다.");
#endif
    }

    // 씬 안의 모든 CoinText(TextMeshProUGUI) 수집 (비활성 포함)
    void RefreshUiTargets()
    {
        _targets.Clear();

        // 1) 이름이 CoinText 인 TextMeshProUGUI 전부(비활성 포함)
        var allTmp = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        foreach (var t in allTmp)
        {
            if (t != null && t.name == UI_OBJECT_NAME)
            {
                _targets.Add(t);
            }
        }

        // 2) 호환용: 인스펙터에 수동 지정한 coinText가 있으면 포함
        if (coinText && !_targets.Contains(coinText))
            _targets.Add(coinText);
    }

    // ===== 코인 조작 =====
    public void AddCoin(int amount)
    {
        currentCoins += amount;
        UpdateCoinText();
        SaveCoins();
    }

    public bool SpendCoin(int amount)
    {
        if (currentCoins >= amount)
        {
            currentCoins -= amount;
            UpdateCoinText();
            SaveCoins();
            return true;
        }
        else
        {
            Debug.LogWarning($"코인이 부족합니다! (현재 코인: {currentCoins})");
            return false;
        }
    }

    public int GetCoinCount() => currentCoins;

    // ===== UI 갱신 =====
    void UpdateCoinText()
    {
        // 리스트가 비어 있으면 한 번 더 탐색(씬 전환 직후 타이밍 케어)
        if (_targets.Count == 0) RefreshUiTargets();

        // 모든 타깃에 동일 텍스트 반영
        for (int i = _targets.Count - 1; i >= 0; i--)
        {
            var t = _targets[i];
            if (!t)
            {
                _targets.RemoveAt(i);
                continue;
            }
            t.text = currentCoins.ToString();
        }
    }

    // ===== 저장 =====
    void SaveCoins()
    {
        PlayerPrefs.SetInt(COIN_PREFS_KEY, currentCoins);
        PlayerPrefs.Save();
#if UNITY_EDITOR
        Debug.Log($"코인 저장 완료. 현재 코인: {currentCoins}");
#endif
    }

    [ContextMenu("Reset All Coins (PlayerPrefs)")]
    public void ClearAllCoins()
    {
        PlayerPrefs.DeleteKey(COIN_PREFS_KEY);
        PlayerPrefs.Save();
        currentCoins = 0;
        UpdateCoinText();
        Debug.Log("[CoinManager] 모든 코인 초기화 완료");
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void LateUpdate()
    {
        if (!Application.isPlaying) return;
        if (Input.GetKeyDown(KeyCode.F9))
            ClearAllCoins();
    }
#endif

    // ===== 선택: 코드로 직접 타깃 등록하고 싶을 때 =====
    public void RegisterTarget(TextMeshProUGUI target)
    {
        if (!target) return;
        if (!_targets.Contains(target)) _targets.Add(target);
        UpdateCoinText();
    }

    public void UnregisterTarget(TextMeshProUGUI target)
    {
        if (!target) return;
        _targets.Remove(target);
    }
}
