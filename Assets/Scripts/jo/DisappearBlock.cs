using UnityEngine;
using System.Collections;

public class DisappearBlock : MonoBehaviour
{


    public float delayBeforeBlink = 1f;
    public float blinkDuration = 1f;
    public float blinkSpeed = 0.15f;
    public float respawnTime = 2f;

    private SpriteRenderer sr;
    private Collider2D col;

    public bool isVisible = true;
    private bool activated = false;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!activated && collision.collider.CompareTag("Player"))
        {
            activated = true;
            StartCoroutine(DisappearRoutine());
        }
    }

    IEnumerator DisappearRoutine()
    {
        yield return new WaitForSeconds(delayBeforeBlink);

        float timer = 0f;
        while (timer < blinkDuration)
        {
            sr.enabled = !sr.enabled;
            timer += blinkSpeed;
            yield return new WaitForSeconds(blinkSpeed);
        }

        // 사라짐
        sr.enabled = false;
        col.enabled = false;
        isVisible = false;

        yield return new WaitForSeconds(respawnTime);

        // 점선 상태로 생성
        sr.enabled = true;
        col.enabled = false;
        isVisible = true;
        activated = false;
    }

    void LateUpdate()
    {
        // ★ 보이지 않는 상태라면 collider를 어떤 스크립트가 켜도 강제로 꺼버림
        if (!isVisible && col.enabled)
            col.enabled = false;


    }

   
}
