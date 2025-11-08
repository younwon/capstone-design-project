using UnityEngine;

public class ColorToggleSFX : MonoBehaviour
{
    [Header("Clips: Q W E R 순서")]
    public AudioClip[] colorClips = new AudioClip[4];
    public float cooldown = 0.1f;

    float[] lastPlay = new float[4];

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q)) PlayToggle(0);
        if (Input.GetKeyDown(KeyCode.W)) PlayToggle(1);
        if (Input.GetKeyDown(KeyCode.E)) PlayToggle(2);
        if (Input.GetKeyDown(KeyCode.R)) PlayToggle(3);
    }

    void PlayToggle(int idx)
    {
        if (idx < 0 || idx >= colorClips.Length) return;
        if (Time.time - lastPlay[idx] < cooldown) return;
        lastPlay[idx] = Time.time;
        AudioManager.I?.PlaySFX(colorClips[idx]);
    }
}
