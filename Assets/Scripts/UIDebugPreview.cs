using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEngine;

[ExecuteAlways]  // 에디터/플레이 둘 다 호출되지만 아래에서 플레이 중은 무시
public class UIDebugPreview : MonoBehaviour
{
    [Tooltip("에디터에서만 강제로 보여줍니다 (플레이 모드에는 영향 없음).")]
    public bool previewInEditor = false;

    [Tooltip("GameOverUI의 CanvasGroup. 비워두면 자동으로 찾습니다.")]
    public CanvasGroup target;

    void OnEnable() { Apply(); }
    void OnValidate() { Apply(); }

    void Apply()
    {
        // 플레이 중에는 건드리지 않음 (실제 게임 로직 유지)
        if (Application.isPlaying) return;

        if (!target) target = GetComponent<CanvasGroup>();
        if (!target) return;

        // 보이기/숨기기 (활성화는 유지하되 알파/레이캐스트만 제어)
        target.alpha = previewInEditor ? 1f : 0f;
        target.interactable = previewInEditor;
        target.blocksRaycasts = previewInEditor;

        // 혹시 비활성 상태였다면 편집용으로 켜둠
        if (!gameObject.activeSelf) gameObject.SetActive(true);
    }
}
#endif

