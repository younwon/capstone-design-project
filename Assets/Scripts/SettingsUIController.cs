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

    void Start()
    {
        float savedBgm = PlayerPrefs.GetFloat(KEY_BGM, 0.8f);
        float savedSfx = PlayerPrefs.GetFloat(KEY_SFX, 0.8f);
        initBgmStep = Mathf.RoundToInt(savedBgm * MAX_STEP);
        initSfxStep = Mathf.RoundToInt(savedSfx * MAX_STEP);
        curBgmStep = initBgmStep;
        curSfxStep = initSfxStep;

        prevBgmStep = curBgmStep;
        prevSfxStep = curSfxStep;

        initMuteAll = PlayerPrefs.GetInt(KEY_MUTEALL, 0) == 1;
        initVibration = PlayerPrefs.GetInt(KEY_VIB, 1) == 1;
        curMuteAll = initMuteAll;
        curVibration = initVibration;

        UpdateBgmUI(false);
        UpdateSfxUI(false);
        if (toggleAllSound) toggleAllSound.SetIsOnWithoutNotify(curMuteAll);
        if (toggleVibration) toggleVibration.SetIsOnWithoutNotify(curVibration);
        ApplyMuteAllVisual(curMuteAll);

        // 행 버튼
        btnBgmMinus.onClick.AddListener(() => ChangeBgm(-1));
        btnBgmPlus.onClick.AddListener(() => ChangeBgm(+1));
        btnSfxMinus.onClick.AddListener(() => ChangeSfx(-1));
        btnSfxPlus.onClick.AddListener(() => ChangeSfx(+1));

        // 하단 버튼
        btnCancel.onClick.AddListener(OnCancel); // 되돌리기(창은 그대로)
        btnSave.onClick.AddListener(OnSave);   // 저장만

        // 체크박스
        if (toggleAllSound) toggleAllSound.onValueChanged.AddListener(OnToggleAllSound);
        if (toggleVibration) toggleVibration.onValueChanged.AddListener(OnToggleVibration);

        // 닫기(X) & 팝업
        if (btnCloseX) btnCloseX.onClick.AddListener(TryClose);
        if (btnPopupDiscard) btnPopupDiscard.onClick.AddListener(ExitWithoutSave);
        if (btnPopupSaveAndExit) btnPopupSaveAndExit.onClick.AddListener(SaveAndExit);

        if (popupUnsaved) popupUnsaved.SetActive(false); // 초기엔 숨김
    }

    // ----- BGM / SFX -----
    void ChangeBgm(int delta)
    {
        int before = curBgmStep;
        curBgmStep = Mathf.Clamp(curBgmStep + delta, 0, MAX_STEP);
        dirtyBgm = (curBgmStep != initBgmStep);
        if (curBgmStep != before) UpdateBgmUI(true);
    }

    void ChangeSfx(int delta)
    {
        int before = curSfxStep;
        curSfxStep = Mathf.Clamp(curSfxStep + delta, 0, MAX_STEP);
        dirtySfx = (curSfxStep != initSfxStep);
        if (curSfxStep != before) UpdateSfxUI(true);
    }

    void UpdateBgmUI(bool animate)
    {
        txtBgmValue.text = curBgmStep.ToString();
        if (bgBgmValueBox) bgBgmValueBox.sprite = dirtyBgm ? pendingSprite : normalSprite;
    }

    void UpdateSfxUI(bool animate)
    {
        txtSfxValue.text = curSfxStep.ToString();
        if (bgSfxValueBox) bgSfxValueBox.sprite = dirtySfx ? pendingSprite : normalSprite;
    }

    // ----- ALL MUTE -----
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

            AudioManager.I.SetBgm(0f);
            AudioManager.I.SetSfx(0f);
        }
        else
        {
            curBgmStep = Mathf.Clamp(prevBgmStep, 0, MAX_STEP);
            curSfxStep = Mathf.Clamp(prevSfxStep, 0, MAX_STEP);
            dirtyBgm = (curBgmStep != initBgmStep);
            dirtySfx = (curSfxStep != initSfxStep);
            UpdateBgmUI(false);
            UpdateSfxUI(false);

            AudioManager.I.SetBgm(curBgmStep / (float)MAX_STEP);
            AudioManager.I.SetSfx(curSfxStep / (float)MAX_STEP);
        }

        ApplyMuteAllVisual(curMuteAll);
    }

    void ApplyMuteAllVisual(bool muted)
    {
        btnBgmMinus.interactable = !muted;
        btnBgmPlus.interactable = !muted;
        btnSfxMinus.interactable = !muted;
        btnSfxPlus.interactable = !muted;
    }

    // ----- VIBRATION -----
    void OnToggleVibration(bool on)
    {
        curVibration = on;
        dirtyVibration = (curVibration != initVibration);
    }

    // ----- SAVE / CANCEL (창은 닫지 않음) -----
    void OnSave()
    {
        if (curMuteAll)
        {
            AudioManager.I.SetBgm(0f);
            AudioManager.I.SetSfx(0f);
            PlayerPrefs.SetInt(KEY_MUTEALL, 1);
            PlayerPrefs.SetFloat(KEY_BGM, 0f);
            PlayerPrefs.SetFloat(KEY_SFX, 0f);
        }
        else
        {
            float bgm = curBgmStep / (float)MAX_STEP;
            float sfx = curSfxStep / (float)MAX_STEP;
            AudioManager.I.SetBgm(bgm);
            AudioManager.I.SetSfx(sfx);
            PlayerPrefs.SetInt(KEY_MUTEALL, 0);
            PlayerPrefs.SetFloat(KEY_BGM, bgm);
            PlayerPrefs.SetFloat(KEY_SFX, sfx);
        }

        PlayerPrefs.SetInt(KEY_VIB, curVibration ? 1 : 0);
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
        curBgmStep = initBgmStep; dirtyBgm = false;
        curSfxStep = initSfxStep; dirtySfx = false;
        curMuteAll = initMuteAll; dirtyMuteAll = false;
        curVibration = initVibration; dirtyVibration = false;

        if (toggleAllSound) toggleAllSound.SetIsOnWithoutNotify(curMuteAll);
        if (toggleVibration) toggleVibration.SetIsOnWithoutNotify(curVibration);

        UpdateBgmUI(false);
        UpdateSfxUI(false);
        ApplyMuteAllVisual(curMuteAll);
    }

    // ----- 닫기/팝업 -----
    void TryClose()  // X 버튼에서 호출
    {
        if (HasDirty()) OpenPopup();
        else CloseSettings();
    }

    bool HasDirty()
    {
        return dirtyBgm || dirtySfx || dirtyMuteAll || dirtyVibration;
    }

    void OpenPopup()
    {
        if (popupUnsaved) popupUnsaved.SetActive(true);
    }

    void ClosePopup()
    {
        if (popupUnsaved) popupUnsaved.SetActive(false);
    }

    void ExitWithoutSave() // 팝업: 저장 안 하고 닫기
    {
        ClosePopup();
        CloseSettings();
    }

    void SaveAndExit()     // 팝업: 저장하고 닫기
    {
        OnSave();
        ClosePopup();
        CloseSettings();
    }

    void CloseSettings()
    {
        if (settingsPanel) settingsPanel.SetActive(false); // PauseWhenOpen이 timeScale 복원해줄 것
    }
}


