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
    // [변경됨] 기존 Vibration 토글을 Fullscreen 토글로 사용
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

    const int MAX_STEP = 5;
    const string KEY_BGM = "SET_BGM";
    const string KEY_SFX = "SET_SFX";
    const string KEY_MUTEALL = "SET_MUTEALL";
    // [변경됨] 키 이름 변경
    const string KEY_FULLSCREEN = "SET_FULLSCREEN";

    int initBgmStep, initSfxStep;
    int curBgmStep, curSfxStep;
    bool dirtyBgm, dirtySfx;

    bool initMuteAll, curMuteAll, dirtyMuteAll;
    // [변경됨] 진동 변수를 전체화면 변수로 변경
    bool initFullscreen, curFullscreen, dirtyFullscreen;

    int prevBgmStep, prevSfxStep;

    void Start()
    {
        // 게임 시작 시 저장된 해상도 적용 (UI 열기 전에도 적용되어야 함)
        ApplySavedResolutionAtStart();

        btnBgmMinus.onClick.AddListener(() => ChangeBgm(-1));
        btnBgmPlus.onClick.AddListener(() => ChangeBgm(+1));
        btnSfxMinus.onClick.AddListener(() => ChangeSfx(-1));
        btnSfxPlus.onClick.AddListener(() => ChangeSfx(+1));

        btnCancel.onClick.AddListener(OnCancel);
        btnSave.onClick.AddListener(OnSave);

        if (toggleAllSound) toggleAllSound.onValueChanged.AddListener(OnToggleAllSound);

        // [변경됨] 리스너 연결
        if (toggleFullscreen) toggleFullscreen.onValueChanged.AddListener(OnToggleFullscreen);

        if (btnCloseX) btnCloseX.onClick.AddListener(TryClose);
        if (btnPopupDiscard) btnPopupDiscard.onClick.AddListener(ExitWithoutSave);
        if (btnPopupSaveAndExit) btnPopupSaveAndExit.onClick.AddListener(SaveAndExit);

        if (popupUnsaved) popupUnsaved.SetActive(false);
    }

    // [신규 추가] 게임 켜지자마자 저장된 설정대로 화면 맞추기
    void ApplySavedResolutionAtStart()
    {
        // 기본값은 1(전체화면)
        bool isFull = PlayerPrefs.GetInt(KEY_FULLSCREEN, 1) == 1;
        ApplyResolutionLogic(isFull);
    }

    void OnEnable()
    {
        LoadFromPrefsToInit();
        CopyInitToCurrent();

        prevBgmStep = curBgmStep;
        prevSfxStep = curSfxStep;

        RefreshAllUI();
        ApplyPreviewVolumesFromCurrent();
    }

    // ... (BGM, SFX 관련 코드는 동일) ...
    void ChangeBgm(int delta)
    { /* 기존과 동일 */
        int before = curBgmStep;
        curBgmStep = Mathf.Clamp(curBgmStep + delta, 0, MAX_STEP);
        dirtyBgm = (curBgmStep != initBgmStep);
        if (curBgmStep != before) { UpdateBgmUI(true); if (!curMuteAll && AudioManager.I) AudioManager.I.SetBgm(curBgmStep / (float)MAX_STEP); }
    }
    void ChangeSfx(int delta)
    { /* 기존과 동일 */
        int before = curSfxStep;
        curSfxStep = Mathf.Clamp(curSfxStep + delta, 0, MAX_STEP);
        dirtySfx = (curSfxStep != initSfxStep);
        if (curSfxStep != before) { UpdateSfxUI(true); if (!curMuteAll && AudioManager.I) AudioManager.I.SetSfx(curSfxStep / (float)MAX_STEP); }
    }
    void UpdateBgmUI(bool animate) { if (txtBgmValue) txtBgmValue.text = curBgmStep.ToString(); if (bgBgmValueBox) bgBgmValueBox.sprite = dirtyBgm ? pendingSprite : normalSprite; }
    void UpdateSfxUI(bool animate) { if (txtSfxValue) txtSfxValue.text = curSfxStep.ToString(); if (bgSfxValueBox) bgSfxValueBox.sprite = dirtySfx ? pendingSprite : normalSprite; }
    void OnToggleAllSound(bool on)
    { /* 기존과 동일 */
        curMuteAll = on; dirtyMuteAll = (curMuteAll != initMuteAll);
        if (on) { prevBgmStep = curBgmStep; prevSfxStep = curSfxStep; curBgmStep = 0; dirtyBgm = (curBgmStep != initBgmStep); curSfxStep = 0; dirtySfx = (curSfxStep != initSfxStep); UpdateBgmUI(false); UpdateSfxUI(false); if (AudioManager.I) { AudioManager.I.SetBgm(0f); AudioManager.I.SetSfx(0f); } }
        else { curBgmStep = Mathf.Clamp(prevBgmStep, 0, MAX_STEP); curSfxStep = Mathf.Clamp(prevSfxStep, 0, MAX_STEP); dirtyBgm = (curBgmStep != initBgmStep); dirtySfx = (curSfxStep != initSfxStep); UpdateBgmUI(false); UpdateSfxUI(false); if (AudioManager.I) { AudioManager.I.SetBgm(curBgmStep / (float)MAX_STEP); AudioManager.I.SetSfx(curSfxStep / (float)MAX_STEP); } }
        ApplyMuteAllVisual(curMuteAll);
    }
    void ApplyMuteAllVisual(bool muted) { if (btnBgmMinus) btnBgmMinus.interactable = !muted; if (btnBgmPlus) btnBgmPlus.interactable = !muted; if (btnSfxMinus) btnSfxMinus.interactable = !muted; if (btnSfxPlus) btnSfxPlus.interactable = !muted; }

    // ---------------- FULLSCREEN (구 Vibration) ----------------
    void OnToggleFullscreen(bool on)
    {
        curFullscreen = on;
        // 초기값과 다르면 dirty 표시 (아직 적용은 안 함)
        dirtyFullscreen = (curFullscreen != initFullscreen);
    }

    // ---------------- SAVE / CANCEL ----------------
    void OnSave()
    {
        // 1. 사운드 저장 (기존 로직)
        if (curMuteAll)
        {
            if (AudioManager.I) { AudioManager.I.SetBgm(0f); AudioManager.I.SetSfx(0f); }
            PlayerPrefs.SetInt(KEY_MUTEALL, 1); PlayerPrefs.SetFloat(KEY_BGM, 0f); PlayerPrefs.SetFloat(KEY_SFX, 0f);
        }
        else
        {
            float bgm = curBgmStep / (float)MAX_STEP;
            float sfx = curSfxStep / (float)MAX_STEP;
            if (AudioManager.I) { AudioManager.I.SetBgm(bgm); AudioManager.I.SetSfx(sfx); }
            PlayerPrefs.SetInt(KEY_MUTEALL, 0); PlayerPrefs.SetFloat(KEY_BGM, bgm); PlayerPrefs.SetFloat(KEY_SFX, sfx);
        }

        // 2. [변경됨] Fullscreen 저장 및 적용
        // 여기서 실제로 화면 해상도를 변경합니다.
        PlayerPrefs.SetInt(KEY_FULLSCREEN, curFullscreen ? 1 : 0);
        ApplyResolutionLogic(curFullscreen);

        PlayerPrefs.Save();

        // 초기값 갱신
        initBgmStep = curBgmStep; dirtyBgm = false;
        initSfxStep = curSfxStep; dirtySfx = false;
        initMuteAll = curMuteAll; dirtyMuteAll = false;
        initFullscreen = curFullscreen; dirtyFullscreen = false;

        UpdateBgmUI(false);
        UpdateSfxUI(false);
        ApplyMuteAllVisual(curMuteAll);
    }

    // [신규] 실제 해상도 변경 로직 분리
    void ApplyResolutionLogic(bool isFull)
    {
        if (isFull)
        {
            // 모니터 최대 해상도로 꽉 채우기
            Resolution maxRes = Screen.resolutions[Screen.resolutions.Length - 1];
            Screen.SetResolution(maxRes.width, maxRes.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            // 540x960 크기로 강제로 창 모드
            Screen.SetResolution(540, 960, FullScreenMode.Windowed);
        }
    }

    void OnCancel()
    {
        CopyInitToCurrent();
        if (toggleAllSound) toggleAllSound.SetIsOnWithoutNotify(curMuteAll);

        // [변경됨] UI만 되돌리고 화면 모드는 바꾸지 않음 (어차피 Save 안 했으니 그대로)
        if (toggleFullscreen) toggleFullscreen.SetIsOnWithoutNotify(curFullscreen);

        RefreshAllUI();
        ApplyPreviewVolumesFromCurrent();
    }

    // ---------------- 닫기/팝업 ----------------
    void TryClose()
    {
        if (HasDirty()) OpenPopup();
        else CloseSettings();
    }

    // [변경됨] dirtyVibration -> dirtyFullscreen
    bool HasDirty() => dirtyBgm || dirtySfx || dirtyMuteAll || dirtyFullscreen;

    void OpenPopup() { if (popupUnsaved) popupUnsaved.SetActive(true); }
    void ClosePopup() { if (popupUnsaved) popupUnsaved.SetActive(false); }

    void ExitWithoutSave()
    {
        CopyInitToCurrent();
        if (toggleAllSound) toggleAllSound.SetIsOnWithoutNotify(curMuteAll);
        // [변경됨]
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
        // 1. 시간 정지 해제 (필수)
        Time.timeScale = 1f;

        // 2. ★★★ [여기가 핵심] 클릭 잠금 해제 요청 ★★★
        // 타이틀 관리자를 찾아서 "이제 터치 받아도 돼!(ArmStartCatcher)" 라고 알려줍니다.
        var titleController = FindObjectOfType<TitleUIController>();
        if (titleController != null)
        {
            // 만약 이 부분에서 빨간 줄이 뜨면 
            // TitleUIController.cs의 ArmStartCatcher 함수 앞에 'public'을 붙여주세요.
            titleController.ArmStartCatcher();
        }

        // 3. 창 닫기
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

        // [변경됨]
        initFullscreen = PlayerPrefs.GetInt(KEY_FULLSCREEN, 1) == 1; // 기본값 1 (전체화면)
    }

    void CopyInitToCurrent()
    {
        curBgmStep = initBgmStep;
        curSfxStep = initSfxStep;
        curMuteAll = initMuteAll;
        // [변경됨]
        curFullscreen = initFullscreen;

        dirtyBgm = dirtySfx = dirtyMuteAll = dirtyFullscreen = false;
    }

    void ApplyPreviewVolumesFromCurrent()
    { /* 기존과 동일 */
        float bgm = curMuteAll ? 0f : (curBgmStep / (float)MAX_STEP);
        float sfx = curMuteAll ? 0f : (curSfxStep / (float)MAX_STEP);
        if (AudioManager.I) { AudioManager.I.SetBgm(bgm); AudioManager.I.SetSfx(sfx); }
        ApplyMuteAllVisual(curMuteAll);
    }

    void RefreshAllUI()
    {
        UpdateBgmUI(false);
        UpdateSfxUI(false);
        if (toggleAllSound) toggleAllSound.SetIsOnWithoutNotify(curMuteAll);
        // [변경됨]
        if (toggleFullscreen) toggleFullscreen.SetIsOnWithoutNotify(curFullscreen);
        ApplyMuteAllVisual(curMuteAll);
    }
}