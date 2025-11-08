// Assets/Scripts/Audio/UIButtonVolumeNudge.cs
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonVolumeNudge : MonoBehaviour
{
    public enum VolumeTarget { Master, BGM, SFX }

    [Header("Target & Step")]
    public VolumeTarget target = VolumeTarget.SFX;
    [Tooltip("한 번 클릭 시 증감량(0~1)")]
    [Range(0.01f, 1f)] public float step = 0.1f;
    [Tooltip("증가(+1) / 감소(-1)")]
    public int nudgeSign = +1;

    [Header("Preview SFX")]
    [SerializeField] AudioClip previewClip;
    [Tooltip("프리뷰 소리를 AudioMixer에 연결하고 싶을 때 지정(선택)")]
    [SerializeField] UnityEngine.Audio.AudioMixerGroup mixerGroup;

    [Header("Options")]
    [Tooltip("BGM/SFX 조절 시 프리뷰 볼륨에 Master를 반영")]
    public bool respectMaster = true;
    [Tooltip("프리뷰 재생 시 피치 약간 랜덤")]
    public Vector2 randomPitch = new Vector2(0.98f, 1.02f);

    Button _btn;
    AudioSource _previewSrc;

    void Awake()
    {
        _btn = GetComponent<Button>();
        _btn.onClick.AddListener(OnClick);
    }

    void OnDestroy()
    {
        if (_btn) _btn.onClick.RemoveListener(OnClick);
    }

    void EnsurePreviewSource()
    {
        if (_previewSrc) return;
        _previewSrc = gameObject.AddComponent<AudioSource>();
        _previewSrc.playOnAwake = false;
        _previewSrc.loop = false;
        _previewSrc.spatialBlend = 0f; // 2D
        if (mixerGroup) _previewSrc.outputAudioMixerGroup = mixerGroup;
        _previewSrc.volume = 1f; // 최종 볼륨은 PlayOneShot의 volumeScale로 제어
    }

    public void OnClick()
    {
        if (AudioManager.I == null) return;

        // 1) 현재 값 읽기
        float master = Mathf.Clamp01(AudioManager.I.masterVolume);
        float bgm = Mathf.Clamp01(AudioManager.I.bgmVolume);
        float sfx = Mathf.Clamp01(AudioManager.I.sfxVolume);

        // 2) 목표(증감) 계산
        float val = target switch
        {
            VolumeTarget.Master => master,
            VolumeTarget.BGM => bgm,
            _ => sfx
        };

        float newVal = Mathf.Clamp01(val + step * Mathf.Sign(nudgeSign));

        // 3) 실제 볼륨 적용
        switch (target)
        {
            case VolumeTarget.Master: AudioManager.I.SetMasterVolume(newVal); break;
            case VolumeTarget.BGM: AudioManager.I.SetBGMVolume(newVal); break;
            case VolumeTarget.SFX: AudioManager.I.SetSFXVolume(newVal); break;
        }

        // 최신 값 다시 읽기(믹서 연동 시 오차 방지)
        master = Mathf.Clamp01(AudioManager.I.masterVolume);
        bgm = Mathf.Clamp01(AudioManager.I.bgmVolume);
        sfx = Mathf.Clamp01(AudioManager.I.sfxVolume);

        // 4) 프리뷰 재생 볼륨 계산
        //    - Master 조절: newVal 그대로 (마스터 자체가 기준)
        //    - BGM 조절: newVal * (respectMaster ? master : 1)
        //    - SFX 조절: newVal * (respectMaster ? master : 1)
        float previewVol = 1f;
        switch (target)
        {
            case VolumeTarget.Master:
                previewVol = newVal;
                break;
            case VolumeTarget.BGM:
                previewVol = newVal * (respectMaster ? master : 1f);
                break;
            case VolumeTarget.SFX:
                previewVol = newVal * (respectMaster ? master : 1f);
                break;
        }
        previewVol = Mathf.Clamp01(previewVol);

        // 5) 프리뷰 재생
        if (previewClip != null && previewVol > 0f)
        {
            EnsurePreviewSource();
            float pitch = (randomPitch.x <= 0f && randomPitch.y <= 0f)
                            ? 1f
                            : Random.Range(Mathf.Min(randomPitch.x, randomPitch.y),
                                           Mathf.Max(randomPitch.x, randomPitch.y));
            _previewSrc.pitch = pitch;

            // PlayOneShot(volumeScale)은 AudioSource.volume(=1)과 곱해지므로 previewVol이 최종 볼륨이 됨
            _previewSrc.PlayOneShot(previewClip, previewVol);
        }
    }
}
