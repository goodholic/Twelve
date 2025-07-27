# 📁 Twelve Game 프로젝트 구조 가이드

## 🎯 목적
이 문서는 Twelve Game 프로젝트의 폴더 구조와 파일 배치 규칙을 설명합니다.

## 📂 최상위 폴더 구조

```
Assets/Scripts/
├── Battle/          # 게임 전투 시스템
├── Data/           # 게임 데이터 및 ScriptableObjects
├── Systems/        # 게임 시스템 (사운드, 이펙트, 최적화 등)
├── UI/            # 사용자 인터페이스
└── Editor/        # 에디터 전용 스크립트 (Unity에서 자동 인식)
```

## 🔍 각 폴더별 상세 설명

### 📁 Battle/ 폴더
게임의 핵심 전투 시스템 관련 스크립트

**포함 파일:**
- `GameManager.cs` - 게임 상태 및 턴 관리
- `BattleUIManager.cs` - 전투 화면 UI 관리
- `BoardManager.cs` - 게임 보드 관리
- `AIPlayer.cs` - AI 플레이어 로직
- `BattleSystem.cs` - 전투 시스템 코어
- `VideoCharacterController.cs` - 동영상 캐릭터 컨트롤러
- `RuntimeCharacterGenerator.cs` - 런타임 캐릭터 생성

### 📁 Data/ 폴더
게임 데이터, 타입 정의, ScriptableObjects

**하위 구조:**
```
Data/
├── CSV/                    # CSV 데이터 처리 시스템
│   ├── Import/            # CSV 임포트 관련
│   └── Utilities/         # CSV 유틸리티
├── ScriptableObjects/     # SO 데이터 객체들
└── *.cs                   # 데이터 타입 및 클래스
```

**포함 파일:**
- `CharacterData.cs` - 캐릭터 데이터 정의
- `JobClass.cs` - 직업 타입 정의
- `Rarity.cs` - 레어도 타입 정의
- `CharacterUnit.cs` - 캐릭터 유닛 클래스
- `PlaceholderTypes.cs` - 플레이스홀더 타입들

### 📁 Systems/ 폴더
게임의 전반적인 시스템들

**하위 구조:**
```
Systems/
├── Core/              # 핵심 시스템 (게임 초기화, 로딩 등)
├── Sound/             # 사운드 시스템
├── Effect/            # 이펙트 시스템
├── Notification/      # 알림 시스템
├── Leaderboard/       # 리더보드 시스템
├── Optimization/      # 최적화 관련
└── Resolution/        # 해상도 관리
```

### 📁 UI/ 폴더
사용자 인터페이스 관련 스크립트

**하위 구조:**
```
UI/
├── Menu/              # 메인 메뉴, 로비
├── Inventory/         # 인벤토리 시스템
├── Gacha/            # 가챠 시스템
├── Shop/             # 상점 시스템
├── Setting/          # 설정 화면
├── Tutorial/         # 튜토리얼 시스템
├── Upgrade/          # 업그레이드 시스템
└── Book/             # 도감 시스템
```

### 📁 Editor/ 폴더 (Unity 에디터 전용)
Unity 에디터에서만 사용되는 스크립트들

**하위 구조:**
```
Editor/
├── CharacterData/     # 캐릭터 데이터 에디터 도구
├── CSV/              # CSV 관련 에디터 도구
└── *.cs              # 기타 에디터 유틸리티
```

## 📋 파일 배치 규칙

### ✅ 올바른 배치 예시
- **데이터 클래스**: `Data/` 폴더
- **전투 관련**: `Battle/` 폴더
- **UI 스크립트**: `UI/[기능별]/` 폴더
- **시스템 스크립트**: `Systems/[기능별]/` 폴더
- **에디터 스크립트**: `Editor/[기능별]/` 폴더

### ❌ 피해야 할 배치
- 에디터 스크립트를 `Scripts/` 하위에 두기
- 기능이 다른 스크립트를 같은 폴더에 두기
- 데이터 타입을 UI 폴더에 두기

## 🔄 파일 이동 시 주의사항

### 1. 참조 업데이트
파일을 이동할 때는 다른 스크립트에서의 참조를 확인하고 업데이트해야 합니다.

### 2. 네임스페이스 확인
각 폴더별로 적절한 네임스페이스를 사용하고 있는지 확인합니다:
- `TwelveGame.Battle` - Battle 폴더
- `TwelveGame.UI` - UI 폴더
- `TwelveGame.Data` - Data 폴더
- `TwelveGame.Systems` - Systems 폴더

### 3. Assembly Definition 확인
파일 이동 후 Assembly Definition 파일 설정이 올바른지 확인합니다.

## 🛠 유지보수 가이드라인

### 새 스크립트 추가 시
1. **기능 파악**: 스크립트의 주요 기능 확인
2. **적절한 폴더 선택**: 위 구조에 따라 적절한 폴더 선택
3. **네임스페이스 적용**: 해당 폴더의 네임스페이스 적용
4. **문서 업데이트**: 필요시 이 가이드 문서 업데이트

### 폴더 구조 변경 시
1. **팀원과 상의**: 구조 변경 전 팀원들과 상의
2. **점진적 변경**: 한 번에 큰 변경보다는 점진적 변경
3. **테스트**: 변경 후 빌드 및 기능 테스트
4. **문서 업데이트**: 변경사항을 이 문서에 반영

## 📝 변경 이력

### 2024-12-XX - 폴더 구조 정리
- 에디터 스크립트를 `Assets/Editor`로 통합
- `CharacterData.cs`를 `Data/` 폴더로 이동
- `BattleUIManager` 이름 변경으로 충돌 해결
- `Rarity.cs`를 `Data/` 폴더로 이동
- 시스템 관련 파일들을 `Systems/Core/`로 정리

---

💡 **팁**: 이 구조를 따르면 코드의 가독성과 유지보수성이 크게 향상됩니다! 