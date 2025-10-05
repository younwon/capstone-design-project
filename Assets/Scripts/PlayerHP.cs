using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHP : MonoBehaviour
{
    // [기존 변수 - 변경 없음]
    public int maxHP = 1;
    public float invulnSeconds = 0f;
    public HeartsUI ui;

    // [피격 효과 변수 - 변경 없음]
    [Header("Hit Effect")]
    public Sprite hitSprite;
    public float hitEffectDuration = 0.5f;

    // [기존 변수 - 변경 없음]
    int hp;
    bool invuln;
    SpriteRenderer sr;

    //  [추가된 변수] 애니메이터를 제어하기 위함
    Animator anim;


    void Awake()
    {
        hp = maxHP;
        sr = GetComponentInChildren<SpriteRenderer>();
        //  [추가된 코드] Animator 컴포넌트를 가져옵니다.
        anim = GetComponentInChildren<Animator>();
        if (ui) ui.Set(hp, maxHP);
    }

    //  [수정된 함수] 이제 이 함수가 '애니메이터 On/Off' 코루틴을 호출합니다.
    public void TriggerHitReaction()
    {
        // 이전에 실행중인 효과가 있다면 중지하고 새로 시작합니다.
        StopCoroutine("ShowHitEffectByDisablingAnimator");
        StartCoroutine(ShowHitEffectByDisablingAnimator());
    }

    //  [수정된 함수] '애니메이터 On/Off' 방식의 피격 효과 코루틴
    private IEnumerator ShowHitEffectByDisablingAnimator()
    {
        // 애니메이터와 스프라이트가 모두 있을 때만 실행
        if (anim && sr && hitSprite)
        {
            // 1. 애니메이터를 끕니다.
            anim.enabled = false;
            // 2. 스프라이트를 hitSprite로 직접 변경합니다.
            sr.sprite = hitSprite;

            // 3. 설정된 시간만큼 기다립니다.
            yield return new WaitForSeconds(hitEffectDuration);

            // 4. 플레이어가 아직 죽지 않았다면 애니메이터를 다시 켭니다.
            if (hp > 0)
            {
                anim.enabled = true;
            }
            // 참고: 애니메이터가 켜지면 자동으로 현재 상태(예: 공중)에 맞는 스프라이트로 돌아갑니다.
        }
    }




    public void Damage(int amount = 1)
    {
        if (invuln || hp <= 0) return;
        hp = Mathf.Max(0, hp - amount);
        if (ui) ui.Set(hp, maxHP);

        // 실제 데미지를 입었을 때도 동일한 피격 효과를 호출합니다.
        TriggerHitReaction();

        if (hp <= 0)
        {
            var death = GetComponent<PlayerDeath>();
            if (death) death.Die("HP 0");
            else SimpleGameOver.TryGameOver("HP 0 (PlayerDeath 없음)");
            return;
        }

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
            // 애니메이터가 꺼져있을 수 있으므로 깜빡임 효과는 작동하지 않을 수 있습니다.
            if (sr && anim.enabled) sr.enabled = ((int)(t * 10f) % 2) == 0;
            yield return null;
        }
        if (sr) sr.enabled = true;
        invuln = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) Damage(1);
    }
}


