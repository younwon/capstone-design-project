// PauseWhenOpen.cs
using UnityEngine;

public class PauseWhenOpen : MonoBehaviour
{
    float prevScale = 1f;

    void OnEnable()
    {
        prevScale = Time.timeScale;
        Time.timeScale = 0f; // 물리/애니메이션(Scaled) 정지
    }

    void OnDisable()
    {
        Time.timeScale = prevScale;
    }
}

