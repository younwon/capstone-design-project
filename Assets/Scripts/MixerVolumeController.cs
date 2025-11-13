using UnityEngine;
using UnityEngine.Audio;

public class MixerVolumeController : MonoBehaviour
{
    public static MixerVolumeController I;

    [Header("Mixer & Groups")]
    public AudioMixer mixer;                       // 전체 Mixer
    public AudioMixerGroup bgmGroup;               // BGM 출력 그룹(선택)
    public AudioMixerGroup sfxGroup;               // SFX 출력 그룹(선택)

    [Header("Exposed Parameter Names (dB)")]
    public string masterParam = "Master_dB";
    public string bgmParam = "BGM_dB";
    public string sfxParam = "SFX_dB";

    [Header("Persist Keys (PlayerPrefs)")]
    public string keyMaster = "vol_master";
    public string keyBgm = "vol_bgm";
    public string keySfx = "vol_sfx";

    [Header("Range")]
    [Tooltip("0은 완전 음소거, 1은 0dB. 0일 때 설정할 dB(대부분 -80dB)")]
    public float minDb = -80f;

    [Range(0f, 1f)] public float master = 1f;
    [Range(0f, 1f)] public float bgm = 1f;
    [Range(0f, 1f)] public float sfx = 1f;

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        // 로드
        if (PlayerPrefs.HasKey(keyMaster)) master = Mathf.Clamp01(PlayerPrefs.GetFloat(keyMaster));
        if (PlayerPrefs.HasKey(keyBgm)) bgm = Mathf.Clamp01(PlayerPrefs.GetFloat(keyBgm));
        if (PlayerPrefs.HasKey(keySfx)) sfx = Mathf.Clamp01(PlayerPrefs.GetFloat(keySfx));

        ApplyAll();
    }

    // 0..1 -> dB 변환
    float ToDb(float v)
    {
        if (v <= 0.0001f) return minDb;
        return Mathf.Log10(v) * 20f; // 1 -> 0dB, 0.5 -> -6.02dB
    }

    public void ApplyAll()
    {
        if (mixer)
        {
            mixer.SetFloat(masterParam, ToDb(master));
            mixer.SetFloat(bgmParam, ToDb(bgm));
            mixer.SetFloat(sfxParam, ToDb(sfx));
        }
    }

    public void SetMaster(float v)
    {
        master = Mathf.Clamp01(v);
        if (mixer) mixer.SetFloat(masterParam, ToDb(master));
        PlayerPrefs.SetFloat(keyMaster, master); PlayerPrefs.Save();
    }
    public void SetBgm(float v)
    {
        bgm = Mathf.Clamp01(v);
        if (mixer) mixer.SetFloat(bgmParam, ToDb(bgm));
        PlayerPrefs.SetFloat(keyBgm, bgm); PlayerPrefs.Save();
    }
    public void SetSfx(float v)
    {
        sfx = Mathf.Clamp01(v);
        if (mixer) mixer.SetFloat(sfxParam, ToDb(sfx));
        PlayerPrefs.SetFloat(keySfx, sfx); PlayerPrefs.Save();
    }

    // 읽기
    public float GetMaster() => master;
    public float GetBgm() => bgm;
    public float GetSfx() => sfx;
}
