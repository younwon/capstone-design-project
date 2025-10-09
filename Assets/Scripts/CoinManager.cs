using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance;
    public int currentCoins = 0;
    public TextMeshProUGUI coinText;
    private const string COIN_PREFS_KEY = "CURRENT_COINS";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        currentCoins = PlayerPrefs.GetInt(COIN_PREFS_KEY, 0);

        // 씬이 로드될 때마다 실행될 이벤트 리스너 등록
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // 스크립트가 파괴될 때 이벤트 리스너 제거
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 씬이 로드될 때 호출되는 함수
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 새 씬에서 "CoinText"라는 이름의 오브젝트를 찾아 연결
        GameObject coinTextObject = GameObject.Find("CoinText");
        if (coinTextObject != null)
        {
            coinText = coinTextObject.GetComponent<TextMeshProUGUI>();
            // UI 연결 후 코인 개수 업데이트
            UpdateCoinText();
            Debug.Log("새로운 씬의 코인 텍스트 오브젝트를 찾았습니다.");
        }
        else
        {
            Debug.LogError("새로운 씬에서 'CoinText' 오브젝트를 찾을 수 없습니다.");
        }
    }

    public void AddCoin(int amount)
    {
        currentCoins += amount;
        UpdateCoinText();
        SaveCoins();
    }

    public void SpendCoin(int amount)
    {
        if (currentCoins >= amount)
        {
            currentCoins -= amount;
            UpdateCoinText();
            SaveCoins();
        }
        else
        {
            Debug.LogWarning("코인이 부족합니다! (현재 코인: " + currentCoins + ")");
        }
    }

    public int GetCoinCount()
    {
        return currentCoins;
    }

    private void UpdateCoinText()
    {
        if (coinText != null)
        {
            coinText.text = currentCoins.ToString();
        }
    }

    private void SaveCoins()
    {
        PlayerPrefs.SetInt(COIN_PREFS_KEY, currentCoins);
        PlayerPrefs.Save();
        Debug.Log($"코인 저장 완료. 현재 코인: {currentCoins}");
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

#if UNITY_EDITOR
    void LateUpdate()
    {
        if (!Application.isPlaying) return;
        if (Input.GetKeyDown(KeyCode.F9))
        {
            ClearAllCoins();
        }
    }
#endif
}