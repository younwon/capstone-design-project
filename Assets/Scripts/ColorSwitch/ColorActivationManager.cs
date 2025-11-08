// Assets/Scripts/ColorSwitch/ColorActivationManager.cs
using System;
using UnityEngine;

public class ColorActivationManager : MonoBehaviour
{
    public static ColorActivationManager I { get; private set; }

    [Serializable] public class ColorToggle { public string name; public bool isActive; public Color color; }

    [Header("Initial")]
    [SerializeField] private BlockColor initialActive = BlockColor.Green;

    [Header("Key Order (Q/W/E/R)")]
    [SerializeField]
    private BlockColor[] keyOrder = new[] {
        BlockColor.Green, BlockColor.Yellow, BlockColor.Blue, BlockColor.Orange
    };

    [Header("SFX")]
    [Tooltip("색 전환 성공 시 재생")]
    public AudioClip sfxActivate;
    [Tooltip("같은 색을 다시 눌러도 소리를 낼지")]
    public bool playOnSameColor = false;
    [Tooltip("연타시 소리 스팸 방지(초)")]
    [Range(0f, 0.3f)] public float minSfxInterval = 0.08f;
    float _sfxTimer = 0f;

    [Header("Optional UI mirror")]
    public ColorToggle[] colors;

    public BlockColor ActiveColor { get; private set; }
    public event Action<BlockColor> OnColorChanged;

    [Header("Debug")]
    public bool logEvents = false;

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
    }
    void OnDestroy() { if (I == this) I = null; }

    void Start()
    {
        SetActiveColor(initialActive, force: true, playSfx: false);
    }

    void Update()
    {
        _sfxTimer += Time.unscaledDeltaTime;

        if (Input.GetKeyDown(KeyCode.Q)) SetActiveByIndex(0);
        if (Input.GetKeyDown(KeyCode.W)) SetActiveByIndex(1);
        if (Input.GetKeyDown(KeyCode.E)) SetActiveByIndex(2);
        if (Input.GetKeyDown(KeyCode.R)) SetActiveByIndex(3);
    }

    public void SetActiveByIndex(int index)
    {
        if (keyOrder == null || index < 0 || index >= keyOrder.Length) return;
        var target = keyOrder[index];
        bool isSame = target.Equals(ActiveColor);
        bool shouldPlay = playOnSameColor || !isSame;
        SetActiveColor(target, force: playOnSameColor && isSame, playSfx: shouldPlay);
    }

    public void SetActiveColor(BlockColor color, bool force = false, bool playSfx = true)
    {
        bool changed = !color.Equals(ActiveColor);
        if (!force && !changed) { TryPlayActivateSfx(); return; }

        ActiveColor = color;
        SyncColorTogglesArray(color);

        if (logEvents) Debug.Log($"[ColorActivation] Active = {ActiveColor}");

        OnColorChanged?.Invoke(ActiveColor);
        if (playSfx) TryPlayActivateSfx();
    }

    void TryPlayActivateSfx()
    {
        if (sfxActivate == null || AudioManager.I == null) return;
        if (_sfxTimer < minSfxInterval) return;
        _sfxTimer = 0f;
        AudioManager.I.PlaySFX(sfxActivate);
    }

    void SyncColorTogglesArray(BlockColor active)
    {
        if (colors == null || colors.Length == 0) return;
        for (int i = 0; i < colors.Length; i++)
        {
            if (i < keyOrder.Length) colors[i].isActive = (keyOrder[i] == active);
        }
    }

    [ContextMenu("Test Play SFX")]
    void TestPlaySfx()
    {
        if (AudioManager.I && sfxActivate) AudioManager.I.PlaySFX(sfxActivate);
        else Debug.LogWarning("[ColorActivation] AudioManager 또는 sfxActivate가 비었습니다.");
    }
}
