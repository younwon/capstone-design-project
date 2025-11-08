using UnityEngine;

[RequireComponent(typeof(PlayerController2D))]
public class PlayerAudioEvents : MonoBehaviour
{
    public AudioClip jumpClip;
    public AudioClip deathClip;
    [Range(0.9f, 1.1f)] public float pitchVar = 0.05f;

    void OnEnable()
    {
        PlayerDeath.OnPlayerDied += OnPlayerDead;
    }

    void OnDisable()
    {
        PlayerDeath.OnPlayerDied -= OnPlayerDead;
    }

    public void PlayJump()
    {
        float pitch = 1f + Random.Range(-pitchVar, pitchVar);
        AudioManager.I?.PlaySFX(jumpClip, pitch);
    }

    void OnPlayerDead(Transform t)
    {
        if (deathClip) AudioManager.I?.PlaySFX(deathClip);
        AudioManager.I?.FadeOutAll(1.5f);
    }
}
