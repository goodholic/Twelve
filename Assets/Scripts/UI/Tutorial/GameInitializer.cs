using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace GuildMaster.Battle
{
    /// <summary>
    /// 게임 초기화 및 테스트를 위한 스크립트
    /// </summary>
    public class GameInitializer : MonoBehaviour
    {
        [Header("테스트 설정")]
        [SerializeField] private bool autoStartBattle = true;
        [SerializeField] private AITurnController.AIDifficulty aiDifficulty = AITurnController.AIDifficulty.Normal;
        
        [Header("테스트용 캐릭터 데이터")]
        [SerializeField] private List<CharacterData> testPlayerCharacters = new List<CharacterData>();
        [SerializeField] private List<CharacterData> testEnemyCharacters = new List<CharacterData>();
        
        [Header("매니저 참조")]
        private TurnBasedBattleManager battleManager;
        private TileGridManager tileGridManager;
        private AITurnController aiController;
        
        void Start()
        {
            InitializeManagers();
            
            if (autoStartBattle)
            {
                StartTestBattle();
            }
        }
        
        /// <summary>
        /// 매니저 초기화
        /// </summary>
        void InitializeManagers()
        {
            // 매니저 찾기 또는 생성
            battleManager = FindObjectOfType<TurnBasedBattleManager>();
            if (battleManager == null)
            {
                GameObject bmGO = new GameObject("TurnBasedBattleManager");
                battleManager = bmGO.AddComponent<TurnBasedBattleManager>();
            }
            
            tileGridManager = FindObjectOfType<TileGridManager>();
            if (tileGridManager == null)
            {
                GameObject tgGO = new GameObject("TileGridManager");
                tileGridManager = tgGO.AddComponent<TileGridManager>();
            }
            
            aiController = FindObjectOfType<AITurnController>();
            if (aiController == null)
            {
                GameObject aiGO = new GameObject("AITurnController");
                aiController = aiGO.AddComponent<AITurnController>();
            }
            
            // AI 난이도 설정
            aiController.SetDifficulty(aiDifficulty);
        }
        
        /// <summary>
        /// 테스트 전투 시작
        /// </summary>
        public void StartTestBattle()
        {
            // 테스트 데이터가 없으면 생성
            if (testPlayerCharacters.Count == 0)
            {
                CreateTestCharacters();
            }
            
            // 전투 시작
            if (battleManager != null)
            {
                battleManager.StartBattle(testPlayerCharacters, testEnemyCharacters);
            }
        }
        
        /// <summary>
        /// 테스트용 캐릭터 생성
        /// </summary>
        void CreateTestCharacters()
        {
            // 플레이어 팀 생성
            testPlayerCharacters.Clear();
            testPlayerCharacters.Add(CreateTestCharacter("전사 알렉스", JobClass.Warrior, 5, Rarity.Common));
            testPlayerCharacters.Add(CreateTestCharacter("기사 헬레나", JobClass.Knight, 5, Rarity.Uncommon));
            testPlayerCharacters.Add(CreateTestCharacter("마법사 리나", JobClass.Mage, 5, Rarity.Common));
            testPlayerCharacters.Add(CreateTestCharacter("사제 마리아", JobClass.Priest, 5, Rarity.Common));
            testPlayerCharacters.Add(CreateTestCharacter("암살자 카이", JobClass.Assassin, 5, Rarity.Rare));
            testPlayerCharacters.Add(CreateTestCharacter("궁수 로빈", JobClass.Ranger, 5, Rarity.Common));
            testPlayerCharacters.Add(CreateTestCharacter("현자 소피아", JobClass.Sage, 5, Rarity.Uncommon));
            testPlayerCharacters.Add(CreateTestCharacter("전사 토르", JobClass.Warrior, 6, Rarity.Rare));
            testPlayerCharacters.Add(CreateTestCharacter("기사 롤랜드", JobClass.Knight, 6, Rarity.Rare));
            testPlayerCharacters.Add(CreateTestCharacter("마법사 멀린", JobClass.Mage, 7, Rarity.Epic));
            
            // 적 팀 생성
            testEnemyCharacters.Clear();
            testEnemyCharacters.Add(CreateTestCharacter("오크 전사", JobClass.Warrior, 5, Rarity.Common));
            testEnemyCharacters.Add(CreateTestCharacter("오크 버서커", JobClass.Warrior, 6, Rarity.Uncommon));
            testEnemyCharacters.Add(CreateTestCharacter("고블린 도적", JobClass.Assassin, 4, Rarity.Common));
            testEnemyCharacters.Add(CreateTestCharacter("고블린 궁수", JobClass.Ranger, 4, Rarity.Common));
            testEnemyCharacters.Add(CreateTestCharacter("다크 메이지", JobClass.Mage, 6, Rarity.Rare));
            testEnemyCharacters.Add(CreateTestCharacter("언데드 기사", JobClass.Knight, 7, Rarity.Rare));
            testEnemyCharacters.Add(CreateTestCharacter("타락한 사제", JobClass.Priest, 5, Rarity.Uncommon));
            testEnemyCharacters.Add(CreateTestCharacter("늑대인간", JobClass.Assassin, 6, Rarity.Rare));
            testEnemyCharacters.Add(CreateTestCharacter("리치", JobClass.Sage, 8, Rarity.Epic));
            testEnemyCharacters.Add(CreateTestCharacter("드래곤 나이트", JobClass.Knight, 9, Rarity.Legendary));
        }
        
        /// <summary>
        /// 테스트 캐릭터 데이터 생성
        /// </summary>
        CharacterData CreateTestCharacter(string name, JobClass jobClass, int level, Rarity rarity)
        {
            CharacterData data = ScriptableObject.CreateInstance<CharacterData>();
            
            data.ID = $"test_{name.ToLower().Replace(" ", "_")}";
            data.Name = name;
            data.jobClass = jobClass;
            data.Level = level;
            data.rarity = rarity;
            
            // 레벨과 희귀도에 따른 스탯 계산
            float levelMultiplier = 1f + (level - 1) * 0.1f;
            float rarityMultiplier = 1f + (int)rarity * 0.2f;
            
            // 직업별 기본 스탯
            switch (jobClass)
            {
                case JobClass.Warrior:
                    data.HP = Mathf.RoundToInt(150 * levelMultiplier * rarityMultiplier);
                    data.Attack = Mathf.RoundToInt(15 * levelMultiplier * rarityMultiplier);
                    data.Defense = Mathf.RoundToInt(10 * levelMultiplier * rarityMultiplier);
                    data.Speed = 8;
                    break;
                    
                case JobClass.Knight:
                    data.HP = Mathf.RoundToInt(200 * levelMultiplier * rarityMultiplier);
                    data.Attack = Mathf.RoundToInt(12 * levelMultiplier * rarityMultiplier);
                    data.Defense = Mathf.RoundToInt(18 * levelMultiplier * rarityMultiplier);
                    data.Speed = 6;
                    break;
                    
                case JobClass.Mage:
                    data.HP = Mathf.RoundToInt(80 * levelMultiplier * rarityMultiplier);
                    data.Attack = Mathf.RoundToInt(8 * levelMultiplier * rarityMultiplier);
                    data.Defense = Mathf.RoundToInt(5 * levelMultiplier * rarityMultiplier);
                    data.MagicPower = Mathf.RoundToInt(20 * levelMultiplier * rarityMultiplier);
                    data.Speed = 9;
                    break;
                    
                case JobClass.Priest:
                    data.HP = Mathf.RoundToInt(100 * levelMultiplier * rarityMultiplier);
                    data.Attack = Mathf.RoundToInt(6 * levelMultiplier * rarityMultiplier);
                    data.Defense = Mathf.RoundToInt(8 * levelMultiplier * rarityMultiplier);
                    data.MagicPower = Mathf.RoundToInt(18 * levelMultiplier * rarityMultiplier);
                    data.Speed = 7;
                    break;
                    
                case JobClass.Assassin:
                    data.HP = Mathf.RoundToInt(90 * levelMultiplier * rarityMultiplier);
                    data.Attack = Mathf.RoundToInt(18 * levelMultiplier * rarityMultiplier);
                    data.Defense = Mathf.RoundToInt(6 * levelMultiplier * rarityMultiplier);
                    data.Speed = 15;
                    data.CritRate = 0.25f;
                    break;
                    
                case JobClass.Ranger:
                    data.HP = Mathf.RoundToInt(110 * levelMultiplier * rarityMultiplier);
                    data.Attack = Mathf.RoundToInt(16 * levelMultiplier * rarityMultiplier);
                    data.Defense = Mathf.RoundToInt(8 * levelMultiplier * rarityMultiplier);
                    data.Speed = 12;
                    data.Accuracy = 0.98f;
                    break;
                    
                case JobClass.Sage:
                    data.HP = Mathf.RoundToInt(120 * levelMultiplier * rarityMultiplier);
                    data.Attack = Mathf.RoundToInt(10 * levelMultiplier * rarityMultiplier);
                    data.Defense = Mathf.RoundToInt(10 * levelMultiplier * rarityMultiplier);
                    data.MagicPower = Mathf.RoundToInt(15 * levelMultiplier * rarityMultiplier);
                    data.Speed = 10;
                    break;
            }
            
            data.MP = 50 + level * 10;
            data.CritDamage = 1.5f + (int)rarity * 0.1f;
            data.Description = $"테스트용 {name} 캐릭터";
            
            return data;
        }
        
        /// <summary>
        /// UI 버튼용 - 전투 시작
        /// </summary>
        public void OnStartBattleButton()
        {
            StartTestBattle();
        }
        
        /// <summary>
        /// UI 버튼용 - AI 난이도 변경
        /// </summary>
        public void SetAIDifficulty(int difficulty)
        {
            aiDifficulty = (AITurnController.AIDifficulty)difficulty;
            if (aiController != null)
            {
                aiController.SetDifficulty(aiDifficulty);
            }
        }
        
        /// <summary>
        /// 디버그용 - 즉시 승리
        /// </summary>
        [ContextMenu("Debug - Instant Victory")]
        public void DebugInstantVictory()
        {
            if (battleManager != null && battleManager.IsBattleActive())
            {
                // 모든 타일에 아군 배치
                for (int x = 0; x < 6; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        Tile tileA = tileGridManager.GetTile(Tile.TileType.A, x, y);
                        Tile tileB = tileGridManager.GetTile(Tile.TileType.B, x, y);
                        
                        if (tileA != null && !tileA.isOccupied && testPlayerCharacters.Count > 0)
                        {
                            var character = CreateTestCharacter("Debug Hero", JobClass.Warrior, 10, Rarity.Legendary);
                            GameObject charGO = new GameObject("DebugHero");
                            CharacterUnit unit = charGO.AddComponent<CharacterUnit>();
                            unit.Initialize(character, Tile.Team.Ally);
                            tileGridManager.PlaceCharacter(unit, tileA);
                        }
                        
                        if (tileB != null && !tileB.isOccupied && testPlayerCharacters.Count > 0)
                        {
                            var character = CreateTestCharacter("Debug Hero", JobClass.Knight, 10, Rarity.Legendary);
                            GameObject charGO = new GameObject("DebugHero");
                            CharacterUnit unit = charGO.AddComponent<CharacterUnit>();
                            unit.Initialize(character, Tile.Team.Ally);
                            tileGridManager.PlaceCharacter(unit, tileB);
                        }
                    }
                }
            }
        }
    }
}