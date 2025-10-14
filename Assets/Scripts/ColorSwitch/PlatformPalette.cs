using System.Collections;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "ColorClimb/PlatformPalette")]
public class PlatformPalette : ScriptableObject
{
    public GameObject prefabGreen;
    public GameObject prefabYellow;
    public GameObject prefabBlue;
    public GameObject prefabOrange;

    // [추가] 가시 버전 팔레트를 연결하기 위한 설정
    [Header("Spike Version (Optional)")]
    [Tooltip("이 팔레트의 '가시 버전' 팔레트를 여기에 연결합니다.")]
    public PlatformPalette spikeVersionPalette;
    [Tooltip("가시 버전 팔레트가 사용될 확률(%)")]
    [Range(0, 100)] public int spikeChance = 0;

    // [추가된 부분] 스프링 블록 에셋을 연결하기 위한 설정
    [Header("Spring Version (Optional)")]
    [Tooltip("스프링 블록 정보를 담은 'SinglePlatformAsset'을 여기에 연결합니다.")]
    public SinglePlatformAsset springVersionAsset; // SinglePlatformAsset 타입으로 변경
    [Tooltip("스프링 블록이 사용될 확률(%)")]
    [Range(0f, 100f)] public float springChance = 0f;

    public GameObject GetPrefab(BlockColor c)
    {
        return c switch
        {
            BlockColor.Green => prefabGreen,
            BlockColor.Yellow => prefabYellow,
            BlockColor.Blue => prefabBlue,
            BlockColor.Orange => prefabOrange,
            _ => null
        };
    }

    public IReadOnlyList<BlockColor> AllColors =>
        new[] { BlockColor.Green, BlockColor.Yellow, BlockColor.Blue, BlockColor.Orange };
}
