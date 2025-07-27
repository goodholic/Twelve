# 🗂️ CSV 데이터 시스템 종합 가이드

## 📁 폴더 구조

```
Assets/Scripts/Data/CSV/
├── CSVDataSystemManager.cs      # 📋 메인 관리자 (Unity 메뉴 제공)
├── Import/
│   └── CSVCharacterImporter.cs  # 📥 CSV → CharacterData 변환
├── Export/
│   └── CSVCharacterExporter.cs  # 📤 CharacterData → CSV 변환
├── Utilities/
│   ├── CSVCharacterDataProcessor.cs  # ⚙️ 캐릭터 데이터 기본 설정 처리
│   ├── CSVNamingUtility.cs          # 📝 파일명 생성 및 네이밍 규칙
│   └── CSVDatabaseGenerator.cs      # 🗄️ 데이터베이스 생성
├── Validation/
│   ├── CSVSystemValidator.cs        # ✅ 시스템 유효성 검사
│   └── CSVBackupManager.cs          # 💾 백업 관리
└── CSV_SYSTEM_GUIDE.md            # 📖 이 문서
```

## 🔧 주요 기능별 분류

### 1. 📋 시스템 관리 (CSVDataSystemManager)
- **파일**: `CSVDataSystemManager.cs`
- **역할**: CSV 시스템 전체 통합 관리
- **Unity 메뉴**: `Tools/CSV System/`
  - ✅ `Import Character Data` - 캐릭터 데이터 임포트
  - 🔄 `Full System Sync` - 전체 시스템 동기화
  - 📤 `Export Character Data` - 캐릭터 데이터 내보내기
  - 🔍 `Validate System` - 시스템 유효성 검사

### 2. 📥 데이터 가져오기 (Import)
- **파일**: `CSVCharacterImporter.cs`
- **역할**: CSV 파일을 읽어서 `CharacterData` ScriptableObject들을 생성
- **지원 형식**: 
  - `character_csv_data.txt` (메인 캐릭터 데이터)
  - `ally_one_star_characters.csv`
  - `ally_two_star_characters.csv`
  - `enemy_one_star_characters.csv`
  - `enemy_two_star_characters.csv`
  - `items.csv`

### 3. 📤 데이터 내보내기 (Export)
- **파일**: `CSVCharacterExporter.cs` (예정)
- **역할**: ScriptableObject들을 CSV 파일로 내보내기
- **기능**: 
  - 기존 CSV 백업
  - 타임스탬프가 포함된 내보내기 파일 생성
  - 선택적 필드 내보내기

### 4. ⚙️ 유틸리티 (Utilities)

#### CSVCharacterDataProcessor
- **역할**: 캐릭터 데이터의 기본 설정 자동 처리
- **기능**:
  - 직업별 공격 패턴 자동 설정
  - 레어도별 스탯 조정
  - 동영상 애니메이션 기본 설정
  - 데이터 유효성 검사

#### CSVNamingUtility
- **역할**: 파일명 생성과 네이밍 규칙 관리
- **기능**:
  - 안전한 파일명 생성 (특수문자 제거)
  - 계층적 폴더 구조 생성
  - 중복 파일명 처리
  - 백업 파일명 생성

#### CSVDatabaseGenerator
- **역할**: 캐릭터 리스트로부터 데이터베이스 생성
- **기능**:
  - 통합 데이터베이스 생성
  - 직업별/레어도별 세분화 데이터베이스
  - 통계 정보 생성
  - 중복 ID 검사

## 📂 파일 경로 상수

```csharp
// 주요 폴더 경로
CSV_FOLDER_PATH = "Assets/CSV"
GENERATED_FOLDER_PATH = "Assets/Characters/Generated"
BACKUP_FOLDER_PATH = "Assets/CSV/Backups"

// CSV 파일 경로
CHARACTER_CSV_PATH = "Assets/CSV/character_csv_data.txt"
ALLY_ONE_STAR_CSV_PATH = "Assets/CSV/ally_one_star_characters.csv"
ALLY_TWO_STAR_CSV_PATH = "Assets/CSV/ally_two_star_characters.csv"
ENEMY_ONE_STAR_CSV_PATH = "Assets/CSV/enemy_one_star_characters.csv"
ENEMY_TWO_STAR_CSV_PATH = "Assets/CSV/enemy_two_star_characters.csv"
ITEMS_CSV_PATH = "Assets/CSV/items.csv"
```

## 🚀 사용법

### 1. 캐릭터 데이터 임포트
```csharp
// Unity 메뉴에서
Tools → CSV System → Import Character Data

// 코드에서
CSVDataSystemManager.ImportCharacterData();
```

### 2. 전체 시스템 동기화
```csharp
// Unity 메뉴에서
Tools → CSV System → Full System Sync

// 코드에서
CSVDataSystemManager.FullSystemSync();
```

### 3. 직접 임포터 사용
```csharp
var importer = new CSVCharacterImporter();
var characters = importer.ImportFromCSV("Assets/CSV/character_csv_data.txt");

var database = CSVDatabaseGenerator.CreateCharacterDatabase(characters);
```

## 📝 네이밍 규칙

### 파일명
- **캐릭터 데이터**: `CHAR101_철혈로젤리아.asset`
- **캐릭터 프리팹**: `CHAR101_철혈로젤리아_Prefab.prefab`
- **데이터베이스**: `CharacterDatabase.asset`
- **백업**: `CharacterDatabase_Backup_20241225_143022.asset`

### 폴더 구조
```
Assets/Characters/Generated/
├── Epic/
│   ├── Warriors/
│   ├── Knights/
│   └── Mages/
├── Rare/
│   ├── Warriors/
│   └── Archers/
└── ByJob/
    ├── CharacterDatabase_Warrior.asset
    └── CharacterDatabase_Mage.asset
```

## ⚙️ 자동 설정 규칙

### 직업별 공격 패턴
- **Warrior**: Cross (십자) + Melee (근접) + Range 1.0
- **Knight**: Cross (십자) + Melee (근접) + Range 1.0
- **Mage**: Line (직선) + Magic (마법) + Range 2.0
- **Priest**: Cross (십자) + Magic (마법) + Range 2.0
- **Rogue**: Diagonal (대각선) + Melee (근접) + Range 1.0
- **Sage**: Knight (나이트) + Magic (마법) + Range 2.0
- **Archer**: Line (직선) + Ranged (원거리) + Range 3.0

### 레어도별 기본값
- **Common**: Cost 1, Star 1
- **Uncommon**: Cost 2, Star 2
- **Rare**: Cost 3, Star 3
- **Epic**: Cost 4, Star 4
- **Legendary**: Cost 5, Star 5
- **Mythic**: Cost 6, Star 6

## 🔍 데이터 유효성 검사

### 필수 필드 검사
- ID 존재 여부
- 캐릭터 이름 존재 여부
- 기본 HP > 0
- 기본 공격력 >= 0

### 밸런스 경고
- HP > 2000 또는 < 100
- 공격력 > 300
- 크리티컬 확률 > 50%
- 명중률 > 100%

## 💾 백업 시스템

- **자동 백업**: 데이터 덮어쓰기 전 자동 백업 생성
- **백업 위치**: `Assets/CSV/Backups/`
- **백업 형식**: `파일명_backup_YYYYMMDD_HHMMSS.확장자`
- **백업 대상**: CSV 파일, 데이터베이스 파일

## 🎯 확장 계획

### 예정된 기능들
1. **CSVCharacterExporter** - 캐릭터 데이터를 CSV로 내보내기
2. **CSVSystemValidator** - 전체 시스템 유효성 검사
3. **CSVBackupManager** - 백업 관리 시스템
4. **실시간 동기화** - CSV 파일 변경 감지 및 자동 동기화
5. **배치 처리** - 대용량 데이터 처리 최적화

### 네임스페이스 정리
```csharp
namespace GuildMaster.CSV
{
    // 모든 CSV 관련 클래스들이 이 네임스페이스 사용
}
```

## 🔧 트러블슈팅

### 일반적인 문제들

1. **파일을 찾을 수 없음**
   - CSV 파일이 올바른 경로에 있는지 확인
   - `CSVDataSystemManager.CheckCSVFilesExist()` 호출

2. **컴파일 에러**
   - namespace 확인
   - using 구문 확인
   - Unity Editor 전용 코드는 `#if UNITY_EDITOR` 감싸기

3. **중복 ID 오류**
   - CSV 파일의 ID 컬럼 확인
   - `CSVDatabaseGenerator.CheckForDuplicateIds()` 사용

4. **파일명 오류**
   - `CSVNamingUtility.SanitizeFileName()` 사용
   - 특수문자 제거 확인

## 📞 지원

문제가 발생하면 다음을 확인하세요:
1. Unity Console의 에러 메시지
2. CSV 파일 형식 확인
3. 폴더 권한 확인
4. 이 가이드의 네이밍 규칙 준수 여부

---

> 📝 **마지막 업데이트**: 2024-12-25  
> 🔄 **버전**: v1.0  
> 👨‍�� **관리자**: CSV 시스템 팀 