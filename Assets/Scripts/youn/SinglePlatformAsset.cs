using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Assets/Create 메뉴에 "ColorClimb/Single Platform Asset" 항목을 추가합니다.
[CreateAssetMenu(menuName = "ColorClimb/Single Platform Asset")]
public class SinglePlatformAsset : ScriptableObject
{
    [Tooltip("여기에 스프링 블록과 같은 단일 프리팹을 연결하세요.")]
    public GameObject prefab;

    // 혹시 모를 확장을 위해 프리팹을 반환하는 간단한 함수를 만들어 둡니다.
    public GameObject GetPrefab()
    {
        return prefab;
    }
}