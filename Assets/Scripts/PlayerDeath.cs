// /Scripts/Player/PlayerDeath.cs
// 원본 동작 보존 + 사운드 훅(사망음/전역 페이드) 추가
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDeath : MonoBehaviour
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
    }

    public void Die(string reason = "Lava")
    {
        if (IsDead) return;
        IsDead = true;
        StartCoroutine(DieRoutine(reason));
    }

    IEnumerator DieRoutine(string reason)
    {
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

        if (ScoreSystem.I) ScoreSystem.I.CommitBestIfHigher();
        SimpleGameOver.TryGameOver(reason);
    }
}
