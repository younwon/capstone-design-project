using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public string[] scenesToLoad; // 여러 씬 이름을 담을 배열

    // 인덱스를 사용하여 씬을 로드하는 함수
    public void LoadSceneByIndex(int index)
    {
        if (index >= 0 && index < scenesToLoad.Length)
        {
            SceneManager.LoadScene(scenesToLoad[index]);
        }
        else
        {
            Debug.LogError("잘못된 씬 인덱스입니다.");
        }
    }
}