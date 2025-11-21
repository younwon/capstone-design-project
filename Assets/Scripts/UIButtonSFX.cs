using UnityEngine;

public class UIButtonSFX : MonoBehaviour
{
    public AudioClip clickClip;
    public AudioClip hoverClip;

    public void PlayClick()
    {
        if (clickClip && AudioManager.I)
            AudioManager.I.PlaySFX(clickClip);
    }

    public void PlayHover()
    {
        if (hoverClip && AudioManager.I)
            AudioManager.I.PlaySFX(hoverClip);
    }
}
