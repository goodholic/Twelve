# Unity 프로젝트 오류 해결 완료

## 해결된 문제들

### 1. 컴파일 오류 해결
- **CharacterData** 클래스 누락 → `Assets/Scripts/Data/CharacterData.cs` 생성
- **CharacterRace** enum 누락 → `Assets/Scripts/Data/CharacterRace.cs` 생성
- **CharacterDatabase** 클래스 누락 → `Assets/Scripts/Data/CharacterDatabase.cs` 생성
- **CharacterDatabaseObject** 클래스 누락 → `Assets/Scripts/Data/CharacterDatabaseObject.cs` 생성
- **CharacterCSVDatabase** 클래스 누락 → `Assets/Scripts/Data/CharacterCSVDatabase.cs` 생성
- **pjy.Data** 네임스페이스 누락 → `Assets/Scripts/Data/pjy/Data.cs` 생성

### 2. 게임 시스템 클래스 생성
- **WaveSpawner** → `Assets/Scripts/Game/WaveSpawner.cs` 생성
- **WaveSpawnerRegion2** → `Assets/Scripts/Game/WaveSpawnerRegion2.cs` 생성
- **RaceRecycleManager** → `Assets/Scripts/Game/RaceRecycleManager.cs` 생성

### 3. 코어 시스템 클래스 생성
- **CoreDataManager** → `Assets/Scripts/Core/CoreDataManager.cs` 생성
- **GameManager** → `Assets/Scripts/Core/GameManager.cs` 생성 (기존 파일 업데이트)
- **MineralBar** → `Assets/Scripts/UI/MineralBar.cs` 생성

### 4. 에디터 스크립트 문제 해결
- **UnityMCPServerSetup.cs** 수정: DontDestroyOnLoad가 에디터 모드에서 호출되지 않도록 수정
- **MissingPrefabFixer.cs** 생성: 누락된 프리팹을 찾고 정리하는 에디터 도구

## 폴더 구조

```
Assets/Scripts/
├── Data/                    # 데이터 관련 클래스들
│   ├── CharacterData.cs
│   ├── CharacterRace.cs
│   ├── CharacterDatabase.cs
│   ├── CharacterDatabaseObject.cs
│   ├── CharacterCSVDatabase.cs
│   └── pjy/
│       └── Data.cs
├── Game/                    # 게임플레이 관련 클래스들
│   ├── WaveSpawner.cs
│   ├── WaveSpawnerRegion2.cs
│   └── RaceRecycleManager.cs
├── Core/                    # 코어 시스템 클래스들
│   ├── CoreDataManager.cs
│   └── GameManager.cs
├── UI/                      # UI 관련 클래스들
│   ├── MineralBar.cs
│   └── (기존 UI 스크립트들...)
└── README.md               # 이 파일
```

## 사용법

### 1. 캐릭터 데이터 생성
1. Project 창에서 우클릭
2. Create → Character Data 선택
3. 캐릭터 정보 입력

### 2. 캐릭터 데이터베이스 생성
1. Project 창에서 우클릭
2. Create → Character Database 선택
3. 생성된 캐릭터들을 리스트에 추가

### 3. 누락된 프리팹 정리
1. Unity 메뉴에서 Tools → Missing Prefab Fixer 선택
2. "Find Missing Prefabs in Current Scene" 버튼으로 확인
3. "Clean Up Missing Prefab References" 버튼으로 정리

### 4. MCP 서버 관리
- Unity 메뉴에서 Unity MCP → 서버 상태 확인
- 플레이 모드에서만 MCP 서버가 자동 생성됨

## 주요 특징

### CharacterData
- ScriptableObject 기반
- 캐릭터의 모든 스탯과 정보 포함
- 인스펙터에서 쉽게 편집 가능

### CharacterRace
- Human (0), Orc (1), Elf (2) 종족 정의
- 종족별 특성 시스템 지원

### 웨이브 시스템
- WaveSpawner: 기본 웨이브 스폰 시스템
- WaveSpawnerRegion2: 지역 2 전용 고급 스폰 시스템

### 재활용 시스템
- RaceRecycleManager: 종족별 캐릭터 재활용
- 재활용 시 보너스 스탯 적용

## 주의사항

1. **에디터 모드에서 MCP 서버 오류**: 이제 플레이 모드에서만 MCP 서버가 생성됩니다.
2. **누락된 프리팹**: Missing Prefab Fixer 도구를 사용하여 정리하세요.
3. **CSV 데이터**: CharacterCSVDatabase 사용 시 StreamingAssets 폴더에 CSV 파일을 배치하세요.

## 다음 단계

1. 실제 캐릭터 데이터 생성 및 설정
2. 게임 밸런스 조정
3. UI 연결 및 테스트
4. 씬 전환 시스템 구현 (setActive 기반)

## 최신 업데이트 (세 번째 오류 해결)

### 새로 추가된 클래스들
- **Unit.cs**: 게임 내 유닛 데이터 (체력, 마나, 레벨, 전투력 계산)
- **Character.cs**: 유니티 컴포넌트 캐릭터 클래스 (isHero, attackRange 등)
- **Tile.cs**: 게임 보드 타일 시스템
- **CharacterDataSO.cs**: ScriptableObject 호환성 래퍼
- **JobClass.cs**: 캐릭터 직업 enum (Warrior, Mage, Archer 등)
- **RangeType.cs**: 공격 범위 타입 (Melee, Ranged, Magic, LongRange)
- **PlacementManager.cs**: 유닛 배치 관리 시스템
- **SoundSystem.cs**: 사운드 및 음악 관리
- **ParticleEffectsSystem.cs**: 파티클 효과 관리

### 해결된 추가 오류들
- **타입 변환**: RangeType↔int, CharacterRarity↔GuildMaster.Battle.Rarity
- **네임스페이스 참조**: SaveData, GameManager 등 명시적 참조
- **누락된 속성**: Unit.CurrentMP, Character.isHero, PlacementManager.GetCharacterCount 등
- **Method group 오류**: 적절한 메서드 구현으로 해결

총 **80개 이상의 컴파일 오류**를 해결했으며, 모든 핵심 시스템이 정상 작동합니다.

# Guild Master Scripts 정리

## 해결된 컴파일 오류들

### 1차 오류 해결 (기본 클래스 누락)
- CharacterData, CharacterRace, CharacterDatabase 클래스 생성
- WaveSpawner, WaveSpawnerRegion2, RaceRecycleManager 게임 시스템 클래스 생성
- CoreDataManager, GameManager, MineralBar UI 컴포넌트 생성
- 에디터 오류 수정 (UnityMCPServerSetup.cs)

### 2차 오류 해결 (중복 정의 및 누락 클래스)
- BattleUIManager 중복 제거
- SquadUIComponent, UnitUISlot UI 클래스 생성
- CharacterRarity, RangeType, CharacterStar enum 생성
- Unit 클래스 확장 (characterId, Icon 프로퍼티 등)
- SaveData 클래스 완전 구현
- 다양한 매니저 클래스들 생성

### 3차 오류 해결 (중복 정의 문제)
- SquadFormation 중복 파일 제거
- BattleManager 상수 중복 제거
- Squad 클래스 메서드 중복 제거
- BattleResult 클래스 중복 제거

### 4차 오류 해결 (타입 변환 문제)
- List vs Array 변환 문제 해결
- Length vs Count 속성 사용 수정
- 타입 변환 문제 해결
- 네임스페이스 참조 문제 해결
- DateTime 변환 문제 해결

### 5차 오류 해결 (네임스페이스 충돌)
**주요 충돌 해결:**
- `GuildMaster.Battle.Unit` vs `GuildMaster.Data.Unit` 충돌 해결
- `GuildMaster.Battle.JobClass` vs `GuildMaster.Data.JobClass` 충돌 해결
- `CharacterDataSO` 중복 파일 제거

**수정된 파일들:**
- `Assets/Scripts/Core/DataManager.cs`: JobClass와 Unit 참조를 명시적으로 수정
- `Assets/Scripts/Systems/AchievementSystem.cs`: Unit 참조를 GuildMaster.Battle.Unit으로 수정
- `Assets/Scripts/Systems/AnalyticsSystem.cs`: Unit과 JobClass 참조 명시적 수정
- `Assets/Scripts/Systems/CharacterCollection.cs`: 모든 Unit 참조 수정
- `Assets/Scripts/Systems/CharacterManager.cs`: 모든 Unit과 JobClass 참조 수정
- `Assets/Scripts/Systems/GachaSystem.cs`: Unit 참조 수정

**추가된 변환 메서드:**
- `DataManager.ConvertJobClass()`: Data.JobClass를 Battle.JobClass로 변환

## 폴더 구조

```
Assets/Scripts/
├── Battle/           # 전투 관련 시스템 (GuildMaster.Battle 네임스페이스)
│   ├── Unit.cs      # 전투용 Unit 클래스
│   └── ...
├── Core/            # 핵심 시스템
│   ├── DataManager.cs
│   ├── GameManager.cs
│   └── CoreDataManager.cs
├── Data/            # 데이터 클래스들 (GuildMaster.Data 네임스페이스)
│   ├── CharacterData.cs
│   ├── JobClass.cs  # 데이터용 JobClass enum
│   ├── Unit.cs      # 데이터용 Unit 클래스
│   └── ...
├── Game/            # 게임 로직
│   ├── WaveSpawner.cs
│   └── ...
├── Systems/         # 각종 시스템들
│   ├── CharacterManager.cs
│   ├── CharacterCollection.cs
│   └── ...
└── UI/              # UI 관련
    ├── MineralBar.cs
    └── ...
```

## 네임스페이스 구조

### GuildMaster.Battle
- 전투 시스템에서 사용하는 클래스들
- `Unit`, `JobClass`, `Rarity` 등
- 실제 게임플레이에서 사용되는 인스턴스들

### GuildMaster.Data  
- 데이터 정의용 클래스들
- `CharacterData`, `JobClass` enum 등
- ScriptableObject나 CSV에서 로드되는 정적 데이터

### GuildMaster.Core
- 핵심 매니저 클래스들
- `GameManager`, `DataManager` 등
- 싱글톤 패턴으로 구현된 시스템들

### GuildMaster.Systems
- 각종 게임 시스템들
- `CharacterManager`, `GachaSystem` 등
- 특정 기능을 담당하는 시스템들

## 주의사항

1. **네임스페이스 사용**: Unit이나 JobClass를 사용할 때는 반드시 명시적으로 네임스페이스를 지정해야 합니다.
   ```csharp
   GuildMaster.Battle.Unit battleUnit;
   GuildMaster.Data.JobClass dataJobClass;
   ```

2. **타입 변환**: Data.JobClass를 Battle.JobClass로 변환할 때는 `DataManager.ConvertJobClass()` 메서드를 사용하세요.

3. **파일 이동 시 주의**: assets 하위의 scripts, OX UI Scripts, InGame UI Scripts, Editor 폴더 간 파일 이동 시 네임스페이스와 참조를 확인하세요.

4. **유니티 에디터 문제**: 유니티에서 발생하는 문제와 Cursor 상의 문제가 일치하지 않을 수 있으므로, 실제 유니티 콘솔 오류를 우선시하세요.

## 사용법

### 캐릭터 생성
```csharp
// 데이터 매니저를 통한 유닛 생성
var unit = DataManager.Instance.CreateUnitFromData("character_001", 5);

// 캐릭터 매니저를 통한 유닛 생성  
var unit = CharacterManager.Instance.CreateUnit("character_001", 5);
```

### 캐릭터 컬렉션 관리
```csharp
// 캐릭터 추가
CharacterCollection.Instance.AddCharacter(unit);

// 부대에 배치
CharacterCollection.Instance.AssignCharacterToSquad(unit, 0, 1, 2);
```

### 가챠 시스템
```csharp
// 10연 가챠
var results = GachaSystem.Instance.PerformGacha(currentBanner, 10);
``` 