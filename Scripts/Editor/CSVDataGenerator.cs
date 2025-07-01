using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace GuildMaster.Editor
{
    public class CSVDataGenerator : EditorWindow
    {
        [MenuItem("GuildMaster/Generate CSV Data")]
        public static void ShowWindow()
        {
            GetWindow<CSVDataGenerator>("CSV Data Generator");
        }

        void OnGUI()
        {
            GUILayout.Label("CSV Data Generator", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Generate All CSV Files"))
            {
                GenerateAllCSVFiles();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Generate Character Data"))
            {
                GenerateCharacterData();
            }
            
            if (GUILayout.Button("Generate Building Data"))
            {
                GenerateBuildingData();
            }
            
            if (GUILayout.Button("Generate Building Effects"))
            {
                GenerateBuildingEffects();
            }
            
            if (GUILayout.Button("Generate Building Production"))
            {
                GenerateBuildingProduction();
            }
            
            if (GUILayout.Button("Generate Skill Data"))
            {
                GenerateSkillData();
            }
        }
        
        void GenerateAllCSVFiles()
        {
            GenerateCharacterData();
            GenerateBuildingData();
            GenerateBuildingEffects();
            GenerateBuildingProduction();
            GenerateSkillData();
            
            AssetDatabase.Refresh();
            Debug.Log("All CSV files generated successfully!");
        }
        
        void GenerateCharacterData()
        {
            string csvPath = Path.Combine(Application.dataPath, "CSV", "character_data.csv");
            EnsureDirectoryExists(Path.GetDirectoryName(csvPath));
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ID,Name,JobClass,Level,Rarity,HP,MP,Attack,Defense,MagicPower,Speed,CritRate,CritDamage,Accuracy,Evasion,Skill1,Skill2,Skill3,Description");
            
            // 전사 (Warrior)
            sb.AppendLine("char_001,철검의 라이언,Warrior,1,Common,120,30,15,10,5,8,0.10,1.5,0.95,0.05,101,102,103,\"검술에 능한 신참 전사. 고향을 지키기 위해 모험을 시작했다.\"");
            sb.AppendLine("char_002,방패의 그레이스,Warrior,3,Uncommon,180,40,22,15,7,10,0.12,1.5,0.95,0.05,101,104,105,\"방어와 공격의 균형을 추구하는 여전사. 동료를 지키는 것이 사명이다.\"");
            sb.AppendLine("char_003,광전사 토르,Warrior,5,Rare,250,50,32,18,10,12,0.15,1.8,0.93,0.07,101,106,107,\"전투의 열기에 휩싸이면 더욱 강해지는 광전사. 북방 출신의 용사.\"");
            sb.AppendLine("char_004,검성 아서,Warrior,7,Epic,350,60,45,25,15,15,0.20,2.0,0.97,0.08,101,108,109,\"전설의 검을 다루는 마스터. 수많은 전투에서 승리를 거둔 영웅.\"");
            sb.AppendLine("char_005,불멸의 레오나르드,Warrior,10,Legendary,500,80,65,35,20,18,0.25,2.5,0.98,0.10,101,110,111,\"죽음을 극복한 전설의 전사. 그의 검은 어떤 적도 베어낼 수 있다.\"");
            
            // 기사 (Knight)
            sb.AppendLine("char_006,견습기사 폴,Knight,1,Common,150,25,12,18,5,6,0.05,1.3,0.90,0.03,201,202,203,\"정의를 추구하는 신참 기사. 약자를 보호하는 것이 꿈이다.\"");
            sb.AppendLine("char_007,성기사 헬레나,Knight,3,Uncommon,220,35,18,28,8,8,0.07,1.3,0.92,0.04,201,204,205,\"신의 가호를 받은 성스러운 기사. 치유의 힘도 갖추고 있다.\"");
            sb.AppendLine("char_008,철벽의 롤랜드,Knight,5,Rare,320,45,25,42,12,10,0.08,1.5,0.94,0.05,201,206,207,\"난공불락의 방패술을 구사하는 기사. 어떤 공격도 막아낸다.\"");
            sb.AppendLine("char_009,왕국기사단장 가웨인,Knight,7,Epic,450,55,35,60,18,12,0.10,1.7,0.96,0.06,201,208,209,\"왕국 최고의 기사. 부하들에게 용기를 북돋우는 지휘관.\"");
            sb.AppendLine("char_010,성검의 수호자 미카엘,Knight,10,Legendary,650,70,50,85,25,15,0.12,2.0,0.98,0.08,201,210,211,\"신성한 검과 방패를 다루는 전설의 기사. 악을 정화하는 힘을 지녔다.\"");
            
            // 마법사 (Wizard)
            sb.AppendLine("char_011,견습마법사 리나,Wizard,1,Common,80,60,8,5,20,9,0.15,1.8,0.90,0.10,301,302,303,\"마법학교를 갓 졸업한 신참 마법사. 화염 마법에 재능이 있다.\"");
            sb.AppendLine("char_012,원소술사 카일,Wizard,3,Uncommon,110,85,12,8,32,12,0.18,1.8,0.92,0.12,301,304,305,\"4대 원소를 자유자재로 다루는 마법사. 균형잡힌 마법 실력을 보유.\"");
            sb.AppendLine("char_013,빙결의 엘사,Wizard,5,Rare,150,120,18,12,48,15,0.20,2.0,0.94,0.15,301,306,307,\"얼음 마법의 대가. 적을 얼려 움직임을 봉쇄하는 전술을 구사한다.\"");
            sb.AppendLine("char_014,대마법사 멀린,Wizard,7,Epic,200,160,25,18,68,18,0.25,2.2,0.96,0.18,301,308,309,\"고대 마법의 비밀을 아는 현자. 시공간을 조작하는 힘을 지녔다.\"");
            sb.AppendLine("char_015,마도왕 솔로몬,Wizard,10,Legendary,280,220,35,25,95,22,0.30,2.5,0.98,0.20,301,310,311,\"모든 마법의 정점에 선 존재. 그의 주문은 현실을 바꿀 수 있다.\"");
            
            // 성직자 (Priest)
            sb.AppendLine("char_016,수습사제 마리아,Priest,1,Common,90,50,6,8,18,7,0.05,1.2,0.92,0.08,401,402,403,\"치유의 길을 걷기 시작한 사제. 따뜻한 마음을 가진 치유사.\"");
            sb.AppendLine("char_017,빛의 사제 루시아,Priest,3,Uncommon,130,75,10,12,28,10,0.07,1.2,0.94,0.10,401,404,405,\"신성한 빛으로 동료를 치유하고 언데드를 정화하는 사제.\"");
            sb.AppendLine("char_018,대사제 베네딕트,Priest,5,Rare,180,110,15,18,42,12,0.08,1.3,0.95,0.12,401,406,407,\"깊은 신앙심으로 기적을 행하는 대사제. 죽은 자도 되살릴 수 있다.\"");
            sb.AppendLine("char_019,성녀 잔다르크,Priest,7,Epic,250,150,22,25,60,15,0.10,1.5,0.97,0.15,401,408,409,\"신의 계시를 받은 성녀. 전투와 치유를 동시에 수행할 수 있다.\"");
            sb.AppendLine("char_020,대천사의 화신 가브리엘,Priest,10,Legendary,350,200,30,35,85,18,0.12,1.8,0.99,0.18,401,410,411,\"천상의 힘을 지상에 구현하는 존재. 모든 상처와 저주를 치유한다.\"");
            
            // 암살자 (Assassin)
            sb.AppendLine("char_021,그림자 신참 카이,Assassin,1,Common,85,35,18,6,8,15,0.25,2.0,0.85,0.20,501,502,503,\"어둠 속에서 훈련받은 신참 암살자. 빠른 몸놀림이 특기.\"");
            sb.AppendLine("char_022,독칼의 나타샤,Assassin,3,Uncommon,120,50,28,10,12,20,0.30,2.0,0.87,0.25,501,504,505,\"독을 다루는 여성 암살자. 적을 서서히 약화시키는 전술을 구사.\"");
            sb.AppendLine("char_023,그림자 무희 아키라,Assassin,5,Rare,165,70,42,15,18,26,0.35,2.2,0.90,0.30,501,506,507,\"동방의 암살술을 익힌 자객. 분신술로 적을 교란시킨다.\"");
            sb.AppendLine("char_024,밤의 군주 레이븐,Assassin,7,Epic,220,90,60,22,25,32,0.40,2.5,0.93,0.35,501,508,509,\"암살자 길드의 수장. 그림자를 자유자재로 조종한다.\"");
            sb.AppendLine("char_025,죽음의 그림자 아자젤,Assassin,10,Legendary,300,120,85,30,35,40,0.50,3.0,0.95,0.40,501,510,511,\"죽음 그 자체인 전설의 암살자. 그의 칼날은 절대 빗나가지 않는다.\"");
            
            // 궁수 (Ranger)
            sb.AppendLine("char_026,신참 궁수 로빈,Ranger,1,Common,95,40,16,8,6,12,0.20,1.8,0.98,0.15,601,602,603,\"숲에서 자란 젊은 궁수. 동물들과 교감하는 능력이 있다.\"");
            sb.AppendLine("char_027,매의 눈 실바나,Ranger,3,Uncommon,140,55,25,12,10,16,0.25,1.8,0.99,0.18,601,604,605,\"절대 빗나가지 않는 명사수. 바람을 읽어 화살의 궤적을 조정한다.\"");
            sb.AppendLine("char_028,정령궁수 레골라스,Ranger,5,Rare,195,80,38,18,15,22,0.30,2.0,0.99,0.22,601,606,607,\"정령의 축복을 받은 궁수. 마법이 깃든 화살을 사용한다.\"");
            sb.AppendLine("char_029,천궁의 아르테미스,Ranger,7,Epic,270,105,55,25,22,28,0.35,2.2,1.00,0.25,601,608,609,\"달의 여신의 가호를 받은 궁수. 한 번에 여러 적을 관통하는 기술을 지녔다.\"");
            sb.AppendLine("char_030,신궁 오딧세우스,Ranger,10,Legendary,380,140,80,35,30,35,0.40,2.5,1.00,0.30,601,610,611,\"전설의 영웅 궁수. 그의 화살은 시공간을 초월하여 목표를 맞춘다.\"");
            
            // 현자 (Sage)
            sb.AppendLine("char_031,지혜의 탐구자 소피아,Sage,1,Common,100,55,10,10,15,10,0.12,1.5,0.93,0.12,701,702,703,\"지식을 추구하는 젊은 현자. 마법과 무술을 동시에 수련한다.\"");
            sb.AppendLine("char_032,균형의 현자 타오,Sage,3,Uncommon,150,80,16,16,25,13,0.15,1.5,0.95,0.15,701,704,705,\"동양 철학을 바탕으로 음양의 조화를 추구하는 현자.\"");
            sb.AppendLine("char_033,별의 현자 갈릴레오,Sage,5,Rare,210,115,24,24,38,17,0.18,1.7,0.96,0.18,701,706,707,\"천문학과 점성술에 정통한 현자. 별의 힘을 빌려 싸운다.\"");
            sb.AppendLine("char_034,대현자 아리스토텔레스,Sage,7,Epic,300,155,35,35,55,21,0.22,2.0,0.98,0.20,701,708,709,\"고대의 지혜를 모두 습득한 대현자. 모든 학문의 정점에 선 자.\"");
            sb.AppendLine("char_035,만물의 현자 헤르메스,Sage,10,Legendary,420,210,50,50,80,26,0.28,2.3,0.99,0.25,701,710,711,\"신과 인간의 경계에 선 존재. 우주의 진리를 깨달은 궁극의 현자.\"");
            
            // 추가 캐릭터들 (다양한 직업과 레벨)
            sb.AppendLine("char_036,화염검사 이그니스,Warrior,4,Uncommon,210,45,28,20,12,11,0.13,1.6,0.94,0.06,101,112,113,\"불꽃을 검에 담아 싸우는 특수한 전사. 화산 지대 출신.\"");
            sb.AppendLine("char_037,서리방패 프로스트,Knight,4,Uncommon,260,40,22,35,10,9,0.06,1.4,0.93,0.04,201,212,213,\"얼음의 힘으로 동료를 보호하는 기사. 북방 왕국의 수호자.\"");
            sb.AppendLine("char_038,폭풍술사 템페스트,Wizard,4,Uncommon,125,100,15,10,40,14,0.19,1.9,0.93,0.13,301,312,313,\"번개와 폭풍을 다루는 마법사. 날씨를 조종하는 능력을 지녔다.\"");
            sb.AppendLine("char_039,자연의 치유사 드루이드,Priest,4,Uncommon,155,90,13,15,35,11,0.08,1.3,0.95,0.11,401,412,413,\"자연의 힘으로 치유하는 특별한 사제. 숲의 정령들과 교감한다.\"");
            sb.AppendLine("char_040,그림자 쌍검 듀얼,Assassin,4,Uncommon,145,60,35,13,15,23,0.32,2.1,0.88,0.27,501,512,513,\"양손에 단검을 든 암살자. 화려한 연속 공격이 특기.\"");
            sb.AppendLine("char_041,폭발화살 봄버,Ranger,4,Uncommon,165,70,32,15,13,19,0.27,1.9,0.98,0.20,601,612,613,\"폭발하는 특수 화살을 사용하는 궁수. 공성전 전문가.\"");
            sb.AppendLine("char_042,룬의 현자 오딘,Sage,4,Uncommon,180,100,20,20,32,15,0.16,1.6,0.96,0.16,701,712,713,\"고대 룬 문자의 힘을 사용하는 현자. 북방 신화의 계승자.\"");
            
            // 고레벨 추가 캐릭터
            sb.AppendLine("char_043,용살자 지크프리트,Warrior,8,Epic,400,70,55,30,18,17,0.22,2.2,0.98,0.09,101,114,115,\"용을 사냥하는 전설적인 전사. 용의 비늘로 만든 갑옷을 착용.\"");
            sb.AppendLine("char_044,황금기사 미다스,Knight,8,Epic,520,65,42,70,22,14,0.11,1.8,0.97,0.07,201,214,215,\"황금으로 빛나는 갑옷의 기사. 부와 명예의 수호자.\"");
            sb.AppendLine("char_045,시간술사 크로노스,Wizard,8,Epic,240,180,30,22,75,20,0.27,2.3,0.97,0.19,301,314,315,\"시간을 조작하는 희귀한 마법사. 과거와 미래를 볼 수 있다.\"");
            sb.AppendLine("char_046,빛의 대사제 루미너스,Priest,8,Epic,320,170,28,30,68,17,0.11,1.6,0.98,0.16,401,414,415,\"순수한 빛의 힘을 다루는 대사제. 모든 어둠을 몰아낸다.\"");
            sb.AppendLine("char_047,혈월의 암살자 블러드문,Assassin,8,Epic,280,110,70,28,30,35,0.45,2.7,0.94,0.37,501,514,515,\"달이 붉게 물든 밤에만 나타나는 전설의 암살자.\"");
            sb.AppendLine("char_048,바람의 전령 에올로스,Ranger,8,Epic,340,125,65,30,28,31,0.37,2.3,1.00,0.28,601,614,615,\"바람을 타고 이동하는 신출귀몰한 궁수. 어디서든 나타날 수 있다.\"");
            sb.AppendLine("char_049,진리의 탐구자 플라톤,Sage,8,Epic,370,180,42,42,62,24,0.25,2.1,0.99,0.22,701,714,715,\"이데아의 세계를 탐구하는 철학자. 진리의 힘으로 싸운다.\"");
            
            // 특별 캐릭터
            sb.AppendLine("char_050,무명의 영웅,Warrior,6,Rare,300,55,40,23,14,14,0.18,1.9,0.96,0.08,101,116,117,\"이름 없는 영웅. 수많은 전투에서 살아남은 베테랑.\"");
            sb.AppendLine("char_051,떠돌이 기사 에런트,Knight,6,Rare,380,50,32,50,16,11,0.09,1.6,0.95,0.06,201,216,217,\"주인 없는 떠돌이 기사. 정의를 위해 홀로 싸운다.\"");
            sb.AppendLine("char_052,금지된 마법사 타부,Wizard,6,Rare,180,140,22,15,55,17,0.23,2.1,0.95,0.17,301,316,317,\"금지된 마법을 연구하는 이단 마법사. 위험하지만 강력하다.\"");
            sb.AppendLine("char_053,타락한 사제 폴른,Priest,6,Rare,220,130,20,22,50,14,0.10,1.4,0.96,0.14,401,416,417,\"어둠에 물든 타락한 사제. 저주와 치유를 동시에 사용한다.\"");
            sb.AppendLine("char_054,붉은 그림자 크림슨,Assassin,6,Rare,200,85,52,20,22,29,0.38,2.4,0.91,0.32,501,516,517,\"피로 물든 암살자. 잔인하지만 효율적인 살수.\"");
            sb.AppendLine("char_055,은빛 화살 실버,Ranger,6,Rare,230,95,48,22,20,25,0.32,2.1,0.99,0.24,601,616,617,\"은으로 만든 화살을 사용하는 궁수. 마물 사냥 전문가.\"");
            sb.AppendLine("char_056,방랑 현자 노마드,Sage,6,Rare,270,135,30,30,45,19,0.20,1.8,0.97,0.19,701,716,717,\"세계를 떠도는 방랑 현자. 각지의 지혜를 수집한다.\"");
            
            // 최종 추가 캐릭터
            sb.AppendLine("char_057,초보 모험가 알렉스,Warrior,2,Common,150,35,18,13,8,9,0.11,1.4,0.94,0.05,101,102,118,\"모험을 막 시작한 열정적인 전사. 무한한 가능성을 지녔다.\"");
            sb.AppendLine("char_058,수호의 맹세 가디언,Knight,9,Epic,580,75,48,80,26,16,0.12,1.9,0.98,0.08,201,218,219,\"절대 수호의 맹세를 한 기사. 동료가 쓰러지면 더욱 강해진다.\"");
            sb.AppendLine("char_059,마나의 지배자 아르카나,Wizard,9,Epic,270,200,32,24,82,21,0.29,2.4,0.98,0.20,301,318,319,\"순수 마나를 직접 조작하는 대마법사. 마법의 본질을 꿰뚫었다.\"");
            sb.AppendLine("char_060,구원의 손길 살바토레,Priest,9,Epic,380,190,32,32,72,19,0.12,1.7,0.99,0.17,401,418,419,\"죽음의 문턱에서 생명을 구하는 기적의 사제. 부활의 권능을 지녔다.\"");
            
            File.WriteAllText(csvPath, sb.ToString());
            Debug.Log($"Character data CSV generated at: {csvPath}");
        }
        
        void GenerateBuildingData()
        {
            string csvPath = Path.Combine(Application.dataPath, "CSV", "building_data.csv");
            EnsureDirectoryExists(Path.GetDirectoryName(csvPath));
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("buildingId,buildingName,buildingType,category,sizeX,sizeY,gold,wood,stone,mana,buildTime,requiredLevel,maxLevel,description");
            
            // Core Buildings
            sb.AppendLine("guild_hall,길드 홀,GuildHall,Core,3,3,0,0,0,0,0,1,10,\"길드의 중심 건물. 모든 활동의 시작점이며 길드 레벨을 결정한다.\"");
            
            // Military Buildings
            sb.AppendLine("training_ground,훈련소,TrainingGround,Military,2,2,500,200,100,0,60,1,5,\"모험가들이 훈련하여 경험치를 획득하는 시설. 레벨업 속도가 증가한다.\"");
            sb.AppendLine("barracks,병영,Barracks,Military,2,2,800,300,200,50,120,2,5,\"전사와 기사를 전문적으로 훈련하는 시설. 물리 공격력이 향상된다.\"");
            sb.AppendLine("archery_range,궁술 연습장,ArcheryRange,Military,2,2,700,400,100,0,90,2,5,\"궁수를 훈련하는 전문 시설. 명중률과 크리티컬 확률이 증가한다.\"");
            sb.AppendLine("armory,무기고,Armory,Military,2,2,1000,200,500,100,150,3,5,\"장비를 보관하고 강화하는 시설. 전투력이 전반적으로 상승한다.\"");
            
            // Support Buildings
            sb.AppendLine("laboratory,연구소,Laboratory,Support,2,2,1200,300,300,200,180,3,5,\"새로운 기술과 스킬을 연구하는 시설. 연구 속도가 증가한다.\"");
            sb.AppendLine("temple,신전,Temple,Support,2,2,800,100,400,150,120,2,5,\"성직자를 훈련하고 축복을 받는 시설. 치유 효과가 증가한다.\"");
            sb.AppendLine("library,도서관,Library,Support,2,2,600,350,250,100,150,3,5,\"지식을 축적하는 시설. 경험치 획득량과 스킬 효과가 증가한다.\"");
            sb.AppendLine("tavern,주점,Tavern,Support,2,2,400,300,100,0,60,1,5,\"모험가들이 휴식하고 정보를 교환하는 장소. 사기가 회복된다.\"");
            
            // Economic Buildings
            sb.AppendLine("shop,상점,Shop,Economic,2,2,600,200,100,0,90,1,5,\"물품을 사고파는 시설. 골드를 자동으로 생산한다.\"");
            sb.AppendLine("warehouse,창고,Storage,Economic,3,3,300,400,200,0,60,1,5,\"자원을 보관하는 시설. 자원 저장 한계가 증가한다.\"");
            sb.AppendLine("market,시장,Market,Economic,3,3,1500,500,300,100,240,4,5,\"대규모 거래가 이루어지는 장소. 거래 효율과 골드 생산이 증가한다.\"");
            
            // Special Buildings
            sb.AppendLine("mage_tower,마법탑,MageTower,Special,2,3,1500,200,600,500,300,5,5,\"마법사를 훈련하고 마법을 연구하는 탑. 마법 공격력이 크게 상승한다.\"");
            sb.AppendLine("scout_post,정찰대,ScoutPost,Special,1,1,300,200,50,0,45,2,3,\"주변 지역을 정찰하는 시설. 새로운 던전과 이벤트를 발견한다.\"");
            sb.AppendLine("chapel,예배당,Chapel,Special,2,2,1000,200,300,200,180,4,5,\"특별한 축복을 받는 신성한 장소. 부활과 상태이상 면역 효과를 제공한다.\"");
            
            File.WriteAllText(csvPath, sb.ToString());
            Debug.Log($"Building data CSV generated at: {csvPath}");
        }
        
        void GenerateBuildingEffects()
        {
            string csvPath = Path.Combine(Application.dataPath, "CSV", "building_effects.csv");
            EnsureDirectoryExists(Path.GetDirectoryName(csvPath));
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("buildingId,effectType,baseValue,perLevelIncrease,description");
            
            // Guild Hall Effects
            sb.AppendLine("guild_hall,ReputationGain,10,5,\"길드의 명성 획득량이 증가합니다.\"");
            sb.AppendLine("guild_hall,MaxAdventurers,20,10,\"최대 보유 가능한 모험가 수가 증가합니다.\"");
            
            // Training Buildings Effects
            sb.AppendLine("training_ground,TrainingSpeed,0.1,0.05,\"모험가 훈련 속도가 증가합니다.\"");
            sb.AppendLine("training_ground,ExpGain,0.1,0.05,\"경험치 획득량이 증가합니다.\"");
            
            sb.AppendLine("barracks,AttackBonus,5,3,\"전사와 기사의 공격력이 증가합니다.\"");
            sb.AppendLine("barracks,DefenseBonus,3,2,\"전사와 기사의 방어력이 증가합니다.\"");
            
            sb.AppendLine("archery_range,AccuracyBonus,0.05,0.02,\"궁수의 명중률이 증가합니다.\"");
            sb.AppendLine("archery_range,CriticalBonus,0.03,0.02,\"궁수의 크리티컬 확률이 증가합니다.\"");
            
            sb.AppendLine("armory,AttackBonus,3,2,\"모든 모험가의 공격력이 증가합니다.\"");
            sb.AppendLine("armory,DefenseBonus,3,2,\"모든 모험가의 방어력이 증가합니다.\"");
            
            // Support Buildings Effects
            sb.AppendLine("laboratory,ResearchSpeed,0.1,0.05,\"연구 속도가 증가합니다.\"");
            sb.AppendLine("laboratory,SkillEffect,0.05,0.03,\"스킬 효과가 증가합니다.\"");
            
            sb.AppendLine("temple,HealingBonus,0.1,0.05,\"치유 효과가 증가합니다.\"");
            sb.AppendLine("temple,ResurrectionChance,0.02,0.01,\"부활 확률이 증가합니다.\"");
            
            sb.AppendLine("library,ExpGain,0.15,0.05,\"경험치 획득량이 증가합니다.\"");
            sb.AppendLine("library,SkillLearning,0.1,0.05,\"스킬 학습 속도가 증가합니다.\"");
            
            sb.AppendLine("tavern,MoraleRecovery,5,3,\"모험가의 사기 회복 속도가 증가합니다.\"");
            sb.AppendLine("tavern,RecruitmentBonus,0.05,0.03,\"모험가 모집 성공률이 증가합니다.\"");
            
            // Economic Buildings Effects
            sb.AppendLine("warehouse,StorageCapacity,1000,500,\"자원 저장 한계가 증가합니다.\"");
            sb.AppendLine("warehouse,ResourceProtection,0.1,0.05,\"자원 손실을 방지합니다.\"");
            
            sb.AppendLine("market,TradeEfficiency,0.1,0.05,\"거래 효율이 증가합니다.\"");
            sb.AppendLine("market,GoldProduction,20,10,\"골드 생산량이 증가합니다.\"");
            
            // Special Buildings Effects
            sb.AppendLine("mage_tower,MagicPowerBonus,10,5,\"마법사의 마법 공격력이 증가합니다.\"");
            sb.AppendLine("mage_tower,ManaRegeneration,5,3,\"마나 재생 속도가 증가합니다.\"");
            
            sb.AppendLine("scout_post,ExplorationSpeed,0.2,0.1,\"탐험 속도가 증가합니다.\"");
            sb.AppendLine("scout_post,DungeonDiscovery,0.1,0.05,\"던전 발견 확률이 증가합니다.\"");
            
            sb.AppendLine("chapel,BuffDuration,0.1,0.05,\"버프 지속시간이 증가합니다.\"");
            sb.AppendLine("chapel,StatusResistance,0.1,0.05,\"상태이상 저항력이 증가합니다.\"");
            
            File.WriteAllText(csvPath, sb.ToString());
            Debug.Log($"Building effects CSV generated at: {csvPath}");
        }
        
        void GenerateBuildingProduction()
        {
            string csvPath = Path.Combine(Application.dataPath, "CSV", "building_production.csv");
            EnsureDirectoryExists(Path.GetDirectoryName(csvPath));
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("buildingId,goldPerHour,woodPerHour,stonePerHour,manaPerHour");
            
            // Production Buildings
            sb.AppendLine("guild_hall,50,0,0,0");
            sb.AppendLine("shop,100,0,0,0");
            sb.AppendLine("market,150,0,0,0");
            sb.AppendLine("tavern,80,0,0,0");
            sb.AppendLine("temple,30,0,0,10");
            sb.AppendLine("mage_tower,0,0,0,20");
            sb.AppendLine("library,20,0,0,5");
            
            // Non-production buildings
            sb.AppendLine("training_ground,0,0,0,0");
            sb.AppendLine("barracks,0,0,0,0");
            sb.AppendLine("archery_range,0,0,0,0");
            sb.AppendLine("armory,0,0,0,0");
            sb.AppendLine("laboratory,0,0,0,0");
            sb.AppendLine("warehouse,0,0,0,0");
            sb.AppendLine("scout_post,0,0,0,0");
            sb.AppendLine("chapel,10,0,0,5");
            
            File.WriteAllText(csvPath, sb.ToString());
            Debug.Log($"Building production CSV generated at: {csvPath}");
        }
        
        void GenerateSkillData()
        {
            string csvPath = Path.Combine(Application.dataPath, "CSV", "skill_data.csv");
            EnsureDirectoryExists(Path.GetDirectoryName(csvPath));
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("skillId,skillName,description,skillType,targetType,damageMultiplier,healAmount,buffType,buffAmount,buffDuration,manaCost,cooldown,range,areaOfEffect");
            
            // Warrior Skills (101-119)
            sb.AppendLine("101,기본 공격,\"대상에게 물리 피해를 입힙니다.\",Attack,Enemy,1.0,0,None,0,0,0,0,1,0");
            sb.AppendLine("102,강타,\"강력한 일격으로 150% 피해를 입힙니다.\",Attack,Enemy,1.5,0,None,0,0,20,3,1,0");
            sb.AppendLine("103,전투 함성,\"아군 전체의 공격력을 증가시킵니다.\",Buff,AllAllies,0,0,AttackUp,0.2,5,30,10,0,0");
            sb.AppendLine("104,방어 태세,\"자신의 방어력을 크게 증가시킵니다.\",Buff,Self,0,0,DefenseUp,0.5,3,15,5,0,0");
            sb.AppendLine("105,회전 베기,\"주변 적들에게 광역 피해를 입힙니다.\",Attack,Area,1.2,0,None,0,0,35,8,1,1");
            sb.AppendLine("106,광폭화,\"공격력과 속도가 증가하지만 방어력이 감소합니다.\",Buff,Self,0,0,AttackUp,0.5,10,40,15,0,0");
            sb.AppendLine("107,파괴의 일격,\"적에게 막대한 피해를 입히고 방어력을 감소시킵니다.\",Attack,Enemy,2.0,0,None,0,0,50,12,1,0");
            sb.AppendLine("108,검술의 극의,\"연속 공격으로 다수의 적을 공격합니다.\",Attack,AllEnemies,0.8,0,None,0,0,60,15,0,0");
            sb.AppendLine("109,불굴의 의지,\"일정 시간 동안 죽지 않습니다.\",Buff,Self,0,0,Immunity,1.0,3,80,30,0,0");
            sb.AppendLine("110,전설의 검기,\"전방의 모든 적에게 순수 피해를 입힙니다.\",Attack,AllEnemies,3.0,0,None,0,0,100,25,0,0");
            sb.AppendLine("111,영웅의 오라,\"모든 아군의 능력치를 대폭 상승시킵니다.\",Buff,AllAllies,0,0,AttackUp,0.3,10,120,40,0,0");
            
            // Knight Skills (201-219)
            sb.AppendLine("201,방패 타격,\"방패로 적을 공격하고 기절시킵니다.\",Attack,Enemy,0.8,0,None,0,0,15,4,1,0");
            sb.AppendLine("202,철벽 방어,\"받는 피해를 크게 감소시킵니다.\",Buff,Self,0,0,DefenseUp,0.7,5,20,8,0,0");
            sb.AppendLine("203,도발,\"주변 적들이 자신을 공격하도록 유도합니다.\",Debuff,Area,0,0,None,0,3,10,6,2,2");
            sb.AppendLine("204,신성한 방패,\"아군 한 명에게 보호막을 부여합니다.\",Buff,Ally,0,0,Shield,100,0,25,10,3,0");
            sb.AppendLine("205,심판의 망치,\"신성 피해를 입히고 언데드에게 추가 피해를 줍니다.\",Attack,Enemy,1.3,0,None,0,0,30,8,1,0");
            sb.AppendLine("206,수호자의 맹세,\"아군이 받는 피해를 대신 받습니다.\",Buff,Ally,0,0,None,0,10,35,15,3,0");
            sb.AppendLine("207,정의의 진군,\"전진하며 경로상의 모든 적을 밀어냅니다.\",Attack,Area,1.0,0,None,0,0,40,12,3,1");
            sb.AppendLine("208,왕의 격려,\"모든 아군의 방어력과 체력 재생을 증가시킵니다.\",Buff,AllAllies,0,0,DefenseUp,0.3,8,50,20,0,0");
            sb.AppendLine("209,최후의 보루,\"치명적인 피해를 1회 무시하고 체력을 회복합니다.\",Buff,Self,0,200,Immunity,1.0,2,60,30,0,0");
            sb.AppendLine("210,성스러운 진,\"범위 내 아군을 지속적으로 치유하고 보호합니다.\",Buff,Area,0,50,DefenseUp,0.2,20,80,35,3,3");
            sb.AppendLine("211,천상의 수호,\"모든 아군에게 무적 효과를 부여합니다.\",Buff,AllAllies,0,0,Immunity,1.0,3,150,60,0,0");
            
            // Wizard Skills (301-319)
            sb.AppendLine("301,파이어볼,\"화염구를 발사해 적에게 마법 피해를 입힙니다.\",Attack,Enemy,1.2,0,None,0,0,20,2,3,0");
            sb.AppendLine("302,프로스트 볼트,\"얼음 화살로 적을 공격하고 둔화시킵니다.\",Attack,Enemy,1.0,0,SpeedDown,0.3,3,25,3,3,0");
            sb.AppendLine("303,아케인 미사일,\"유도 마법탄을 발사합니다.\",Attack,Enemy,0.8,0,None,0,0,15,1,4,0");
            sb.AppendLine("304,번개 사슬,\"적들 사이를 튀어다니는 번개를 발사합니다.\",Attack,Enemy,1.5,0,None,0,0,40,6,3,0");
            sb.AppendLine("305,마나 실드,\"받는 피해를 마나로 흡수합니다.\",Buff,Self,0,0,Shield,0.5,10,30,10,0,0");
            sb.AppendLine("306,눈보라,\"광역에 지속적인 냉기 피해를 입힙니다.\",Attack,Area,0.5,0,SpeedDown,0.5,10,50,15,4,3");
            sb.AppendLine("307,순간이동,\"짧은 거리를 순간이동하고 마법 강화를 받습니다.\",Buff,Self,0,0,MagicPowerUp,0.3,5,20,8,0,0");
            sb.AppendLine("308,시간 정지,\"적들의 시간을 멈춥니다.\",Debuff,AllEnemies,0,0,SpeedDown,1.0,3,80,25,0,0");
            sb.AppendLine("309,원소 폭발,\"모든 원소의 힘을 모아 거대한 폭발을 일으킵니다.\",Attack,Area,3.0,0,None,0,0,100,30,5,5");
            sb.AppendLine("310,아케인 파워,\"마법 공격력이 대폭 상승하고 마나가 재생됩니다.\",Buff,Self,0,0,MagicPowerUp,1.0,10,60,20,0,0");
            sb.AppendLine("311,메테오,\"하늘에서 거대한 운석을 떨어뜨립니다.\",Attack,Area,5.0,0,None,0,0,150,45,6,4");
            
            // Priest Skills (401-419)
            sb.AppendLine("401,치유,\"아군의 체력을 회복시킵니다.\",Heal,Ally,0,100,None,0,0,20,2,3,0");
            sb.AppendLine("402,축복,\"아군의 모든 능력치를 소폭 상승시킵니다.\",Buff,Ally,0,0,AttackUp,0.15,10,25,8,3,0");
            sb.AppendLine("403,신성한 빛,\"언데드에게 피해를 입히고 아군을 치유합니다.\",Heal,Area,0.8,50,None,0,0,30,5,2,2");
            sb.AppendLine("404,정화,\"아군의 디버프를 제거합니다.\",Heal,Ally,0,30,None,0,0,15,3,3,0");
            sb.AppendLine("405,수호의 기도,\"아군 전체에게 보호막을 부여합니다.\",Buff,AllAllies,0,0,Shield,80,0,40,12,0,0");
            sb.AppendLine("406,부활,\"죽은 아군을 되살립니다.\",Heal,Ally,0,0.5,None,0,0,100,60,3,0");
            sb.AppendLine("407,성역,\"지정 지역에 지속적인 치유 효과를 생성합니다.\",Heal,Area,0,30,DefenseUp,0.2,15,50,20,4,3");
            sb.AppendLine("408,신의 분노,\"모든 적에게 신성 피해를 입힙니다.\",Attack,AllEnemies,1.5,0,None,0,0,60,15,0,0");
            sb.AppendLine("409,천사의 가호,\"아군 한 명을 완전히 회복시키고 강화합니다.\",Heal,Ally,0,999,AttackUp,0.5,10,80,30,3,0");
            sb.AppendLine("410,기적,\"모든 아군을 부활시키고 완전히 회복시킵니다.\",Heal,AllAllies,0,0.8,None,0,0,200,120,0,0");
            sb.AppendLine("411,신성한 진노,\"거대한 신성 폭발로 모든 악을 정화합니다.\",Attack,AllEnemies,3.0,0,None,0,0,150,40,0,0");
            
            // Assassin Skills (501-519)
            sb.AppendLine("501,기습,\"적의 뒤에서 공격해 치명타를 입힙니다.\",Attack,Enemy,1.5,0,None,0,0,15,3,1,0");
            sb.AppendLine("502,독칼,\"적을 중독시켜 지속 피해를 입힙니다.\",Attack,Enemy,0.8,0,None,0,10,20,5,1,0");
            sb.AppendLine("503,은신,\"일정 시간 동안 모습을 감춥니다.\",Buff,Self,0,0,SpeedUp,0.5,5,25,10,0,0");
            sb.AppendLine("504,연막탄,\"연막을 생성해 적의 명중률을 감소시킵니다.\",Debuff,Area,0,0,AccuracyDown,0.3,8,30,8,2,2");
            sb.AppendLine("505,그림자 분신,\"분신을 생성해 함께 공격합니다.\",Buff,Self,0,0,AttackUp,0.5,10,40,15,0,0");
            sb.AppendLine("506,암살,\"적의 급소를 노려 즉사시킬 확률이 있습니다.\",Attack,Enemy,2.5,0,None,0,0,50,12,1,0");
            sb.AppendLine("507,그림자 질주,\"빠르게 이동하며 경로상의 적을 공격합니다.\",Attack,Area,1.2,0,None,0,0,35,10,3,1");
            sb.AppendLine("508,칼날 폭풍,\"주변에 칼날을 흩뿌려 광역 피해를 입힙니다.\",Attack,Area,1.0,0,None,0,0,45,15,2,2");
            sb.AppendLine("509,죽음의 표식,\"적에게 표식을 남겨 받는 피해를 증가시킵니다.\",Debuff,Enemy,0,0,DefenseDown,0.5,10,30,20,3,0");
            sb.AppendLine("510,그림자 처형,\"체력이 낮은 적을 즉시 처형합니다.\",Attack,Enemy,5.0,0,None,0,0,80,30,1,0");
            sb.AppendLine("511,어둠의 지배자,\"그림자 속에서 무적이 되고 모든 공격이 치명타가 됩니다.\",Buff,Self,0,0,CritRateUp,1.0,8,100,45,0,0");
            
            // Ranger Skills (601-619)
            sb.AppendLine("601,정밀 사격,\"높은 명중률로 적을 공격합니다.\",Attack,Enemy,1.2,0,None,0,0,10,2,4,0");
            sb.AppendLine("602,폭발 화살,\"폭발하는 화살로 범위 피해를 입힙니다.\",Attack,Area,1.0,0,None,0,0,25,5,3,1");
            sb.AppendLine("603,추적 화살,\"적을 자동으로 추적하는 화살을 발사합니다.\",Attack,Enemy,1.3,0,None,0,0,20,4,5,0");
            sb.AppendLine("604,독 화살,\"적을 중독시키는 화살을 발사합니다.\",Attack,Enemy,0.9,0,None,0,8,22,6,4,0");
            sb.AppendLine("605,매의 눈,\"명중률과 크리티컬 확률이 증가합니다.\",Buff,Self,0,0,AccuracyUp,0.3,10,30,12,0,0");
            sb.AppendLine("606,다중 사격,\"여러 발의 화살을 동시에 발사합니다.\",Attack,AllEnemies,0.7,0,None,0,0,35,8,0,0");
            sb.AppendLine("607,바람의 가호,\"이동 속도와 공격 속도가 증가합니다.\",Buff,Self,0,0,SpeedUp,0.5,8,40,15,0,0");
            sb.AppendLine("608,관통 사격,\"적을 관통하는 강력한 화살을 발사합니다.\",Attack,Area,1.8,0,None,0,0,50,12,5,1");
            sb.AppendLine("609,화살비,\"하늘에서 화살이 쏟아집니다.\",Attack,Area,0.5,0,None,0,10,60,20,4,3");
            sb.AppendLine("610,달의 화살,\"달빛이 깃든 화살로 모든 적을 공격합니다.\",Attack,AllEnemies,2.0,0,None,0,0,80,25,0,0");
            sb.AppendLine("611,신궁의 일격,\"시공간을 초월하는 절대 명중의 화살을 발사합니다.\",Attack,Enemy,5.0,0,None,0,0,100,40,10,0");
            
            // Sage Skills (701-719)
            sb.AppendLine("701,지혜의 일격,\"물리와 마법이 조화된 공격을 가합니다.\",Attack,Enemy,1.1,0,None,0,0,15,3,2,0");
            sb.AppendLine("702,명상,\"마나를 회복하고 정신력을 강화합니다.\",Buff,Self,0,0,ManaRegen,50,5,20,8,0,0");
            sb.AppendLine("703,균형의 오라,\"주변 아군의 능력치를 균등하게 상승시킵니다.\",Buff,Area,0,0,AttackUp,0.2,10,30,12,3,3");
            sb.AppendLine("704,원소 조화,\"모든 원소 저항력이 증가합니다.\",Buff,AllAllies,0,0,DefenseUp,0.25,8,35,15,0,0");
            sb.AppendLine("705,지식의 폭발,\"정신력으로 적에게 순수 피해를 입힙니다.\",Attack,Enemy,1.5,0,None,0,0,40,10,3,0");
            sb.AppendLine("706,시공 왜곡,\"시간과 공간을 조작해 적을 혼란시킵니다.\",Debuff,Area,0,0,SpeedDown,0.7,5,50,18,4,2");
            sb.AppendLine("707,별의 인도,\"모든 아군의 명중률과 회피율을 상승시킵니다.\",Buff,AllAllies,0,0,AccuracyUp,0.2,15,45,20,0,0");
            sb.AppendLine("708,고대의 지혜,\"잊혀진 마법을 시전합니다.\",Attack,AllEnemies,2.0,0,None,0,0,70,25,0,0");
            sb.AppendLine("709,진리의 빛,\"모든 환상을 깨뜨리고 진실을 드러냅니다.\",Debuff,AllEnemies,0,0,DefenseDown,0.3,10,60,30,0,0");
            sb.AppendLine("710,우주의 조화,\"모든 아군을 완벽한 상태로 만듭니다.\",Buff,AllAllies,0,200,AttackUp,0.5,15,100,45,0,0");
            sb.AppendLine("711,창조와 파괴,\"생명과 죽음의 경계를 조작하는 궁극의 힘을 발휘합니다.\",Attack,AllEnemies,4.0,0,None,0,0,150,60,0,0");
            
            File.WriteAllText(csvPath, sb.ToString());
            Debug.Log($"Skill data CSV generated at: {csvPath}");
        }
        
        void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}