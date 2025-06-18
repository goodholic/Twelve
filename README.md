# Twelve - Unity Tower Defense Auto-Battler

## 🎮 프로젝트 개요
- **게임 장르**: Tower Defense + 실시간 PvP 하이브리드 모바일 게임
- **게임 제목**: Twelve
- **개발 시작일**: 2025-06-18
- **Unity 버전**: 6000.2.0b5
- **대상 플랫폼**: 모바일 (Android/iOS)
- **게임 모드**: PvP(플레이어 대 플레이어) 및 PvE(플레이어 대 AI)
- **그래픽 스타일**: 2.5등신 SD 캐주얼 스타일
- **게임 분위기**: 병맛 유머 → 감동적 엔딩

### 🎯 게임 컨셉
Twelve는 명일방주의 타워 디펜스와 클래시로얄의 실시간 대전을 결합한 전략 게임입니다. 플레이어는 타일 기반 맵에서 캐릭터를 소환하고 배치하여 몬스터 웨이브를 방어하는 동시에, 상대방의 성을 공격해야 합니다.

### 🎨 게임 특징
- **쉬운 접근성**: 원버튼 소환 시스템으로 누구나 쉽게 시작
- **깊이 있는 전략**: 3종족 시너지, 라인 운영, 합성 타이밍 등 다양한 전략 요소
- **실시간 대전**: AI와 동일한 조건에서 경쟁하는 공정한 대전
- **차별화 요소**: 타워형 캐릭터가 이동하며 전투하는 독특한 시스템

### 최근 업데이트 (2025-06-18)
#### 웨이포인트 경로 시스템 구현
좌측, 중간, 우측에 캐릭터를 놓을 시 각 웨이포인트대로 지나가도록 시스템을 개선했습니다:

1. **RouteWaypointManager**: 플레이어별, 경로별 웨이포인트 관리
   - Player 1/2 각각 좌/중/우 3개 경로
   - 타일 Y좌표 기반 자동 경로 결정
   - 동적 웨이포인트 생성 지원

2. **웨이포인트 에디터 도구**: Unity 에디터에서 쉬운 경로 설정
   - 웨이포인트 추가/삭제/정렬
   - 자동 경로 생성
   - 시각적 경로 편집

3. **향상된 캐릭터 이동**: CharacterMovement 업데이트
   - 부드러운 웨이포인트 추적
   - 점프 포인트 지원
   - 전투 중 이동 일시정지

### 구현된 주요 기능

## 📋 핵심 기능 요구사항 (14개)

### 1️⃣ 소환 시스템
- **타일 기반 소환**: 플레이어/AI 각자의 소환 타일에 캐릭터 배치
- **원 버튼 소환**: 미네랄 30 소모하여 랜덤 캐릭터, 랜덤 위치 소환
- **타워형 캐릭터**: 소환된 캐릭터는 고정 타워처럼 작동
- **소환 제한**: 플레이어당 최대 50마리까지만 소환 가능

### 2️⃣ 이동/전투 시스템
- **3라인 시스템**: 왼쪽/중앙/오른쪽 웨이포인트로 구성
- **드래그 이동**: 캐릭터를 드래그하여 라인 변경 가능
- **전투 우선순위**:
  1. 적 탐지 시 → 사정거리 내에서 공격, 거리 밖이면 웨이포인트 이동 계속
  2. 웨이포인트 상실 시 → 좌측은 좌측 중간성(500HP), 우측은 우측 중간성(500HP) 목표
  3. 중간성 파괴 시 → 최종성(1000HP) 목표로 이동

### 3️⃣ 합성 시스템
- **자동 합성**: 주사위 버튼으로 같은 등급 3개 자동 합성
- **합성 규칙**: 1성×3 → 2성, 2성×3 → 3성
- **위치**: 합성된 캐릭터는 가장 뒤쪽 캐릭터 위치에 생성
- **타워 스택**: 같은 캐릭터끼리는 한 타일에 3개까지 스택 가능

### 4️⃣ 종족 시스템
- **3종족**: 휴먼(Human), 오크(Orc), 엘프(Elf)
- **덱 구성**: 각 종족 3명 + 자유 1명 (총 10명)
- **리사이클**: 특정 종족을 다른 2종족 중 하나로 변환
- **종족 시너지**: 같은 종족이 많을수록 해당 종족 강화 효과 증가

### 5️⃣ 강화 시스템
- **업그레이드** (인게임 전):
  - 같은 종족/등급 캐릭터 + 10골드 = 경험치 1%
  - 100% = 1레벨업 (최대 30레벨)
- **강화** (인게임 중):
  - 종족별 강화 버튼 → 공격력/공격속도 5% 증가

### 6️⃣ 전투 진행
- **웨이브**: 5마리 몬스터 동시 생성 (3라인에 분배)
- **성 체력**: 중간성 3개(각 500HP), 최종성 1개(1000HP)
- **체력바**: 성, 캐릭터, 몬스터 머리 위 표시
- **AI 플레이어**: 플레이어와 동일한 행동 가능 (isAI 플래그로 구분)

### 7️⃣ 추가 시스템
- **5웨이브 보상**: 5, 10, 15웨이브 클리어 시 랜덤 2성 캐릭터 3개 중 1개 선택
- **CSV 연동**: ScriptableObject ↔ CSV 양방향 동기화
- **100개 스테이지**: 점진적 난이도 상승
- **가챠 시스템**: 단일/10연차 캐릭터 뽑기

## 🎯 게임 구조 정리

### 게임 플로우
```
사전 준비 → 인게임 전투 → 결과/보상
   ↓            ↓           ↓
덱 구성      웨이브 방어   캐릭터 획득
업그레이드    실시간 대전   다음 전투 준비
```

### 전투 매커니즘
1. **방어**: 자신의 진영으로 오는 몬스터 웨이브 방어
2. **공격**: 소환한 캐릭터가 상대 진영으로 진격
3. **전략**: 3라인 운영, 합성 타이밍, 종족 시너지 활용

### 핵심 전략 요소
- **종족 조합**: 휴먼/오크/엘프 시너지 극대화
- **라인 배치**: 3라인별 캐릭터 분배 및 이동
- **합성 타이밍**: 효율적인 캐릭터 성장 관리
- **리사이클 전략**: 종족 통일을 통한 강화 이점

## 기능 명세

### 핵심 기능
- [x] 기본 게임 구조 (Tower Defense + Auto-Battler)
- [x] 웨이브 기반 전투 (20웨이브)
- [x] 캐릭터 배치 및 합성 메커니즘
- [x] 가챠/소환 시스템
- [x] Canvas에서 World 2D 좌표계로 전환 (WorldTileGenerator 구현 완료)
- [x] 타워 스택 시스템 (같은 캐릭터 3개까지 한 위치에)
- [x] 향상된 성 전투 시스템
- [x] 캐릭터 소환 제한 (50마리)
- [x] 고급 AI 플레이어 시스템

### 기술 구현 계획

#### 좌표계 전환 (Canvas → World 2D)
- **현재**: UI Canvas 기반 캐릭터 배치
- **목표**: World Space 2D 좌표계로 전환
- **구현 방향**:
  - PlacementManager를 World Space 기반으로 수정
  - 캐릭터 프리팹을 World Space 오브젝트로 변환
  - HP바는 World Space Canvas 유지

#### 타워 스택 시스템
- **TowerStackSystem.cs**: 같은 캐릭터 스택 관리
- **시각적 표현**: 스택 수에 따른 크기 조정
- **제한**: 한 타일에 최대 3개까지

#### 전투 시스템 개선
- **CastleAttackSystem.cs**: 성의 공격 로직
- **타겟팅 확장**: Monster, Character, Castle 타입
- **공격 우선순위 시스템**

#### AI 시스템
- **BasePlayer.cs**: 플레이어 기본 클래스
- **AIBrain.cs**: AI 의사결정 로직
- **isAI 플래그**: 플레이어 타입 구분

## 파일 구조
```
Assets/
├── Scripts/                    # 핵심 게임 로직
│   ├── pjy/                   # 주요 게임플레이 스크립트
│   │   ├── Characters/        # 캐릭터 관련
│   │   ├── Managers/          # 매니저 클래스
│   │   ├── Data/             # 데이터 구조
│   │   └── Gameplay/         # 게임플레이 로직
│   ├── Character.cs          # 캐릭터 기본 클래스
│   ├── GameManager.cs        # 게임 상태 관리
│   ├── PlacementManager.cs   # 캐릭터 배치
│   └── WaveSpawner.cs        # 웨이브 시스템
├── OX UI Scripts/             # 로비 및 메타게임 UI
├── InGame UI Scripts/         # 인게임 UI 컴포넌트
├── Editor/                    # 에디터 확장 도구
│   ├── DataExportEditor.cs  # 데이터 내보내기
│   └── CSVDataSyncEditor.cs  # CSV 동기화
├── CSV/                       # 게임 데이터 파일
└── Prefabs/
    └── Data/                  # ScriptableObject 데이터
```

## 개발 현황 및 로드맵

### 🟢 완성된 기능 (구현 완료)

#### 1. 핵심 게임 시스템
- **✅ 캐릭터 소환 제한** (`Assets/Scripts/pjy/Managers/PlacementManager.cs`)
  - 플레이어/AI 각각 최대 50마리 제한
  - CanSummonCharacter() 메서드로 제한 체크
  - GetCharacterCount() 메서드로 현재 수 확인
  - UI와 연동하여 실시간 표시

- **✅ 타워 스택 시스템** (`Assets/Scripts/pjy/Gameplay/Tile.cs`)
  - 같은 캐릭터 3개까지 한 타일에 배치 가능
  - 시각적 위치 조정: 1개(100%), 2개(80%, 좌우), 3개(70%, 삼각형)
  - UpdateCharacterPositions() 메서드로 자동 배치

- **✅ 향상된 성 전투 시스템** (`Assets/Scripts/pjy/Gameplay/EnhancedCastleSystem.cs`)
  - 성의 공격 능력: 중간성 30, 최종성 50 공격력
  - 방어 버프: 주변 아군에게 20% 방어력 증가
  - 범위 공격: 3명 이상 적 근처 시 발동
  - CastleHealthManager와 연동하여 체력 동기화
- **✅ GameManager** (`Assets/Scripts/pjy/Managers/GameManager.cs`)
  - 게임 상태 관리 (시작, 일시정지, 종료)
  - 웨이브 진행 시스템 (20웨이브)
  - 승리/패배 조건 처리
  - 플레이어 등록 및 관리

- **✅ WaveSpawner** (`Assets/Scripts/pjy/Managers/WaveSpawner.cs`)
  - 101개 챕터별 몬스터 프리팹 관리
  - 3라인 동시 스폰 시스템
  - 5웨이브마다 보상 시스템
  - 챕터별 몬스터 교체 로직

- **✅ PlacementManager** (`Assets/Scripts/pjy/Managers/PlacementManager.cs`)
  - 캐릭터 배치 시스템
  - 50마리 제한 구현
  - Missing Script 자동 해결 기능
  - 타일 기반 배치 확인

#### 2. 캐릭터 시스템
- **✅ Character 기본 클래스** (`Assets/Scripts/pjy/Characters/Character.cs`)
  - 컴포넌트 기반 아키텍처
  - IDamageable 인터페이스 구현
  - 스탯 관리 시스템
  - 타일 배치 및 경로 선택

- **✅ CharacterCombat** (`Assets/Scripts/pjy/Characters/CharacterCombat.cs`)
  - 타겟 탐지 및 공격 시스템
  - 범위/광역 공격 구현
  - 데미지 계산 로직
  - 공격 이펙트 재생

- **✅ CharacterMovement** (`Assets/Scripts/pjy/Characters/CharacterMovement.cs`)
  - 웨이포인트 기반 이동
  - 3개 경로 선택 시스템
  - 점프 포인트 지원
  - 전투 중 일시정지

- **✅ Monster** (`Assets/Scripts/pjy/Characters/Monster.cs`)
  - 웨이포인트 경로 추적
  - 성 공격 시스템
  - 챕터별 스탯 스케일링
  - 상태이상 효과 (슬로우, 출혈, 스턴)

#### 3. 전투 및 타겟팅
- **✅ CastleAttackSystem** (`Assets/Scripts/pjy/Gameplay/CastleAttackSystem.cs`)
  - 성의 자동 공격 구현
  - 다중 타겟 시스템
  - 우선순위 기반 타겟팅

- **✅ TowerStackSystem** (`Assets/Scripts/pjy/Characters/TowerStackSystem.cs`)
  - 같은 캐릭터 3개까지 스택
  - 시각적 크기 조정
  - 스택별 공격력 증가

#### 4. 자원 및 소환
- **✅ SummonManager** (`Assets/Scripts/pjy/Managers/SummonManager.cs`)
  - 원버튼 랜덤 소환
  - 미네랄 30 소비 시스템
  - 랜덤 위치 배치
  - 소환 이펙트

- **✅ MergeManager** (`Assets/Scripts/pjy/Managers/MergeManager.cs`)
  - 3개 합성 → 상위 등급
  - 타일 기반 합성 감지
  - 합성 애니메이션
  - StarMergeDatabase 연동

#### 5. AI 시스템
- **✅ BasePlayer/AIPlayer/HumanPlayer** (`Assets/Scripts/pjy/Gameplay/`)
  - 플레이어 베이스 클래스 구조
  - AI/Human 구분 시스템
  - isAI 플래그 기반 동작

- **✅ AIBrain** (`Assets/Scripts/pjy/Gameplay/AIBrain.cs`)
  - AI 의사결정 가중치 시스템
  - 상태 분석 (위협도, 경제력)
  - 학습률 기반 개선
  - 결정 기록 저장

#### 6. UI 시스템
- **✅ LobbySceneManager** (`Assets/Scripts/pjy/UI/LobbySceneManager.cs`)
  - 100개 스테이지 관리
  - 저장/불러오기 시스템
  - 스테이지 잠금/해제
  - 골드/다이아몬드 표시

- **✅ DeckPanelManager** (`Assets/Scripts/pjy/UI/DeckPanelManager.cs`)
  - 10칸 덱 구성 (종족별 3칸 + 자유 1칸)
  - 드래그 앤 드롭 UI
  - 덱 저장/불러오기
  - 종족 제한 검증

- **✅ DrawPanelManager** (`Assets/Scripts/pjy/UI/DrawPanelManager.cs`)
  - 가챠 시스템 UI
  - 단일/10연차 구현
  - 인벤토리 200칸 관리
  - 확률 표시

#### 7. 데이터 관리
- **✅ CharacterDatabase** (`Assets/Scripts/pjy/Data/CharacterDatabase.cs`)
  - ScriptableObject 기반
  - CSV 동기화 시스템
  - 캐릭터 데이터 구조

- **✅ CSVToCharacterDataTool** (`Assets/Editor/CSVToCharacterDataTool.cs`)
  - CSV → ScriptableObject 변환
  - 다중 파일 일괄 처리
  - 데이터 검증

### 🟡 부분 구현 (개선 필요)

#### 1. 웨이포인트 시스템
- **⚠️ RouteWaypointManager** (`Assets/Scripts/pjy/Managers/RouteWaypointManager.cs`)
  ```csharp
  // TODO: 동적 웨이포인트 생성 시 정렬 로직 개선 필요
  // TODO: 점프 포인트 시각화 개선
  ```

#### 2. 자동 합성
- **⚠️ AutoMergeManager** (`Assets/Scripts/pjy/Managers/AutoMergeManager.cs`)
  ```csharp
  // TODO: 합성 우선순위 커스터마이징
  // TODO: 특정 캐릭터 합성 제외 옵션
  ```

#### 3. 캐릭터 인벤토리
- **⚠️ CharacterInventoryManager** (`Assets/Scripts/pjy/Managers/CharacterInventoryManager.cs`)
  - 초기화 타이밍 문제 존재
  - null 캐릭터 데이터 처리 필요
  - 강제 초기화 로직 임시 해결책

### 🔴 미구현 기능 (개발 예정)

#### 1. 네트워킹 시스템
- **❌ Multiplayer 지원**
  ```csharp
  // using Fusion; // 임시로 주석처리
  // NetworkRunner 관련 코드 비활성화
  ```
  - Unity Netcode 설정은 있으나 미구현
  - 실시간 PvP 매칭 시스템 필요
  - 동기화 로직 구현 필요

#### 2. 고급 AI 기능
- **✅ AI 학습 시스템** (`Assets/Scripts/pjy/AI/`)
  - AdvancedAISystem: 패턴 인식 및 학습 알고리즘 구현
  - AILearningData: ScriptableObject 기반 학습 데이터 영구 저장
  - EnhancedAIBrain: 전술적 의사결정 및 행동 실행
  - 구현된 기능:
    - 3-그램 패턴 인식 알고리즘
    - 상대 전략 분석 (Aggressive/Defensive/Economic/Balanced)
    - 동적 난이도 조절 (승률 40-60% 목표)
    - 학습률 기반 파라미터 최적화
    - 전술 모드 자동 전환

#### 3. 특수 시스템
- **❌ 길드 시스템**
  - 길드 생성/가입/탈퇴
  - 길드전 매칭
  - 길드 보상

- **❌ 시즌 시스템**
  - 시즌별 컨텐츠
  - 랭킹 초기화
  - 시즌 보상

- **❌ 업적 시스템**
  - 다양한 업적 조건
  - 보상 시스템
  - 진행도 추적

#### 4. 추가 컨텐츠
- **❌ 신규 종족 (Undead)**
  - 언데드 전용 메커니즘
  - 특수 능력 구현
  - 밸런스 조정

- **✅ 보스 몬스터** (`Assets/Scripts/pjy/Characters/BossMonster.cs`)
  - BossMonster: 3페이즈 시스템 구현
  - BossSpawnManager: 특정 웨이브(5, 10, 15, 20)에서 보스 자동 스폰
  - BossHealthBarUI: 화면 상단 보스 전용 체력바
  - 구현된 기능:
    - 페이즈별 패턴 변화 (광역 공격, 미니언 소환, 버프/디버프)
    - 체력 기반 페이즈 전환 (70%, 40%, 0%)
    - 챕터별 보스 강화 시스템
    - 특별 보상 (골드 5배, 다이아몬드)

### 📋 버그 및 개선사항

#### 우선순위 높음
1. **CharacterInventoryManager 초기화 문제**
   - 증상: null 캐릭터 데이터 발견
   - 임시해결: CreateDefaultCharacters()
   - 근본해결: 초기화 순서 재설계 필요

2. **UI 스케일링 문제**
   - 다양한 해상도에서 UI 깨짐
   - Canvas Scaler 설정 조정 필요

#### 우선순위 중간
1. **성능 최적화**
   - 50마리 이상 시 프레임 드롭
   - 오브젝트 풀링 확대 적용 필요

2. **밸런스 조정**
   - 챕터별 난이도 곡선 개선
   - 캐릭터 스탯 밸런싱

#### 우선순위 낮음
1. **비주얼 개선**
   - 파티클 이펙트 추가
   - 애니메이션 다양화
   - 사운드 효과 구현

### 🚀 개발 로드맵

#### Phase 1 (현재 - 1개월)
- [x] 캐릭터 소환 제한 시스템 구현 완료
- [x] 향상된 성 전투 시스템 구현 완료
- [ ] CharacterInventoryManager 초기화 버그 수정
- [ ] 기본 사운드 시스템 구현
- [ ] 튜토리얼 시스템 구현
- [ ] 밸런스 1차 조정

#### Phase 2 (1-2개월)
- [ ] 멀티플레이어 기초 구현
- [ ] AI 고급 기능 추가
- [ ] 신규 캐릭터 10종 추가
- [ ] 성능 최적화

#### Phase 3 (2-3개월)
- [ ] 길드 시스템 구현
- [ ] 시즌 시스템 도입
- [ ] 보스 몬스터 추가
- [ ] 신규 종족 출시

#### Phase 4 (3개월 이후)
- [ ] 글로벌 출시 준비
- [ ] 추가 언어 지원
- [ ] 라이브 서비스 시작
- [ ] 지속적인 컨텐츠 업데이트

## 주요 클래스 설계

### TowerStackSystem
- 같은 ID의 캐릭터 스택 관리
- 시각적 크기 조정
- 스택 제한 (최대 3개)

### CastleAttackSystem
- 성의 자동 공격 구현
- 다중 타겟 시스템
- 공격 우선순위 설정

### RouteWaypointManager
- 플레이어별 독립적인 경로 관리 (P1/P2)
- 3개 경로 (좌/중/우) × 2 플레이어 = 총 6개 경로
- Y좌표 기반 자동 경로 결정
- 에디터 도구로 시각적 웨이포인트 편집
- 동적 웨이포인트 생성 및 정렬

### AIPlayer
- BasePlayer 상속
- AIBrain 컴포넌트 사용
- 자동 의사결정 시스템

## Unity 엔진 작업 가이드 (처음부터 끝까지)

### 1단계: 개발 환경 설정

#### Unity 설치 및 프로젝트 설정
1. **Unity Hub 설치**
   - Unity Hub 공식 홈페이지에서 다운로드
   - Unity 6000.2.0b5 버전 설치
   - Visual Studio 2022 또는 Visual Studio Code 연동 설정

2. **프로젝트 열기**
   ```
   1. Unity Hub 실행
   2. "Open" 버튼 클릭
   3. 프로젝트 경로 선택: C:\Users\super\OneDrive\문서\GitHub\Twelve
   4. Unity 버전 6000.2.0b5 선택하여 프로젝트 열기
   ```

3. **초기 설정 확인**
   - Build Settings: Android/iOS 플랫폼 설정
   - Player Settings: 앱 이름, 아이콘, 해상도 설정
   - Quality Settings: 모바일 최적화 설정

### 2단계: 프로젝트 구조 이해

#### 핵심 폴더 구조
```
Assets/
├── CSV/                           # 게임 데이터 (캐릭터, 아이템 등)
├── Editor/                        # 에디터 확장 도구들
├── Prefabs/                       # 게임 오브젝트 프리팹들
│   └── Data/                      # ScriptableObject 데이터
├── Scripts/
│   ├── pjy/                       # 메인 게임 로직
│   │   ├── Characters/            # 캐릭터 관련 스크립트
│   │   ├── Data/                  # 데이터 구조체들
│   │   ├── Gameplay/              # 게임플레이 로직
│   │   ├── Managers/              # 시스템 매니저들
│   │   └── UI/                    # UI 관련 스크립트
│   ├── Character.cs               # 캐릭터 기본 클래스
│   ├── GameManager.cs             # 게임 상태 관리
│   └── [기타 핵심 스크립트들]
├── Scenes/                        # 게임 씬들
│   ├── LobbyScene.unity           # 메인 메뉴
│   └── GameScene.unity            # 실제 게임플레이
└── Resources/                     # 런타임 리소스 로딩용
```

### 3단계: 게임 데이터 설정

#### CSV 데이터 관리
1. **캐릭터 데이터 편집**
   ```
   Assets/CSV/ 폴더의 파일들:
   - ally_one_star_characters.csv    (1성 아군)
   - ally_two_star_characters.csv    (2성 아군)
   - ally_three_star_characters.csv  (3성 아군)
   - enemy_one_star_characters.csv   (1성 적군)
   - items.csv                       (아이템 데이터)
   ```

2. **ScriptableObject 생성**
   ```
   Tools > Character Tools > CSV to ScriptableObject Converter
   1. CSV 파일들 선택
   2. 출력 폴더 설정
   3. 변환 실행
   4. Assets/Prefabs/Data/Characters/ 확인
   ```

3. **데이터베이스 연결**
   ```
   Assets/Prefabs/Data/CharacterDatabase.asset
   1. Inspector에서 Characters 배열 확장
   2. 생성된 CharacterData들을 드래그 앤 드롭
   3. Apply 클릭하여 저장
   ```

### 4단계: 씬 설정 및 매니저 구성

#### LobbyScene 설정
1. **필수 매니저 오브젝트들**
   ```
   - LobbySceneManager (UI 관리)
   - CharacterInventoryManager (캐릭터 인벤토리)
   - DeckPanelManager (덱 구성)
   - DrawPanelManager (가챠 시스템)
   - ShopManager (상점)
   ```

2. **UI 연결 확인**
   ```
   각 매니저의 Inspector에서:
   - UI 패널들이 올바르게 연결되었는지 확인
   - 버튼 이벤트가 설정되었는지 확인
   - 데이터베이스 참조가 올바른지 확인
   ```

#### GameScene 설정
1. **핵심 게임 매니저들**
   ```
   - GameManager (게임 상태 총괄)
   - WaveSpawner (웨이브 관리)
   - PlacementManager (캐릭터 배치)
   - TileManager (타일 시스템)
   - SummonManager (소환 시스템)
   - MergeManager (합성 시스템)
   ```

2. **필수 설정 항목들**
   ```
   GameManager:
   - Wave Spawner 연결
   - Placement Manager 연결
   - 결과 UI 패널 연결
   
   WaveSpawner:
   - 몬스터 프리팹 배열 (101개 챕터)
   - 웨이포인트 설정 (좌/중/우 × 2플레이어)
   - 성 오브젝트 연결 (중간성 3개, 최종성 1개)
   
   PlacementManager:
   - Tile Manager 연결
   - Character Database 연결
   ```

### 5단계: 타일 시스템 구축 (월드 좌표)

#### Canvas에서 World Space로 전환
기존 Canvas 기반 타일 시스템을 World Space 2D로 전환하는 방법:

##### 1. WorldTileGenerator 사용법
1. **새로운 GameObject 생성**
   ```
   1. Hierarchy에서 Create Empty
   2. 이름을 "TileGenerator"로 변경
   3. WorldTileGenerator.cs 컴포넌트 추가
   ```

2. **타일 프리팹 준비**
   ```
   타일 프리팹 구조:
   - GameObject (Tile)
     ├── SpriteRenderer (타일 비주얼)
     ├── Collider2D (클릭/배치 감지)
     └── Tile.cs (타일 로직)
   ```

3. **WorldTileGenerator 설정**
   ```
   Inspector 설정:
   - Tile Prefab: 기본 타일 프리팹 연결
   - Grid Rows: 7 (기본값)
   - Grid Columns: 12 (기본값)
   - Tile Size: 1.0 (Unity 단위)
   - Tile Spacing: 0.1 (타일 간격)
   - Grid Start Position: (-6, -3, 0)
   - Center Grid: ✓ (중앙 정렬)
   ```

##### 2. 타일 영역 구성
```
7x12 그리드 구조:
┌─────────────────────────┐
│ Player 2 영역 (3줄)     │ → Placeable2 타일
├─────────────────────────┤
│ 중립 영역 (1줄)         │ → Walkable 타일 (3라인)
├─────────────────────────┤
│ Player 1 영역 (3줄)     │ → Placeable 타일
└─────────────────────────┘
```

##### 3. 월드 좌표 계산 방식
```csharp
// 타일 위치 계산 예시
float xPos = startPos.x + (col * (tileSize + tileSpacing));
float yPos = startPos.y + (row * (tileSize + tileSpacing));
Vector3 tilePosition = new Vector3(xPos, yPos, 0);

// 중앙 정렬 계산
if (centerGrid)
{
    float gridWidth = (columns * tileSize) + ((columns - 1) * spacing);
    float gridHeight = (rows * tileSize) + ((rows - 1) * spacing);
    startPos.x -= gridWidth / 2f;
    startPos.y -= gridHeight / 2f;
}
```

##### 4. 기존 TileGridEditor와의 연동
기존 Canvas 방식의 타일 설정을 월드 좌표로 변환:

1. **TileGridEditor 사용 (에디터에서 수동 배치)**
   ```
   - 씬에 타일을 수동으로 배치
   - TileGridEditor로 타일 타입 일괄 설정
   - 7x12 그리드 참조로 관리
   ```

2. **WorldTileGenerator 사용 (런타임 자동 생성)**
   ```
   - 게임 시작 시 자동으로 타일 생성
   - 프리팹 기반으로 일관된 타일 생성
   - 동적 레벨 생성 가능
   ```

##### 5. 타일 타입별 설정
```csharp
// Player 1 영역 (하단 3줄)
if (row < player1Rows) {
    tile.tileType = Tile.TileType.Placeable;
    tile.isRegion2 = false;
}
// Player 2 영역 (상단 3줄)
else if (row >= gridRows - player2Rows) {
    tile.tileType = Tile.TileType.Placeable2;
    tile.isRegion2 = true;
}
// 중립 영역 (중간 1줄) - 3라인 시스템
else {
    if (col < 4) tile.tileType = Tile.TileType.WalkableLeft;
    else if (col < 8) tile.tileType = Tile.TileType.WalkableCenter;
    else tile.tileType = Tile.TileType.WalkableRight;
}
```

##### 6. 월드 좌표 타일 검색
```csharp
// 특정 위치의 타일 찾기
Tile GetTileAtWorldPosition(Vector3 worldPos)
{
    foreach (Tile tile in allTiles)
    {
        float distance = Vector3.Distance(tile.transform.position, worldPos);
        if (distance < tileSize / 2f)
        {
            return tile;
        }
    }
    return null;
}
```

##### 7. 카메라 설정 (World Space용)
```
Main Camera 설정:
- Position: (0, 0, -10)
- Orthographic Size: 5-7 (그리드가 화면에 맞게)
- Projection: Orthographic
```

##### 8. 타일 시각화 옵션
```
색상 구분:
- Player 1 타일: 파란색 (0.3f, 0.5f, 1f, 0.5f)
- Player 2 타일: 빨간색 (1f, 0.5f, 0.3f, 0.5f)
- 중립 타일: 회색 (0.5f, 0.5f, 0.5f, 0.5f)
```

#### 타일 생성 모범 사례

1. **성능 최적화**
   - 타일을 한 번에 생성하고 재사용
   - 불필요한 Update() 호출 최소화
   - 타일 풀링 시스템 고려

2. **유연한 설계**
   - 타일 크기와 간격을 Inspector에서 조정 가능
   - 다양한 타일 프리팹 지원
   - 런타임 타일 타입 변경 가능

3. **디버그 기능**
   - Scene 뷰에서 그리드 영역 시각화
   - 타일 인덱스와 타입 표시
   - 클릭한 타일 정보 출력

### 6단계: 캐릭터 및 몬스터 제작

#### 캐릭터 프리팹 제작
1. **기본 구조 생성**
   ```
   캐릭터 GameObject 생성:
   1. Create Empty GameObject
   2. 이름을 캐릭터명으로 변경
   3. 필수 컴포넌트 추가:
      - Character.cs (메인 컴포넌트)
      - CharacterStats.cs
      - CharacterCombat.cs
      - CharacterMovement.cs
      - CharacterVisual.cs
      - SpriteRenderer
      - Collider2D (Trigger)
      - Rigidbody2D
   ```

2. **컴포넌트 설정**
   ```
   Character.cs 설정:
   - Character Data 연결
   - 스프라이트 할당
   - 스탯 설정 (CSV 데이터 기반)
   
   CharacterCombat.cs 설정:
   - 공격 대상 타입
   - 공격 범위
   - 공격 이펙트 프리팹
   
   CharacterMovement.cs 설정:
   - 이동 속도
   - 경로 설정 (RouteType)
   ```

3. **프리팹 저장**
   ```
   Assets/Prefabs/Characters/ 폴더에 저장
   - 각 캐릭터별로 별도 프리팹 생성
   - 프리팹 이름은 CharacterData와 일치시키기
   ```

#### 몬스터 프리팹 제작
1. **Monster 오브젝트 생성**
   ```
   필수 컴포넌트:
   - Monster.cs
   - SpriteRenderer
   - Collider2D
   - Rigidbody2D
   - 체력 바 (World Canvas)
   ```

2. **웨이포인트 이동 설정**
   ```
   Monster.cs에서:
   - 웨이포인트 배열 설정
   - 이동 속도 조정
   - 성 공격 로직 확인
   ```

### 6단계: 게임플레이 시스템 구현

#### 전투 시스템 설정
1. **공격 시스템**
   ```
   CharacterCombat.cs 수정:
   - 타겟 탐지 로직
   - 데미지 계산
   - 이펙트 재생
   - 사운드 효과
   ```

2. **체력 시스템**
   ```
   IDamageable 인터페이스 구현:
   - TakeDamage() 메서드
   - 체력 바 업데이트
   - 사망 처리
   ```

#### 합성 시스템 구현
1. **MergeManager 설정**
   ```
   - 같은 캐릭터 3개 감지
   - 타일 위치 확인
   - 합성 애니메이션
   - 상위 등급 캐릭터 생성
   ```

2. **AutoMergeManager (선택사항)**
   ```
   - 자동 합성 조건 설정
   - UI 토글 연결
   - 합성 우선순위 설정
   ```

### 7단계: UI 시스템 구현

#### 인게임 UI
1. **필수 UI 요소들**
   ```
   - 웨이브 카운터
   - 미네랄 표시
   - 소환 버튼
   - 성 체력 바
   - 캐릭터 카운트 (현재/최대)
   - 게임 속도 조절 버튼
   - 보스 체력바 (화면 상단)
   ```

2. **UI 이벤트 연결**
   ```
   각 버튼의 OnClick 이벤트:
   - 소환 버튼 → SummonManager.SummonCharacter()
   - 속도 버튼 → Time.timeScale 조정
   - 일시정지 → GameManager.PauseGame()
   ```

#### 로비 UI
1. **스테이지 선택**
   ```
   LobbySceneManager.cs:
   - 100개 스테이지 버튼 생성
   - 잠금/해제 상태 관리
   - 클리어 기록 표시
   ```

2. **가챠 시스템 UI**
   ```
   DrawPanelManager.cs:
   - 단일/10연차 버튼
   - 확률 표시
   - 애니메이션 효과
   - 결과 팝업
   ```

### 8단계: 보스 시스템 구현

#### 보스 몬스터 설정
1. **BossMonster 프리팹 제작**
   ```
   1. Monster 프리팹을 복제
   2. BossMonster.cs 컴포넌트 추가
   3. 보스 설정:
      - Boss Type: ChapterBoss
      - Max Phases: 3
      - Health Multiplier: 5
      - Damage Multiplier: 2
      - Phase Health Thresholds: [0.7, 0.4, 0]
   ```

2. **BossSpawnManager 설정**
   ```
   GameScene에 추가:
   1. Create Empty → "BossSpawnManager"
   2. BossSpawnManager.cs 추가
   3. Boss Waves 설정: [5, 10, 15, 20]
   4. 챕터별 보스 프리팹 연결
   ```

3. **보스 체력바 UI**
   ```
   Canvas에 추가:
   1. 보스 전용 체력바 프리팹 생성
   2. 화면 상단에 배치
   3. BossHealthBarUI.cs 연결
   4. 페이즈 인디케이터 3개 추가
   ```

4. **WaveSpawner 연동**
   ```
   WaveSpawner에 추가:
   1. WaveSpawnerBossIntegration.cs 추가
   2. Enable Boss System: ✓
   3. Boss Health Bar Prefab 연결
   ```

### 9단계: 테스트 및 디버깅

#### 게임플레이 테스트
1. **기본 플로우 테스트**
   ```
   GameScene에서:
   1. Play 모드 실행
   2. 캐릭터 소환 테스트
   3. 웨이브 진행 확인
   4. 전투 로직 검증
   5. 승리/패배 조건 확인
   ```

2. **밸런스 조정**
   ```
   CSV 파일 수정:
   1. 캐릭터 스탯 조정
   2. CSV to ScriptableObject 재변환
   3. 게임 내 반영 확인
   4. 재테스트
   ```

#### 성능 최적화
1. **프로파일러 사용**
   ```
   Window > Analysis > Profiler
   - CPU 사용량 체크
   - 메모리 누수 확인
   - 렌더링 최적화
   ```

2. **오브젝트 풀링 확인**
   ```
   - Bullet 풀링 동작 확인
   - Effect 풀링 설정
   - 불필요한 Instantiate/Destroy 제거
   ```

## 🎮 플레이어 게임플레이 가이드

### 게임 시작하기
1. **로비 화면**
   - 스테이지 선택: 100개 스테이지 중 선택
   - 덱 구성: 휴먼/오크/엘프 각 3명 + 자유 1명
   - 가챠 시스템: 새로운 캐릭터 획득

2. **인게임 전투**
   - 미네랄 30 소모하여 캐릭터 소환
   - 최대 50마리까지만 소환 가능 (UI에서 실시간 확인)
   - 드래그로 캐릭터 라인 변경 가능

### 핵심 전략
1. **캐릭터 배치**
   - 같은 캐릭터끼리는 한 타일에 3개까지 배치 가능
   - 3개가 모이면 자동 합성 (1성→2성, 2성→3성)
   - 타일별로 다른 캐릭터는 배치 불가

2. **종족 시너지**
   - 같은 종족이 많을수록 강화 효과 증가
   - 종족별 강화 버튼으로 공격력/공격속도 5% 증가
   - 리사이클로 종족 통일 가능

3. **라인 운영**
   - 3라인 시스템: 좌/중/우
   - 캐릭터 드래그로 라인 변경
   - 라인별 웨이포인트 따라 이동

4. **소환 제한 관리**
   - 화면 상단에 "캐릭터: 현재/50" 표시
   - 80% 이상: 노란색 경고
   - 100% 도달: 빨간색 표시, 더 이상 소환 불가
   - 효율적인 합성으로 공간 확보 필요

5. **성 방어와 공격**
   - 성이 적을 공격: 중간성 30, 최종성 50 공격력
   - 방어 버프: 최종성 주변 아군 20% 방어력 증가
   - 범위 공격: 3명 이상 적이 모이면 발동
   - 성 체력 실시간 표시: 정상(녹색) → 경고(노란색) → 위험(빨간색)

6. **보스 전투**
   - 5, 10, 15, 20 웨이브에서 보스 등장
   - 보스 체력바가 화면 상단에 표시
   - 3페이즈 전투:
     - 페이즈 1 (100-70%): 기본 공격만
     - 페이즈 2 (70-40%): 광역 공격, 미니언 소환 추가
     - 페이즈 3 (40-0%): 모든 패턴 + 광폭화
   - 보스 처치 시 특별 보상:
     - 일반 몬스터의 5배 골드
     - 추가 다이아몬드 보상
   - 주의사항:
     - 보스는 일반 몬스터보다 5배 체력
     - 페이즈 전환 시 잠시 무적
     - 미니언은 빠르게 처리 필요

### 승리 조건
- 20웨이브 모두 방어 성공
- 상대방 최종성(1000HP) 파괴

### 패배 조건
- 자신의 최종성이 파괴됨
- 중간성 3개(각 500HP) 모두 파괴 후 최종성 공격받음

### 9단계: 빌드 및 배포

#### 빌드 설정
1. **Player Settings**
   ```
   - Company Name 설정
   - Product Name: "Twelve"
   - Bundle Identifier 설정
   - 아이콘 및 스플래시 설정
   ```

2. **Build Settings**
   ```
   - LobbyScene과 GameScene 추가
   - 플랫폼별 설정 (Android/iOS)
   - Development Build 체크 (테스트용)
   ```

#### 최종 테스트
1. **디바이스 테스트**
   ```
   - 터치 입력 확인
   - 성능 체크
   - 메모리 사용량 모니터링
   - 크래시 로그 확인
   ```

2. **품질 검증**
   ```
   - 모든 기능 정상 동작 확인
   - UI 반응성 테스트
   - 게임 밸런스 최종 검토
   ```

### 개발 팁 및 주의사항

#### 코딩 규칙
```csharp
// 필수 using 문
using TMPro;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 컴포넌트 기반 설계
public class ExampleComponent : MonoBehaviour
{
    [Header("필수 참조")]
    [SerializeField] private ComponentReference reference;
    
    [Header("설정값")]
    [SerializeField] private float configValue = 1.0f;
    
    // 이벤트 기반 통신
    public static event System.Action<int> OnValueChanged;
}
```

#### 자주 발생하는 문제들
1. **NullReference 방지**
   ```csharp
   if (component != null)
   {
       component.DoSomething();
   }
   ```

2. **성능 최적화**
   ```csharp
   // 매 프레임 검색 피하기
   private Camera mainCamera;
   void Start() { mainCamera = Camera.main; }
   
   // 코루틴 활용
   StartCoroutine(UpdateEverySecond());
   ```

3. **메모리 관리**
   ```csharp
   // 이벤트 구독 해제
   void OnDestroy()
   {
       EventManager.OnGameEnd -= HandleGameEnd;
   }
   ```

## 개발 가이드

### 웨이포인트 시스템 설정
1. **RouteWaypointManager 추가**:
   - GameScene에 빈 GameObject 생성
   - RouteWaypointManager 컴포넌트 추가
   
2. **웨이포인트 경로 설정**:
   - Inspector에서 각 플레이어의 좌/중/우 경로 설정
   - 에디터 도구 사용:
     - "Add Waypoint": 현재 Scene View 위치에 웨이포인트 추가
     - "Auto": 해당 경로의 Walkable 타일 기반 자동 생성
     - "Clear": 경로 초기화
   
3. **캐릭터 배치 시 자동 경로 할당**:
   - Y > 1.5: 좌측 경로
   - -1.5 < Y < 1.5: 중앙 경로  
   - Y < -1.5: 우측 경로

### 테스트 방법
1. Unity Hub에서 프로젝트 열기
2. GameScene에서 Play 모드 실행
3. 캐릭터 배치 및 전투 테스트
4. 디버그 로그로 시스템 확인

### 데이터 관리

#### CSV 파일에서 ScriptableObject 생성
1. **Tools > Character Tools > CSV to ScriptableObject Converter** 실행
2. CSV 폴더 경로 확인 (`Assets/CSV`)
3. 출력 폴더 경로 설정 (`Assets/Prefabs/Data/Characters`)
4. 변환할 CSV 파일 선택:
   - `ally_one_star_characters.csv` (1성 아군 캐릭터)
   - `ally_two_star_characters.csv` (2성 아군 캐릭터)  
   - `ally_three_star_characters.csv` (3성 아군 캐릭터)
   - `enemy_one_star_characters.csv` (1성 적 캐릭터)
   - 기타 캐릭터 관련 CSV 파일들
5. **선택된 CSV 파일들을 ScriptableObject로 변환** 버튼 클릭

#### 기존 동기화 도구
- **Tools > CSV Data Sync Manager**: CSV ↔ ScriptableObject 동기화
- **Tools > Data Export Manager**: 데이터 생성 및 내보내기
- CSV 파일 편집 시 자동 동기화 활성화

#### CSV 파일 형식
CSV 파일은 다음 컬럼들을 지원합니다:
```
이름, 초기 별, 종족, 공격력, 공격속도, 공격범위, 최대 HP, 이동속도, 공격 타입, 광역공격, 비용, 가중치
```

**컬럼 설명:**
- **이름**: 캐릭터명 (필수)
- **초기 별**: 1, 2, 3 (캐릭터 등급)
- **종족**: Human, Orc, Elf, Undead
- **공격력**: 기본 공격 데미지
- **공격속도**: 공격 간격 (낮을수록 빠름)
- **공격범위**: 공격 사거리
- **최대 HP**: 캐릭터 체력
- **이동속도**: 이동 속도
- **공격 타입**: Melee(근접), Ranged(원거리), LongRange(장거리)
- **광역공격**: 예/아니오 (범위 공격 여부)
- **비용**: 소환 비용
- **가중치**: 가챠 확률 가중치 (선택사항)

### 코드 컨벤션
- C# 표준 명명 규칙 준수
- 컴포넌트 기반 설계
- 단일 책임 원칙 준수
- 충분한 주석 작성

## 알려진 이슈
- 네트워킹 코드 존재하나 비활성화 상태
- 일부 UI 요소 플레이스홀더 상태
- 캐릭터 밸런스 조정 진행 중

## 트러블슈팅

### CharacterInventoryManager 에러 해결

#### 문제: 캐릭터 데이터베이스가 비어있거나 null 캐릭터가 발견됨

**증상:**
```
[CharacterInventoryManager] null 캐릭터 데이터 발견
[CharacterInventoryManager] 가챠풀이 비어있음 - 대안 로딩 방법들 시도 중...
[CharacterInventoryManager] 현재 ownedCharacters.Count: 0
```

**원인:**
1. CharacterDatabase.asset의 characters 배열이 크기는 있지만 모든 슬롯이 null로 설정됨
2. CSV 데이터와 ScriptableObject 간의 동기화 문제
3. 데이터베이스 초기화 시점 문제

**해결 방법:**

#### 1단계: CSV 데이터 확인
1. `Assets/CSV/` 폴더의 캐릭터 데이터 파일 확인:
   - `ally_one_star_characters.csv`
   - `ally_two_star_characters.csv` 
   - `ally_three_star_characters.csv`
2. CSV 파일에 올바른 캐릭터 데이터가 있는지 확인

#### 2단계: 데이터 동기화 실행
1. Unity 메뉴에서 **Tools > CSV Data Sync Manager** 열기
2. **Auto Sync** 옵션 활성화
3. **Force Sync All** 버튼 클릭하여 강제 동기화
4. Console에서 동기화 로그 확인

#### 3단계: 데이터베이스 수동 재생성
1. Unity 메뉴에서 **Tools > Data Export Manager** 열기
2. **Clear Database** 클릭
3. **Generate Sample Data** 클릭
4. 각 캐릭터 슬롯에 데이터 할당 확인
5. **Export to CSV** 실행

#### 4단계: CSV에서 ScriptableObject 직접 생성
1. **Tools > Character Tools > CSV to ScriptableObject Converter** 실행
2. 변환할 CSV 파일들 선택
3. **선택된 CSV 파일들을 ScriptableObject로 변환** 클릭
4. `Assets/Prefabs/Data/Characters/` 폴더에 생성된 .asset 파일들 확인

#### 5단계: ScriptableObject 직접 확인
1. `Assets/Prefabs/Data/CharacterDatabase.asset` 선택
2. Inspector에서 Characters 배열 확인
3. 모든 슬롯이 None (CharacterData)인 경우:
   - 배열 크기를 0으로 변경
   - Apply 후 다시 원하는 크기로 설정
   - 생성된 CharacterData .asset 파일들을 각 슬롯에 드래그 앤 드롭

#### 6단계: 캐릭터 데이터 생성 (비어있는 경우)
```csharp
// 개발자용: 기본 캐릭터 데이터 생성 코드
// Unity 메뉴 > Tools > Generate Sample Data 사용 권장
```

#### 7단계: 초기화 순서 문제 해결
CharacterInventoryManager.cs에서 초기화 문제가 발생하는 경우:

1. **Awake() 호출 순서 확인**
   - CharacterInventoryManager가 다른 매니저보다 먼저 초기화되는지 확인
   - Script Execution Order에서 우선순위 설정 (`Edit > Project Settings > Script Execution Order`)

2. **데이터베이스 참조 확인**
   - LobbyScene의 CharacterInventoryManager Inspector에서
   - `Character Database Object` 필드에 올바른 .asset 파일이 연결되었는지 확인

3. **강제 초기화 로직 활용**
   - 시스템이 자동으로 기본 캐릭터를 생성하도록 설계됨
   - `CreateDefaultCharacters()` 메서드가 실행되어 임시 데이터 생성

#### 8단계: 데이터 무결성 검증
```csharp
// Inspector에서 확인할 항목들:
// 1. CharacterData.characterName이 비어있지 않은지
// 2. CharacterData.attackPower > 0인지  
// 3. CharacterData.maxHP > 0인지
// 4. CharacterData.race가 설정되어 있는지
```

#### 임시 해결책
시스템이 자동으로 기본 캐릭터 8개를 생성하므로, 에러가 발생해도 게임은 정상 동작합니다:
- RandomChar_0~7 (Human, Orc, Elf 종족)
- 기본 스탯으로 설정됨
- 이후 CSV 동기화로 실제 데이터로 교체 가능

#### 예방책
1. **정기적인 데이터 백업**: `Assets/Prefabs/Data/` 폴더 백업
2. **CSV 검증**: 새로운 캐릭터 추가 시 CSV 형식 확인
3. **자동 테스트**: 게임 시작 시 캐릭터 데이터 유효성 검사
4. **버전 관리**: 데이터 변경 시 Git 커밋으로 추적

### 기타 일반적인 문제

#### UI 관련 문제
- **DeckPanelManager 에러**: CharacterInventoryManager 초기화 후 자동 해결
- **빈 인벤토리**: 위의 CharacterInventoryManager 해결 방법 적용

#### 성능 관련 문제  
- **프레임 드롭**: 캐릭터 50마리 제한으로 관리됨
- **메모리 누수**: Object Pooling으로 최적화됨

#### 데이터 동기화 문제
- **CSV 변경사항 반영 안됨**: Auto Sync 활성화 또는 Force Sync 실행
- **ScriptableObject 덮어쓰기**: 데이터 백업 후 재생성

## 버전 히스토리

### v1.0.0 (2025-06-18)
- 3단계 개발 완료
- 기획서 작성 완료
- 핵심 시스템 구현
  - 캐릭터 시스템
  - 전투 시스템
  - 웨이브 시스템
  - 합성 시스템
- 웨이포인트 경로 시스템 구현
- AI 플레이어 구현
- 100개 스테이지 프레임워크
- README.md 상세 기획서 작성

### v1.1.0 (2025-06-18) - 캐릭터 소환 제한 구현
- **✅ 캐릭터 소환 제한 시스템 완성**
  - PlacementManager에서 50마리 제한 관리
  - SummonManager에서 소환 시 제한 체크
  - CharacterCountUI 추가: 실시간 캐릭터 수 표시
  - 색상 경고 시스템: 정상(흰색) → 경고(노란색, 80%) → 최대(빨간색, 100%)

### v1.2.0 (2025-06-18) - 향상된 성 전투 시스템 구현
- **✅ 향상된 성 전투 시스템 완성**
  - EnhancedCastleSystem 구현: 성이 공격 능력 보유
  - 중간성(500HP)과 최종성(1000HP) 개별 관리
  - 방어 버프 시스템: 주변 아군에게 20% 방어력 증가
  - 범위 공격: 3명 이상의 적이 근처에 있을 때 발동
  - CastleStatusUI: 성 체력, 공격 상태, 버프 효과 표시

#### 새로운 컴포넌트: EnhancedCastleSystem
```csharp
// Assets/Scripts/pjy/Gameplay/EnhancedCastleSystem.cs
// 성의 공격 및 방어 기능 구현
// CastleHealthManager와 연동하여 체력 관리
```

#### 새로운 UI 컴포넌트: CastleStatusUI
```csharp
// Assets/Scripts/pjy/UI/CastleStatusUI.cs
// 각 성의 체력, 공격 상태, 방어 버프 표시
// 중간성 3개 + 최종성 1개의 상태를 실시간 모니터링
```

#### 개발자 설정 가이드
1. **EnhancedCastleSystem 설정**
   - 각 성 오브젝트에 EnhancedCastleSystem 컴포넌트 추가
   - Castle Type: Middle 또는 Final 선택
   - Route Type: Left/Center/Right (중간성의 경우)
   - 공격 범위, 공격력, 공격 속도 설정
   - 방어 버프 범위와 효과 설정

2. **CastleStatusUI 설정**
   - GameScene에 "CastleStatusUI" GameObject 생성
   - CastleStatusUI.cs 컴포넌트 추가
   - 각 성별 UI 요소 연결 (Slider, Text, Image)
   - 체력 색상: 녹색(60%+) → 노란색(30-60%) → 빨간색(30%-)

3. **Character 클래스 업데이트**
   - ApplyDefenseBoost() 메서드 추가
   - 방어 버프를 받으면 데미지 20% 감소
   - 버프 시각 효과: 하늘색 깜빡임

### 향후 업데이트 예정
- 멀티플레이어 모드
- 신규 캐릭터 추가
- 길드 시스템
- 랭킹 시스템
- 시즌별 콘텐츠

## 🌍 월드 좌표계 타일 시스템 (구현 완료)

### 타일 시스템 아키텍처

#### 1. WorldTileGenerator (`Assets/Scripts/pjy/Managers/WorldTileGenerator.cs`)
월드 좌표계에서 타일을 자동 생성하는 핵심 매니저입니다.

**주요 기능:**
- **7x12 그리드 생성**: 월드 좌표계에서 타일 자동 배치
- **3개 영역 구분**:
  - Player 1 영역: 하단 3줄 (파란색)
  - Player 2 영역: 상단 3줄 (빨간색)
  - 중립 영역: 중간 1줄 (회색)
- **3라인 시스템**: 좌측/중앙/우측 라인 자동 설정
- **동적 타일 생성**: 런타임에 타일 추가/제거 가능

**설정 옵션:**
```csharp
// 타일 생성 설정
[SerializeField] private GameObject tilePrefab;              // 기본 타일 프리팹
[SerializeField] private float tileSize = 1.0f;             // 타일 크기
[SerializeField] private float tileSpacing = 0.1f;          // 타일 간격
[SerializeField] private Vector3 gridStartPosition;         // 그리드 시작 위치
[SerializeField] private bool centerGrid = true;            // 중앙 정렬 여부
```

#### 2. TileGridEditor (`Assets/Scripts/pjy/Gameplay/TileGridEditor.cs`)
에디터에서 타일 상태를 시각적으로 편집할 수 있는 도구입니다.

**주요 기능:**
- **시각적 그리드 편집**: 7x12 그리드를 버튼으로 표시
- **13가지 타일 타입**: None, Walkable, Placeable 등
- **타일 참조 관리**: Scene의 타일과 직접 연결
- **일괄 적용**: 모든 타일 상태를 한 번에 업데이트

**타일 타입:**
```
- None (X): 비활성 타일
- Walkable (W1): 지역1 이동 가능
- Walkable2 (W2): 지역2 이동 가능
- WalkableLeft/Center/Right: 라인별 이동 타일
- Placeable (P1/P2): 캐릭터 배치 가능
- PlaceTile/Placed2 (O1/O2): 캐릭터 배치됨
```

#### 3. Tile 클래스 (`Assets/Scripts/pjy/Gameplay/Tile.cs`)
개별 타일의 상태와 동작을 관리합니다.

**핵심 특징:**
- **타워 스택 시스템**: 같은 캐릭터 3개까지 한 타일에 배치 가능
- **다중 캐릭터 관리**: List<Character>로 여러 캐릭터 추적
- **시각적 위치 조정**:
  - 1개: 100% 크기, 중앙 배치
  - 2개: 80% 크기, 좌우 배치
  - 3개: 70% 크기, 삼각형 배치

**캐릭터 배치 로직:**
```csharp
public bool CanPlaceCharacter(Character character = null)
{
    if (isBlocked) return false;
    
    if (IsPlaceableType())
    {
        // 같은 캐릭터끼리는 3개까지 가능
        if (character != null && occupyingCharacters.Count > 0)
        {
            Character first = occupyingCharacters[0];
            if (first.characterName == character.characterName && first.star == character.star)
            {
                return occupyingCharacters.Count < 3;
            }
            return false; // 다른 종류는 불가
        }
        return occupyingCharacters.Count < 3;
    }
    return false;
}
```

#### 4. TileManager (`Assets/Scripts/pjy/Managers/TileManager.cs`)
모든 타일을 중앙에서 관리하는 싱글톤 매니저입니다.

**주요 기능:**
- **타일 분류**: 라인별, 지역별, 타입별 타일 그룹 관리
- **캐릭터 이동**: 드래그로 라인 변경 지원
- **타일 상태 관리**: 캐릭터 배치/제거 시 자동 업데이트
- **AI 지원**: 플레이어별 사용 가능한 타일 제공

### 타일 시스템 사용 가이드

#### 1. 새로운 타일 그리드 생성
```csharp
// 1. GameObject 생성
GameObject tileGenerator = new GameObject("TileGenerator");
WorldTileGenerator generator = tileGenerator.AddComponent<WorldTileGenerator>();

// 2. 타일 프리팹 할당
generator.tilePrefab = yourTilePrefab;

// 3. 그리드 생성
generator.GenerateTileGrid();
```

#### 2. 에디터에서 타일 편집
1. TileGridEditor 컴포넌트를 GameObject에 추가
2. Grid State에서 각 칸 클릭하여 타일 타입 변경
3. Tile References에 Scene의 타일 연결
4. "Apply to 7×12 Tile References" 버튼 클릭

#### 3. 런타임 타일 관리
```csharp
// 타일 찾기
Tile tile = TileManager.Instance.GetTileAt(row, col);
Tile worldTile = TileManager.Instance.GetTileAtWorldPosition(worldPos);

// 캐릭터 배치
if (tile.CanPlaceCharacter(character))
{
    tile.AddOccupyingCharacter(character);
}

// 라인 이동
TileManager.Instance.MoveCharacterToRoute(character, RouteType.Left);
```

### 타일 시스템 최적화

1. **성능 최적화**
   - 타일 생성 시 오브젝트 풀링 사용 가능
   - 불필요한 타일 비활성화로 렌더링 부하 감소
   - 타일 상태 변경 시 최소한의 업데이트만 수행

2. **확장성**
   - 새로운 타일 타입 쉽게 추가 가능
   - 타일별 특수 효과 구현 가능
   - 다양한 그리드 크기 지원

3. **디버깅**
   - Gizmos로 타일 영역 시각화
   - 타일 상태 실시간 모니터링
   - 에디터 도구로 빠른 수정 가능

## 버전 히스토리

### v1.5.0 (2025-06-18)
- **보스 몬스터 시스템 구현**
  - BossMonster: 3페이즈 전투 시스템
  - BossSpawnManager: 5웨이브마다 보스 자동 스폰
  - BossHealthBarUI: 전용 보스 체력바 UI
  - 페이즈별 패턴: 광역 공격, 미니언 소환, 버프/디버프
  - 챕터별 보스 강화 및 특별 보상

### v1.4.0 (2025-06-18)
- **고급 AI 시스템 구현**
  - AdvancedAISystem: 패턴 인식, 학습, 동적 난이도 조절
  - AILearningData: ScriptableObject 기반 학습 데이터 영구 저장
  - EnhancedAIBrain: 전술적 의사결정 및 실시간 전략 전환
  - 3-그램 패턴 분석으로 플레이어 행동 예측
  - 4가지 전술 모드: Aggressive, Defensive, Economic, Adaptive
  - 동적 난이도: AI 승률 40-60% 유지 목표

### v1.3.0 (2025-06-18)
- **CharacterInventoryManager 초기화 버그 수정**
  - ImprovedCharacterInventoryManager 구현
  - 안정적인 초기화 순서 보장
  - 재시도 로직 및 폴백 메커니즘 추가

### v1.2.0 (2025-06-18)
- **향상된 성 전투 시스템 구현**
  - EnhancedCastleSystem: 성의 공격 및 방어 능력
  - CastleStatusUI: 실시간 성 상태 표시
  - 방어 버프 시스템 추가

### v1.1.0 (2025-06-18)
- **캐릭터 소환 제한 UI 구현**
  - CharacterCountUI: 실시간 캐릭터 수 표시
  - 50마리 제한 시각적 피드백

### v1.0.0 (2025-06-18)
- 초기 릴리즈
- 기본 게임 시스템 구현

## 라이선스
[라이선스 정보 추가 필요]
