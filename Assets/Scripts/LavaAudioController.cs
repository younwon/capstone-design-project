using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class LavaAudioController : MonoBehaviour
{
    [Header("Refs")]
    public LavaRiser lava;
    public Transform player;
    public Camera mainCam;

    [Header("Audio")]
    public AudioClip boilClip;
    public AnimationCurve volumeCurve = AnimationCurve.Linear(0, 0, 10, 1);
    public AnimationCurve pitchCurve = AnimationCurve.Linear(0, 0.8f, 10, 1f);
    public float fadeOutTime = 1f;

    AudioSource src;
    bool visible;
    bool fadingOut;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        src.loop = true;
        src.playOnAwake = false;
        if (!mainCam) mainCam = Camera.main;
    }

    void Update()
    {
        if (!lava || !player || !mainCam) return;
        float dist = Mathf.Abs(player.position.y - lava.transform.position.y);

        float vol = volumeCurve.Evaluate(dist);
        float pitch = pitchCurve.Evaluate(dist);

        if (IsVisibleInCamera())
        {
            if (!src.isPlaying && boilClip) src.Play();
            fadingOut = false;
            src.volume = Mathf.Lerp(src.volume, vol, Time.deltaTime * 3f);
            src.pitch = pitch;
        }
        else
        {
            if (!fadingOut && src.isPlaying)
                StartCoroutine(FadeOutAndStop());
        }
    }

    bool IsVisibleInCamera()
    {
        if (!mainCam) return false;
        Vector3 vp = mainCam.WorldToViewportPoint(lava.transform.position);
        return vp.y > 0 && vp.y < 1.1f; // 화면 안쪽이면 true
    }

    IEnumerator FadeOutAndStop()
    {
        fadingOut = true;
        float startVol = src.volume;
        float t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(startVol, 0f, t / fadeOutTime);
            yield return null;
        }
        src.Stop();
        src.volume = 0f;
    }

    public void StopAllOnDeath(float fadeTime = 1.5f)
    {
        if (src.isPlaying)
            StartCoroutine(FadeOutAndStop());
    }
}
