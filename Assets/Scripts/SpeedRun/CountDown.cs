using TMPro;
using UnityEngine;

public class CountDown : MonoBehaviour
{
    [Header("Refs")]
    public TextMeshProUGUI timerText;                 // 상단 타이머 텍스트
    public PlayerDeath playerClassic;                 // (옵션) 클래식용 PlayerDeath
    public PlayerDeath_SpeedRun speedRunDeath;        // (옵션) 스피드런 전용 PlayerDeath

    [Header("Best Record UI (옵션)")]
    public TextMeshProUGUI bestValueText;             // "MAX 000 M" 같은 표시 (있으면 갱신)

    [Header("Time")]
    [Tooltip("제한 시간(초). 타이틀/런 시작시 이 값으로 복원됩니다.")]
    [SerializeField] float defaultSeconds = 60f;

    // 외부에서 읽기 좋은 프로퍼티
    public float InitialSeconds => Mathf.Max(0f, defaultSeconds);
    public float ElapsedSeconds => Mathf.Clamp(InitialSeconds - timeRemaining, 0f, InitialSeconds);

    [HideInInspector] public float timeRemaining;     // 현재 남은 시간
    bool countdownStarted = false;                    // 실제로 플레이어가 움직여서 카운트다운이 시작됐는가
    bool countdownEnded = false;                      // 0에 도달했는가

    // ================================================================
    //  SpeedRun 10초 이하 경고 SFX (Loop + 서서히 가속 + SFX볼륨 연동)
    // ================================================================
    [Header("SFX (SpeedRun)")]
    [Tooltip("스피드런에서 10초 이하부터 재생되는 째깍째깍 경고음")]
    [SerializeField] AudioClip sfxTickTock;

    [Tooltip("10초에서의 피치(기본 속도)")]
    [SerializeField] float normalPitch = 1.0f;

    [Tooltip("0초에 가까워질 때의 피치(최대 속도)")]
    [SerializeField] float fastPitch = 1.6f;

    [Tooltip("피치가 바뀔 때 부드럽게 보간하는 속도")]
    [SerializeField] float pitchLerpSpeed = 8f;

    AudioSource tickTockSource;
    bool tickTockActive = false;
    // ================================================================

    void Awake()
    {
        // 레퍼런스 자동 탐색(비어 있을 때만)
        if (!playerClassic) playerClassic = FindObjectOfType<PlayerDeath>(true);
        if (!speedRunDeath) speedRunDeath = FindObjectOfType<PlayerDeath_SpeedRun>(true);

        // 런타임 상태 초기화
        timeRemaining = InitialSeconds;
        countdownStarted = false;
        countdownEnded = false;

        // 전용 경고음 소스 준비
        if (sfxTickTock != null)
        {
            tickTockSource = gameObject.AddComponent<AudioSource>();
            tickTockSource.playOnAwake = false;
            tickTockSource.loop = true;
            tickTockSource.clip = sfxTickTock;
            tickTockSource.volume = 1f;
            tickTockSource.spatialBlend = 0f;
            tickTockSource.pitch = normalPitch;

            // AudioMixer를 쓰는 프로젝트면 SFX 그룹 연결(설정창 dB 반영)
            if (AudioManager.I && AudioManager.I.SfxGroup)
                tickTockSource.outputAudioMixerGroup = AudioManager.I.SfxGroup;
        }

        tickTockActive = false;
    }

    void Start()
    {
        UpdateBestRecordUI();

        if (timerText)
        {
            timerText.color = Color.white;
            timerText.text = timeRemaining.ToString("F2");
        }
    }

    void Update()
    {
        // 게임이 아직 시작되지 않았다면 아무 것도 하지 않음(타이틀 상태)
        if (!StartGate.GameStarted) return;

        // 첫 입력(이동/점프) 시 카운트다운 시작
        if (!countdownStarted && PlayerStartedMoving())
            countdownStarted = true;

        if (!countdownStarted || countdownEnded) return;

        // 남은 시간 감소
        timeRemaining -= Time.deltaTime;
        if (timeRemaining < 0f) timeRemaining = 0f;

        // UI 갱신
        if (timerText)
        {
            timerText.text = timeRemaining.ToString("F2");
            if (timeRemaining < 10f) timerText.color = Color.red;
        }

        // ================================================================
        // 10초 이하 경고음 (스피드런에서만)
        //  - 10초 이하 진입: 루프 Play
        //  - 10초→0초로 갈수록 pitch가 서서히 증가
        //  - SFX 설정값을 직접 곱해서(믹서 없어도) 반영
        // ================================================================
        if (ScoreSystem_Speed.I != null && tickTockSource != null)
        {
            bool under10 = (timeRemaining < 10f && timeRemaining > 0f);

            if (under10)
            {
                if (!tickTockActive)
                {
                    tickTockActive = true;
                    tickTockSource.pitch = normalPitch;

                    if (!tickTockSource.isPlaying)
                        tickTockSource.Play();
                }

                // ★ CHANGED: 10초->0초 사이에서 서서히 빨라지도록 pitch 보간
                float kPitch = Mathf.InverseLerp(10f, 0f, timeRemaining); // 10초=0, 0초=1
                float targetPitch = Mathf.Lerp(normalPitch, fastPitch, kPitch);

                tickTockSource.pitch = Mathf.Lerp(
                    tickTockSource.pitch,
                    targetPitch,
                    Time.unscaledDeltaTime * pitchLerpSpeed
                );

                // SFX 볼륨 설정 영향(믹서 없어도 적용되게)
                tickTockSource.volume = GetCurrentSfxMix01();
            }
            else
            {
                if (tickTockActive)
                {
                    tickTockActive = false;
                    if (tickTockSource.isPlaying)
                        tickTockSource.Stop();

                    tickTockSource.pitch = normalPitch; // 다음 진입 대비 리셋
                }
            }
        }
        // ================================================================

        // 0이 되면 타임업 처리
        if (timeRemaining <= 0f)
        {
            countdownEnded = true;

            // 0초에서 무조건 경고음 종료
            StopTickTockImmediate();

            // 스피드런: 표시 스코어 프리즈 + 기록 확정
            if (ScoreSystem_Speed.I) ScoreSystem_Speed.I.FreezeScoreAndCommit();

            // 스피드런 전용 죽음 연출이 있으면 그걸 먼저 사용
            if (speedRunDeath != null)
            {
                speedRunDeath.Die("TimeUp");
            }
            else
            {
                // 백업 경로: 클래식용 PlayerDeath 또는 즉시 GameOver
                if (playerClassic) playerClassic.Die("Countdown Ended");
                else SimpleGameOver.TryGameOver("Countdown Ended");
            }
        }
    }

    // -------- 타이틀/런 진입시 안전 초기화 --------
    public void PrepareForTitle()
    {
        timeRemaining = InitialSeconds;
        countdownStarted = false;
        countdownEnded = false;

        if (timerText)
        {
            timerText.color = Color.white;
            timerText.text = timeRemaining.ToString("F2");
        }

        StopTickTockImmediate();
    }

    public void PrepareForRun()
    {
        timeRemaining = InitialSeconds;
        countdownStarted = false;   // 움직이면 true로 전환
        countdownEnded = false;

        if (timerText)
        {
            timerText.color = Color.white;
            timerText.text = timeRemaining.ToString("F2");
        }

        StopTickTockImmediate();
    }

    // 경고음 즉시 종료 유틸
    void StopTickTockImmediate()
    {
        tickTockActive = false;
        if (tickTockSource != null)
        {
            if (tickTockSource.isPlaying)
                tickTockSource.Stop();

            tickTockSource.pitch = normalPitch;
        }
    }

    // 현재 설정된 SFX 볼륨(0~1) 가져오기
    float GetCurrentSfxMix01()
    {
        if (AudioManager.I == null) return 1f;
        float m = Mathf.Clamp01(AudioManager.I.masterVolume);
        float s = Mathf.Clamp01(AudioManager.I.sfxVolume);
        return m * s;
    }

    // -------- 보조 --------
    bool PlayerStartedMoving()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool jump = Input.GetKeyDown(KeyCode.Space);
        return Mathf.Abs(horizontal) > 0f || Mathf.Abs(vertical) > 0f || jump;
    }

    void UpdateBestRecordUI()
    {
        if (!bestValueText) return;

        if (ScoreSystem_Speed.I != null)
        {
            bestValueText.text = "MAX " + $"{ScoreSystem_Speed.I.BestEverScore:0} M";
        }
        else if (ScoreSystem.I != null)
        {
            bestValueText.text = "MAX " + $"{ScoreSystem.I.BestEverScore:0} M";
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying && timerText)
            timerText.text = Mathf.Max(0f, defaultSeconds).ToString("F2");
    }
#endif
}



