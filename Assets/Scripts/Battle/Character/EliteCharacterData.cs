using System.Collections.Generic;
using UnityEngine;
using GuildMaster.Battle;

namespace GuildMaster.Data
{
    /// <summary>
    /// 18명의 정예 캐릭터 데이터 정의
    /// </summary>
    [CreateAssetMenu(fileName = "EliteCharacterData", menuName = "GuildMaster/Elite Character Data")]
    public class EliteCharacterData : ScriptableObject
    {
        [System.Serializable]
        public class EliteCharacter
        {
            public string characterId;
            public string characterName;
            public string title;
            public JobClass jobClass;
            public Rarity rarity;
            public string storyRole; // 스토리에서의 역할
            
            // 기본 스탯
            public int baseHP;
            public int baseMP;
            public int baseAttack;
            public int baseDefense;
            public int baseMagicPower;
            public int baseSpeed;
            
            // 추가 스탯
            public float critRate;
            public float critDamage;
            public float accuracy;
            public float evasion;
            
            // 스킬
            public List<string> skillIds;
            
            // 성장 곡선
            public AnimationCurve hpGrowthCurve;
            public AnimationCurve attackGrowthCurve;
            public AnimationCurve defenseGrowthCurve;
            
            // 스토리 및 설정
            public string backstory;
            public string personality;
            public List<string> relationships; // 다른 캐릭터와의 관계
            
            // 비주얼
            public Sprite portrait;
            public GameObject modelPrefab;
            public Color themeColor;
        }
        
        [Header("18명의 정예 캐릭터")]
        public List<EliteCharacter> eliteCharacters = new List<EliteCharacter>();
        
        // 정예 18명 캐릭터 정의
        void OnEnable()
        {
            if (eliteCharacters.Count == 0)
            {
                InitializeEliteCharacters();
            }
        }
        
        void InitializeEliteCharacters()
        {
            eliteCharacters.Clear();
            
            // === 전사 계열 (5명) ===
            
            // 1. 아서 - 주인공, 길드 리더
            eliteCharacters.Add(new EliteCharacter
            {
                characterId = "elite_001",
                characterName = "아서",
                title = "정의의 검",
                jobClass = JobClass.Warrior,
                rarity = Rarity.Legendary,
                storyRole = "주인공",
                baseHP = 2500,
                baseMP = 100,
                baseAttack = 180,
                baseDefense = 120,
                baseMagicPower = 50,
                baseSpeed = 85,
                critRate = 0.15f,
                critDamage = 1.5f,
                accuracy = 0.95f,
                evasion = 0.1f,
                skillIds = new List<string> { "101", "102", "103" },
                backstory = "몰락한 왕국의 마지막 왕자. 정의를 위해 길드를 창설했다.",
                personality = "정의롭고 리더십이 뛰어남",
                relationships = new List<string> { "란슬롯 - 절친", "모르가나 - 라이벌" }
            });
            
            // 2. 란슬롯 - 아서의 친구
            eliteCharacters.Add(new EliteCharacter
            {
                characterId = "elite_002",
                characterName = "란슬롯",
                title = "완벽한 기사",
                jobClass = JobClass.Knight,
                rarity = Rarity.Epic,
                storyRole = "주인공의 친구",
                baseHP = 3000,
                baseMP = 80,
                baseAttack = 150,
                baseDefense = 180,
                baseMagicPower = 40,
                baseSpeed = 70,
                critRate = 0.1f,
                critDamage = 1.3f,
                accuracy = 0.9f,
                evasion = 0.05f,
                skillIds = new List<string> { "201", "202", "203" },
                backstory = "최강의 기사. 아서를 따라 길드에 합류했다.",
                personality = "충성스럽고 명예를 중시",
                relationships = new List<string> { "아서 - 절친", "귀네비어 - 연인" }
            });
            
            // 3. 가웨인 - 태양의 기사
            eliteCharacters.Add(new EliteCharacter
            {
                characterId = "elite_003",
                characterName = "가웨인",
                title = "태양의 기사",
                jobClass = JobClass.Warrior,
                rarity = Rarity.Rare,
                storyRole = "동료",
                baseHP = 2200,
                baseMP = 90,
                baseAttack = 200,
                baseDefense = 100,
                baseMagicPower = 60,
                baseSpeed = 90,
                critRate = 0.2f,
                critDamage = 1.6f,
                accuracy = 0.92f,
                evasion = 0.08f,
                skillIds = new List<string> { "104", "105", "106" },
                backstory = "태양의 축복을 받은 기사. 낮에는 힘이 3배가 된다.",
                personality = "열정적이고 자신감 넘침",
                relationships = new List<string> { "아서 - 충성", "모드레드 - 경쟁" }
            });
            
            // 4. 모드레드 - 반역의 기사
            eliteCharacters.Add(new EliteCharacter
            {
                characterId = "elite_004",
                characterName = "모드레드",
                title = "반역의 기사",
                jobClass = JobClass.Warrior,
                rarity = Rarity.Epic,
                storyRole = "라이벌",
                baseHP = 2300,
                baseMP = 110,
                baseAttack = 190,
                baseDefense = 110,
                baseMagicPower = 70,
                baseSpeed = 95,
                critRate = 0.25f,
                critDamage = 1.7f,
                accuracy = 0.88f,
                evasion = 0.12f,
                skillIds = new List<string> { "107", "108", "109" },
                backstory = "아서의 숨겨진 혈육. 복잡한 감정을 품고 있다.",
                personality = "야심차고 반항적",
                relationships = new List<string> { "아서 - 복잡한 감정", "모르가나 - 협력" }
            });
            
            // 5. 베디비어 - 충성의 기사
            eliteCharacters.Add(new EliteCharacter
            {
                characterId = "elite_005",
                characterName = "베디비어",
                title = "충성의 방패",
                jobClass = JobClass.Knight,
                rarity = Rarity.Rare,
                storyRole = "동료",
                baseHP = 2800,
                baseMP = 70,
                baseAttack = 140,
                baseDefense = 200,
                baseMagicPower = 30,
                baseSpeed = 65,
                critRate = 0.08f,
                critDamage = 1.2f,
                accuracy = 0.95f,
                evasion = 0.03f,
                skillIds = new List<string> { "204", "205", "206" },
                backstory = "아서의 가장 충실한 기사. 끝까지 곁을 지킨다.",
                personality = "과묵하고 신중함",
                relationships = new List<string> { "아서 - 절대적 충성" }
            });
            
            // === 마법사 계열 (4명) ===
            
            // 6. 멀린 - 대마법사
            eliteCharacters.Add(new EliteCharacter
            {
                characterId = "elite_006",
                characterName = "멀린",
                title = "대현자",
                jobClass = JobClass.Sage,
                rarity = Rarity.Legendary,
                storyRole = "조언자",
                baseHP = 1800,
                baseMP = 300,
                baseAttack = 80,
                baseDefense = 80,
                baseMagicPower = 250,
                baseSpeed = 75,
                critRate = 0.18f,
                critDamage = 1.8f,
                accuracy = 0.98f,
                evasion = 0.15f,
                skillIds = new List<string> { "301", "302", "303" },
                backstory = "시간을 거스르는 대마법사. 아서의 스승이자 조언자.",
                personality = "지혜롭고 신비로움",
                relationships = new List<string> { "아서 - 스승", "모르가나 - 숙적" }
            });
            
            // 7. 모르가나 - 어둠의 마녀
            eliteCharacters.Add(new EliteCharacter
            {
                characterId = "elite_007",
                characterName = "모르가나",
                title = "어둠의 마녀",
                jobClass = JobClass.Mage,
                rarity = Rarity.Epic,
                storyRole = "안티히어로",
                baseHP = 1600,
                baseMP = 280,
                baseAttack = 70,
                baseDefense = 70,
                baseMagicPower = 220,
                baseSpeed = 80,
                critRate = 0.22f,
                critDamage = 1.9f,
                accuracy = 0.92f,
                evasion = 0.18f,
                skillIds = new List<string> { "304", "305", "306" },
                backstory = "아서의 이복 누나. 어둠의 마법을 다룬다.",
                personality = "교활하고 야심적",
                relationships = new List<string> { "아서 - 복잡한 관계", "멀린 - 숙적" }
            });
            
            // 8. 니뮤에 - 호수의 여인
            eliteCharacters.Add(new EliteCharacter
            {
                characterId = "elite_008",
                characterName = "니뮤에",
                title = "호수의 여인",
                jobClass = JobClass.Mage,
                rarity = Rarity.Rare,
                storyRole = "신비한 조력자",
                baseHP = 1500,
                baseMP = 260,
                baseAttack = 60,
                baseDefense = 75,
                baseMagicPower = 200,
                baseSpeed = 85,
                critRate = 0.15f,
                critDamage = 1.6f,
                accuracy = 0.96f,
                evasion = 0.2f,
                skillIds = new List<string> { "307", "308", "309" },
                backstory = "신비한 호수의 정령. 엑스칼리버를 수호한다.",
                personality = "신비롭고 초연함",
                relationships = new List<string> { "멀린 - 제자", "아서 - 후원자" }
            });
            
            // 9. 모르드 - 불의 마법사
            eliteCharacters.Add(new EliteCharacter
            {
                characterId = "elite_009",
                characterName = "모르드",
                title = "불꽃의 현자",
                jobClass = JobClass.Mage,
                rarity = Rarity.Rare,
                storyRole = "동료",
                baseHP = 1400,
                baseMP = 240,
                baseAttack = 65,
                baseDefense = 65,
                baseMagicPower = 210,
                baseSpeed = 78,
                critRate = 0.2f,
                critDamage = 1.7f,
                accuracy = 0.9f,
                evasion = 0.1f,
                skillIds = new List<string> { "310", "311", "312" },
                backstory = "불의 원소를 다루는 마법사. 열정적인 성격.",
                personality = "열정적이고 충동적",
                relationships = new List<string> { "멀린 - 제자" }
            });
            
            // === 성직자 계열 (3명) ===
            
            // 10. 갈라하드 - 성배의 기사
            eliteCharacters.Add(new EliteCharacter
            {
                characterId = "elite_010",
                characterName = "갈라하드",
                title = "성배의 기사",
                jobClass = JobClass.Priest,
                rarity = Rarity.Epic,
                storyRole = "순수한 영혼",
                baseHP = 1900,
                baseMP = 220,
                baseAttack = 90,
                baseDefense = 130,
                baseMagicPower = 180,
                baseSpeed = 72,
                critRate = 0.1f,
                critDamage = 1.4f,
                accuracy = 0.98f,
                evasion = 0.08f,
                skillIds = new List<string> { "401", "402", "403" },
                backstory = "가장 순수한 영혼의 기사. 성배를 찾는 사명을 띤다.",
                personality = "순수하고 헌신적",
                relationships = new List<string> { "란슬롯 - 아버지", "아서 - 충성" }
            });
            
            // 11. 비비안 - 치유의 성녀
            eliteCharacters.Add(new EliteCharacter
            {
                characterId = "elite_011",
                characterName = "비비안",
                title = "치유의 성녀",
                jobClass = JobClass.Priest,
                rarity = Rarity.Rare,
                storyRole = "치유사",
                baseHP = 1700,
                baseMP = 250,
                baseAttack = 70,
                baseDefense = 100,
                baseMagicPower = 190,
                baseSpeed = 75,
                critRate = 0.08f,
                critDamage = 1.3f,
                accuracy = 0.95f,
                evasion = 0.12f,
                skillIds = new List<string> { "404", "405", "406" },
                backstory = "신성한 힘으로 동료들을 치유하는 성녀.",
                personality = "자비롭고 온화함",
                relationships = new List<string> { "갈라하드 - 동료" }
            });
            
            // 12. 펠리아스 - 수호의 성직자
            eliteCharacters.Add(new EliteCharacter
            {
                characterId = "elite_012",
                characterName = "펠리아스",
                title = "수호의 빛",
                jobClass = JobClass.Priest,
                rarity = Rarity.Uncommon,
                storyRole = "동료",
                baseHP = 1600,
                baseMP = 200,
                baseAttack = 75,
                baseDefense = 120,
                baseMagicPower = 160,
                baseSpeed = 70,
                critRate = 0.05f,
                critDamage = 1.2f,
                accuracy = 0.93f,
                evasion = 0.1f,
                skillIds = new List<string> { "407", "408", "409" },
                backstory = "동료들을 보호하는 수호의 성직자.",
                personality = "침착하고 신중함",
                relationships = new List<string> { "비비안 - 동료" }
            });
            
            // === 도적 계열 (3명) ===
            
            // 13. 트리스탄 - 그림자 검객
            eliteCharacters.Add(new EliteCharacter
            {
                characterId = "elite_013",
                characterName = "트리스탄",
                title = "그림자 검객",
                jobClass = JobClass.Rogue,
                rarity = Rarity.Rare,
                storyRole = "암살자",
                baseHP = 1500,
                baseMP = 120,
                baseAttack = 170,
                baseDefense = 70,
                baseMagicPower = 80,
                baseSpeed = 120,
                critRate = 0.35f,
                critDamage = 2.0f,
                accuracy = 0.95f,
                evasion = 0.25f,
                skillIds = new List<string> { "501", "502", "503" },
                backstory = "그림자 속에서 적을 처단하는 암살자.",
                personality = "냉정하고 과묵함",
                relationships = new List<string> { "이졸데 - 연인" }
            });
            
            // 14. 이졸데 - 환영의 무희
            eliteCharacters.Add(new EliteCharacter
            {
                characterId = "elite_014",
                characterName = "이졸데",
                title = "환영의 무희",
                jobClass = JobClass.Rogue,
                rarity = Rarity.Rare,
                storyRole = "첩보원",
                baseHP = 1400,
                baseMP = 140,
                baseAttack = 160,
                baseDefense = 65,
                baseMagicPower = 90,
                baseSpeed = 125,
                critRate = 0.3f,
                critDamage = 1.8f,
                accuracy = 0.92f,
                evasion = 0.3f,
                skillIds = new List<string> { "504", "505", "506" },
                backstory = "환영으로 적을 교란하는 무희 출신 첩보원.",
                personality = "매혹적이고 신비로움",
                relationships = new List<string> { "트리스탄 - 연인" }
            });
            
            // 15. 케이 - 정찰병
            eliteCharacters.Add(new EliteCharacter
            {
                characterId = "elite_015",
                characterName = "케이",
                title = "날쌘 정찰병",
                jobClass = JobClass.Rogue,
                rarity = Rarity.Uncommon,
                storyRole = "정찰병",
                baseHP = 1300,
                baseMP = 100,
                baseAttack = 150,
                baseDefense = 60,
                baseMagicPower = 70,
                baseSpeed = 130,
                critRate = 0.28f,
                critDamage = 1.7f,
                accuracy = 0.9f,
                evasion = 0.28f,
                skillIds = new List<string> { "507", "508", "509" },
                backstory = "아서의 의형제. 뛰어난 정찰 능력을 지닌다.",
                personality = "재치있고 유머러스함",
                relationships = new List<string> { "아서 - 의형제" }
            });
            
            // === 궁수 계열 (3명) ===
            
            // 16. 로빈 - 명사수
            eliteCharacters.Add(new EliteCharacter
            {
                characterId = "elite_016",
                characterName = "로빈",
                title = "전설의 명사수",
                jobClass = JobClass.Archer,
                rarity = Rarity.Epic,
                storyRole = "원거리 딜러",
                baseHP = 1600,
                baseMP = 100,
                baseAttack = 180,
                baseDefense = 75,
                baseMagicPower = 60,
                baseSpeed = 100,
                critRate = 0.25f,
                critDamage = 1.75f,
                accuracy = 0.99f,
                evasion = 0.15f,
                skillIds = new List<string> { "601", "602", "603" },
                backstory = "백발백중의 명사수. 정의를 위해 활을 든다.",
                personality = "정의롭고 유쾌함",
                relationships = new List<string> { "아서 - 동맹" }
            });
            
            // 17. 아탈란테 - 사냥꾼
            eliteCharacters.Add(new EliteCharacter
            {
                characterId = "elite_017",
                characterName = "아탈란테",
                title = "황금 사냥꾼",
                jobClass = JobClass.Archer,
                rarity = Rarity.Rare,
                storyRole = "사냥꾼",
                baseHP = 1500,
                baseMP = 110,
                baseAttack = 170,
                baseDefense = 70,
                baseMagicPower = 65,
                baseSpeed = 110,
                critRate = 0.22f,
                critDamage = 1.65f,
                accuracy = 0.97f,
                evasion = 0.18f,
                skillIds = new List<string> { "604", "605", "606" },
                backstory = "자연과 교감하는 뛰어난 사냥꾼.",
                personality = "독립적이고 자유로움",
                relationships = new List<string> { "로빈 - 라이벌" }
            });
            
            // 18. 윌리엄 - 바람의 궁수
            eliteCharacters.Add(new EliteCharacter
            {
                characterId = "elite_018",
                characterName = "윌리엄",
                title = "바람의 궁수",
                jobClass = JobClass.Archer,
                rarity = Rarity.Uncommon,
                storyRole = "동료",
                baseHP = 1400,
                baseMP = 90,
                baseAttack = 160,
                baseDefense = 65,
                baseMagicPower = 55,
                baseSpeed = 115,
                critRate = 0.2f,
                critDamage = 1.6f,
                accuracy = 0.95f,
                evasion = 0.2f,
                skillIds = new List<string> { "607", "608", "609" },
                backstory = "바람을 읽는 젊은 궁수. 로빈의 제자.",
                personality = "열정적이고 성실함",
                relationships = new List<string> { "로빈 - 스승" }
            });
        }
        
        public EliteCharacter GetCharacterById(string characterId)
        {
            return eliteCharacters.Find(c => c.characterId == characterId);
        }
        
        public List<EliteCharacter> GetCharactersByJobClass(JobClass jobClass)
        {
            return eliteCharacters.FindAll(c => c.jobClass == jobClass);
        }
        
        public List<EliteCharacter> GetCharactersByRarity(Rarity rarity)
        {
            return eliteCharacters.FindAll(c => c.rarity == rarity);
        }
    }
}