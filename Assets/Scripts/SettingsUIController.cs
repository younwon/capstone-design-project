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
    [SerializeField] Toggle toggleVibration;  // VIBRATION

    [Header("Bottom Buttons")]
    [SerializeField] Button btnCancel; // 창 하단 Cancel (되돌리기)
    [SerializeField] Button btnSave;   // 창 하단 Save (저장만)

    [Header("Value Box Sprites")]
    [SerializeField] Sprite normalSprite;   // 흰 숫자칸
    [SerializeField] Sprite pendingSprite;  // 빨간 숫자칸

    [Header("Close / Popup")]
    [SerializeField] Button btnCloseX;           // 우상단 X 버튼
    [SerializeField] GameObject settingsPanel;   // SettingsPanel 루트 (닫을 때 끔)
    [SerializeField] GameObject popupUnsaved;    // Popup_Unsaved 루트
    [SerializeField] Button btnPopupDiscard;     // 팝업: EXIT(저장 안 함)
    [SerializeField] Button btnPopupSaveAndExit; // 팝업: SAVE AND EXIT

    const int MAX_STEP = 5; // 0~5
    const string KEY_BGM = "SET_BGM";
    const string KEY_SFX = "SET_SFX";
    const string KEY_MUTEALL = "SET_MUTEALL";
    const string KEY_VIB = "SET_VIB";

    int initBgmStep, initSfxStep;
    int curBgmStep, curSfxStep;
    bool dirtyBgm, dirtySfx;

    bool initMuteAll, curMuteAll, dirtyMuteAll;
    bool initVibration, curVibration, dirtyVibration;

    // 음소거 전 단계 저장(복원용)
    int prevBgmStep, prevSfxStep;

    // ---------------- Lifecycle ----------------
    void Start()
    {
        // 리스너만 연결 (값 로드는 OnEnable에서 처리)
        btnBgmMinus.onClick.AddListener(() => ChangeBgm(-1));
        btnBgmPlus.onClick.AddListener(() => ChangeBgm(+1));
        btnSfxMinus.onClick.AddListener(() => ChangeSfx(-1));
        btnSfxPlus.onClick.AddListener(() => ChangeSfx(+1));

        btnCancel.onClick.AddListener(OnCancel);
        btnSave.onClick.AddListener(OnSave);

        if (toggleAllSound) toggleAllSound.onValueChanged.AddListener(OnToggleAllSound);
        if (toggleVibration) toggleVibration.onValueChanged.AddListener(OnToggleVibration);

        if (btnCloseX) btnCloseX.onClick.AddListener(TryClose);
        if (btnPopupDiscard) btnPopupDiscard.onClick.AddListener(ExitWithoutSave);
        if (btnPopupSaveAndExit) btnPopupSaveAndExit.onClick.AddListener(SaveAndExit);

        if (popupUnsaved) popupUnsaved.SetActive(false);
    }

    void OnEnable()
    {
        // 창을 열 때마다 항상 저장된 값으로 초기화
        LoadFromPrefsToInit();
        CopyInitToCurrent();

        // 음소거 복원 대비
        prevBgmStep = curBgmStep;
        prevSfxStep = curSfxStep;

        RefreshAllUI();
        ApplyPreviewVolumesFromCurrent();
    }

    // ---------------- BGM / SFX ----------------
    void ChangeBgm(int delta)
    {
        int before = curBgmStep;
        curBgmStep = Mathf.Clamp(curBgmStep + delta, 0, MAX_STEP);
        dirtyBgm = (curBgmStep != initBgmStep);
        if (curBgmStep != before)
        {
            UpdateBgmUI(true);
            if (!curMuteAll && AudioManager.I) AudioManager.I.SetBgm(curBgmStep / (float)MAX_STEP);
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
            if (!curMuteAll && AudioManager.I) AudioManager.I.SetSfx(curSfxStep / (float)MAX_STEP);
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

    // ---------------- ALL MUTE ----------------
    void OnToggleAllSound(bool on)
    {
        curMuteAll = on;
        dirtyMuteAll = (curMuteAll != initMuteAll);

        if (on)
        {
            prevBgmStep = curBgmStep;
            prevSfxStep = curSfxStep;

            curBgmStep = 0; dirtyBgm = (curBgmStep != initBgmStep);
            curSfxStep = 0; dirtySfx = (curSfxStep != initSfxStep);
            UpdateBgmUI(false);
            UpdateSfxUI(false);

            if (AudioManager.I)
            {
                AudioManager.I.SetBgm(0f);
                AudioManager.I.SetSfx(0f);
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
                AudioManager.I.SetBgm(curBgmStep / (float)MAX_STEP);
                AudioManager.I.SetSfx(curSfxStep / (float)MAX_STEP);
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

    // ---------------- VIBRATION ----------------
    void OnToggleVibration(bool on)
    {
        curVibration = on;
        dirtyVibration = (curVibration != initVibration);
    }

    // ---------------- SAVE / CANCEL ----------------
    void OnSave()
    {
        if (curMuteAll)
        {
            if (AudioManager.I)
            {
                AudioManager.I.SetBgm(0f);
                AudioManager.I.SetSfx(0f);
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
                AudioManager.I.SetBgm(bgm);
                AudioManager.I.SetSfx(sfx);
            }
            PlayerPrefs.SetInt(KEY_MUTEALL, 0);
            PlayerPrefs.SetFloat(KEY_BGM, bgm);
            PlayerPrefs.SetFloat(KEY_SFX, sfx);
        }

        PlayerPrefs.SetInt(KEY_VIB, curVibration ? 1 : 0);
        PlayerPrefs.Save();

#if UNITY_ANDROID || UNITY_IOS
        if (curVibration) Handheld.Vibrate();
#endif

        // 초기값 갱신
        initBgmStep = curBgmStep; dirtyBgm = false;
        initSfxStep = curSfxStep; dirtySfx = false;
        initMuteAll = curMuteAll; dirtyMuteAll = false;
        initVibration = curVibration; dirtyVibration = false;

        UpdateBgmUI(false);
        UpdateSfxUI(false);
        ApplyMuteAllVisual(curMuteAll);
    }

    void OnCancel() // 저장 전 상태로 되돌리기(창은 유지)
    {
        CopyInitToCurrent();
        if (toggleAllSound) toggleAllSound.SetIsOnWithoutNotify(curMuteAll);
        if (toggleVibration) toggleVibration.SetIsOnWithoutNotify(curVibration);

        RefreshAllUI();
        ApplyPreviewVolumesFromCurrent();
    }

    // ---------------- 닫기/팝업 ----------------
    void TryClose()  // X 버튼에서 호출
    {
        if (HasDirty()) OpenPopup();
        else CloseSettings();
    }

    bool HasDirty() => dirtyBgm || dirtySfx || dirtyMuteAll || dirtyVibration;

    void OpenPopup() { if (popupUnsaved) popupUnsaved.SetActive(true); }
    void ClosePopup() { if (popupUnsaved) popupUnsaved.SetActive(false); }

    void ExitWithoutSave() // 팝업: 저장 안 하고 닫기
    {
        // 편집값을 원래(init)로 롤백 + 미리듣기도 원복
        CopyInitToCurrent();
        if (toggleAllSound) toggleAllSound.SetIsOnWithoutNotify(curMuteAll);
        if (toggleVibration) toggleVibration.SetIsOnWithoutNotify(curVibration);
        RefreshAllUI();
        ApplyPreviewVolumesFromCurrent();

        ClosePopup();
        CloseSettings();
    }

    void SaveAndExit() // 팝업: 저장하고 닫기
    {
        OnSave();
        ClosePopup();
        CloseSettings();
    }

    void CloseSettings()
    {
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    // ---------------- Helpers ----------------
    void LoadFromPrefsToInit()
    {
        float savedBgm = PlayerPrefs.GetFloat(KEY_BGM, 0.8f);
        float savedSfx = PlayerPrefs.GetFloat(KEY_SFX, 0.8f);
        initBgmStep = Mathf.Clamp(Mathf.RoundToInt(savedBgm * MAX_STEP), 0, MAX_STEP);
        initSfxStep = Mathf.Clamp(Mathf.RoundToInt(savedSfx * MAX_STEP), 0, MAX_STEP);

        initMuteAll = PlayerPrefs.GetInt(KEY_MUTEALL, 0) == 1;
        initVibration = PlayerPrefs.GetInt(KEY_VIB, 1) == 1;
    }

    void CopyInitToCurrent()
    {
        curBgmStep = initBgmStep;
        curSfxStep = initSfxStep;
        curMuteAll = initMuteAll;
        curVibration = initVibration;

        dirtyBgm = dirtySfx = dirtyMuteAll = dirtyVibration = false;
    }

    void ApplyPreviewVolumesFromCurrent()
    {
        float bgm = curMuteAll ? 0f : (curBgmStep / (float)MAX_STEP);
        float sfx = curMuteAll ? 0f : (curSfxStep / (float)MAX_STEP);
        if (AudioManager.I)
        {
            AudioManager.I.SetBgm(bgm);
            AudioManager.I.SetSfx(sfx);
        }
        ApplyMuteAllVisual(curMuteAll);
    }

    void RefreshAllUI()
    {
        UpdateBgmUI(false);
        UpdateSfxUI(false);
        if (toggleAllSound) toggleAllSound.SetIsOnWithoutNotify(curMuteAll);
        if (toggleVibration) toggleVibration.SetIsOnWithoutNotify(curVibration);
        ApplyMuteAllVisual(curMuteAll);
    }
}

