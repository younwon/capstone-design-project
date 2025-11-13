using UnityEngine;
using UnityEngine.Audio;

public class PauseAudioSnapshot : MonoBehaviour
{
    public static PauseAudioSnapshot I;

    [Header("Mixer & Snapshots")]
    public AudioMixer mixer;
    public AudioMixerSnapshot normalSnapshot;
    public AudioMixerSnapshot pausedSnapshot;

    [Header("Transition")]
    public float transitionTime = 0.3f;

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ToNormal()
    {
        if (mixer && normalSnapshot)
            normalSnapshot.TransitionTo(transitionTime);
    }

    public void ToPaused()
    {
        if (mixer && pausedSnapshot)
            pausedSnapshot.TransitionTo(transitionTime);
    }
}
