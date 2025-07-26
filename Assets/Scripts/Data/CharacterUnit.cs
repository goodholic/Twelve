using UnityEngine;
using System.Collections.Generic;
using GuildMaster.Data;

/// <summary>
/// 삭제된 CharacterUnit을 대체하는 간단한 플레이스홀더 클래스
/// </summary>
public class CharacterUnit : MonoBehaviour
{
    // 기본 스탯
    public int currentHP = 100;
    public int maxHP = 100;
    public int currentMP = 50;
    public int maxMP = 50;
    public int attack = 10;
    public int defense = 5;
    public int attackPower = 10;
    public int level = 1;
    public int magicPower = 10;
    public int magicResistance = 5;
    public float critRate = 0.1f;
    
    // 직업
    public JobClass jobClass = JobClass.Warrior;
    
    // 상태 효과
    public List<object> activeBuffs = new List<object>();
    public List<object> activeDebuffs = new List<object>();
    
    // 캐릭터 데이터
    public CharacterData characterData;
} 