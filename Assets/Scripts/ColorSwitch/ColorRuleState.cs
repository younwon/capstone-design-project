using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ColorRuleState
{
    public int lanes;
    public int[] lastRowPerLane;      // 각 레인에서 마지막으로 놓은 행
    public BlockColor[] lastColorPerLane;
    public bool[] hasLaneHistory;

    public bool hasGlobal;
    public BlockColor globalColor;
    public int globalStreak;

    public static ColorRuleState Create(int lanes)
    {
        return new ColorRuleState
        {
            lanes = lanes,
            lastRowPerLane = new int[lanes],
            lastColorPerLane = new BlockColor[lanes],
            hasLaneHistory = new bool[lanes],
            hasGlobal = false,
            globalColor = BlockColor.Green,
            globalStreak = 0
        };
    }
}

