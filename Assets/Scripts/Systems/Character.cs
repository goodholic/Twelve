using UnityEngine;
using GuildMaster.Battle;
using GuildMaster.Data;

namespace GuildMaster.Systems
{
    /// <summary>
    /// 캐릭터 클래스 - Unit 클래스의 별칭 또는 래퍼
    /// </summary>
    public class Character : Unit
    {
        public Character(string name, int level, JobClass jobClass, Rarity rank = Rarity.Common) 
            : base(name, level, jobClass, rank)
        {
        }
        
        // 추가적인 캐릭터 관련 기능들을 여기에 구현할 수 있습니다.
    }
} 