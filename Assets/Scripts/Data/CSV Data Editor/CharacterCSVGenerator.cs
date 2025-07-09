using System.Text;
using System.IO;
using UnityEngine;

public class CharacterCSVGenerator : MonoBehaviour
{
    // CSV 헤더
    private const string CSV_HEADER = "id,name,jobClass,level,rarity,baseHP,baseMP,baseAttack,baseDefense,baseMagicPower,baseSpeed,critRate,critDamage,accuracy,evasion,skill1Id,skill2Id,skill3Id,description";
    
    // 캐릭터 데이터를 생성하는 메서드
    public static string GenerateCharacterCSV()
    {
        StringBuilder csvContent = new StringBuilder();
        csvContent.AppendLine(CSV_HEADER);
        
        // 전사(Warrior) - 8명
        csvContent.AppendLine("CHAR001,철혈의 발키리아 아이샤,Warrior,1,Common,1200,100,150,120,30,80,5.0,150.0,95.0,5.0,SKILL_W001,SKILL_W002,SKILL_W003,강인한 체력과 힘을 가진 베테랑 전사");
        csvContent.AppendLine("CHAR002,폭풍의 여전사 세라핌,Warrior,1,Rare,1500,120,180,140,40,85,7.0,160.0,93.0,7.0,SKILL_W001,SKILL_W004,SKILL_W005,폭풍처럼 거친 전투를 즐기는 전사");
        csvContent.AppendLine("CHAR003,붉은 장미 로자리아,Warrior,1,Epic,1800,140,220,160,50,90,10.0,170.0,92.0,10.0,SKILL_W001,SKILL_W006,SKILL_W007,붉은 갑옷을 입은 무자비한 전사");
        csvContent.AppendLine("CHAR004,용맹한 레오나,Warrior,1,Common,1100,90,140,110,25,82,4.0,145.0,96.0,4.0,SKILL_W001,SKILL_W002,SKILL_W008,사자의 용맹함을 가진 젊은 전사");
        csvContent.AppendLine("CHAR005,철벽의 브륀힐드,Warrior,1,Rare,1600,130,170,180,35,75,6.0,155.0,94.0,6.0,SKILL_W001,SKILL_W009,SKILL_W010,방어에 특화된 수호 전사");
        csvContent.AppendLine("CHAR006,광란의 니케,Warrior,1,Epic,1700,110,250,100,45,95,12.0,180.0,90.0,8.0,SKILL_W001,SKILL_W011,SKILL_W012,분노에 휩싸인 광전사");
        csvContent.AppendLine("CHAR007,검무희 아르테미아,Warrior,1,Legendary,2000,160,280,180,60,100,15.0,200.0,98.0,12.0,SKILL_W001,SKILL_W013,SKILL_W014,전설의 검을 다루는 전사");
        csvContent.AppendLine("CHAR008,초보 전사 루나,Warrior,1,Common,1000,80,120,100,20,78,3.0,140.0,92.0,3.0,SKILL_W001,SKILL_W002,SKILL_W015,이제 막 모험을 시작한 신입 전사");
        
        // 기사(Knight) - 8명
        csvContent.AppendLine("CHAR009,성기사 세실리아,Knight,1,Rare,1400,150,140,200,80,70,4.0,140.0,96.0,3.0,SKILL_K001,SKILL_K002,SKILL_K003,신성한 힘을 가진 기사");
        csvContent.AppendLine("CHAR010,흑장미 기사 노아르,Knight,1,Epic,1600,180,180,220,100,75,6.0,150.0,94.0,5.0,SKILL_K001,SKILL_K004,SKILL_K005,어둠의 힘을 다루는 기사");
        csvContent.AppendLine("CHAR011,백은의 기사 실비아,Knight,1,Common,1300,140,130,190,70,68,3.0,135.0,97.0,2.0,SKILL_K001,SKILL_K002,SKILL_K006,빛나는 은빛 갑옷의 기사");
        csvContent.AppendLine("CHAR012,방패기사 에이지스,Knight,1,Rare,1500,160,120,250,90,65,2.0,130.0,98.0,4.0,SKILL_K001,SKILL_K007,SKILL_K008,거대한 방패를 든 수호기사");
        csvContent.AppendLine("CHAR013,용기사 드라코니아,Knight,1,Epic,1700,200,200,240,110,78,8.0,160.0,95.0,6.0,SKILL_K001,SKILL_K009,SKILL_K010,용의 힘을 가진 기사");
        csvContent.AppendLine("CHAR014,근위기사 가디아나,Knight,1,Common,1200,130,125,185,65,72,3.5,138.0,96.0,3.0,SKILL_K001,SKILL_K002,SKILL_K011,왕실을 지키는 근위기사");
        csvContent.AppendLine("CHAR015,황금기사 아우렐리아,Knight,1,Legendary,1900,220,220,280,130,80,10.0,170.0,99.0,8.0,SKILL_K001,SKILL_K012,SKILL_K013,황금빛 갑옷의 전설적인 기사");
        csvContent.AppendLine("CHAR016,견습기사 엘리자,Knight,1,Common,1100,120,110,170,60,66,2.0,125.0,94.0,2.0,SKILL_K001,SKILL_K002,SKILL_K014,기사의 길을 걷는 견습생");
        
        // 마법사(Wizard) - 8명
        csvContent.AppendLine("CHAR017,대마법사 아카디아,Wizard,1,Epic,800,400,80,60,300,85,8.0,170.0,98.0,4.0,SKILL_M001,SKILL_M002,SKILL_M003,강력한 마법을 구사하는 대마법사");
        csvContent.AppendLine("CHAR018,화염술사 피로사,Wizard,1,Rare,700,350,70,50,280,82,7.0,165.0,97.0,3.0,SKILL_M001,SKILL_M004,SKILL_M005,불꽃을 다루는 화염 마법사");
        csvContent.AppendLine("CHAR019,빙결술사 프리지아,Wizard,1,Rare,750,360,75,55,285,80,6.0,160.0,96.0,5.0,SKILL_M001,SKILL_M006,SKILL_M007,얼음 마법의 달인");
        csvContent.AppendLine("CHAR020,번개술사 일렉트라,Wizard,1,Common,650,320,65,45,260,88,5.0,155.0,95.0,6.0,SKILL_M001,SKILL_M008,SKILL_M009,번개를 부르는 마법사");
        csvContent.AppendLine("CHAR021,암흑술사 리리스,Wizard,1,Epic,850,420,85,65,320,83,9.0,175.0,94.0,7.0,SKILL_M001,SKILL_M010,SKILL_M011,어둠의 마법을 사용하는 술사");
        csvContent.AppendLine("CHAR022,시공술사 크로니아,Wizard,1,Legendary,900,450,90,70,350,90,10.0,180.0,99.0,10.0,SKILL_M001,SKILL_M012,SKILL_M013,시간과 공간을 조작하는 마법사");
        csvContent.AppendLine("CHAR023,원소술사 엘레멘티나,Wizard,1,Common,600,300,60,40,250,86,4.0,150.0,93.0,4.0,SKILL_M001,SKILL_M002,SKILL_M014,4원소를 다루는 기본 마법사");
        csvContent.AppendLine("CHAR024,견습마법사 미라벨,Wizard,1,Common,550,280,55,35,230,84,3.0,145.0,92.0,3.0,SKILL_M001,SKILL_M002,SKILL_M015,마법탑의 견습생");
        
        // 성직자(Priest) - 7명
        csvContent.AppendLine("CHAR025,대사제 세레스티나,Priest,1,Epic,900,350,60,80,250,75,3.0,130.0,99.0,8.0,SKILL_P001,SKILL_P002,SKILL_P003,신성한 힘의 대사제");
        csvContent.AppendLine("CHAR026,치유사 헤스티아,Priest,1,Common,850,320,55,75,230,72,2.0,125.0,98.0,7.0,SKILL_P001,SKILL_P004,SKILL_P005,치유에 특화된 성직자");
        csvContent.AppendLine("CHAR027,성녀 루미나리아,Priest,1,Rare,950,380,65,85,270,76,4.0,135.0,99.0,9.0,SKILL_P001,SKILL_P006,SKILL_P007,축복받은 성스러운 여성직자");
        csvContent.AppendLine("CHAR028,빛의사제 솔라리스,Priest,1,Rare,920,360,62,82,260,74,3.5,132.0,98.0,8.0,SKILL_P001,SKILL_P008,SKILL_P009,빛의 축복을 내리는 사제");
        csvContent.AppendLine("CHAR029,정화사제 퓨리피아,Priest,1,Common,880,340,58,78,240,73,2.5,128.0,97.0,7.0,SKILL_P001,SKILL_P010,SKILL_P011,악을 정화하는 성직자");
        csvContent.AppendLine("CHAR030,축복사제 베네딕타,Priest,1,Epic,980,400,68,88,280,77,5.0,140.0,99.0,10.0,SKILL_P001,SKILL_P012,SKILL_P013,강력한 축복을 내리는 사제");
        csvContent.AppendLine("CHAR031,수련사제 노비시아,Priest,1,Common,800,300,50,70,220,70,1.0,120.0,96.0,6.0,SKILL_P001,SKILL_P002,SKILL_P014,성직의 길을 걷는 수련생");
        
        // 도적(Rogue) - 7명
        csvContent.AppendLine("CHAR032,그림자 암살자 섀도우나,Rogue,1,Epic,700,200,200,70,50,120,20.0,200.0,95.0,25.0,SKILL_R001,SKILL_R002,SKILL_R003,그림자 속의 암살자");
        csvContent.AppendLine("CHAR033,도둑 실피아,Rogue,1,Common,650,180,180,65,45,115,18.0,190.0,94.0,22.0,SKILL_R001,SKILL_R004,SKILL_R005,재빠른 손재주의 도둑");
        csvContent.AppendLine("CHAR034,독살자 벨라도나,Rogue,1,Rare,720,220,190,72,55,118,19.0,195.0,93.0,24.0,SKILL_R001,SKILL_R006,SKILL_R007,독을 다루는 암살자");
        csvContent.AppendLine("CHAR035,쌍검 도적 듀엘라,Rogue,1,Rare,680,210,210,68,48,122,22.0,205.0,96.0,26.0,SKILL_R001,SKILL_R008,SKILL_R009,쌍검을 사용하는 도적");
        csvContent.AppendLine("CHAR036,밤의 추적자 녹터나,Rogue,1,Epic,750,240,220,75,60,125,25.0,210.0,97.0,28.0,SKILL_R001,SKILL_R010,SKILL_R011,어둠 속의 추적자");
        csvContent.AppendLine("CHAR037,환영술사 미라주,Rogue,1,Common,630,190,170,62,42,113,16.0,185.0,92.0,20.0,SKILL_R001,SKILL_R012,SKILL_R013,환영을 만드는 도적");
        csvContent.AppendLine("CHAR038,초보도적 루키아,Rogue,1,Common,600,170,160,60,40,110,15.0,180.0,90.0,18.0,SKILL_R001,SKILL_R002,SKILL_R014,도적 길드의 신입");
        
        // 현자(Sage) - 7명
        csvContent.AppendLine("CHAR039,대현자 소피아나,Sage,1,Legendary,1000,300,150,100,200,95,12.0,160.0,98.0,15.0,SKILL_S001,SKILL_S002,SKILL_S003,지혜와 힘을 겸비한 대현자");
        csvContent.AppendLine("CHAR040,전투현자 베리타스,Sage,1,Epic,950,280,160,95,180,92,10.0,155.0,97.0,13.0,SKILL_S001,SKILL_S004,SKILL_S005,전투에 특화된 현자");
        csvContent.AppendLine("CHAR041,지혜의 현자 미네르바,Sage,1,Rare,900,260,140,90,190,90,8.0,150.0,96.0,12.0,SKILL_S001,SKILL_S006,SKILL_S007,깊은 지혜를 가진 현자");
        csvContent.AppendLine("CHAR042,균형의 현자 에퀴나,Sage,1,Common,850,240,130,85,170,88,6.0,145.0,95.0,10.0,SKILL_S001,SKILL_S008,SKILL_S009,균형잡힌 능력의 현자");
        csvContent.AppendLine("CHAR043,예언자 카산드라,Sage,1,Rare,920,270,145,92,185,91,9.0,152.0,98.0,14.0,SKILL_S001,SKILL_S010,SKILL_S011,미래를 보는 예언자");
        csvContent.AppendLine("CHAR044,고대현자 아르카나,Sage,1,Epic,980,290,155,98,195,93,11.0,158.0,97.0,14.0,SKILL_S001,SKILL_S012,SKILL_S013,고대의 지식을 가진 현자");
        csvContent.AppendLine("CHAR045,수련현자 스콜라,Sage,1,Common,800,220,120,80,160,85,5.0,140.0,94.0,8.0,SKILL_S001,SKILL_S002,SKILL_S014,현자의 길을 걷는 수련생");
        
        // 궁수(Archer) - 7명
        csvContent.AppendLine("CHAR046,신궁 아르테나,Archer,1,Epic,850,150,180,80,60,110,15.0,180.0,99.0,18.0,SKILL_A001,SKILL_A002,SKILL_A003,전설적인 실력의 신궁");
        csvContent.AppendLine("CHAR047,바람의 궁수 에올리아,Archer,1,Rare,800,140,170,75,55,108,13.0,175.0,98.0,16.0,SKILL_A001,SKILL_A004,SKILL_A005,바람처럼 빠른 궁수");
        csvContent.AppendLine("CHAR048,사냥꾼 다이애나,Archer,1,Common,750,130,160,70,50,105,11.0,170.0,97.0,14.0,SKILL_A001,SKILL_A006,SKILL_A007,숙련된 사냥꾼");
        csvContent.AppendLine("CHAR049,정찰병 스카우티아,Archer,1,Common,720,125,155,68,48,112,10.0,165.0,96.0,15.0,SKILL_A001,SKILL_A008,SKILL_A009,민첩한 정찰병");
        csvContent.AppendLine("CHAR050,엘프궁수 엘피나,Archer,1,Rare,820,145,175,77,58,107,14.0,178.0,99.0,17.0,SKILL_A001,SKILL_A010,SKILL_A011,엘프족 궁수");
        csvContent.AppendLine("CHAR051,저격수 레티클,Archer,1,Epic,880,160,190,82,62,106,16.0,185.0,99.9,19.0,SKILL_A001,SKILL_A012,SKILL_A013,정밀한 저격수");
        csvContent.AppendLine("CHAR052,견습궁수 트레나,Archer,1,Common,700,120,150,65,45,102,9.0,160.0,95.0,12.0,SKILL_A001,SKILL_A002,SKILL_A014,궁술을 배우는 견습생");
        
        // 총사(Gunner) - 8명
        csvContent.AppendLine("CHAR053,명사수 샤를레나,Gunner,1,Epic,780,180,210,65,40,100,18.0,190.0,99.5,10.0,SKILL_G001,SKILL_G002,SKILL_G003,백발백중의 명사수");
        csvContent.AppendLine("CHAR054,기관총사 마셰나,Gunner,1,Rare,820,200,190,70,45,95,12.0,170.0,95.0,8.0,SKILL_G001,SKILL_G004,SKILL_G005,연사력의 기관총사");
        csvContent.AppendLine("CHAR055,폭발전문가 데토나,Gunner,1,Rare,850,220,200,75,50,92,14.0,180.0,94.0,7.0,SKILL_G001,SKILL_G006,SKILL_G007,폭발물 전문가");
        csvContent.AppendLine("CHAR056,쌍권총 듀얼리나,Gunner,1,Common,750,170,180,62,38,105,16.0,175.0,96.0,12.0,SKILL_G001,SKILL_G008,SKILL_G009,쌍권총 사용자");
        csvContent.AppendLine("CHAR057,대포술사 캐노니아,Gunner,1,Epic,900,240,220,80,55,88,10.0,195.0,93.0,6.0,SKILL_G001,SKILL_G010,SKILL_G011,거대한 대포의 사용자");
        csvContent.AppendLine("CHAR058,라이플우먼 리플리아,Gunner,1,Common,720,160,170,60,35,98,15.0,165.0,97.0,9.0,SKILL_G001,SKILL_G012,SKILL_G013,기본 라이플 사수");
        csvContent.AppendLine("CHAR059,전설의 총잡이 레전다,Gunner,1,Legendary,880,260,240,85,60,102,20.0,200.0,99.8,15.0,SKILL_G001,SKILL_G014,SKILL_G015,전설적인 총잡이");
        csvContent.AppendLine("CHAR060,초보총사 루키나,Gunner,1,Common,680,150,160,58,32,96,13.0,160.0,94.0,8.0,SKILL_G001,SKILL_G002,SKILL_G016,총을 다루기 시작한 초보자");
        
        return csvContent.ToString();
    }
    
    // CSV 파일로 저장하는 메서드
    public static void SaveCharacterCSVToFile(string filePath)
    {
        string csvContent = GenerateCharacterCSV();
        File.WriteAllText(filePath, csvContent);
        Debug.Log($"캐릭터 CSV 파일이 생성되었습니다: {filePath}");
    }
    
    // Unity Editor에서 실행할 수 있는 메서드
    [ContextMenu("Generate Character CSV")]
    public void GenerateCSV()
    {
        string path = Application.dataPath + "/CSV/character_data.csv";
        SaveCharacterCSVToFile(path);
    }
}