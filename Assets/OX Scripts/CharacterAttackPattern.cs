using System;
using UnityEngine;

/// <summary>
/// 5x5 격자에서 공격 가능 여부를 저장하는 클래스
/// [2,2]가 캐릭터 중심, true면 그 칸을 공격 가능
/// </summary>
[Serializable]
public class CharacterAttackPattern
{
    [SerializeField]
    private bool[] pattern = new bool[25]; // 5x5

    public bool IsAttackable(int r, int c)
    {
        return pattern[r * 5 + c];
    }

    public void SetAttackable(int r, int c, bool value)
    {
        pattern[r * 5 + c] = value;
    }
}
