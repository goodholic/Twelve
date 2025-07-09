// GuildMaster.Game 네임스페이스 별칭 정의
// 컴파일 오류 해결을 위한 호환성 파일

using GuildMaster.Data;

namespace GuildMaster.Game
{
    // CharacterData 별칭 - GuildMaster.Data.CharacterData를 사용
    public class CharacterData : GuildMaster.Data.CharacterData
    {
        public CharacterData() : base() { }
        public CharacterData(GuildMaster.Data.CharacterData other) : base(other) { }
    }
}