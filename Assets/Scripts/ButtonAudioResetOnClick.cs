using UnityEngine;
using UnityEngine.EventSystems;

[AddComponentMenu("Audio/Button Audio Reset OnClick")]
public class ButtonAudioResetOnClick : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("즉시 복구(페이드 없음)")]
    public bool forceInstant = true;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (PauseAudioDucker.I == null) return;
        if (forceInstant) PauseAudioDucker.I.ForceNormalNow();
        else PauseAudioDucker.I.ToNormal();
        // 이후 기존 버튼 로직(씬 전환/타이틀 복귀)은 그대로 진행됩니다.
    }
}
