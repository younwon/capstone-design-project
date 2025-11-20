using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GraphicsSettings : MonoBehaviour
{
    public Toggle fullscreenToggle;
    private const string FullscreenKey = "IsFullscreen";

    void Start()
    {
        bool isFullscreen = PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
        fullscreenToggle.isOn = isFullscreen;

        // 시작하자마자 모드 적용
        SetFullscreen(isFullscreen);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        if (isFullscreen)
        {
            // 모니터 최대 해상도로 꽉 채우기
            Resolution maxRes = Screen.resolutions[Screen.resolutions.Length - 1];
            Screen.SetResolution(maxRes.width, maxRes.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
          
            //  540x960 크기로 강제로 창 모드
            Screen.SetResolution(540, 960, FullScreenMode.Windowed);
        }

        PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
    }
}