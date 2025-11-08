using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class ResultCounterAudio : MonoBehaviour
{
    public AudioClip tickClip;
    public AudioClip confirmClip;
    public AudioClip newRecordClip;
    public float tickInterval = 0.05f;

    AudioSource src;

    void Awake()
    {
        src = GetComponent<AudioSource>();
    }

    public IEnumerator PlayCountingSFX(int ticks, bool newRecord)
    {
        for (int i = 0; i < ticks; i++)
        {
            src.PlayOneShot(tickClip);
            yield return new WaitForSecondsRealtime(tickInterval);
        }
        src.PlayOneShot(confirmClip);
        if (newRecord && newRecordClip) src.PlayOneShot(newRecordClip);
    }
}
