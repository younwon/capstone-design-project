using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
    // -------- 데이터 구조 (유지보수 쉬움) --------
    [System.Serializable]
    public class SpawnEntry         // 한 종류 묶음(여러 프리팹 중 1개 랜덤)
    {
        public string label;
        public GameObject[] prefabs;
        [Range(0, 100)] public float weight = 0;  // 가중치(% 아님, 합만 맞으면 됨)
    }

    [System.Serializable]
    public class SpawnPhase         // 특정 시각 이후에 활성화될 테이블
    {
        public float startTimeSec;          // 이 시각부터 적용 (0, 30, 60, 90 …)
        public SpawnEntry[] entries;        // 예: Normal 95, Spring 5 …
    }

    [Header("Refs")]
    public GridManager grid;
    public Camera camRef;

    [Header("Phases (예시대로 채워 넣을 것)")]
    public SpawnPhase[] phases;

    [Header("Timing")]
    public float spawnDelayStart = 1.20f;   // 착지 후 대기
    public float spawnDelayMin = 0.90f;   // 최소 대기
    public float accelHeight = 999f;    // (미사용) 카메라 기반 난이도는 꺼둠

    [Header("Drop Speed (시간 경과로 상승)")]
    public float baseDropSpeed = 1.0f;    // 0초 기준
    public float maxDropSpeed = 2.4f;    // 상한
    public float speedStepPeriod = 30f;     // 30초/15초 등
    public float speedStepAdd = 0.20f;   // 단계마다 +0.20

    [Header("Spawn Pos")]
    public float spawnViewportY = 1.06f;    // 화면 상단보다 6% 위에서 생성

    private float startTime;
    private BlockFaller current;

    void Awake()
    {
        if (!grid) grid = GridManager.I ? GridManager.I : FindObjectOfType<GridManager>();
        if (!camRef) camRef = Camera.main;
    }

    void OnEnable()
    {
        BlockFaller.Active = null;
        startTime = Time.time;
        StartCoroutine(Loop());
    }

    IEnumerator Loop()
    {
        while (true)
        {
            // 한 번에 1개만
            if (current == null || current.landed)
            {
                if (current != null)
                    yield return new WaitForSeconds(GetSpawnDelay());

                current = SpawnOne();
            }
            yield return null;
        }
    }

    float GetSpawnDelay()
    {
        // 필요하면 여기서도 시간에 따라 줄이는 로직 넣을 수 있음
        return spawnDelayStart; // 지금은 고정(원하면 Mathf.Lerp 사용)
    }

    float GetDropSpeedNow()
    {
        int steps = Mathf.Max(0, Mathf.FloorToInt((Time.time - startTime) / Mathf.Max(0.001f, speedStepPeriod)));
        float s = baseDropSpeed + steps * speedStepAdd;
        return Mathf.Min(s, maxDropSpeed);
    }

    SpawnPhase CurrentPhase()
    {
        float t = Time.time - startTime;
        SpawnPhase cur = null;
        for (int i = 0; i < (phases?.Length ?? 0); i++)
            if (t >= phases[i].startTimeSec) cur = phases[i]; else break; // startTimeSec이 오름차순이라고 가정
        return cur;
    }

    GameObject PickFromPhase(SpawnPhase phase)
    {
        if (phase == null || phase.entries == null || phase.entries.Length == 0)
            return null;

        float total = 0f;
        foreach (var e in phase.entries) total += Mathf.Max(0f, e.weight);
        if (total <= 0f) return null;

        float r = Random.Range(0f, total);
        foreach (var e in phase.entries)
        {
            float w = Mathf.Max(0f, e.weight);
            if (r < w)
            {
                if (e.prefabs == null || e.prefabs.Length == 0) return null;
                return e.prefabs[Random.Range(0, e.prefabs.Length)];
            }
            r -= w;
        }
        return null;
    }

    BlockFaller SpawnOne()
    {
        // 현재 페이즈에서 프리팹 뽑기
        var prefab = PickFromPhase(CurrentPhase());
        if (!prefab) { Debug.LogWarning("Spawner: 현재 페이즈에 유효한 프리팹이 없습니다."); return null; }

        // 화면 ‘바로 위’에 생성
        Vector3 top = camRef.ViewportToWorldPoint(new Vector3(0.5f, spawnViewportY, 0f));
        int lane0 = Random.Range(0, grid.lanes);
        Vector3 pos = new Vector3(grid.GetLaneCenter(lane0), top.y, 0f);

        var go = Instantiate(prefab, pos, Quaternion.identity);

        var f = go.GetComponent<BlockFaller>();
        if (!f) { Debug.LogWarning("Spawner: BlockFaller 필요"); Destroy(go); return null; }

        f.dropSpeed = GetDropSpeedNow();
        f.SetActive(true);     // 이 블럭만 Q/E + 소프트드롭 입력
        return f;
    }

    [ContextMenu("Spawn Now (Ignore Loop)")]
    public void SpawnNow() { current = SpawnOne(); }
}


