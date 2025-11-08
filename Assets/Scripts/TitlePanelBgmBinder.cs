using UnityEngine;

public class TitlePanelBgmBinder : MonoBehaviour
{
    void OnEnable()
    {
        if (BgmController.I) BgmController.I.PlayTitle();
    }
}
