using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonSFX : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    public AudioClip clickClip;
    public AudioClip hoverClip;

    public void OnPointerClick(PointerEventData e)
    {
        if (clickClip) AudioManager.I?.PlaySFX(clickClip);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (hoverClip) AudioManager.I?.PlaySFX(hoverClip);
    }
}
