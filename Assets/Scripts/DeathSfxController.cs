using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

[AddComponentMenu("Audio/Death SFX Controller")]
public class DeathSfxController : MonoBehaviour
{
    [Header("Clips")]
    [SerializeField] private AudioClip lavaHitClip;   // A: 용암 접촉 순간
    [SerializeField] private AudioClip fallStartClip; // B: 위로 튄 뒤 하강 시작할 때

    [Header("Routing (optional)")]
    [SerializeField] private AudioMixerGroup sfxOutput; // SFX 그룹 있으면 연결

    [Header("Options")]
    [Tooltip("하강으로 판정하기 전, 최소 상승 지속 시간(초). 너무 민감하게 트리거되는 걸 방지")]
    [SerializeField, Range(0f, 0.5f)] private float minUpTime = 0.05f;
    [Tooltip("하강 시작 판정 시 y속도가 이 값 이하일 때로 제한(안전용)")]
    [SerializeField] private float fallStartVyThreshold = -0.01f;
    [Tooltip("두 사운드의 상대 볼륨")]
    [SerializeField, Range(0f, 1f)] private float lavaHitVol = 1f;
    [SerializeField, Range(0f, 1f)] private float fallStartVol = 1f;

    AudioSource _src;
    Coroutine _waitCo;

    void Awake()
    {
        _src = gameObject.AddComponent<AudioSource>();
        _src.playOnAwake = false;
        _src.loop = false;
        _src.spatialBlend = 0f;
        if (sfxOutput) _src.outputAudioMixerGroup = sfxOutput;
        _src.volume = 1f; // 최종 음량은 PlayOneShot의 volumeScale로 제어
    }

    // A: 용암 닿는 순간 호출
    public void OnLavaHit()
    {
        if (lavaHitClip) _src.PlayOneShot(lavaHitClip, lavaHitVol);
    }

    // B: 위로 튄 직후 호출(플레이어가 bounceForce로 상승 시작한 시점)
    public void OnDeathBounced(Rigidbody2D rb)
    {
        if (!fallStartClip || rb == null) return;

        // 중복 대기 방지
        if (_waitCo != null) StopCoroutine(_waitCo);
        _waitCo = StartCoroutine(CoWaitFallThenPlay(rb));
    }

    IEnumerator CoWaitFallThenPlay(Rigidbody2D rb)
    {
        // 1) 최소 상승 시간 보장
        float t = 0f;
        while (t < minUpTime)
        {
            t += Time.unscaledDeltaTime; // 일시정지 무관하게
            yield return null;
        }

        // 2) y속도가 0을 지나 하강(fallStartVyThreshold 이하)이 되는 순간까지 대기
        //    Rigidbody2D가 비활성/파괴되면 중단
        while (rb && rb.velocity.y > fallStartVyThreshold)
        {
            yield return null;
        }

        if (rb && fallStartClip)
            _src.PlayOneShot(fallStartClip, fallStartVol);

        _waitCo = null;
    }
}
