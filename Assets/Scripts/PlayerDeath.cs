using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDeath : MonoBehaviour
{
    [Header("Visual")]
    public Sprite deadSprite;           // 죽은 스프라이트

    [Header("Motion")]
    public float bounceForce = 7f;      // 위로 톡 튐 (1회)
    public float gravityOnDeath = 2.2f; // 낙하 중력

    [Header("Timing")]
    public float delayBeforeUI = 1.2f;  // UI 띄우기까지 대기

    // ⬇⬇⬇ 변경: Vector3 → Transform
    public static System.Action<Transform> OnPlayerDied;

    public bool IsDead { get; private set; }

    Rigidbody2D rb;
    SpriteRenderer sr;
    Animator anim;
    Collider2D[] colls;
    MonoBehaviour[] controllers;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
        colls = GetComponentsInChildren<Collider2D>(true);

        controllers = new MonoBehaviour[]
        {
            GetComponent<PlayerController2D>(),
            // 다른 조작 스크립트 있으면 여기에 추가
        };
    }

    public void Die(string reason = "Lava")
    {
        if (IsDead) return;
        IsDead = true;
        StartCoroutine(DieRoutine(reason));
    }

    IEnumerator DieRoutine(string reason)
    {
        // 1) 입력/애니메이션 끄기
        foreach (var c in controllers) if (c) c.enabled = false;
        if (anim) anim.enabled = false;

        // 2) 스프라이트 교체
        if (deadSprite && sr) sr.sprite = deadSprite;

        // 3) 물리 설정: X 이동 금지 + 회전 금지
        rb.simulated = true;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = gravityOnDeath;
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;

        // 4) 지형 충돌 끄기
        foreach (var c in colls) c.enabled = false;

        // 5) ‘앗 뜨거!’ 위로 한 번만 톡 튀기
        rb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);

        // ⬇⬇⬇ 변경: 카메라에 Transform 전달
        OnPlayerDied?.Invoke(transform);

        // 6) 잠시 후 게임오버 처리
        yield return new WaitForSeconds(delayBeforeUI);

        if (ScoreSystem.I) ScoreSystem.I.CommitBestIfHigher();
        SimpleGameOver.TryGameOver(reason);
    }
}



