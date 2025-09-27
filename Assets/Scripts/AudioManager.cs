using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I;   // 싱글턴 인스턴스

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    void Awake()
    {
        if (I == null) I = this;
        else Destroy(gameObject);
    }

    // 배경음 볼륨 0~1
    public void SetBgm(float volume)
    {
        if (bgmSource) bgmSource.volume = Mathf.Clamp01(volume);
    }

    // 효과음 볼륨 0~1
    public void SetSfx(float volume)
    {
        if (sfxSource) sfxSource.volume = Mathf.Clamp01(volume);
    }
}
