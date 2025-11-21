using UnityEngine;

public class SceneBgmPlayer : MonoBehaviour
{
    [Header("이 씬에서 사용할 BGM")]
    public AudioClip bgmClip;
    public bool loop = true;

    [Header("동일 곡 처리")]
    public bool restartIfSame = false;

    [Header("자동 재생 옵션")]
    public bool playOnStart = true;

    [Header("페이드 옵션")]
    public float fadeTime = 0.8f;

    void Start()
    {
        if (!playOnStart) return;
        Play();
    }

    // ✅ 씬 재진입/오브젝트 재활성화 때도 확실히 재생
    void OnEnable()
    {
        if (!playOnStart) return;
        Play();
    }

    public void Play()
    {
        if (AudioManager.I == null) return;
        if (bgmClip == null) return;

        AudioManager.I.PlayBGM_Fade(bgmClip, fadeTime, restartIfSame);
    }

    public void Stop(float fadeOutTime = 0.5f)
    {
        if (AudioManager.I == null) return;

        if (fadeOutTime <= 0f)
            AudioManager.I.StopBGM();
        else
            AudioManager.I.StopBGM(fadeOutTime);
    }
}




