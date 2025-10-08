using UnityEngine;
using UnityEngine.UI;

public class LavaProximityFX : MonoBehaviour
{
    public LavaRiser lava;
    public Transform cam;
    public Image screenTint;
    public CameraShaker shaker;

    [Header("Distances (world units)")]
    public float startBelow = 0.6f;
    public float fullAt = 0.05f;

    [Header("Visual")]
    [Range(0f, 1f)] public float maxAlpha = 0.12f;
    public float fadeSpeed = 8f;

    [Header("Shake")]
    [Range(0f, 1f)] public float shakeAtFull = 0.22f;

    void Awake()
    {
        if (!cam) cam = Camera.main ? Camera.main.transform : null;
        if (!lava) lava = FindObjectOfType<LavaRiser>();
        if (!shaker && Camera.main) shaker = Camera.main.GetComponent<CameraShaker>();
    }

    void LateUpdate()
    {
        // 타이틀 대기 중엔 즉시 0 유지
        if (!StartGate.GameStarted) { ResetImmediate(); return; }

        if (!lava || !cam || !screenTint) return;

        var c = Camera.main;
        float camBottom = cam.position.y - c.orthographicSize;
        float dist = lava.transform.position.y - camBottom;

        float t01 = Mathf.InverseLerp(-startBelow, fullAt, dist);
        t01 = Mathf.Clamp01(t01);

        var col = screenTint.color;
        float targetA = t01 * maxAlpha;
        col.a = Mathf.MoveTowards(col.a, targetA, fadeSpeed * Time.deltaTime);
        screenTint.color = col;

        if (shaker) shaker.SetExternal(t01 * shakeAtFull);
    }

    public void ResetImmediate()
    {
        if (screenTint)
        {
            var c = screenTint.color;
            if (c.a != 0f) { c.a = 0f; screenTint.color = c; }
        }
        if (shaker) shaker.SetExternal(0f);
    }
}



