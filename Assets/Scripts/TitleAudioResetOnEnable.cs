using UnityEngine;

[AddComponentMenu("Audio/Title Audio Reset OnEnable")]
public class TitleAudioResetOnEnable : MonoBehaviour
{
    [Tooltip("활성 시 페이드 없이 즉시 복구할지")]
    public bool forceInstant = true;

    void OnEnable()
    {
        if (PauseAudioDucker.I == null) return;
        if (forceInstant) PauseAudioDucker.I.ForceNormalNow();
        else PauseAudioDucker.I.ToNormal();
    }
}
