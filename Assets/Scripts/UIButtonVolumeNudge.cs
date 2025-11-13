using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonVolumeNudge : MonoBehaviour
{
    public enum VolumeTarget { Master, BGM, SFX }

    [Header("Target & Step")]
    public VolumeTarget target = VolumeTarget.BGM;
    [Range(0.01f, 1f)] public float step = 0.1f;    // 1회 증감량(0..1)
    public int nudgeSign = +1;                     // +1:올림, -1:내림

    [Header("Preview SFX")]
    [SerializeField] AudioClip previewClip;
    [SerializeField] UnityEngine.Audio.AudioMixerGroup previewOutputGroup; // 보통 SFX 그룹

    [Header("Options")]
    public bool respectMaster = true;              // 프리뷰 볼륨에 Master 곱하기
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
        _previewSrc.spatialBlend = 0f;
        if (previewOutputGroup) _previewSrc.outputAudioMixerGroup = previewOutputGroup;
        _previewSrc.volume = 1f; // 최종 볼륨은 PlayOneShot의 volumeScale로 제어
    }

    void OnClick()
    {
        if (!MixerVolumeController.I) return;

        // 현재값
        float m = MixerVolumeController.I.GetMaster();
        float b = MixerVolumeController.I.GetBgm();
        float s = MixerVolumeController.I.GetSfx();

        float val = target switch
        {
            VolumeTarget.Master => m,
            VolumeTarget.BGM => b,
            _ => s
        };

        // 증감
        float newVal = Mathf.Clamp01(val + step * Mathf.Sign(nudgeSign));

        // 적용
        switch (target)
        {
            case VolumeTarget.Master: MixerVolumeController.I.SetMaster(newVal); break;
            case VolumeTarget.BGM: MixerVolumeController.I.SetBgm(newVal); break;
            case VolumeTarget.SFX: MixerVolumeController.I.SetSfx(newVal); break;
        }

        // 프리뷰 재생(선택)
        if (previewClip != null)
        {
            // 최신값 다시 읽기(마스터 반영)
            m = MixerVolumeController.I.GetMaster();
            b = MixerVolumeController.I.GetBgm();
            s = MixerVolumeController.I.GetSfx();

            float previewVol = 1f;
            switch (target)
            {
                case VolumeTarget.Master: previewVol = newVal; break;
                case VolumeTarget.BGM: previewVol = newVal * (respectMaster ? m : 1f); break;
                case VolumeTarget.SFX: previewVol = newVal * (respectMaster ? m : 1f); break;
            }
            previewVol = Mathf.Clamp01(previewVol);

            if (previewVol > 0f)
            {
                EnsurePreviewSource();
                float pitch = (randomPitch.x <= 0f && randomPitch.y <= 0f)
                    ? 1f
                    : Random.Range(Mathf.Min(randomPitch.x, randomPitch.y),
                                   Mathf.Max(randomPitch.x, randomPitch.y));
                _previewSrc.pitch = pitch;
                _previewSrc.PlayOneShot(previewClip, previewVol);
            }
        }
    }
}
