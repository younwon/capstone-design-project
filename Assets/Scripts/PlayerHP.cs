using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHP : MonoBehaviour
{
    public int maxHP = 1;               // 원코인: 1로 설정(필요시 인스펙터에서 바꿔도 됨)
    public float invulnSeconds = 0f;    // 원코인에선 무적 의미 X → 0 권장
    public HeartsUI ui;                 // HeartsUI는 사용 안 하면 비워둬도 됨

    int hp;
    bool invuln;
    SpriteRenderer sr;

    void Awake()
    {
        hp = maxHP;
        sr = GetComponentInChildren<SpriteRenderer>();
        if (ui) ui.Set(hp, maxHP);
    }

    public void Damage(int amount = 1)
    {
        if (invuln || hp <= 0) return;

        hp = Mathf.Max(0, hp - amount);
        if (ui) ui.Set(hp, maxHP);

        if (hp <= 0)
        {
            var death = GetComponent<PlayerDeath>();
            if (death) death.Die("HP 0");
            else SimpleGameOver.TryGameOver("HP 0 (PlayerDeath 없음)");

            return;
        }



        // ✅ 원코인 모드에선 무적 코루틴 생략
        if (invulnSeconds > 0f && maxHP > 1)
            StartCoroutine(IEInvuln());
    }

    IEnumerator IEInvuln()
    {
        invuln = true;
        float t = 0f;
        while (t < invulnSeconds)
        {
            t += Time.deltaTime;
            if (sr) sr.enabled = ((int)(t * 10f) % 2) == 0; // 깜빡임
            yield return null;
        }
        if (sr) sr.enabled = true;
        invuln = false;
    }

    // 테스트용
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) Damage(1);
    }
}



