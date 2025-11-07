using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneTracker : MonoBehaviour
{
    public static SceneTracker Instance;

    // 이전 씬의 이름을 저장합니다. 외부에서는 읽기만 가능합니다.
    public string PreviousSceneName { get; private set; } = "";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 씬 로드 전에 호출되어 현재 씬 이름을 '이전 씬'으로 저장합니다.
    public void SetPreviousScene(string sceneName)
    {
        PreviousSceneName = sceneName;
        Debug.Log($"[SceneTracker] 이전 씬 이름 저장: {sceneName}");
    }
}