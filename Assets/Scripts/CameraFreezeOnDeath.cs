using UnityEngine;
using System.Collections;

public class CameraFreezeOnDeath : MonoBehaviour
{
    [Header("Follow component (optional)")]
    [Tooltip("카메라가 플레이어를 따라가게 하는 스크립트가 있으면 넣어주세요")]
    public MonoBehaviour followComponent;

    [Header("Freeze Policy")]
    public bool freezeByDelay = true;        // true면 시간으로, false면 화면 아래로 벗어날 때
    public float freezeDelayRealtime = 0.7f; // 죽은 뒤 이 시간만큼은 계속 따라감
    public float screenBottomMargin = 0.2f;  // 화면 아래에서 이 만큼 더 내려갔을 때 고정

    bool frozen = false;
    Vector3 frozenPos;
    Camera cam;
    Transform dyingPlayer;

    void Awake() => cam = GetComponent<Camera>();

    void OnEnable() { PlayerDeath.OnPlayerDied += OnPlayerDied; }
    void OnDisable() { PlayerDeath.OnPlayerDied -= OnPlayerDied; }

    void OnPlayerDied(Transform player)
    {
        dyingPlayer = player;
        if (freezeByDelay)
            StartCoroutine(IEFreezeAfterDelay());
        else
            StartCoroutine(IEFreezeWhenBelowScreen());
    }

    IEnumerator IEFreezeAfterDelay()
    {
        yield return new WaitForSecondsRealtime(freezeDelayRealtime);
        FreezeNow();
    }

    IEnumerator IEFreezeWhenBelowScreen()
    {
        // 플레이어가 화면 아래로 사라질 때까지 기다렸다가 고정
        while (true)
        {
            if (dyingPlayer == null) break;
            // 카메라 화면의 실제 바닥 y
            float bottomY = transform.position.y - cam.orthographicSize;
            if (dyingPlayer.position.y < bottomY - screenBottomMargin)
                break;
            yield return null;
        }
        FreezeNow();
    }

    void FreezeNow()
    {
        if (followComponent) followComponent.enabled = false;
        frozenPos = transform.position;
        frozen = true;
    }

    void LateUpdate()
    {
        if (frozen) transform.position = frozenPos;
    }
}

