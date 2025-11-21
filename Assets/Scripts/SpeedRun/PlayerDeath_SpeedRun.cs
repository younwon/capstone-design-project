using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDeath_SpeedRun : MonoBehaviour
{
    [Header("Visual")]
    public Sprite deadSprite;

    [Header("Motion")]
    public float bounceForce = 7f;
    public float gravityOnDeath = 2.2f;

    [Header("Timing")]
    public float delayBeforeUI = 1.2f;

    public static System.Action<Transform> OnPlayerDied;
    public bool IsDead { get; private set; }

    Rigidbody2D rb;
    SpriteRenderer sr;
    Animator anim;
    Collider2D[] colls;
    MonoBehaviour[] controllers;

    [Header("SFX")]
    public AudioClip deathClip;
    public bool fadeAllOnDeath = true;
    public float fadeTimeOnDeath = 1.5f;

    // ─────────────────────────────
    // Camera Freeze (죽을 때만 추적 OFF)
    // ─────────────────────────────
    [Header("Camera Freeze")]
    [Tooltip("플레이어를 따라다니는 카메라 스크립트를 Drag&Drop (예: CameraFollow, Cinemachine 카메라 등)")]
    public Behaviour cameraFollowComponent;   // 추적 스크립트
    bool cameraFrozen = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
        colls = GetComponentsInChildren<Collider2D>(true);

        controllers = new MonoBehaviour[]
        {
            GetComponent<PlayerController2D>(),
        };

        // (선택) 자동 탐색 시도: 메인 카메라에 붙은 "Follow" 이름 스크립트
        if (!cameraFollowComponent && Camera.main)
        {
            // 필요하면 여기서 프로젝트 스크립트명으로 정확히 GetComponent<YourFollowType>() 해도 됨
            foreach (var b in Camera.main.GetComponents<Behaviour>())
            {
                if (b != null && b.enabled && b.GetType().Name.ToLower().Contains("follow"))
                {
                    cameraFollowComponent = b;
                    break;
                }
            }
        }
    }

    // ▶ 씬이 리로드되면 이 컴포넌트도 다시 Enable됨.
    //    그 때 카메라 추적을 무조건 ON으로 복구해 두면 안전.
    void OnEnable()
    {
        if (cameraFollowComponent) cameraFollowComponent.enabled = true;
        cameraFrozen = false;
        IsDead = false; // 안전 차원 초기화(원래 흐름 유지)
    }

    public void Die(string reason = "TimeUp")
    {
        if (IsDead) return;
        IsDead = true;
        StartCoroutine(DieRoutine(reason));
    }

    IEnumerator DieRoutine(string reason)
    {
        // ▷ 죽는 순간 카메라 추적 끄기 → 화면 고정
        if (cameraFollowComponent && cameraFollowComponent.enabled)
        {
            cameraFollowComponent.enabled = false;
            cameraFrozen = true;
        }

        // ─ 기존 연출 그대로 ─
        foreach (var c in controllers) if (c) c.enabled = false;
        if (anim) anim.enabled = false;

        if (deadSprite && sr) sr.sprite = deadSprite;

        rb.simulated = true;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = gravityOnDeath;
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;

        foreach (var c in colls) c.enabled = false;

        rb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);

        if (AudioManager.I)
        {
            if (deathClip) AudioManager.I.PlaySFX(deathClip);
        }

        OnPlayerDied?.Invoke(transform);

        yield return new WaitForSeconds(delayBeforeUI);

        if (ScoreSystem_Speed.I) ScoreSystem_Speed.I.CommitBestCurrent();
        SimpleGameOver.TryGameOver(reason);
    }
}



