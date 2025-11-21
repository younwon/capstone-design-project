using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUIController : MonoBehaviour
{
    [Header("BGM Row")]
    [SerializeField] Button btnBgmMinus;
    [SerializeField] Button btnBgmPlus;
    [SerializeField] TMP_Text txtBgmValue;
    [SerializeField] Image bgBgmValueBox;

    [Header("SFX Row")]
    [SerializeField] Button btnSfxMinus;
    [SerializeField] Button btnSfxPlus;
    [SerializeField] TMP_Text txtSfxValue;
    [SerializeField] Image bgSfxValueBox;

    [Header("Checkboxes")]
    [SerializeField] Toggle toggleAllSound;   // ALL SOUND
    [SerializeField] Toggle toggleFullscreen; // FULLSCREEN 

    [Header("Bottom Buttons")]
    [SerializeField] Button btnCancel; // 창 하단 Cancel
    [SerializeField] Button btnSave;   // 창 하단 Save

    [Header("Value Box Sprites")]
    [SerializeField] Sprite normalSprite;
    [SerializeField] Sprite pendingSprite;

    [Header("Close / Popup")]
    [SerializeField] Button btnCloseX;
    [SerializeField] GameObject settingsPanel;
    [SerializeField] GameObject popupUnsaved;
    [SerializeField] Button btnPopupDiscard;
    [SerializeField] Button btnPopupSaveAndExit;

    // ===== 추가: 세팅 패널 버튼 기본 클릭 SFX =====
    [Header("SFX for Settings Panel Buttons")]
    [SerializeField] AudioClip defaultSettingsClickSfx;

    const int MAX_STEP = 5;
    const string KEY_BGM = "SET_BGM";
    const string KEY_SFX = "SET_SFX";
    const string KEY_MUTEALL = "SET_MUTEALL";
    const string KEY_FULLSCREEN = "SET_FULLSCREEN";

    int initBgmStep, initSfxStep;
    int curBgmStep, curSfxStep;
    bool dirtyBgm, dirtySfx;

    bool initMuteAll, curMuteAll, dirtyMuteAll;
    bool initFullscreen, curFullscreen, dirtyFullscreen;

    int prevBgmStep, prevSfxStep;

    void Start()
    {
        ApplySavedResolutionAtStart();

        btnBgmMinus.onClick.AddListener(() => ChangeBgm(-1));
        btnBgmPlus.onClick.AddListener(() => ChangeBgm(+1));
        btnSfxMinus.onClick.AddListener(() => ChangeSfx(-1));
        btnSfxPlus.onClick.AddListener(() => ChangeSfx(+1));

        btnCancel.onClick.AddListener(OnCancel);
        btnSave.onClick.AddListener(OnSave);

        if (toggleAllSound) toggleAllSound.onValueChanged.AddListener(OnToggleAllSound);
        if (toggleFullscreen) toggleFullscreen.onValueChanged.AddListener(OnToggleFullscreen);

        if (btnCloseX) btnCloseX.onClick.AddListener(TryClose);
        if (btnPopupDiscard) btnPopupDiscard.onClick.AddListener(ExitWithoutSave);
        if (btnPopupSaveAndExit) btnPopupSaveAndExit.onClick.AddListener(SaveAndExit);

        if (popupUnsaved) popupUnsaved.SetActive(false);
    }

    void ApplySavedResolutionAtStart()
    {
        bool isFull = PlayerPrefs.GetInt(KEY_FULLSCREEN, 1) == 1;
        ApplyResolutionLogic(isFull);
    }

    void OnEnable()
    {
        // ===== [핵심 1] CanvasGroup이 꺼진 채로 남는 문제 방지 =====
        ForceEnableCanvasGroup();

        // ===== [핵심 2] 팝업이 켜진 채로 남아 레이캐스트 막는 문제 방지 =====
        if (popupUnsaved) popupUnsaved.SetActive(false);

        // ===== [핵심 3] 세팅 패널 버튼들에 클릭 SFX 자동 부착 =====
        EnsureSettingsButtonsSfx();

        LoadFromPrefsToInit();
        CopyInitToCurrent();

        prevBgmStep = curBgmStep;
        prevSfxStep = curSfxStep;

        RefreshAllUI();
        ApplyPreviewVolumesFromCurrent();
    }

    // CanvasGroup 강제 복구
    void ForceEnableCanvasGroup()
    {
        GameObject root = settingsPanel ? settingsPanel : gameObject;

        var cg = root.GetComponent<CanvasGroup>();
        if (!cg) cg = root.GetComponentInChildren<CanvasGroup>(true);

        if (cg)
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
    }

    // 세팅 패널 버튼에만 기본 클릭音 자동 연결
    void EnsureSettingsButtonsSfx()
    {
        if (!defaultSettingsClickSfx) return; // 클립 없으면 아무것도 안 함

        GameObject root = settingsPanel ? settingsPanel : gameObject;
        var buttons = root.GetComponentsInChildren<Button>(true);

        foreach (var b in buttons)
        {
            if (!b) continue;

            // 이미 전용 스크립트가 있거나, 특수 버튼이면 스킵하고 싶다면 여기서 조건 추가 가능
            // (지금은 안전하게 "중복 리스너만" 방지)
            bool alreadyBound = false;
            var listeners = b.onClick.GetPersistentEventCount();
            // Persistent count로는 런타임 AddListener 체크가 안 되지만
            // 중복 바인딩 방지는 아래 helper 컴포넌트로 처리
            var helper = b.GetComponent<SettingsPanelAutoSFX>();
            if (!helper)
            {
                helper = b.gameObject.AddComponent<SettingsPanelAutoSFX>();
                helper.clickClip = defaultSettingsClickSfx;
            }
            else
            {
                alreadyBound = true;
            }

            if (!alreadyBound)
                helper.BindOnce(b);
        }
    }

    // ================== BGM / SFX 조절 ==================

    void ChangeBgm(int delta)
    {
        int before = curBgmStep;
        curBgmStep = Mathf.Clamp(curBgmStep + delta, 0, MAX_STEP);
        dirtyBgm = (curBgmStep != initBgmStep);

        if (curBgmStep != before)
        {
            UpdateBgmUI(true);

            if (!curMuteAll && AudioManager.I)
            {
                float v = curBgmStep / (float)MAX_STEP;
                AudioManager.I.SetBGMVolume(v);
            }
        }
    }

    void ChangeSfx(int delta)
    {
        int before = curSfxStep;
        curSfxStep = Mathf.Clamp(curSfxStep + delta, 0, MAX_STEP);
        dirtySfx = (curSfxStep != initSfxStep);

        if (curSfxStep != before)
        {
            UpdateSfxUI(true);

            if (!curMuteAll && AudioManager.I)
            {
                float v = curSfxStep / (float)MAX_STEP;
                AudioManager.I.SetSFXVolume(v);
            }
        }
    }

    void UpdateBgmUI(bool animate)
    {
        if (txtBgmValue) txtBgmValue.text = curBgmStep.ToString();
        if (bgBgmValueBox) bgBgmValueBox.sprite = dirtyBgm ? pendingSprite : normalSprite;
    }

    void UpdateSfxUI(bool animate)
    {
        if (txtSfxValue) txtSfxValue.text = curSfxStep.ToString();
        if (bgSfxValueBox) bgSfxValueBox.sprite = dirtySfx ? pendingSprite : normalSprite;
    }

    void OnToggleAllSound(bool on)
    {
        curMuteAll = on;
        dirtyMuteAll = (curMuteAll != initMuteAll);

        if (on)
        {
            prevBgmStep = curBgmStep;
            prevSfxStep = curSfxStep;

            curBgmStep = 0;
            dirtyBgm = (curBgmStep != initBgmStep);

            curSfxStep = 0;
            dirtySfx = (curSfxStep != initSfxStep);

            UpdateBgmUI(false);
            UpdateSfxUI(false);

            if (AudioManager.I)
            {
                AudioManager.I.SetBGMVolume(0f);
                AudioManager.I.SetSFXVolume(0f);
            }
        }
        else
        {
            curBgmStep = Mathf.Clamp(prevBgmStep, 0, MAX_STEP);
            curSfxStep = Mathf.Clamp(prevSfxStep, 0, MAX_STEP);

            dirtyBgm = (curBgmStep != initBgmStep);
            dirtySfx = (curSfxStep != initSfxStep);

            UpdateBgmUI(false);
            UpdateSfxUI(false);

            if (AudioManager.I)
            {
                float bgm = curBgmStep / (float)MAX_STEP;
                float sfx = curSfxStep / (float)MAX_STEP;

                AudioManager.I.SetBGMVolume(bgm);
                AudioManager.I.SetSFXVolume(sfx);
            }
        }

        ApplyMuteAllVisual(curMuteAll);
    }

    void ApplyMuteAllVisual(bool muted)
    {
        if (btnBgmMinus) btnBgmMinus.interactable = !muted;
        if (btnBgmPlus) btnBgmPlus.interactable = !muted;
        if (btnSfxMinus) btnSfxMinus.interactable = !muted;
        if (btnSfxPlus) btnSfxPlus.interactable = !muted;
    }

    void OnToggleFullscreen(bool on)
    {
        curFullscreen = on;
        dirtyFullscreen = (curFullscreen != initFullscreen);
    }

    // ---------------- SAVE / CANCEL ----------------
    void OnSave()
    {
        if (curMuteAll)
        {
            if (AudioManager.I)
            {
                AudioManager.I.SetBGMVolume(0f);
                AudioManager.I.SetSFXVolume(0f);
            }

            PlayerPrefs.SetInt(KEY_MUTEALL, 1);
            PlayerPrefs.SetFloat(KEY_BGM, 0f);
            PlayerPrefs.SetFloat(KEY_SFX, 0f);
        }
        else
        {
            float bgm = curBgmStep / (float)MAX_STEP;
            float sfx = curSfxStep / (float)MAX_STEP;

            if (AudioManager.I)
            {
                AudioManager.I.SetBGMVolume(bgm);
                AudioManager.I.SetSFXVolume(sfx);
            }

            PlayerPrefs.SetInt(KEY_MUTEALL, 0);
            PlayerPrefs.SetFloat(KEY_BGM, bgm);
            PlayerPrefs.SetFloat(KEY_SFX, sfx);
        }

        PlayerPrefs.SetInt(KEY_FULLSCREEN, curFullscreen ? 1 : 0);
        ApplyResolutionLogic(curFullscreen);

        PlayerPrefs.Save();

        initBgmStep = curBgmStep; dirtyBgm = false;
        initSfxStep = curSfxStep; dirtySfx = false;
        initMuteAll = curMuteAll; dirtyMuteAll = false;
        initFullscreen = curFullscreen; dirtyFullscreen = false;

        UpdateBgmUI(false);
        UpdateSfxUI(false);
        ApplyMuteAllVisual(curMuteAll);
    }

    void ApplyResolutionLogic(bool isFull)
    {
        if (isFull)
        {
            Resolution maxRes = Screen.resolutions[Screen.resolutions.Length - 1];
            Screen.SetResolution(maxRes.width, maxRes.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            Screen.SetResolution(540, 960, FullScreenMode.Windowed);
        }
    }

    void OnCancel()
    {
        CopyInitToCurrent();
        if (toggleAllSound) toggleAllSound.SetIsOnWithoutNotify(curMuteAll);
        if (toggleFullscreen) toggleFullscreen.SetIsOnWithoutNotify(curFullscreen);

        RefreshAllUI();
        ApplyPreviewVolumesFromCurrent();
    }

    void TryClose()
    {
        if (HasDirty()) OpenPopup();
        else CloseSettings();
    }

    bool HasDirty() => dirtyBgm || dirtySfx || dirtyMuteAll || dirtyFullscreen;

    void OpenPopup() { if (popupUnsaved) popupUnsaved.SetActive(true); }
    void ClosePopup() { if (popupUnsaved) popupUnsaved.SetActive(false); }

    void ExitWithoutSave()
    {
        CopyInitToCurrent();
        if (toggleAllSound) toggleAllSound.SetIsOnWithoutNotify(curMuteAll);
        if (toggleFullscreen) toggleFullscreen.SetIsOnWithoutNotify(curFullscreen);

        RefreshAllUI();
        ApplyPreviewVolumesFromCurrent();

        ClosePopup();
        CloseSettings();
    }

    void SaveAndExit()
    {
        OnSave();
        ClosePopup();
        CloseSettings();
    }

    void CloseSettings()
    {
        Time.timeScale = 1f;

        var titleController = FindObjectOfType<TitleUIController>();
        if (titleController != null)
            titleController.ArmStartCatcher();

        if (settingsPanel) settingsPanel.SetActive(false);
        else gameObject.SetActive(false);
    }

    // ---------------- Helpers ----------------
    void LoadFromPrefsToInit()
    {
        float savedBgm = PlayerPrefs.GetFloat(KEY_BGM, 0.8f);
        float savedSfx = PlayerPrefs.GetFloat(KEY_SFX, 0.8f);

        initBgmStep = Mathf.Clamp(Mathf.RoundToInt(savedBgm * MAX_STEP), 0, MAX_STEP);
        initSfxStep = Mathf.Clamp(Mathf.RoundToInt(savedSfx * MAX_STEP), 0, MAX_STEP);
        initMuteAll = PlayerPrefs.GetInt(KEY_MUTEALL, 0) == 1;

        initFullscreen = PlayerPrefs.GetInt(KEY_FULLSCREEN, 1) == 1;
    }

    void CopyInitToCurrent()
    {
        curBgmStep = initBgmStep;
        curSfxStep = initSfxStep;
        curMuteAll = initMuteAll;
        curFullscreen = initFullscreen;

        dirtyBgm = dirtySfx = dirtyMuteAll = dirtyFullscreen = false;
    }

    void ApplyPreviewVolumesFromCurrent()
    {
        float bgm = curMuteAll ? 0f : (curBgmStep / (float)MAX_STEP);
        float sfx = curMuteAll ? 0f : (curSfxStep / (float)MAX_STEP);

        if (AudioManager.I)
        {
            AudioManager.I.SetBGMVolume(bgm);
            AudioManager.I.SetSFXVolume(sfx);
        }

        ApplyMuteAllVisual(curMuteAll);
    }

    void RefreshAllUI()
    {
        UpdateBgmUI(false);
        UpdateSfxUI(false);

        if (toggleAllSound) toggleAllSound.SetIsOnWithoutNotify(curMuteAll);
        if (toggleFullscreen) toggleFullscreen.SetIsOnWithoutNotify(curFullscreen);

        ApplyMuteAllVisual(curMuteAll);
    }
}

/// <summary>
/// Settings Panel 전용: 버튼에 기본 클릭 SFX만 안전하게 "추가"해주는 헬퍼.
/// 기존 onClick 리스너는 절대 건드리지 않음.
/// </summary>
public class SettingsPanelAutoSFX : MonoBehaviour
{
    public AudioClip clickClip;
    bool bound = false;

    public void BindOnce(Button b)
    {
        if (bound || !b) return;
        bound = true;

        b.onClick.AddListener(() =>
        {
            if (AudioManager.I && clickClip)
                AudioManager.I.PlaySFX(clickClip);
        });
    }
}


