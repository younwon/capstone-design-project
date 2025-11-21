using UnityEngine;
using UnityEngine.UI;

public class LogoDebug : MonoBehaviour
{
    void OnEnable()
    {
        var img = GetComponent<Image>();
        Debug.Log($"[LogoDebug] Enabled. activeSelf={gameObject.activeSelf}, " +
                  $"imgEnabled={img?.enabled}, spriteNull={img?.sprite == null}");
    }
}
