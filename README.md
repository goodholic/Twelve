# Twelve - Unity Tower Defense Auto-Battler

## 프로젝트 개요
- **게임 장르**: Tower Defense + Auto-Battler 하이브리드 모바일 게임
- **개발 시작일**: 2025-06-18
- **Unity 버전**: 2021.3 이상 권장
- **대상 플랫폼**: 모바일 (Android/iOS)

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
1. **타워 스택 시스템**: 같은 캐릭터끼리는 같은 위치에 타워로써 놓일 수 있지만, 크기가 작아져서 한 자리에 3개까지 놓을 수 있습니다.
2. **성 전투 시스템**: 기존의 하트를 없애는 방식에서 변경하여, 몬스터는 중간 성이나 최종 성과 전투를 하게 됩니다.
3. **소환 제한**: 50마리까지만 소환할 수 있도록 합니다.
4. **전투 관계**: 
   - 몬스터는 성을 공격할 수 있습니다.
   - 성은 몬스터와 캐릭터를 공격합니다.
   - 캐릭터는 성과 캐릭터를 공격합니다.
5. **경로 시스템**: 캐릭터를 좌측, 중간, 우측에 놓을 시 각각의 웨이포인트대로 가도록 합니다.
6. **AI 플레이어**: AI 플레이어와 일반 플레이어가 같은 게임 오브젝트와 코드를 사용하며, isAI 체크 여부로 구분합니다.

## 기능 명세

### 핵심 기능
- [x] 기본 게임 구조 (Tower Defense + Auto-Battler)
- [x] 웨이브 기반 전투 (20웨이브)
- [x] 캐릭터 배치 및 합성 메커니즘
- [x] 가챠/소환 시스템
- [ ] Canvas에서 World 2D 좌표계로 전환
- [ ] 타워 스택 시스템 (같은 캐릭터 3개까지 한 위치에)
- [ ] 향상된 성 전투 시스템
- [ ] 캐릭터 소환 제한 (50마리)
- [ ] 고급 AI 플레이어 시스템

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

## 구현 진행상황
- [x] README.md 기획서 작성
- [x] 프로젝트 구조 분석 완료
- [x] Canvas → World 2D 전환 시스템 설계
- [x] TowerStackSystem 스크립트 생성
- [x] 성 공격 시스템 구현 (CastleAttackSystem)
- [x] 캐릭터 소환 제한 구현 (50마리 제한)
- [x] 경로별 웨이포인트 시스템 구현 (RouteWaypointManager)
- [x] AI 플레이어 시스템 구현 (BasePlayer, AIPlayer, AIBrain)
- [ ] 통합 테스트 및 밸런싱

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
- **Tools > CSV Data Sync Manager**: CSV ↔ ScriptableObject 동기화
- **Tools > Data Export Manager**: 데이터 생성 및 내보내기
- CSV 파일 편집 시 자동 동기화 활성화

### 코드 컨벤션
- C# 표준 명명 규칙 준수
- 컴포넌트 기반 설계
- 단일 책임 원칙 준수
- 충분한 주석 작성

## 알려진 이슈
- 네트워킹 코드 존재하나 비활성화 상태
- 일부 UI 요소 플레이스홀더 상태
- 캐릭터 밸런스 조정 진행 중

## 라이선스
[라이선스 정보 추가 필요]
