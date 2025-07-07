using GuildMaster.Data;

namespace GuildMaster.Battle
{
    /// <summary>
    /// UnitStatus 클래스의 별칭 - Unity의 VisualScripting.Unit과의 충돌을 방지
    /// </summary>
    public class GuildUnit : UnitStatus
    {
        public GuildUnit(string name, int level, JobClass jobClass, Rarity rank = Rarity.Common) 
            : base(name, level, jobClass, rank)
        {
        }
    }
}