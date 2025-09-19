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
