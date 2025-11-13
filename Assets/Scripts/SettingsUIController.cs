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

    [Header("Preview on Step Change")]
    [Tooltip("단계 변경 시 미리듣기 할 짧은 클립(버튼 딸깍/띵 같은 소리 권장)")]
    [SerializeField] AudioClip previewClip;
    [Tooltip("프리뷰 출력용 AudioMixerGroup(SFX 그룹 권장)")]
    [SerializeField] UnityEngine.Audio.AudioMixerGroup previewOutputGroup;
    [Tooltip("마스터 볼륨을 프리뷰 볼륨에도 반영")]
    [SerializeField] bool respectMasterForPreview = true;
    [Tooltip("프리뷰 피치 랜덤")]
    [SerializeField] Vector2 previewRandomPitch = new Vector2(0.98f, 1.02f);

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

    AudioSource _previewSrc; // 프리뷰 전용

    // ---------------- Lifecycle ----------------
    void Start()
    {
        // 리스너 연결
        if (btnBgmMinus) btnBgmMinus.onClick.AddListener(() => ChangeBgm(-1, playPreview: true));
        if (btnBgmPlus) btnBgmPlus.onClick.AddListener(() => ChangeBgm(+1, playPreview: true));
        if (btnSfxMinus) btnSfxMinus.onClick.AddListener(() => ChangeSfx(-1, playPreview: true));
        if (btnSfxPlus) btnSfxPlus.onClick.AddListener(() => ChangeSfx(+1, playPreview: true));

        if (btnCancel) btnCancel.onClick.AddListener(OnCancel);
        if (btnSave) btnSave.onClick.AddListener(OnSave);

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
    void ChangeBgm(int delta, bool playPreview)
    {
        int before = curBgmStep;
        curBgmStep = Mathf.Clamp(curBgmStep + delta, 0, MAX_STEP);
        dirtyBgm = (curBgmStep != initBgmStep);

        if (curBgmStep != before)
        {
            UpdateBgmUI(true);
            ApplyLiveVolumes();

            if (playPreview) PlayPreview(forBgm: true);
        }
    }

    void ChangeSfx(int delta, bool playPreview)
    {
        int before = curSfxStep;
        curSfxStep = Mathf.Clamp(curSfxStep + delta, 0, MAX_STEP);
        dirtySfx = (curSfxStep != initSfxStep);

        if (curSfxStep != before)
        {
            UpdateSfxUI(true);
            ApplyLiveVolumes();

            if (playPreview) PlayPreview(forBgm: false);
        }
    }

    float StepTo01(int step) => Mathf.Clamp01(step / (float)MAX_STEP);

    void UpdateBgmUI(bool animate)
    {
        if (txtBgmValue) txtBgmValue.text = curBgmStep.ToString(); // 0~5
        if (bgBgmValueBox) bgBgmValueBox.sprite = dirtyBgm ? pendingSprite : normalSprite;
    }

    void UpdateSfxUI(bool animate)
    {
        if (txtSfxValue) txtSfxValue.text = curSfxStep.ToString(); // 0~5
        if (bgSfxValueBox) bgSfxValueBox.sprite = dirtySfx ? pendingSprite : normalSprite;
    }

    // ---------------- 라이브 적용(미리듣기 겸용) ----------------
    void ApplyLiveVolumes()
    {
        float bgm = curMuteAll ? 0f : StepTo01(curBgmStep);
        float sfx = curMuteAll ? 0f : StepTo01(curSfxStep);

        // Mixer 우선, 없으면 AudioManager 폴백
        if (MixerVolumeController.I)
        {
            MixerVolumeController.I.SetBgm(bgm);
            MixerVolumeController.I.SetSfx(sfx);
        }
        else if (AudioManager.I)
        {
            AudioManager.I.SetBgm(bgm);
            AudioManager.I.SetSfx(sfx);
        }
    }

    void EnsurePreviewSource()
    {
        if (_previewSrc) return;
        _previewSrc = gameObject.AddComponent<AudioSource>();
        _previewSrc.playOnAwake = false;
        _previewSrc.loop = false;
        _previewSrc.spatialBlend = 0f;
        if (previewOutputGroup) _previewSrc.outputAudioMixerGroup = previewOutputGroup;
        _previewSrc.volume = 1f; // 최종 볼륨은 PlayOneShot volumeScale로 제어
    }

    void PlayPreview(bool forBgm)
    {
        if (previewClip == null) return;

        // 단계값 기반 프리뷰 볼륨 계산
        float master = 1f, bgm = StepTo01(curBgmStep), sfx = StepTo01(curSfxStep);

        if (MixerVolumeController.I)
        {
            master = MixerVolumeController.I.GetMaster();
            bgm = MixerVolumeController.I.GetBgm();
            sfx = MixerVolumeController.I.GetSfx();
        }
        else if (AudioManager.I)
        {
            // AudioManager에 마스터 개념이 별도로 없다면 master=1로 둠
            bgm = StepTo01(curBgmStep);
            sfx = StepTo01(curSfxStep);
        }

        float vol = forBgm ? bgm : sfx;
        if (respectMasterForPreview) vol *= master;
        vol = Mathf.Clamp01(vol);
        if (curMuteAll) vol = 0f;

        if (vol <= 0f) return;

        EnsurePreviewSource();
        float pitch = (previewRandomPitch.x <= 0f && previewRandomPitch.y <= 0f)
            ? 1f
            : Random.Range(Mathf.Min(previewRandomPitch.x, previewRandomPitch.y),
                           Mathf.Max(previewRandomPitch.x, previewRandomPitch.y));
        _previewSrc.pitch = pitch;
        _previewSrc.PlayOneShot(previewClip, vol);
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
        }
        else
        {
            curBgmStep = Mathf.Clamp(prevBgmStep, 0, MAX_STEP);
            curSfxStep = Mathf.Clamp(prevSfxStep, 0, MAX_STEP);
            dirtyBgm = (curBgmStep != initBgmStep);
            dirtySfx = (curSfxStep != initSfxStep);
            UpdateBgmUI(false);
            UpdateSfxUI(false);
        }

        ApplyMuteAllVisual(curMuteAll);
        ApplyLiveVolumes(); // 즉시 적용
        if (!on) PlayPreview(forBgm: false); // 음소거 해제 시 살짝 프리뷰
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
            if (MixerVolumeController.I)
            {
                MixerVolumeController.I.SetBgm(0f);
                MixerVolumeController.I.SetSfx(0f);
            }
            else if (AudioManager.I)
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
            float bgm = StepTo01(curBgmStep);
            float sfx = StepTo01(curSfxStep);

            if (MixerVolumeController.I)
            {
                MixerVolumeController.I.SetBgm(bgm);
                MixerVolumeController.I.SetSfx(sfx);
            }
            else if (AudioManager.I)
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
        ApplyLiveVolumes(); // 현재 단계값을 즉시 반영
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
