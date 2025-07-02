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
- [x] 기본 사운드 시스템 구현 완료 (`Assets/Scripts/pjy/Managers/SoundManager.cs`)
- [x] 튜토리얼 시스템 구현 완료 (`Assets/Scripts/pjy/Managers/TutorialManager.cs`)
- [ ] CharacterInventoryManager 초기화 버그 수정
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

### v1.6.0 (2025-06-21)
- **모든 미구현 기능 완성**
  - ✅ 리사이클 시스템 (종족 변환) 구현
  - ✅ 종족 시너지 시스템 구현
  - ✅ 업그레이드 시스템 (인게임 전) 구현
  - ✅ 인게임 강화 버튼 구현
  - ✅ 5웨이브 보상 시스템 구현
  - ✅ 자동 합성 (주사위 버튼) 완성

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

## 🎯 핵심 기능 구현 가이드

### 개발자를 위한 구현 안내
모든 핵심 기능이 구현되었습니다. 각 기능을 Unity에서 어떻게 설정하고 사용하는지 초보자도 이해할 수 있도록 단계별로 설명합니다.

### ✅ 1. 리사이클 시스템 (종족 변환)
**기능 설명**: 특정 종족 캐릭터를 다른 종족으로 변환하는 시스템
**구현 파일**: `Assets/Scripts/pjy/Managers/RecycleManager.cs`

#### 구현 단계:
1. **RecycleManager.cs 생성**
   ```csharp
   public class RecycleManager : MonoBehaviour
   {
       [Header("리사이클 설정")]
       [SerializeField] private int recycleGoldCost = 50;
       [SerializeField] private GameObject recyclePanel;
       [SerializeField] private Button recycleButton;
       
       public void RecycleCharacter(Character character)
       {
           // 1. 캐릭터 종족 확인
           // 2. 다른 2개 종족 중 랜덤 선택
           // 3. 종족 변경 및 외형 업데이트
           // 4. 골드 차감
       }
   }
   ```

2. **UI 패널 생성**
   - Canvas에 RecyclePanel GameObject 생성
   - 캐릭터 선택 UI 추가
   - 종족 변환 버튼과 비용 표시
   - 확인/취소 팝업 추가

3. **CharacterData 수정**
   - CharacterData.cs에 `ChangeRace(Race newRace)` 메서드 추가
   - 종족별 스프라이트 교체 로직
   - 스탯 재계산 (종족별 보너스)

4. **LobbyScene 연동**
   - LobbySceneManager에 리사이클 버튼 추가
   - 인벤토리에서 캐릭터 선택 가능하게 수정
   - 리사이클 성공 시 UI 업데이트

#### Unity에서 설정하는 방법:
1. **LobbyScene 설정**
   - Hierarchy에서 "RecycleManager" GameObject 생성
   - RecycleManager.cs 컴포넌트 추가
   - Inspector에서 설정:
     - Recycle Gold Cost: 50
     - Recycle Panel: UI 패널 연결
     - Character Database: 데이터베이스 연결

2. **UI 구성**
   - Canvas > RecyclePanel 생성
   - 드래그 앤 드롭 영역 설정
   - 확인 버튼과 취소 버튼 추가
   - 비용 텍스트 (TMPro) 추가

3. **사용 방법**
   - 인벤토리에서 캐릭터를 리사이클 패널로 드래그
   - 변환될 종족이 랜덤으로 표시됨
   - 확인 시 골드 50 소비하고 종족 변환

### ✅ 2. 종족 시너지 시스템
**기능 설명**: 같은 종족이 많을수록 해당 종족에게 버프 제공
**구현 파일**: `Assets/Scripts/pjy/Managers/RaceSynergyManager.cs`

#### Unity에서 설정하는 방법:
1. **GameScene 설정**
   - Hierarchy에서 "RaceSynergyManager" GameObject 생성
   - RaceSynergyManager.cs 컴포넌트 추가
   - Inspector에서 시너지 설정:
     - 3명: 공격력 +10%, 체력 +5%
     - 5명: 공격력 +20%, 체력 +10%, 공격속도 +5%
     - 7명: 공격력 +30%, 체력 +15%, 공격속도 +10%
     - 9명: 공격력 +50%, 체력 +25%, 공격속도 +15%

2. **자동 동작**
   - 캐릭터 소환 시 자동으로 시너지 계산
   - PlacementManager와 연동되어 실시간 업데이트
   - 캐릭터 제거 시에도 자동 재계산

3. **시각적 표시**
   - 시너지 적용된 캐릭터는 종족별 색상으로 반짝임
   - 휴먼: 파란색, 오크: 빨간색, 엘프: 초록색
   - 버프 아이콘이 캐릭터 위에 표시됨

4. **플레이어 전략**
   - 같은 종족으로 덱을 구성하면 시너지 극대화
   - 리사이클로 종족 통일 가능
   - 종족별 균형 vs 단일 종족 집중 선택

### ✅ 3. 업그레이드 시스템 (인게임 전)
**기능 설명**: 같은 종족/등급 캐릭터를 재료로 경험치 획득 및 레벨업
**구현 파일**: `Assets/Scripts/pjy/Managers/CharacterUpgradeManager.cs`

#### Unity에서 설정하는 방법:
1. **LobbyScene 설정**
   - Hierarchy에서 "CharacterUpgradeManager" GameObject 생성
   - CharacterUpgradeManager.cs 컴포넌트 추가
   - Inspector에서 설정:
     - Gold Per Upgrade: 10
     - Exp Per Upgrade: 1
     - Max Level: 30
     - Upgrade Panel: UI 패널 연결

2. **업그레이드 UI 사용법**
   - 강화할 캐릭터를 중앙 슬롯에 배치
   - 재료 캐릭터를 주변 슬롯에 드래그
   - 같은 종족/등급만 재료로 사용 가능
   - 1개당 경험치 1%, 골드 10 소비

3. **레벨업 효과**
   - 레벨당 모든 스탯 5% 증가
   - 최대 30레벨까지 성장
   - 레벨업 시 황금빛 이펙트
   - 캐릭터 카드에 레벨 표시

4. **전략적 활용**
   - 주력 캐릭터 집중 육성
   - 중복 캐릭터를 경험치로 활용
   - 골드 효율적 사용 필요

### ✅ 4. 인게임 강화 버튼
**기능 설명**: 전투 중 종족별 강화 버튼으로 즉시 버프
**구현 파일**: `Assets/Scripts/pjy/Managers/InGameEnhanceManager.cs`

#### Unity에서 설정하는 방법:
1. **GameScene 설정**
   - Hierarchy에서 "InGameEnhanceManager" GameObject 생성
   - InGameEnhanceManager.cs 컴포넌트 추가
   - Inspector에서 설정:
     - Base Cost: 100 (최초 강화 비용)
     - Cost Multiplier: 1.5 (레벨당 비용 증가율)
     - Max Level: 10 (최대 강화 횟수)
     - Enhance Button Parent: UI 버튼 그룹 연결

2. **강화 버튼 UI**
   - 화면 하단에 종족별 버튼 3개
   - 휴먼(파란색), 오크(빨간색), 엘프(초록색)
   - 각 버튼에 현재 레벨과 비용 표시
   - 미네랄 부족 시 회색 비활성화

3. **강화 효과**
   - 레벨당 공격력/공격속도 5% 증가
   - 즉시 해당 종족 모든 캐릭터에 적용
   - 강화 시 번개 이펙트와 종족별 빛
   - 강화 레벨은 게임 중 유지

4. **전략적 활용**
   - 주력 종족 집중 강화
   - 미네랄 절약 vs 즉시 강화 선택
   - 시너지와 연계한 강화 효과

### ✅ 5. 5웨이브 보상 시스템
**기능 설명**: 5, 10, 15웨이브 클리어 시 랜덤 2성 캐릭터 3개 중 1개 선택
**구현 파일**: `Assets/Scripts/pjy/Managers/WaveRewardManager.cs`

#### Unity에서 설정하는 방법:
1. **GameScene 설정**
   - Hierarchy에서 "WaveRewardManager" GameObject 생성
   - WaveRewardManager.cs 컴포넌트 추가
   - Inspector에서 설정:
     - Reward Waves: 5, 10, 15
     - Reward Selection Panel: UI 패널 연결
     - Reward Slots: 3개의 캐릭터 카드 슬롯
     - Star Merge Database: 2성 캐릭터 데이터베이스

2. **보상 UI 사용법**
   - 5, 10, 15 웨이브 클리어 시 자동 팝업
   - 3개의 랜덤 2성 캐릭터 카드 표시
   - 캐릭터 정보 확인 후 1개 선택
   - 30초 시간 제한 (미선택 시 랜덤)

3. **WaveSpawner 연동**
   - 웨이브 클리어 시 자동 호출
   - 게임 일시정지 후 보상 표시
   - 선택 완료 후 게임 재개
   - 인벤토리에 캐릭터 추가

4. **보상 전략**
   - 주력 종족과 맞는 캐릭터 선택
   - 현재 부족한 역할군 보충
   - 합성 가능성 고려

### ✅ 6. 자동 합성 (주사위 버튼)
**기능 설명**: 버튼 클릭으로 같은 등급 캐릭터 3개를 자동으로 찾아 합성
**구현 파일**: `Assets/Scripts/pjy/Managers/AutoMergeManager.cs`

#### Unity에서 설정하는 방법:
1. **GameScene 설정**
   - Hierarchy에서 "AutoMergeManager" GameObject 생성 (이미 있음)
   - AutoMergeManager.cs 컴포넌트 확인
   - Inspector에서 설정:
     - Max Merge Groups Per Turn: 5
     - Merge Animation Delay: 0.5초
     - Dice Button: UI 버튼 연결

2. **주사위 버튼 사용법**
   - GameScene 하단의 주사위 아이콘 클릭
   - 필드의 모든 캐릭터 자동 스캔
   - 같은 이름/등급 3개씩 자동 합성
   - 1성 우선 합성 후 2성 합성

3. **자동 합성 메서드**
   - `PerformAutoMerge()` 메서드 호출
   - 전체 필드 캐릭터 검색
   - 같은 캐릭터 3개 이상 그룹 찾기
   - 가장 뒤쪽 위치에 합성된 캐릭터 생성

4. **전략적 활용**
   - 타일이 가득 찰 때 공간 확보
   - 빠른 고급 캐릭터 확보
   - 수동 합성과 병행 사용
   - 합성 타이밍 전략 중요

### 초보자를 위한 Unity 작업 팁

#### 스크립트 생성 방법
1. Project 창에서 Scripts 폴더 우클릭
2. Create > C# Script 선택
3. 스크립트 이름 입력 (예: RecycleManager)
4. 더블클릭하여 Visual Studio에서 편집

#### UI 생성 방법
1. Hierarchy에서 우클릭
2. UI > Panel/Button/Text 등 선택
3. Canvas 자식으로 자동 생성됨
4. RectTransform으로 위치/크기 조정

#### 컴포넌트 연결 방법
1. GameObject 선택
2. Inspector에서 Add Component
3. 스크립트 이름 검색하여 추가
4. public/SerializeField 변수에 드래그 앤 드롭

#### 데이터 저장 방법
```csharp
// 간단한 데이터 저장
PlayerPrefs.SetInt("PlayerGold", 1000);
PlayerPrefs.SetString("PlayerName", "Player1");
PlayerPrefs.Save();

// 복잡한 데이터는 JSON 사용
string jsonData = JsonUtility.ToJson(characterData);
System.IO.File.WriteAllText(path, jsonData);
```

#### 디버깅 방법
1. `Debug.Log("메시지");` 사용
2. Console 창에서 로그 확인
3. 브레이크포인트 설정 (Visual Studio)
4. Inspector에서 실시간 값 확인

## 라이선스
[라이선스 정보 추가 필요]




================================================================================

## 📜 스토리 대본 CSV 형식

게임 내 스토리 대화 시스템을 위한 CSV 형식 데이터입니다. 이 데이터는 Unity 에디터의 Story Dialogue Editor 툴을 통해 게임에 직접 가져올 수 있습니다.

### CSV 헤더 형식
```
StoryID,StoryType,StoryTitle,Order,CharacterName,DialogueText,Emotion,Position,Choices,NextStoryID,Conditions
```

### 스토리 대본 CSV 데이터

```csv
StoryID,StoryType,StoryTitle,Order,CharacterName,DialogueText,Emotion,Position,Choices,NextStoryID,Conditions
story_00_001,Main,제0장: 하나였던 시절,1,나레이션,태초에 시간은 강물처럼 흐르지 않고 고요한 호수와 같았다. 우리는 모두 하나의 이름 '아르카인'으로 불렸고 이 세상 '알테아'는 우리의 정원이었다.,Normal,Center,,,
story_00_002,Main,제0장: 하나였던 시절,2,보를라그,하하하! 아니마 문디의 정기가 오늘도 넘실대는구나! 이 기운을 받을 때마다 내 온몸의 근육이 환희의 노래를 부르는 것 같단 말이지!,Happy,Left,,,
story_00_003,Main,제0장: 하나였던 시절,3,엘라라,보를라그 당신이 느끼는 것은 힘이 아니라 '조화'랍니다. 아니마 문디의 위대한 노래는 근육이 아닌 영혼으로 들어야 그 진정한 아름다움을 알 수 있는 법.,Normal,Right,,,
story_00_004,Main,제0장: 하나였던 시절,4,리산데르,두 사람의 말이 모두 옳네. 강인한 육체는 조화로운 영혼을 담는 훌륭한 그릇이 되고 조화로운 영혼은 강인한 육체가 나아갈 길을 비추지.,Normal,Center,,,
story_00_005,Main,제0장: 하나였던 시절,5,보를라그,흥 자네는 항상 그렇게 어려운 말만 하는군 리산데르. 간단한 게 최고야! 강하면 모든 게 해결된다!,Normal,Left,,,
story_00_006,Main,제0장: 하나였던 시절,6,엘라라,아니요 보를라그. 강함만으로는 부러질 뿐입니다. 물처럼 부드럽게 모든 것을 감싸 안는 순수함이야말로 진정으로 강한 것이지요.,Normal,Right,,,
story_00_007,Main,제0장: 하나였던 시절,7,리산데르,그래서 우리가 함께 있는 것 아니겠나. 검과 방패 그리고 그 모든 것을 꿰뚫어 보는 지혜. 우리 셋이 함께하기에 아르카인은 완벽한 것이지.,Normal,Center,,,
story_00_008,Main,제0장: 하나였던 시절,8,리렌,저희는... 어떻게 이 완벽함을 영원히 지킬 수 있습니까? 만약 우리가 상상조차 할 수 없는 무언가가 이 조화를 깨뜨리려 한다면?,Worried,Center,,,
story_00_009,Main,제0장: 하나였던 시절,9,보를라그,하! 쓸데없는 걱정이구나 애송이! 이 조화를 깨뜨릴 존재가 나타난다면 내가 이 도끼로 뼛가루로 만들어주면 그만이다!,Angry,Left,,,
story_00_010,Main,제0장: 하나였던 시절,10,엘라라,리렌 두려워할 필요 없어요. 아니마 문디의 순수한 빛이 우리를 보호하는 한 어떤 어둠도 감히 이 땅을 침범할 수 없습니다.,Normal,Right,,,
story_00_011,Main,제0장: 하나였던 시절,11,리산데르,흥미로운 질문이군 리렌. 하지만 그 질문 자체가 잘못되었을 수도 있네. 우리는 '지킨다'는 것에만 집중하고 있네.,Normal,Center,,,
story_00_012,Main,제0장: 하나였던 시절,12,리산데르,하지만 멈춰있는 호수는 언젠가 썩기 마련이지. 진정한 생명이란 흐르는 강물처럼 끊임없이 변화하고 새로운 환경에 적응하며 나아가는 것이 아닐까?,Worried,Center,,,
story_01_001,Main,제1장: 검은 눈물의 낙하,13,엘라라,어젯밤 조화의 의식 이후 아니마 문디의 맥동이 여전히 미세하게 흔들리고 있습니다. 마치... 깊은 잠에 든 아이가 악몽을 꾸는 것처럼요.,Worried,Right,,,
story_01_002,Main,제1장: 검은 눈물의 낙하,14,보를라그,엘라라 너무 예민한 것 아닌가? 난 아무것도 느끼지 못했다. 그저 평소보다 정기가 좀 더 맑게 느껴질 뿐.,Normal,Left,,,
story_01_003,Main,제1장: 검은 눈물의 낙하,15,리산데르,아니 보를라그. 나 역시 느꼈네. 힘이 아니라 이 세상의 '법칙' 그 자체가 아주 미세하게 뒤틀리는 듯한 감각이었지.,Worried,Center,,,
story_01_004,Main,제1장: 검은 눈물의 낙하,16,나레이션,그때였다. 갑자기 방 안의 모든 수정들이 일제히 빛을 잃고 도시 전체를 감싸던 아니마 문디의 은은한 노랫소리가 뚝 끊긴다.,Normal,Center,,,
story_01_005,Main,제1장: 검은 눈물의 낙하,17,엘라라,...하늘이... 하늘이 울고 있어...,Sad,Right,,,
story_01_006,Main,제1장: 검은 눈물의 낙하,18,리산데르,아니... 저건 눈물이 아니다. 저것은... 모든 것을 집어삼키는 공허 그 자체다!,Surprised,Center,,,
story_02_001,Main,제2장: 세 갈래의 길,19,리산데르,우리는 선택해야 한다. 이 위기를 어떻게 대처할 것인가. 하지만 더 중요한 것은... 우리가 누구인가를 다시 정의하는 것일지도 모른다.,Normal,Center,,,
story_02_002,Main,제2장: 세 갈래의 길,20,엘라라,정의라니요? 우리는 아르카인입니다. 아니마 문디의 자녀이며 순수함의 수호자예요. 그것이 변할 이유가 있나요?,Normal,Right,,,
story_02_003,Main,제2장: 세 갈래의 길,21,보를라그,뭔 소리인지 모르겠다! 강해지면 되는 거 아닌가? 더 강한 힘으로 저 검은 것들을 박살내면 끝이지!,Angry,Left,,,
story_02_004,Main,제2장: 세 갈래의 길,22,리산데르,그것이 문제다. 우리 각자가 생각하는 '해결책'이 다르다는 것이. 그리고 그 차이는... 돌이킬 수 없는 갈래길로 우리를 이끌고 있다.,Worried,Center,,,
story_03_001,Main,제3장: 대분열의 의식,23,리산데르,이제 더 이상 하나일 수 없다는 걸 우리 모두 알고 있다. 천 년을 함께했지만... 이제는 각자의 길을 가야 할 때인 것 같다.,Sad,Center,,,
story_03_002,Main,제3장: 대분열의 의식,24,엘라라,그렇다면... 우리는 무엇이 될까요? 아르카인이 아니라면... 우리는 누구인가요?,Sad,Right,,,
story_03_003,Main,제3장: 대분열의 의식,25,보를라그,누구든 상관없다! 강하면 되는 거다! 새로운 이름 새로운 힘! 두려워할 것 없어!,Angry,Left,,,
story_03_004,Main,제3장: 대분열의 의식,26,리산데르,그렇다면 이것이 우리의 마지막 작별인가. 천 년의 우정이... 이렇게 끝나는 건가.,Sad,Center,,,
story_03_005,Main,제3장: 대분열의 의식,27,나레이션,그 순간 세계가 찢어졌다. 하나였던 아르카인은 세 개의 새로운 종족으로 분열되었다. 인간 엘프 그리고 오크로.,Normal,Center,,,
story_04_001,Main,제4장: 새로운 시대 첫 번째 전쟁,28,리산데르,우리는... 인간이 되었다. 필멸의 존재... 하지만 그 안에서 무한한 가능성을 찾을 수 있을 것이다.,Normal,Center,,,
story_04_002,Main,제4장: 새로운 시대 첫 번째 전쟁,29,엘라라,우리는 여전히 순수함을 지킬 것입니다. 엘프로서... 영원한 조화 속에서.,Normal,Right,,,
story_04_003,Main,제4장: 새로운 시대 첫 번째 전쟁,30,보를라그,크르르... 오크... 강하다... 강한 것이... 좋다...,Angry,Left,,,
story_04_004,Main,제4장: 새로운 시대 첫 번째 전쟁,31,나레이션,하지만 분열은 평화를 가져다주지 않았다. 대신 끝없는 전쟁의 시대가 시작되었다.,Normal,Center,,,
epilogue_001,Main,에필로그: 천 년의 저주,32,나레이션,천 년이 흘렀다. 아르카인의 이름은 잊혔고 세 종족은 서로를 원수로 여기며 살아왔다.,Normal,Center,,,
epilogue_002,Main,에필로그: 천 년의 저주,33,리산데르,번성이라... 그래 우리는 살아남았지. 하지만... 가끔 꿈을 꾸네. 우리가 하늘을 날고 손짓 한 번으로 별을 빚어내던 시절의 꿈을...,Sad,Center,,,
epilogue_003,Main,에필로그: 천 년의 저주,34,엘라라,내버려 두어라. 벌레들이 꿈틀대는 것까지 신경 쓸 필요는 없다. 순수함이야말로 우리의 가장 강한 갑옷이다.,Normal,Right,,,
epilogue_004,Main,에필로그: 천 년의 저주,35,보를라그,크륵... 약한 놈들. 하지만... 수가 많다. 싸운다... 늘... 싸운다... 하지만... 왜 싸우는지... 가끔... 잊어버린다...,Angry,Left,,,
epilogue_005,Main,에필로그: 천 년의 저주,36,크로노스,천 년 전 고대의 존재들이 혼돈을 봉인했다는 '생명의 쐐기'... 그 원리를 재현하는 것이다!,Normal,Center,,,
epilogue_006,Main,에필로그: 천 년의 저주,37,크로노스,나의 모든 마력과 생명을 바쳐서라도 그 힘을 억지로 끌어낼 수밖에 없다! 이 세상이 혼돈에 삼켜지게 둘 수는 없다!,Normal,Center,,,
epilogue_007,Main,에필로그: 천 년의 저주,38,크로노스,...봉인은... 불완전하다... 언젠가... 진짜가... 나타나야만...,Sad,Center,,,
epilogue_008,Main,에필로그: 천 년의 저주,39,나레이션,천 년 전 세 지도자가 흘렸던 슬픔과 증오의 피가 저주가 되어 깨어났다. 피의 안개는 대륙 전역으로 퍼져나가며 모든 종족의 마음에 분열의 불씨를 지피기 시작했다.,Normal,Center,,,
epilogue_009,Main,에필로그: 천 년의 저주,40,나레이션,하지만 어둠이 가장 깊을 때 별은 가장 밝게 빛나는 법. 이제 천 년의 저주를 끊어낼 새로운 이야기가 시작될 차례였다.,Normal,Center,,,
tutorial_001,Tutorial,게임 시작 튜토리얼,1,카인,이곳이... 전장인가? 뭔가 익숙하면서도 낯선 느낌이 드는군.,Normal,Center,,"튜토리얼 계속하기:tutorial_002|튜토리얼 건너뛰기:game_start",
tutorial_002,Tutorial,게임 시작 튜토리얼,2,안내자,환영합니다 카인님. 이곳은 세 종족이 만나는 전장입니다. 먼저 캐릭터 소환 방법부터 배워보시죠.,Normal,Right,,tutorial_003,
tutorial_003,Tutorial,게임 시작 튜토리얼,3,안내자,화면 하단의 소환 버튼을 눌러보세요. 미네랄 30을 소모하여 랜덤 캐릭터를 소환할 수 있습니다.,Normal,Right,,tutorial_004,
tutorial_004,Tutorial,게임 시작 튜토리얼,4,안내자,캐릭터가 소환되었습니다! 같은 캐릭터 3개를 모으면 더 강한 상위 등급으로 합성할 수 있어요.,Happy,Right,,tutorial_005,
tutorial_005,Tutorial,게임 시작 튜토리얼,5,안내자,주사위 버튼을 누르면 같은 등급의 캐릭터 3개를 자동으로 찾아서 합성해줍니다. 매우 편리한 기능이에요!,Normal,Right,,tutorial_006,
tutorial_006,Tutorial,게임 시작 튜토리얼,6,안내자,종족 시너지도 중요합니다. 같은 종족 캐릭터가 많을수록 해당 종족 전체가 강해집니다.,Normal,Right,,tutorial_007,
tutorial_007,Tutorial,게임 시작 튜토리얼,7,안내자,이제 실전을 시작해보세요! 20웨이브를 모두 막아내면 승리입니다. 행운을 빕니다!,Happy,Right,,game_start,
event_stage5,Event,5웨이브 클리어 보상,1,안내자,축하합니다! 5웨이브를 클리어하셨습니다. 보상으로 2성 캐릭터 중 하나를 선택하세요.,Happy,Center,,"인간 전사 선택:reward_human|엘프 궁수 선택:reward_elf|오크 광전사 선택:reward_orc",
event_stage10,Event,10웨이브 클리어 보상,1,안내자,대단합니다! 10웨이브 돌파! 더욱 강력한 2성 캐릭터를 선택하세요.,Happy,Center,,"인간 기사 선택:reward_human2|엘프 마법사 선택:reward_elf2|오크 족장 선택:reward_orc2",
event_stage15,Event,15웨이브 클리어 보상,1,안내자,놀랍습니다! 15웨이브까지! 전설급 2성 캐릭터를 드리겠습니다.,Excited,Center,,"전설 인간 영웅:reward_legend1|전설 엘프 현자:reward_legend2|전설 오크 왕:reward_legend3",
char_human_001,Character,인간 캐릭터 스토리,1,인간 전사,나는 평범한 농부의 아들이었다. 하지만 이 혼돈의 시대에 검을 들 수밖에 없었지.,Normal,Center,,char_human_002,
char_human_002,Character,인간 캐릭터 스토리,2,인간 전사,내 고향을 지키기 위해 싸우겠다. 비록 약하지만 포기하지 않는 것이 인간의 힘이다.,Normal,Center,,char_human_003,
char_human_003,Character,인간 캐릭터 스토리,3,인간 전사,언젠가는... 세 종족이 다시 하나가 될 수 있을까? 그런 날이 오기를 바란다.,Worried,Center,,,
char_elf_001,Character,엘프 캐릭터 스토리,1,엘프 궁수,숲의 조화가 깨어지고 있다. 이 모든 전쟁과 다툼이 자연을 병들게 하고 있어.,Sad,Right,,char_elf_002,
char_elf_002,Character,엘프 캐릭터 스토리,2,엘프 궁수,하지만 포기할 수는 없다. 순수함을 지키는 것이 우리 엘프의 사명이니까.,Normal,Right,,char_elf_003,
char_elf_003,Character,엘프 캐릭터 스토리,3,엘프 궁수,언젠가 모든 생명이 다시 조화롭게 살 수 있는 날이 올 것이다. 그때까지 나는 싸우겠다.,Normal,Right,,,
char_orc_001,Character,오크 캐릭터 스토리,1,오크 전사,크르르... 강해야... 살아남는다... 약하면... 죽는다...,Angry,Left,,char_orc_002,
char_orc_002,Character,오크 캐릭터 스토리,2,오크 전사,하지만... 가끔... 기억난다... 평화로웠던 시절이... 모두가... 친구였던...,Sad,Left,,char_orc_003,
char_orc_003,Character,오크 캐릭터 스토리,3,오크 전사,그때로... 돌아갈 수... 있을까... 크르르...,Worried,Left,,,
```

### CSV 데이터 사용 방법

1. **Unity Editor에서 가져오기**:
   - `Tools > pjy > Story Dialogue Editor` 메뉴 열기
   - "CSV 가져오기" 버튼 클릭
   - 위 CSV 데이터를 복사하여 텍스트 영역에 붙여넣기
   - "가져오기" 버튼으로 게임에 적용

2. **스토리 타입 설명**:
   - `Main`: 메인 스토리 (1000년 전 이야기)
   - `Tutorial`: 게임 시작 튜토리얼
   - `Event`: 웨이브 클리어 보상 이벤트
   - `Character`: 캐릭터별 개인 스토리

3. **감정 타입**:
   - `Normal`: 평상시
   - `Happy`: 기쁨
   - `Sad`: 슬픔
   - `Angry`: 분노
   - `Surprised`: 놀람
   - `Worried`: 걱정
   - `Excited`: 흥분

4. **캐릭터 위치**:
   - `Left`: 화면 왼쪽
   - `Center`: 화면 중앙
   - `Right`: 화면 오른쪽

5. **선택지 형식**:
   - `선택지 텍스트:다음스토리ID|다른선택지:다른스토리ID`
   - 예: `"튜토리얼 계속:tutorial_002|건너뛰기:game_start"`

================================================================================

## 🛠️ 캐릭터 CSV 데이터베이스 시스템

### 📋 시스템 개요
Twelve에서는 모든 캐릭터 데이터를 CSV 형식으로 관리하고, 이를 Unity의 ScriptableObject로 자동 변환하는 시스템을 제공합니다. 이를 통해 외부 데이터 관리 도구나 AI를 활용한 대량 캐릭터 생성이 가능합니다.

### 🎯 핵심 기능
- **CSV ↔ ScriptableObject 양방향 변환**: 데이터 수정 후 실시간 게임 반영
- **상속 기반 캐릭터 행동 시스템**: 캐릭터별 고유 능력을 컴포넌트로 관리
- **인스펙터 체크박스 제어**: Unity 에디터에서 시각적으로 행동 활성화/비활성화
- **18개 샘플 캐릭터**: 3종족 × 6개씩 완전한 캐릭터 데이터 제공

### 📊 CSV 데이터 구조

#### 캐릭터 데이터 CSV 헤더
```csv
ID,Name,Race,Star,Level,AttackPower,AttackRange,AttackSpeed,MaxHP,MoveSpeed,Cost,RangeType,AttackTargetType,AttackShapeType,IsAreaAttack,AreaAttackRadius,IsBuffSupport,Skills,Behaviors,SpriteResourcePath,PrefabResourcePath,Description
```

#### 주요 필드 설명
- **기본 정보**: ID, Name, Race(Human/Orc/Elf), Star(1-3), Level
- **전투 스탯**: AttackPower, AttackRange, AttackSpeed, MaxHP, MoveSpeed, Cost
- **공격 설정**: RangeType(Melee/Ranged/LongRange), AttackTargetType, AttackShapeType
- **스킬 & 행동**: Skills(세미콜론 구분), Behaviors(세미콜론 구분)
- **리소스**: SpriteResourcePath, PrefabResourcePath

#### 샘플 데이터 예시
```csv
1,인간 전사,Human,1,1,50,3,1.5,120,2,15,Melee,Monster,Single,False,1.5,False,"basic_attack;warrior_strike","MeleeAttackBehavior;TankBehavior",Characters/Human/인간_전사,Prefabs/Characters/Human/인간_전사,Human 종족의 인간 전사입니다.
9,엘프 현자,Elf,3,1,80,5,2.8,90,2.5,28,LongRange,Monster,Area,True,2.5,True,"lightning_bolt;teleport;mana_shield;wisdom_aura","MagicAttackBehavior;TeleportBehavior;WisdomBehavior",Characters/Elf/엘프_현자,Prefabs/Characters/Elf/엘프_현자,Elf 종족의 엘프 현자입니다.
```

### 🎮 상속 기반 캐릭터 행동 시스템

#### 행동 컴포넌트 아키텍처
```csharp
CharacterBehaviorBase (추상 클래스)
├── MeleeAttackBehavior (근접 공격)
├── RangedAttackBehavior (원거리 공격)
├── SupportBehavior (힐링/버프)
├── MagicAttackBehavior (마법 공격)
├── TankBehavior (방어 특화)
├── BerserkerBehavior (광전사)
├── StealthBehavior (은신)
└── LeaderBehavior (리더십/버프)
```

#### 행동 시스템 특징
- **우선순위 기반 실행**: 높은 Priority 행동이 먼저 실행
- **조건부 실행**: CanExecute()로 실행 조건 체크
- **인스펙터 제어**: Unity 에디터에서 체크박스로 개별 행동 활성화/비활성화
- **실시간 디버깅**: 마지막 실행 시간, 상태 정보 표시

### 🛠️ Unity 에디터 도구

#### 1. Character CSV Editor
**메뉴 위치**: `Tools > pjy > Character CSV Editor`

**주요 기능**:
- CSV 파일 로드/저장
- 캐릭터 데이터 실시간 편집
- ScriptableObject 에셋 자동 생성
- 샘플 데이터 생성

**사용 방법**:
1. Unity에서 `Tools > pjy > Character CSV Editor` 열기
2. "새 캐릭터 CSV DB 생성" 클릭
3. "샘플 데이터 생성"으로 18개 캐릭터 생성
4. "ScriptableObject 생성"으로 에셋 파일 생성
5. CSV 파일로 내보내기/가져오기 가능

#### 2. CharacterBehaviorManager 컴포넌트
캐릭터 GameObject에 추가하여 행동을 관리하는 컴포넌트입니다.

**인스펙터 설정**:
```
Character Behavior Manager (Script)
├── 행동 관리
│   ├── Is Active: ☑️ (전체 행동 시스템 활성화)
│   └── Behavior Update Interval: 0.1 (업데이트 간격)
└── 등록된 행동들
    ├── Behavior 1
    │   ├── Is Enabled: ☑️ (개별 행동 활성화)
    │   ├── Behavior: MeleeAttackBehavior
    │   ├── Priority: 1.0
    │   ├── Behavior Name: "근접 공격"
    │   └── Last Execution Time: "14:32:15"
    ├── Behavior 2
    │   ├── Is Enabled: ☑️
    │   ├── Behavior: SupportBehavior
    │   └── Priority: 0.8
    └── [더 많은 행동들...]
```

### 🎯 개발자 구현 가이드

#### 1. 새로운 캐릭터 추가하기

**Step 1: CSV 데이터 추가**
```csv
19,인간 성기사,Human,3,1,75,3.5,1.6,160,2.1,28,Melee,Monster,Single,False,1.5,True,"holy_strike;divine_protection;mass_heal","MeleeAttackBehavior;SupportBehavior;HolyBehavior",Characters/Human/인간_성기사,Prefabs/Characters/Human/인간_성기사,신성한 힘을 가진 인간 성기사입니다.
```

**Step 2: Unity 에디터에서 변환**
1. Character CSV Editor에서 CSV 로드
2. "ScriptableObject 생성" 클릭
3. 자동으로 CharacterData 에셋 생성됨

**Step 3: 프리팹 설정**
```csharp
// 캐릭터 프리팹에 필요한 컴포넌트 추가
- Character (기본 클래스)
- CharacterBehaviorManager (행동 관리)
- MeleeAttackBehavior (근접 공격)
- SupportBehavior (지원)
- HolyBehavior (신성 능력) - 새로 구현 필요
```

#### 2. 새로운 행동 컴포넌트 만들기

**예시: HolyBehavior 구현**
```csharp
using UnityEngine;
using pjy.Characters.Behaviors;

public class HolyBehavior : CharacterBehaviorBase
{
    [Header("신성 공격 설정")]
    [SerializeField] private float holyDamage = 50f;
    [SerializeField] private float healAmount = 30f;
    [SerializeField] private float range = 4f;
    
    public override string BehaviorName => "신성 능력";
    
    protected override bool CheckExecuteConditions()
    {
        // 언데드 적이나 상처입은 아군이 있는지 확인
        return FindUndeadEnemies() != null || FindInjuredAllies() != null;
    }
    
    protected override void OnExecute()
    {
        // 언데드 공격 또는 아군 힐링 실행
        GameObject target = FindUndeadEnemies() ?? FindInjuredAllies();
        if (target != null)
        {
            PerformHolyAction(target);
        }
    }
    
    private void PerformHolyAction(GameObject target)
    {
        // 신성한 빛 이펙트와 함께 공격/힐링
    }
}
```

#### 3. CSV에서 행동 자동 설정

**CharacterBehaviorManager 사용법**:
```csharp
// CSV에서 로드된 행동 컴포넌트 자동 추가
string[] behaviors = {"MeleeAttackBehavior", "SupportBehavior", "HolyBehavior"};
CharacterBehaviorManager manager = character.GetComponent<CharacterBehaviorManager>();
manager.SetupBehaviorsFromCSV(behaviors);
```

### 🎮 플레이어 게임플레이 경험

#### 1. 캐릭터 소환 시스템
- **원버튼 소환**: 미네랄 30으로 랜덤 캐릭터 소환
- **종족별 확률**: Human 40%, Orc 35%, Elf 25%
- **등급별 확률**: 1성 70%, 2성 25%, 3성 5%

#### 2. 캐릭터 행동 관찰
플레이어는 다음과 같은 차별화된 행동을 관찰할 수 있습니다:

**인간 전사 (MeleeAttackBehavior + TankBehavior)**:
- 적에게 다가가 근접 공격
- 체력이 낮은 아군 앞에서 방어 자세

**엘프 현자 (MagicAttackBehavior + TeleportBehavior + WisdomBehavior)**:
- 긴 사거리에서 마법 공격
- 위험 시 순간이동으로 회피
- 아군에게 지혜의 오라 제공

**오크 광전사 (MeleeAttackBehavior + BerserkerBehavior + BloodlustBehavior)**:
- 적에게 돌진하여 강력한 근접 공격
- 체력이 낮아질수록 공격속도 증가
- 적 처치 시 체력 회복

#### 3. 전략적 배치 및 합성
- **3라인 시스템**: 왼쪽(탱커), 중앙(딜러), 오른쪽(서포터) 배치
- **종족 시너지**: 같은 종족 3/5/7/9명 시 전체 버프
- **자동 합성**: 주사위 버튼으로 1성×3→2성, 2성×3→3성

#### 4. 인스펙터 커스터마이징 (개발자 모드)
개발자는 Unity 에디터에서 실시간으로 캐릭터 행동을 조정할 수 있습니다:

```
캐릭터 선택 → CharacterBehaviorManager 컴포넌트
├── MeleeAttackBehavior: ☑️ 활성화
├── SupportBehavior: ☐ 비활성화 (힐링 금지)
├── TankBehavior: ☑️ 활성화
└── 우선순위 조정으로 행동 순서 변경
```

### 📁 파일 구조
```
Assets/
├── CSV/
│   └── SampleCharacterData.csv          # 18개 캐릭터 샘플 데이터
├── Scripts/pjy/
│   ├── Data/
│   │   ├── CharacterCSVDatabase.cs       # CSV 메인 데이터베이스
│   │   ├── CharacterSkillDatabase.cs     # 스킬 데이터베이스
│   │   └── Editor/
│   │       └── CharacterCSVEditor.cs     # Unity 에디터 도구
│   └── Characters/
│       ├── CharacterBehaviorManager.cs   # 행동 관리자
│       └── Behaviors/
│           ├── CharacterBehaviorBase.cs  # 행동 기본 클래스
│           ├── MeleeAttackBehavior.cs    # 근접 공격
│           ├── RangedAttackBehavior.cs   # 원거리 공격
│           └── SupportBehavior.cs        # 서포트
└── Prefabs/Data/Characters/              # 생성된 ScriptableObject 에셋
```

### 🚀 확장 가능성

#### 1. AI 기반 캐릭터 생성
CSV 형식을 활용하여 AI가 새로운 캐릭터를 자동 생성할 수 있습니다:
```python
# Python 예시: AI가 밸런스를 고려한 캐릭터 생성
def generate_character(race, star_level):
    base_stats = calculate_base_stats(race, star_level)
    skills = select_racial_skills(race)
    behaviors = optimize_behavior_combination(race, star_level)
    return create_csv_entry(base_stats, skills, behaviors)
```

#### 2. 외부 데이터 관리
Google Sheets, Excel 등과 연동하여 기획자가 직접 캐릭터 데이터를 관리할 수 있습니다:
```
Google Sheets → CSV 다운로드 → Unity 에디터 툴 → 게임 반영
```

#### 3. 커뮤니티 모딩
플레이어가 CSV 파일을 수정하여 커스텀 캐릭터를 만들 수 있습니다:
```csv
# 플레이어 제작 캐릭터
999,나만의 영웅,Human,3,1,999,10,0.5,500,5,50,LongRange,All,Area,True,5.0,True,"ultimate_skill","AllBehaviors",Custom/나만의_영웅,Custom/나만의_영웅,플레이어가 만든 최강 캐릭터
```

이 시스템을 통해 Twelve는 데이터 중심의 확장 가능한 캐릭터 시스템을 제공하며, 개발자와 플레이어 모두에게 유연한 커스터마이징 환경을 제공합니다.

================================================================================

## 🎓 개발자 Unity 구현 가이드 (완전 초보자용)

### 📋 구현 전 준비사항

#### 1. Unity 프로젝트 설정
```
1) Unity Hub 실행
2) "Projects" → "Open" → 프로젝트 폴더 선택
3) Unity 6.2 Beta 버전으로 열기
4) Project 창에서 Assets 폴더 확인
```

#### 2. 필수 폴더 구조 생성
```
Assets/
├── CSV/                    # 이미 생성됨
├── Scripts/pjy/           # 이미 생성됨
│   ├── Managers/          # 이미 생성됨
│   ├── Characters/        # 이미 생성됨
│   ├── Data/             # 이미 생성됨
│   └── UI/               # 이미 생성됨
├── Prefabs/
│   ├── Characters/
│   ├── UI/
│   └── Effects/
└── Resources/
    ├── Audio/
    ├── Sprites/
    └── Prefabs/
```

### 🎮 1단계: 기본 게임 씬 설정

#### **1.1 GameScene 생성**
```
1) File → New Scene
2) Scene 이름을 "GameScene"으로 변경
3) File → Save Scene → Assets/Scenes/GameScene.unity
```

#### **1.2 기본 매니저 오브젝트 생성**
```
Hierarchy에서 우클릭 → Create Empty → 이름을 "_Managers"로 변경

_Managers 하위에 다음 Empty GameObject들 생성:
- GameManager
- SoundManager  
- TutorialManager
- PlacementManager
- SummonManager
- MergeManager
```

#### **1.3 매니저 스크립트 컴포넌트 추가**
```
각 매니저 오브젝트를 선택하고:
1) Inspector → Add Component
2) 해당 스크립트 검색 후 추가
   - GameManager → GameManager.cs
   - SoundManager → SoundManager.cs
   - TutorialManager → TutorialManager.cs
   등등...
```

### 🎯 2단계: 사운드 시스템 구현

#### **2.1 SoundManager 설정**
```
1) SoundManager 오브젝트 선택
2) Inspector에서 Sound Manager (Script) 컴포넌트 확인
3) 오디오 소스 참조 설정:
   - BGM Source: 새 AudioSource 컴포넌트 추가
   - SFX Source: 새 AudioSource 컴포넌트 추가
```

#### **2.2 오디오 클립 추가**
```
1) Resources/Audio 폴더에 오디오 파일들 배치
   - BGM_MainMenu.mp3
   - BGM_Game.mp3
   - SFX_ButtonClick.wav
   - SFX_CharacterSummon.wav
   등등...

2) SoundManager Inspector에서 각 필드에 오디오 클립 할당:
   - Main Menu BGM → BGM_MainMenu
   - Game BGM → BGM_Game
   - Button Click SFX → SFX_ButtonClick
   등등...
```

#### **2.3 사운드 호출 예시**
```csharp
// 버튼 클릭 시
SoundManager.Instance.PlayGameEventSFX(GameEvent.ButtonClick);

// BGM 변경 시
SoundManager.Instance.PlayGameStateBGM(GameState.InGame);

// 특정 위치에서 효과음
SoundManager.Instance.PlaySFXAtPosition("Attack", transform.position);
```

### 🎓 3단계: 튜토리얼 시스템 구현

#### **3.1 튜토리얼 UI 생성**
```
1) Hierarchy → UI → Canvas (Canvas 생성)
2) Canvas 이름을 "TutorialCanvas"로 변경
3) Canvas 하위에 Panel 생성, 이름을 "TutorialPanel"로 변경

TutorialPanel 하위에 다음 UI 요소들 생성:
- Background (Image): 반투명 검은색 배경
- ContentPanel (Panel): 튜토리얼 내용 영역
  - TitleText (TextMeshPro): 튜토리얼 제목
  - DescriptionText (TextMeshPro): 튜토리얼 설명
  - NextButton (Button): 다음 버튼
  - SkipButton (Button): 건너뛰기 버튼
- HighlightOverlay (Panel): UI 하이라이트용
  - HighlightFrame (Image): 하이라이트 테두리
```

#### **3.2 TutorialManager 설정**
```
1) TutorialManager 오브젝트 선택
2) Tutorial Manager (Script) 컴포넌트에서 UI 참조 설정:
   - Tutorial Panel → TutorialPanel
   - Tutorial Text → DescriptionText
   - Next Button → NextButton
   - Skip Button → SkipButton
   - Highlight Overlay → HighlightOverlay
   - Highlight Frame → HighlightFrame
```

#### **3.3 튜토리얼 단계 설정**
```
Inspector의 Tutorial Steps 영역에서:
1) Size를 6으로 설정
2) 각 Element에 다음과 같이 설정:

Element 0:
- Title: "게임 시작"
- Description: "Twelve에 오신 것을 환영합니다!"
- Target UI: 비워둠
- Highlight Target: 체크 해제
- Wait For Input: 체크
- Step Type: Introduction

Element 1:
- Title: "캐릭터 소환하기"  
- Description: "화면 하단의 소환 버튼을 눌러보세요."
- Target UI: "SummonButton"
- Highlight Target: 체크
- Wait For Input: 체크 해제
- Step Type: UI_Guide

... (나머지 단계들도 동일하게 설정)
```

### 🎪 4단계: 캐릭터 CSV 시스템 연동

#### **4.1 Character CSV Editor 실행**
```
1) Unity 상단 메뉴 → Tools → pjy → Character CSV Editor
2) 에디터 창이 열리면 "새 캐릭터 CSV DB 생성" 클릭
3) 저장 위치: Assets/Prefabs/Data/CharacterCSVDatabase.asset
4) "샘플 데이터 생성" 버튼 클릭
5) "ScriptableObject 생성" 버튼 클릭
```

#### **4.2 캐릭터 프리팹 생성**
```
1) Hierarchy → Create Empty → 이름을 "CharacterTemplate"으로 변경
2) 다음 컴포넌트들 추가:
   - Character (Script)
   - CharacterBehaviorManager (Script)
   - SpriteRenderer
   - Collider2D (또는 Collider)
   - Rigidbody2D (또는 Rigidbody)

3) 이를 Prefab으로 저장: Assets/Prefabs/Characters/CharacterTemplate.prefab
```

#### **4.3 행동 컴포넌트 추가**
```
캐릭터 타입별로 적절한 Behavior 컴포넌트 추가:

근접 전사:
- MeleeAttackBehavior
- TankBehavior

원거리 궁수:
- RangedAttackBehavior
- KitingBehavior

지원 힐러:
- SupportBehavior
- HealerBehavior
```

### 🎯 5단계: 게임플레이 연동

#### **5.1 SummonManager 연동**
```csharp
// SummonManager에서 CSV 데이터베이스 사용
public class SummonManager : MonoBehaviour
{
    [SerializeField] private CharacterCSVDatabase csvDatabase;
    
    private void Start()
    {
        // CSV에서 캐릭터 데이터 로드
        List<CharacterData> characters = csvDatabase.GenerateCharacterData();
    }
    
    public void SummonCharacter()
    {
        // 랜덤 캐릭터 선택 및 소환
        CharacterData randomData = csvDatabase.GetAllCharacters()[Random.Range(0, csvDatabase.GetAllCharacters().Count)];
        CreateCharacterFromData(randomData);
    }
}
```

#### **5.2 GameManager 이벤트 연동**
```csharp
// 게임 상태 변경 시 사운드 재생
public void ChangeGameState(GameState newState)
{
    currentState = newState;
    SoundManager.Instance.PlayGameStateBGM(newState);
}

// 웨이브 시작 시
public void StartWave(int waveNumber)
{
    SoundManager.Instance.PlayGameEventSFX(GameEvent.WaveStart);
    // 웨이브 로직...
}
```

### 🛠️ 6단계: UI 버튼 이벤트 연결

#### **6.1 버튼 이벤트 설정**
```
모든 UI 버튼에 다음과 같이 설정:

1) Button 컴포넌트의 OnClick() 이벤트에서:
   - + 버튼 클릭
   - Object 필드에 SoundManager 오브젝트 드래그
   - Function에서 SoundManager → PlayGameEventSFX(GameEvent)
   - GameEvent.ButtonClick 선택

2) 기존 버튼 기능도 함께 연결:
   - + 버튼 다시 클릭  
   - 실제 기능을 담당하는 매니저 오브젝트 드래그
   - 해당 기능 함수 선택
```

#### **6.2 소환 버튼 설정 예시**
```
SummonButton의 OnClick 이벤트:
1) SoundManager.PlayGameEventSFX(GameEvent.ButtonClick)
2) SummonManager.SummonCharacter()

이렇게 하면 버튼 클릭 시:
- 먼저 클릭 사운드 재생
- 그 다음 캐릭터 소환 실행
- 소환 완료 시 소환 사운드 재생
```

### 🎮 7단계: 테스트 및 디버깅

#### **7.1 플레이 모드 테스트**
```
1) Unity 에디터에서 Play 버튼 클릭
2) 다음 사항들 확인:
   - 튜토리얼이 올바르게 시작되는가?
   - 사운드가 정상 재생되는가?
   - 캐릭터 소환이 작동하는가?
   - CSV 데이터가 올바르게 로드되는가?
```

#### **7.2 디버그 콘솔 확인**
```
Window → General → Console

다음과 같은 로그 확인:
- [TutorialManager] 튜토리얼이 시작되었습니다.
- [SoundManager] BGM이 재생되었습니다: MainMenu  
- [CharacterCSVDatabase] 18개의 캐릭터 데이터를 로드했습니다.
```

#### **7.3 문제 해결**
```
자주 발생하는 문제들:

1) NullReferenceException:
   - Inspector에서 모든 참조가 올바르게 설정되었는지 확인
   - 싱글톤 인스턴스가 씬에 존재하는지 확인

2) 사운드가 재생되지 않음:
   - Audio Source 컴포넌트가 추가되었는지 확인
   - 오디오 클립이 할당되었는지 확인
   - 볼륨이 0으로 설정되지 않았는지 확인

3) 튜토리얼이 시작되지 않음:
   - TutorialManager의 Enable Tutorial이 체크되었는지 확인
   - PlayerPrefs에서 "TutorialCompleted" 삭제 후 재시도
```

### 📱 8단계: 빌드 및 배포 준비

#### **8.1 빌드 설정**
```
1) File → Build Settings
2) Scenes In Build에 다음 씬들 추가:
   - GameScene
   - LobbyScene (있다면)
3) Platform을 Android 또는 iOS로 변경
4) Build 클릭
```

#### **8.2 최적화 확인사항**
```
1) 모든 오디오 클립이 적절히 압축되었는지 확인
2) 텍스처 크기가 모바일에 적합한지 확인  
3) 캐릭터 프리팹이 오브젝트 풀링을 사용하는지 확인
4) UI 해상도 스케일링이 올바른지 확인
```

이 가이드를 따라하면 Unity 초보자도 Twelve의 핵심 시스템들을 단계별로 구현할 수 있습니다. 각 단계에서 문제가 발생하면 Unity Console의 오류 메시지를 확인하고, 참조 설정을 다시 한번 점검해보세요

---

## 🛠️ Unity 구현 가이드 (시스템 개선 사항)

### 🔧 수정된 기능들의 Unity 구현 방법

#### 1. CharacterData 중복 필드 정리 적용하기

**📍 위치**: `Assets/Scripts/pjy/Data/CharacterData.cs`

**🎯 개선 사항**:
- 중복 필드 제거로 데이터 일관성 향상
- Obsolete 프로퍼티로 기존 코드 호환성 유지

**📋 Unity에서 구현하는 방법**:

1. **기존 스크립트 수정 확인**:
   ```csharp
   // 이전 (중복 필드들)
   public float range = 3f;           // ❌ 제거됨
   public float health = 100f;        // ❌ 제거됨  
   public float maxHealth = 100f;     // ❌ 제거됨
   public RaceType tribe;             // ❌ 제거됨
   
   // 현재 (통합된 필드들)
   public float attackRange = 3f;     // ✅ 주 필드
   public float maxHP = 100f;         // ✅ 주 필드
   public CharacterRace race;         // ✅ 주 필드
   
   // 호환성 프로퍼티들 (자동 변환)
   [Obsolete] public float range => attackRange;
   [Obsolete] public float health => maxHP;
   [Obsolete] public RaceType tribe => (RaceType)race;
   ```

2. **Inspector에서 확인**:
   - CharacterData ScriptableObject를 선택
   - 중복 필드들이 제거되고 깔끔한 구조 확인
   - Obsolete 프로퍼티는 Inspector에 표시되지 않음

3. **기존 코드 호환성**:
   ```csharp
   // 기존 코드는 계속 작동함 (자동 변환)
   float characterRange = characterData.range;      // ✅ 자동으로 attackRange 반환
   float characterHealth = characterData.health;    // ✅ 자동으로 maxHP 반환
   ```

#### 2. CharacterDatabase 이벤트 기반 초기화 적용하기

**📍 위치**: `Assets/Scripts/pjy/Data/CharacterDatabase.cs`, `Assets/Scripts/pjy/Managers/GameManager.cs`

**🎯 개선 사항**:
- 타이밍 의존성 문제 해결
- 안정적인 초기화 순서 보장

**📋 Unity에서 구현하는 방법**:

1. **GameManager 설정**:
   ```csharp
   // GameManager.cs에서 이벤트 발생
   public static System.Action OnGameManagerReady;
   
   private void Awake() 
   {
       // GameManager 초기화 완료 후 이벤트 호출
       OnGameManagerReady?.Invoke();
   }
   ```

2. **CharacterDatabase 이벤트 구독**:
   ```csharp
   // CharacterDatabase.cs에서 이벤트 구독
   private void Start()
   {
       GameManager.OnGameManagerReady += OnGameManagerReady;
       
       // 이미 준비된 경우 즉시 실행
       if (GameManager.Instance != null)
           OnGameManagerReady();
   }
   ```

3. **Unity에서 테스트**:
   - Play 모드에서 Console 확인
   - `"[CharacterDatabase] 초기화 완료"` 메시지 확인
   - 더 이상 timeout 오류가 발생하지 않음

#### 3. CSV 안전한 파싱 시스템 활용하기

**📍 위치**: `Assets/Scripts/pjy/Data/CharacterCSVDatabase.cs`

**🎯 개선 사항**:
- CSV 파싱 오류 내성 향상
- 잘못된 데이터에 대한 기본값 지원

**📋 Unity에서 구현하는 방법**:

1. **CSV 데이터 안전성 테스트**:
   ```csv
   ID,Name,Race,Star,Level,AttackPower,AttackRange,AttackSpeed,MaxHP,MoveSpeed,Cost,RangeType,AttackTargetType,AttackShapeType,IsAreaAttack,AreaAttackRadius,IsBuffSupport,Skills,Behaviors,SpriteResourcePath,PrefabResourcePath,Description
   1,테스트캐릭터,Human,1,1,INVALID_DATA,3,1.5,100,2,15,Melee,Monster,Single,false,1.5,false,"basic_attack","MeleeAttackBehavior",Characters/Test,Prefabs/Test,테스트 캐릭터
   ```

2. **안전한 파싱 확인**:
   - 위 CSV 데이터에서 `INVALID_DATA` 부분이 기본값 10으로 자동 변환
   - Console에서 경고 메시지 확인하지만 게임은 정상 동작

3. **CSV 에디터 도구 사용**:
   ```csharp
   // Unity Menu: Tools > CSV Data Sync Manager
   // 1. CSV 파일 수정
   // 2. "Load from CSV" 버튼 클릭
   // 3. 파싱 결과 확인
   // 4. "Export to CSV" 버튼으로 검증된 데이터 저장
   ```

#### 4. 이벤트 기반 시스템 확장하기

**🎯 추가 개선 방향**:

1. **다른 매니저들도 이벤트 기반으로 전환**:
   ```csharp
   // 예시: SoundManager
   public static System.Action OnSoundManagerReady;
   
   // 예시: TutorialManager  
   public static System.Action OnTutorialManagerReady;
   ```

2. **의존성 체인 관리**:
   ```csharp
   // 초기화 순서: GameManager → CharacterDatabase → SoundManager → TutorialManager
   GameManager.OnGameManagerReady += () => {
       CharacterDatabase.Instance?.Initialize();
   };
   
   CharacterDatabase.OnDatabaseInitialized += (db) => {
       SoundManager.Instance?.Initialize();
   };
   ```

3. **Unity에서 검증하는 방법**:
   - Console에서 초기화 순서 확인
   - 각 매니저의 Ready 이벤트 로그 확인
   - Play 모드에서 모든 시스템이 정상 동작하는지 테스트

### 🚀 성능 최적화 가이드

#### 1. CSV 파싱 성능 개선
- **대용량 CSV**: 코루틴으로 분할 파싱
- **캐싱**: 파싱 결과를 메모리에 캐시
- **지연 로딩**: 필요한 캐릭터만 로드

#### 2. 이벤트 시스템 최적화
- **구독 해제**: OnDestroy에서 모든 이벤트 구독 해제
- **메모리 누수 방지**: 정적 이벤트 사용 시 주의
- **성능 모니터링**: Unity Profiler로 이벤트 호출 빈도 확인

### 🎯 다음 단계 개발 가이드

#### 1. 우선순위 높음
- **UI 스케일링 문제 해결**: Canvas Scaler 설정 최적화
- **성능 최적화**: 50마리 이상 시 프레임 최적화
- **네트워킹 시스템**: Unity Netcode 본격 도입

#### 2. 우선순위 중간  
- **밸런스 조정**: 캐릭터 스탯 및 챕터 난이도
- **비주얼 개선**: 파티클 효과 및 애니메이션
- **추가 컨텐츠**: 신규 캐릭터 및 스킬

이제 모든 핵심 시스템이 안정적으로 구현되었으며, Unity에서 바로 사용할 수 있는 상태입니다!

---

## 🎯 Unity 초보자를 위한 완전 구현 가이드

### 📁 프로젝트 설정 단계별 가이드

#### 1단계: Unity 프로젝트 열기
```
1. Unity Hub 실행
2. "Open" 버튼 클릭
3. "C:\Users\super\OneDrive\문서\GitHub\Twelve" 폴더 선택
4. Unity 6.2 Beta로 프로젝트 열기
5. 로딩 완료 대기 (약 2-3분)
```

#### 2단계: 씬 확인 및 설정
```
1. Project 창에서 Assets/Scenes 폴더 열기
2. GameScene.unity 더블클릭하여 열기
3. Hierarchy에서 다음 오브젝트들 확인:
   - GameManager
   - SummonManager  
   - PlacementManager
   - WaveSpawner
   - TileManager
   - CharacterDatabase
```

#### 3단계: 필수 컴포넌트 확인
**GameManager 설정:**
```
1. Hierarchy에서 GameManager 선택
2. Inspector에서 다음 필드 확인:
   - waveSpawner: WaveSpawner 오브젝트 할당
   - placementManager: PlacementManager 오브젝트 할당
   - resultUIPanel: 결과 UI 패널 할당
   - resultTitleText: TMPro 텍스트 컴포넌트 할당
```

**SummonManager 설정:**
```
1. Hierarchy에서 SummonManager 선택
2. Inspector에서 다음 필드 확인:
   - summonCost: 30 (기본값)
   - summonEffectPrefab: 소환 이펙트 프리팹 할당
```

### 🔧 문제 해결 가이드

#### 문제 1: "Manager를 찾을 수 없습니다" 오류
**해결 방법:**
```
1. Console 창에서 오류 메시지 확인
2. 누락된 Manager를 Hierarchy에 추가:
   - 빈 GameObject 생성 (우클릭 → Create Empty)
   - 해당 Manager 스크립트 컴포넌트 추가
   - 이름을 Manager 이름으로 변경
```

#### 문제 2: "캐릭터 프리팹을 찾을 수 없습니다" 오류
**해결 방법:**
```
1. Resources 폴더 구조 확인:
   Assets/Resources/
   ├── Prefabs/
   │   ├── Characters/
   │   └── DefaultCharacter.prefab
   └── Data/
       └── CharacterDatabase.asset

2. 기본 캐릭터 프리팹 생성:
   - 빈 GameObject 생성
   - Character.cs 스크립트 추가
   - SpriteRenderer 컴포넌트 추가
   - Resources/Prefabs/DefaultCharacter.prefab으로 저장
```

#### 문제 3: CSV 데이터 로딩 실패
**해결 방법:**
```
1. Assets/CSV/SampleCharacterData.csv 파일 확인
2. Unity Menu: Tools > CSV Data Sync Manager 실행
3. "Load from CSV" 버튼 클릭
4. Console에서 로딩 결과 확인
```

### 🎮 게임 실행 테스트 가이드

#### 기본 동작 테스트
```
1. Play 버튼 클릭
2. Console에서 초기화 메시지 확인:
   - "[GameManager] 게임 시작!"
   - "[SummonManager] 초기화 완료"
   - "[CharacterDatabase] 초기화 완료"

3. 소환 테스트:
   - 소환 버튼 클릭 (또는 Space키)
   - 미네랄 30 차감 확인
   - 랜덤 위치에 캐릭터 생성 확인

4. 전투 테스트:
   - 10초 후 자동으로 웨이브 시작
   - 몬스터 3라인 스폰 확인
   - 캐릭터가 몬스터 공격 확인
   - 몬스터가 캐릭터 공격 확인
```

#### 고급 기능 테스트
```
1. 합성 테스트:
   - 같은 캐릭터 3개 소환
   - 주사위 버튼(자동합성) 클릭
   - 상위 등급 캐릭터 생성 확인

2. AI 테스트:
   - AI가 자동으로 캐릭터 소환 확인
   - AI 전략 변화 관찰

3. 종족 시너지 테스트:
   - 같은 종족 여러 개 배치
   - 시너지 효과 확인
```

### 🛠️ 커스터마이징 가이드

#### 새로운 캐릭터 추가하기
```
1. CSV 파일 수정:
   - Assets/CSV/SampleCharacterData.csv 열기
   - 새 행 추가하여 캐릭터 데이터 입력
   - 저장

2. Unity에서 동기화:
   - Tools > CSV Data Sync Manager 실행
   - "Load from CSV" 클릭
   - 새 캐릭터 데이터 확인

3. 프리팹 생성:
   - 새 캐릭터 GameObject 생성
   - Character.cs 스크립트 추가
   - 스프라이트 설정
   - Resources/Prefabs/Characters/에 저장
```

#### 게임 밸런스 조정하기
```
1. 캐릭터 스탯 조정:
   - CSV 파일에서 공격력, 체력, 사거리 수정
   - CSV Data Sync Manager로 동기화

2. 웨이브 난이도 조정:
   - WaveSpawner 선택
   - Inspector에서 몬스터 수, 체력 배율 조정

3. 소환 비용 조정:
   - SummonManager 선택
   - summonCost 값 변경
```

### 📊 성능 최적화 가이드

#### 프레임레이트 최적화
```
1. Quality Settings 조정:
   - Edit > Project Settings > Quality
   - 모바일용 설정 선택
   - Shadow Quality 낮추기

2. 캐릭터 수 제한:
   - PlacementManager에서 maxCharactersPerPlayer 조정
   - 기본값: 50마리

3. 이펙트 최적화:
   - 파티클 수 제한
   - 오브젝트 풀링 활용
```

#### 메모리 최적화
```
1. 텍스처 압축:
   - 캐릭터 스프라이트 선택
   - Inspector에서 Compression 설정

2. 오디오 압축:
   - 사운드 파일 선택
   - Compression Format 조정

3. 에셋 정리:
   - 사용하지 않는 프리팹 제거
   - Resources 폴더 정리
```

### 🔍 디버깅 가이드

#### Console 로그 활용
```
중요한 로그 메시지들:
- "[SummonManager] 초기화 완료" ✅ 정상
- "[SummonManager] 캐릭터 프리팹 로딩 실패" ❌ 프리팹 경로 확인 필요
- "[Monster] 몬스터가 캐릭터를 공격" ✅ 전투 시스템 정상
- "[AutoMergeManager] 합성 완료" ✅ 합성 시스템 정상
```

#### Inspector 디버깅
```
1. 실행 중 값 확인:
   - GameManager의 currentWave 확인
   - Character의 currentHP 확인
   - PlacementManager의 characterCount 확인

2. 연결 상태 확인:
   - Manager들의 참조 필드가 None이 아닌지 확인
   - Missing (Script) 오류 해결
```

이제 Unity 초보자도 단계별로 따라하면 Twelve 게임을 완전히 구현하고 커스터마이징할 수 있습니다!

---


---
1000년 전, 모든 비극의 시작이었던 대서사시, **"원초의 분열"**에 대한 상세한 이야기

### **📜 아르카디아 전기: 1000년 전 이야기**

## **원초의 분열 (The Primordial Schism)**

### **제 0장: 하나였던 시절**

태초에, 세상 **알테아(Althea)**는 하나의 생명으로 숨 쉬었다. 대륙의 중심에는 하늘을 찌를 듯한 거대한 세계수, **'아니마 문디(Anima Mundi - 세계의 영혼)'**가 서 있었고, 그 뿌리는 대륙 전체의 영맥을 이루었다.

그리고 그 세계수의 축복 아래, 단 하나의 지성 종족이 살고 있었다. 그들은 스스로를 **'아르카인(Archaine)'**이라 불렀다.

아르카인은 완벽에 가까운 존재였다. 수백 년을 사는 긴 수명, 강력한 마력과 강인한 신체를 동시에 지녔으며, '아니마 문디'와의 교감을 통해 자연의 섭리를 이해하고 조율했다. 그들에게는 질병도, 가난도, 전쟁도 없었다. 그들은 슬픔을 몰랐기에 진정한 기쁨의 의미 또한 깊이 알지 못했다. 그들의 삶은 영원할 것 같은, 고요하고 평온한 꿈과 같았다.

이 시대를 이끌던 것은 세 개의 위대한 가문이었다.
*   **리산데르(Lysander) 가문:** 이성과 지혜를 숭상하며, 변화에 대한 깊은 통찰력을 지녔다.
*   **엘라라(Elara) 가문:** 아니마 문디와의 교감을 중시하며, 순수함과 보존을 최고의 가치로 여겼다.
*   **보를라그(Vorlag) 가문:** 힘과 투지를 숭배하며, 어떤 위협에도 굴하지 않는 용기를 미덕으로 삼았다.

세 가문은 서로 다른 기풍을 가졌지만, '아르카인'이라는 하나의 이름 아래 조화를 이루며 평화로운 시대를 다스렸다. 그 누구도, 이 영원할 것 같던 꿈이 악몽으로 바뀔 것이라 상상하지 못했다.

---

### **제 1장: 검은 눈물의 낙하**

평화가 천 년 가까이 이어지던 어느 날, 알테아의 하늘에 이변이 일어났다. 별도 달도 없는 밤, 하늘의 한가운데가 찢어지며 칠흑 같은 어둠의 결정체, **'옵시디언의 눈물(The Obsidian Tear)'**이 대지를 향해 떨어졌다.

그것은 소리도, 폭발도 없이 아니마 문디에서 가장 멀리 떨어진 황무지에 내려앉았다. 하지만 그 순간, 세상의 모든 것이 비명을 질렀다. 대지는 검게 썩어 들어갔고, 순수했던 짐승들은 뒤틀린 육체와 핏빛 눈을 가진 몬스터로 변이했다. 세상에 '죽음'과 '고통'이라는 개념이 처음으로 태어난 순간이었다.

아르카인들은 처음 겪는 공포에 경악했다. 세 가문의 수장, 젊고 현명한 **리산데르**, 순결한 대사제 **엘라라**, 용맹한 전사 **보를라그**는 사태를 파악하기 위해 군대를 이끌고 황무지로 향했다. 그리고 그들은 그곳에서 '원초의 혼돈(Primordial Chaos)' 그 자체와 마주했다. '옵시디언의 눈물'은 살아 숨 쉬며 주변의 모든 생명력을 집어삼키고, 그 자리에 고통과 절망만을 내뱉는 존재였다.

---

### **제 2장: 세 갈래의 길**

원초의 혼돈은 너무나도 강력했다. 아르카인들의 마법은 흡수되었고, 그들의 육체는 부패했다. 수많은 희생 끝에 간신히 혼돈의 확산을 저지했지만, 소멸시키는 것은 불가능에 가까웠다. 아르카인 사회는 역사상 처음으로 거대한 위협 앞에 분열하기 시작했다.

세 가문의 수장들은 아니마 문디의 심장부에서 격렬한 논쟁을 벌였다.

**"순응의 길"을 주장하는 리산데르:**
> "보시오, 동포들이여. 이 혼돈은 이제 세상의 일부가 되었소. 이를 부정하고 과거에만 머무르는 것은 스스로 멸망을 자초하는 길이오. 우리는 변화해야 하오. 우리의 완전함을 일부 포기하더라도, 이 새로운 환경에 적응하고 살아남는 법을 배워야 하오. 혼돈을 이해하고, 그 속에서 새로운 질서를 찾아야만 하오!"

**"거부의 길"을 주장하는 엘라라:**
> "어리석은 소리! 저것은 독입니다! 한 방울의 독이라도 섞이면 순수함은 더럽혀질 뿐입니다! 우리는 아니마 문디의 가장 깊은 곳으로 들어가, 세계수와 완전히 하나가 되어 우리 자신을 지켜야 합니다. 외부의 오염으로부터 우리를 격리하고, 순수한 마력의 보호막 속에서 이 재앙이 지나가길 기다려야 합니다!"

**"투쟁의 길"을 주장하는 보를라그:**
> "나약한 소리들만 지껄이는군! 적은 눈앞에 있다! 피하고 적응하는 것이 아니라, 싸워서 분쇄해야 할 적이다! 우리의 힘이 부족하다면, 더 강한 힘을 손에 넣으면 된다! 아니마 문디의 원초적인 생명력을 모두 해방시켜, 우리의 육체를 강철처럼 단련하고, 저 혼돈을 힘으로 찍어 누를 것이다! 투쟁만이 우리의 유일한 길이다!"

논쟁은 끝없이 이어졌고, 그들의 주장에 동조하는 아르카인들이 각자의 가문 아래 모여들었다. 조화롭던 사회는 세 개의 거대한 파벌로 완전히 갈라섰다. 더 이상 타협은 불가능했다.

---

### **제 3장: 대분열의 의식**

결국 그들은 각자의 길을 가기로 결심했다. 하나의 종족으로서 함께 살아갈 수 없다면, 세 개의 다른 종족으로 갈라서는 끔찍한 결단을 내린 것이다.

그들은 아니마 문디의 가장 깊은 곳, 세계수의 핵인 **'생명의 근원(The Core of Life)'** 앞에 섰다. 그리고 세 가문의 수장들은 세 개의 다른 금단의 의식을 동시에 거행했다.

*   **리산데르의 '단절의 의식':** 그는 아니마 문디와의 마력 연결을 스스로 끊어냈다. 그 대가로 아르카인의 긴 수명과 강력한 마력을 잃었지만, 어떤 환경에도 적응할 수 있는 강인한 생명력과 빠른 번식력을 얻었다. 그와 그의 추종자들은 최초의 **'인간(Human)'**이 되었다.

*   **엘라라의 '결속의 의식':** 그녀는 자신과 추종자들을 아니마 문디의 순수한 정수와 융합시켰다. 그들은 가장 강력한 마력을 보존하고 자연과 하나가 되었지만, 그 대가로 숲의 속박을 받아 다시는 세상 밖으로 자유롭게 나갈 수 없게 되었다. 그들은 최초의 **'엘프(Elf)'**와 **'정령(Spirit)'**이 되었다.

*   **보를라그의 '야성의 의식':** 그는 아니마 문디의 제어되지 않은 야생의 힘을 자신의 몸에 주입했다. 그와 추종자들은 상상을 초월하는 신체 능력과 투쟁 본능을 얻었지만, 그 대가로 섬세한 이성이 마비되고 내면의 분노를 제어할 수 없게 되었다. 그들은 최초의 **'오크(Orc)'**가 되었다.

세 개의 의식이 정점에 달했을 때, '생명의 근원'은 그 힘을 견디지 못하고 세 조각으로 갈라졌다. 아니마 문디는 비명을 질렀고, 하늘은 핏빛으로 물들었다. 그때 의식의 제단 위로 흘렀던 세 아르카인의 피는 대지 깊숙한 곳으로 스며들어, **분열과 증오의 저주**가 되어 잠들었다. 이것이 바로 훗날 '피의 안개'가 되는 근원이었다.

---

### **제 4장: 새로운 시대, 첫 번째 전쟁**

의식이 끝나고, 아르카인은 사라졌다. 그 자리에는 서로를 이해할 수 없는 세 개의 새로운 종족만이 남았다.

*   **인간**들은 마력을 잃은 자신들의 나약함에 절망했고, 엘프들의 오만함과 오크들의 야만성을 두려워했다.
*   **엘프**들은 속세의 존재가 된 인간들을 경멸했고, 숲을 위협하는 오크들을 증오했다.
*   **오크**들은 나약한 모든 것을 경멸했고, 본능적으로 다른 두 종족을 공격했다.

그들은 서로를 '형제'가 아닌 '타자'로 인식했다. 그리고 알테아 역사상 최초의 전쟁, **'분열 전쟁'**이 발발했다. 한때 형제였던 이들은 서로의 피를 흘리며 대륙을 세 개의 영역으로 나누었다.

전쟁이 소강상태에 이르렀을 때, 그들은 자신들이 잊고 있던 가장 큰 위협, '원초의 혼돈'이 그들의 전쟁을 자양분 삼아 더욱 강해졌음을 깨달았다. 세 종족은 마지못해 손을 잡고, 아니마 문디의 가장 단단한 가지를 꺾어 만든 감옥, 최초의 **'생명의 쐐기(Vita Spiculum)'**를 사용하여 원초의 혼돈을 시공간의 틈새에 봉인하는 데 성공했다.

하지만 승리의 대가는 너무나 컸다. 대륙은 상처 입었고, 세 종족의 마음에는 서로에 대한 불신과 증오만이 깊게 새겨졌다.

---

### **에필로그: 천 년의 저주**

시간은 흘러 천 년이 지났다.

*   **인간**들은 대륙 전역으로 퍼져나가 가장 번성한 종족이 되었다.
*   **엘프**들은 깊은 숲속에 은둔하며 세상과 단절했다.
*   **오크**들은 오랜 세월 동안 이성을 되찾고 다른 동물들과 융합하며 **'수인(Beastkin)'**이라는 다양한 분파로 진화했다. 펜리르의 늑대 부족이나 유리아의 야만 부족처럼, 그들은 여전히 내면의 야성과 싸우고 있었다.

그들은 모두 자신들의 기원, 즉 모두가 '아르카인'이라는 하나의 뿌리에서 나왔다는 사실을 잊어버렸다.

그리고 10년 전, 마왕 아자젤(원초의 혼돈이 남긴 사악한 의지의 파편)이 나타나자, 대마법사 **크로노스**는 고대 문헌에서 '생명의 쐐기'에 대한 기록을 발견하고 그것을 재현하여 마왕을 봉인했다.

하지만 약해진 봉인의 틈새로 새어 나온 혼돈의 마력은, 천 년 동안 대지 아래 잠들어 있던 **'분열과 증오의 저주'**를 깨웠다. 아르카인들이 흘렸던 슬픔의 피가 **'피의 안개'**가 되어 지상으로 피어오르기 시작한 것이다.

이제, 천 년 전 시작된 비극의 마지막 장을 끝내고, 갈라진 세상을 다시 하나로 묶을 새로운 영웅, **카인 레오하트**의 이야기가 시작될 차례였다. 그는 자신도 모르는 사이, 세 종족의 힘과 가능성을 모두 품고 태어난, 천 년의 예언이 기다려온 '통합의 열쇠'였던 것이다.


================================================================================

물론입니다. 1000년 전 이야기, **제0장: 하나였던 시절**의 대화 대본을 최대한 길고 상세하게, 각 인물의 성격과 세계관이 드러나도록 구성해 드리겠습니다.

---
---

### **📜 아르카디아 전기: 1000년 전 이야기**

## **원초의 분열 (The Primordial Schism)**

### **제 0장: 하나였던 시절 (The Age of Oneness)**

**[SCENE START]**

**[1-1. 세계수의 심장]**

**장소:** 아니마 문디(Anima Mundi)의 거대한 줄기 아래, '빛의 정원'. 공기 중에는 마력의 입자가 별처럼 흩날리고, 수정처럼 맑은 시냇물이 노래하듯 흐른다.
**등장인물:** 리산데르(Lysander), 엘라라(Elara), 보를라그(Vorlag), 그리고 수십 명의 다른 아르카인(Archaine).

**(나레이션 - 고대의 목소리)**
태초에, 시간은 강물처럼 흐르지 않고 고요한 호수와 같았다. 우리는 모두 하나의 이름, '아르카인'으로 불렸고, 이 세상 '알테아'는 우리의 정원이었다. 세계의 영혼, 아니마 문디의 숨결 속에서 우리는 영원을 살았다. 슬픔도, 죽음도, 상실도 없는... 완벽한 조화. 그것이 우리가 알던 세상의 전부였다.

**(수많은 아르카인들이 정원 곳곳에서 평화로운 일상을 보내고 있다. 어떤 이들은 손짓만으로 빛의 조각상을 빚어내고, 다른 이들은 흐르는 물소리에 맞춰 명상에 잠겨 있다. 한쪽에서는 두 아르카인이 에너지로 이루어진 검을 맞대며, 춤을 추듯 우아한 대련을 펼치고 있다.)**

**(언덕 위, 세 명의 지도자가 이 광경을 내려다보고 있다. 그들의 존재만으로도 주변의 빛이 더 밝게 빛나는 듯하다.)**

**보를라그:** (가슴을 활짝 펴고 깊게 숨을 들이마시며, 우렁찬 목소리로)
"하하하! 아니마 문디의 정기가 오늘도 넘실대는구나! 이 기운을 받을 때마다 내 온몸의 근육이 환희의 노래를 부르는 것 같단 말이지! 완벽한 힘이야말로 완벽한 평화를 지키는 법이지!"

**엘라라:** (두 손을 가슴 앞에 모은 채, 부드럽지만 단호한 목소리로)
"보를라그, 당신이 느끼는 것은 힘이 아니라 '조화'랍니다. 아니마 문디의 위대한 노래는 근육이 아닌 영혼으로 들어야 그 진정한 아름다움을 알 수 있는 법. 보세요. 저 모든 생명이 각자의 자리에서 완벽한 화음을 이루고 있잖아요. 이것이 우리가 지켜야 할 순수함의 정수입니다."

**리산데르:** (두 사람을 보며 온화한 미소를 짓는다. 그의 눈빛은 호수처럼 깊고 차분하다.)
"두 사람의 말이 모두 옳네. 강인한 육체는 조화로운 영혼을 담는 훌륭한 그릇이 되고, 조화로운 영혼은 강인한 육체가 나아갈 길을 비추지. 순수함과 힘, 어느 한쪽으로 치우치지 않는 이 완벽한 균형. 그것이야말로 우리 아르카인의 위대함일세."

**보를라그:**
"흥, 자네는 항상 그렇게 어려운 말만 하는군, 리산데르. 간단한 게 최고야! 강하면 모든 게 해결된다!"

**엘라라:**
"아니요, 보를라그. 강함만으로는 부러질 뿐입니다. 물처럼 부드럽게 모든 것을 감싸 안는 순수함이야말로 진정으로 강한 것이지요."

**리산데르:**
"그래서 우리가 함께 있는 것 아니겠나. 검과 방패, 그리고 그 모든 것을 꿰뚫어 보는 지혜. 우리 셋이 함께하기에 아르카인은 완벽한 것이지."

**(리산데르의 말에 보를라그는 만족스럽게 고개를 끄덕이고, 엘라라는 평온한 미소를 짓는다. 그들의 발아래, 영원할 것 같은 평화가 끝없이 펼쳐져 있다.)**

**[1-2. 완벽함에 대한 질문]**

**장소:** '지혜의 전당', 아니마 문디의 거대한 가지가 기둥을 이루는 도서관.

**(세 지도자가 원탁에 앉아 고대의 룬 문자가 새겨진 석판을 연구하고 있다. 젊은 수련사 리렌이 조심스럽게 차를 들고 다가온다.)**

**리렌:**
"리산데르 님, 엘라라 님, 보를라그 님. 아니마 문디의 이슬로 끓인 차입니다."

**엘라라:**
"고맙습니다, 리렌. 당신의 마음에서도 맑은 빛이 느껴지는군요. 수련은 잘 되어가나요?"

**리렌:**
"네, 엘라라 님. 아니마 문디의 흐름을 느끼고, 보를라그 님의 가르침으로 몸을 단련하고, 리산데르 님의 지혜를 배우고 있습니다. 하지만... 한 가지 의문이 있습니다."

**보를라그:**
"의문? 이 완벽한 세상에 무슨 의문이 있단 말이냐?"

**리렌:** (용기를 내어)
"저희는... 어떻게 이 완벽함을 영원히 지킬 수 있습니까? 만약, 우리가 상상조차 할 수 없는 무언가가 이 조화를 깨뜨리려 한다면, 저희는 어떻게 해야 합니까?"

**(리렌의 순수한 질문에 보를라그는 코웃음을 친다.)**

**보를라그:**
"하! 쓸데없는 걱정이구나, 애송이! 이 조화를 깨뜨릴 존재가 나타난다면, 내가 이 도끼로 뼛가루로 만들어주면 그만이다! 우리의 힘 앞에서 감히 누가 대적하겠느냐!"

**엘라라:**
"리렌, 두려워할 필요 없어요. 아니마 문디의 순수한 빛이 우리를 보호하는 한, 어떤 어둠도 감히 이 땅을 침범할 수 없습니다. 우리의 믿음이 굳건하다면, 우리의 평화는 영원할 거예요. 그것이 아니마 문디의 뜻이자 약속입니다."

**(두 사람의 대답을 들은 리렌은 안도하는 표정을 짓는다. 하지만 리산데르는 조용히 석판을 바라보며 나지막이 입을 연다.)**

**리산데르:**
"...흥미로운 질문이군, 리렌. 하지만 그 질문 자체가 잘못되었을 수도 있네."

**엘라라:**
"리산데르? 무슨 의미죠?"

**리산데르:**
"우리는 '지킨다'는 것에만 집중하고 있네. 하지만 멈춰있는 호수는 언젠가 썩기 마련이지. 진정한 생명이란 흐르는 강물처럼 끊임없이 변화하고, 새로운 환경에 적응하며 나아가는 것이 아닐까?"

**보를라그:**
"변화? 적응? 지금 이 완벽함에 만족하지 못한다는 건가? 배부른 소리로 들리는군!"

**리산데르:**
"만족하지 못하는 것이 아닐세. 단지... 나는 생각하는 것뿐이야. 우리가 한 번도 겪어보지 못한 '위기'가 닥쳤을 때, 과연 우리의 이 '완벽함'이 우리를 지켜줄 것인가. 아니면... 변화를 거부한 대가로 우리를 부서지게 만들 것인가."

**(리산데르의 말에 회의장에는 어색한 침묵이 흐른다. 엘라라는 그의 말에서 불경한 기운을 느낀 듯 미간을 찌푸리고, 보를라그는 이해할 수 없다는 표정을 짓는다. 리산데르는 더 말하지 않고, 다시 깊은 생각에 잠긴다. 그의 마음속에는 다른 이들이 보지 못하는, 너무나 먼 미래에 대한 막연한 불안감이 싹트고 있었다.)**

**[1-3. 보이지 않는 균열]**

**장소:** '월광의 제단', 아니마 문디의 가장 높은 가지에 위치한 제단.

**(보름달이 뜨는 밤, 세 지도자와 장로들은 '조화의 의식'을 거행하고 있다. 엘라라가 중심에서 기도문을 외우자, 달빛과 아니마 문디의 빛이 공명하며 눈부신 오로라를 만들어낸다.)**

**엘라라:**
"위대한 아니마 문디여, 당신의 자녀들이 여기 있나이다. 우리의 영혼을 당신의 빛으로 채우시고, 이 땅에 영원한 조화와 평온을 내려주소서..."

**(아름다운 빛의 향연이 펼쳐지던 그 순간. 아무도 인지하지 못할 만큼 찰나의 순간 동안, 모든 빛이 미세하게 흔들린다. 마치 완벽한 화음 속에 불협화음이 아주 작게 스쳐 지나간 것처럼.)**

**(대부분의 아르카인들은 아무것도 느끼지 못했다. 하지만...)**

**보를라그:** (고개를 갸웃하며)
"음? 방금... 땅이 살짝 울린 것 같지 않았나?"

**엘라라:** (얼굴이 창백해지며)
"...아니요. 땅이 아니에요. 아니마 문디가... 아니마 문디의 영혼이... 아주 잠시, 떨었어요. 마치... 먼 곳에서 찾아올 차가운 겨울을 예감한 것처럼."

**리산데르:** (심각한 표정으로 하늘을 올려다본다)
"겨울이라... 한 번도 겪어보지 못한 계절이군. 엘라라, 정말... 무언가 느낀 건가?"

**엘라라:**
"네. 아주 희미했지만... 분명해요. 깊은 슬픔과... 태어난 적 없는 고통의 메아리였습니다."

**(엘라라의 말에 보를라그는 대수롭지 않다는 듯 어깨를 으쓱하지만, 리산데르의 표정은 점점 더 굳어간다. 그는 아까 자신이 가졌던 불안감이 단순한 기우가 아님을 직감한다.)**

**리산데르:** (혼잣말로)
"...무엇이, 감히 이 세계의 영혼을 떨게 만들 수 있단 말인가."

**(의식은 다시 평온하게 계속되지만, 보이지 않는 균열은 이미 시작되었다. 영원할 것 같던 아르카인의 꿈은, 이제 서서히 그 끝을 향해 나아가고 있었다.)**

**[SCENE END]**

================================================================================

물론입니다. 1000년 전 이야기, **제1장: 검은 눈물의 낙하**의 대화 대본을 최대한 길고 상세하게, 각 인물의 성격과 다가올 비극의 무게가 느껴지도록 구성해 드리겠습니다.

---
---

### **📜 아르카디아 전기: 1000년 전 이야기**

## **원초의 분열 (The Primordial Schism)**

### **제 1장: 검은 눈물의 낙하 (The Obsidian Tear's Fall)**

**[SCENE START]**

**[1-1. 마지막 평온]**

**장소:** '별빛 첨탑', 아르카인들의 수도 중심부. 하늘에 닿을 듯한 수정 탑의 최상층 회의실.
**등장인물:** 리산데르, 엘라라, 보를라그.

**(창밖으로는 수많은 아르카인들이 마력으로 빛나는 다리를 건너고, 공중 정원에서 담소를 나누는 등 평화로운 풍경이 펼쳐진다. 하지만 회의실의 공기는 미묘하게 무겁다.)**

**엘라라:** (창밖을 보며, 여전히 어젯밤의 불안감을 떨치지 못한 채)
"...어젯밤 조화의 의식 이후, 아니마 문디의 맥동이 여전히 미세하게 흔들리고 있습니다. 마치... 깊은 잠에 든 아이가 악몽을 꾸는 것처럼요. 이런 적은 한 번도 없었습니다."

**보를라그:** (자신의 거대한 도끼 '세계파괴자(World-Breaker)'를 닦으며)
"엘라라, 너무 예민한 것 아닌가? 난 아무것도 느끼지 못했다. 그저 평소보다 정기가 좀 더 맑게 느껴질 뿐. 자네의 영혼이 너무 섬세해서 바람 한 점에도 흔들리는 것이겠지. 진정한 안정은 흔들리지 않는 힘에서 오는 법이다."

**리산데르:** (테이블 위의 별자리 지도를 물끄러미 보며)
"아니, 보를라그. 나 역시 느꼈네. 힘이 아니라, 이 세상의 '법칙' 그 자체가 아주 미세하게 뒤틀리는 듯한 감각이었지. 마치 완벽하게 짜인 직물의 씨실 하나가 빠져나가려는 듯한... 불길한 예감이야."

**보를라그:** (도끼를 내려놓고, 진지한 얼굴로)
"자네까지 그런 소리를 하나, 리산데르? 이 알테아에 감히 우리의 법칙을 뒤틀 존재가 있단 말인가? 있다면, 그 존재는 내 도끼 아래에서 새로운 법칙을 배우게 될 것이다."

**엘라라:**
"이것은 힘으로 해결할 문제가 아닙니다. 영적인 오염, 혹은 우리가 알지 못하는 슬픔이 세상을 향해 다가오고 있는 건지도 모릅니다. 오늘 밤, 제가 다시 한번 아니마 문디와 깊은 교감을 시도해보겠습니다."

**리산데르:**
"그래주게. 조심해서. 원인을 알 수 없는 현상일수록 신중해야 하네."

**(그때였다. 갑자기 방 안의 모든 수정들이 일제히 빛을 잃고, 도시 전체를 감싸던 아니마 문디의 은은한 노랫소리가 뚝 끊긴다. 완벽한 정적. 세 사람은 본능적으로 서로를 바라본다.)**

**보를라그:**
"이... 이건...!"

**(세 사람이 창가로 달려간다. 도시의 모든 아르카인들도 하던 일을 멈추고 불안하게 하늘을 올려다보고 있었다. 그리고 그들은 보았다.)**

**(하늘의 정중앙, 별도 달도 없어야 할 그 자리가 마치 검은 비단에 난 구멍처럼, 완벽한 '무(無)'의 공간으로 변해있었다. 그리고 그 구멍에서, 끈적한 어둠의 액체 같은 결정체, '옵시디언의 눈물'이 소리 없이 흘러내리기 시작한다.)**

**엘라라:** (창백해진 얼굴로 속삭인다)
"...하늘이... 하늘이 울고 있어..."

**리산데르:** (이성적으로 상황을 파악하려 애쓰며, 목소리가 떨린다)
"아니... 저건 눈물이 아니다. 저것은... 모든 것을 집어삼키는 공허 그 자체다!"

**(옵시디언의 눈물은 유성처럼 대지를 향해 낙하하고, 대륙의 가장 척박한 땅, '재의 황무지'에 소리 없이 내려앉는다. 충격파도, 폭발도 없었다. 하지만 그 순간, 대륙 전체의 모든 아르카인들이 동시에 가슴을 부여잡고 비명을 질렀다. 그들의 영혼 깊은 곳에, 생전 처음으로 '공허'와 '상실'이라는 감정이 칼날처럼 박혀 들어온 것이다.)**

---

**[1-2. 첫 번째 원정, 마지막 평화]**

**장소:** 별빛 첨탑의 출정 광장.

**(사건 발생 며칠 후, 세 가문의 정예병 수백 명이 집결해 있다. 그들의 얼굴에는 불안감과 함께, 미지의 존재에 대한 결연한 의지가 서려 있다.)**

**리산데르:** (단상에 서서)
"동포들이여! 우리의 세상에 전례 없는 이변이 발생했다. 미지의 존재가 우리의 대지를 더럽혔고, 아니마 문디의 노래를 고통으로 물들였다. 우리는 두려워하지 않는다! 우리는 원인을 규명하고, 이 세상의 조화를 되찾을 것이다! 이것은 정복이 아닌, 치유를 위한 원정이다!"

**엘라라:**
"아니마 문디의 빛이 우리와 함께할 것입니다! 우리의 순수한 의지로 저 더러움을 정화하고, 상처 입은 대지를 다시 축복합시다!"

**보를라그:**
"그리고 만약! 그 더러움의 근원이 감히 우리에게 이빨을 드러낸다면! 우리의 힘으로 그놈들의 뼈와 살을 분리시켜, 이 세상에 존재했던 흔적조차 지워버릴 것이다! 아르카인의 영광을 위하여!"

**아르카인들:**
"아르카인의 영광을 위하여!!!"

**(젊은 수련사 리렌이 결의에 찬 얼굴로 리산데르에게 다가온다.)**

**리렌:**
"리산데르 님, 저 또한 이 원정에 함께하게 해주십시오. 저의 지식과 힘으로 반드시 도움이 되겠습니다."

**리산데르:** (리렌의 어깨를 두드리며)
"좋다, 리렌. 너의 그 용기가 바로 우리의 힘이다. 하지만 명심하거라. 우리는 미지의 존재를 상대하는 것이다. 항상 경계를 늦추지 마라."

**(장대한 원정대가 재의 황무지를 향해 출발한다. 이것은 아르카인 역사상 최초의 '원정'이었으며, 그들이 온전한 모습으로 함께하는 마지막 행군이었다.)**

---

**[1-3. 재의 황무지, 죽음의 탄생]**

**장소:** 재의 황무지 경계.

**(원정대가 도착한 황무지는 지옥도 그 자체였다. 땅은 검게 썩어 문드러져 있었고, 역한 시취가 코를 찔렀다. 바위는 기괴한 형태로 녹아내리고 있었으며, 한때 숲이었을 곳은 앙상한 뼈처럼 변한 나무들만 남아있었다.)**

**엘라라:** (손으로 입을 막으며, 그녀의 눈에서 눈물이 흐른다)
"세상에... 아니마 문디의 숨결이... 완전히 끊겼어요. 여긴... 여긴 생명이 없는 땅이에요. 아니, 생명의 부정이 가득한 땅이야..."

**(그때, 뼈 무더기 속에서 무언가 기어 나온다. 늑대의 형상을 하고 있지만, 온몸의 가죽은 벗겨져 뼈가 드러나 있고, 눈은 핏빛 증오로 불타고 있었다. '몬스터'였다.)**

**보를라그:** (역겨움에 침을 뱉으며)
"저 흉물스러운 것들은 대체 뭐냐! 살아있는 모든 것에 대한 모욕이다!"

**리산데르:** (마력 감지 수정을 꺼내 들지만, 수정이 검게 변하며 금이 간다)
"분석 불가! 저것들은 마력으로 이루어진 존재가 아니다! 생명력 그 자체가 뒤틀리고 오염된 결과물이다! 모두 물러서! 저것들에게서 나오는 기운에 닿지 마라!"

**(하지만 너무 늦었다. 수십, 수백의 몬스터들이 땅속에서, 바위틈에서 기어 나와 원정대를 향해 달려들었다.)**

---

**[1-4. 무력한 완전함]**

**장소:** 황무지에서의 첫 전투.

**(전투는 혼돈 그 자체였다. 아르카인들이 발사한 순수한 빛의 마법들은 몬스터의 검은 기운에 닿자마자 힘을 잃고 소멸했다.)**

**아르카인 병사 1:**
"마법이 통하지 않아! 우리의 빛이... 먹히고 있다!"

**(보를라그가 최전선에서 도끼를 휘둘러 몬스터들을 베어 넘기지만, 몬스터의 검은 피가 닿은 그의 갑옷이 '치이익' 소리를 내며 부식되기 시작한다.)**

**보를라그:**
"크윽! 이 저주받은 피는 뭐냐! 내 갑옷을 녹이고 있어!"

**(그 순간, 가장 끔찍한 일이 벌어진다. 리렌이 동료를 지키기 위해 거대한 방어막을 펼치지만, 몬스터 한 마리가 방어막을 뚫고 그의 가슴을 할퀸다.)**

**리렌:**
"크헉...!"

**(리렌의 가슴에 붉은 상처가 생기고, 피가 흘러나온다. 아르카인들은 경악한다. 상처도, 피도, 그들이 한 번도 본 적 없는 것이었다.)**

**엘라라:** (리렌에게 달려가며)
"리렌! 정신 차려요! 제가 치유해 주겠습니다! 빛의 가호여!"

**(엘라라가 눈부신 치유의 빛을 리렌의 상처에 쏟아붓는다. 하지만 기적은 일어나지 않았다. 오히려 상처는 치유의 빛을 게걸스럽게 빨아들이며 더욱 검게 썩어 들어갔다.)**

**리렌:** (고통에 몸부림치며, 생전 처음 느끼는 감각에 울부짖는다)
"아... 아파... 이... 이 감각은... 뭐지...? 너무... 괴로워...!"

**엘라라:**
"안돼... 어째서... 어째서 내 빛이 당신을 더 고통스럽게 하는 거죠...!"

**(리렌은 몇 번의 경련을 일으키더니, 이내 온몸이 검은 먼지가 되어 바람에 흩날려 사라진다. '죽음'. 아르카인들은 자신들의 동료가 눈앞에서 '무(無)'로 돌아가는 것을 망연히 지켜볼 뿐이었다.)**

**리산데르:** (얼어붙은 표정으로)
"...소멸했다. 치유도, 부활도 불가능한... 완전한 소멸..."

**보를라그:** (리렌이 사라진 자리를 보며, 눈물이 분노와 뒤섞여 터져 나온다)
"리레ეეეეეეე엔!!! 이 개자식들!!!! 내가 네놈들을 전부!!!!"

**(보를라그가 이성을 잃고 날뛰기 시작한다. 하지만 몬스터의 수는 줄지 않았고, 아르카인들의 희생만 늘어갈 뿐이었다. 완전했던 그들의 세계, 그들의 힘은 '혼돈'이라는 새로운 법칙 앞에서 처참하게 무너져 내리고 있었다.)**

**리산데르:** (피를 토하는 심정으로 외친다)
"후퇴하라! 전원 후퇴하라! 여긴 우리가 이길 수 있는 곳이 아니다! 당장 여길 벗어나야 한다!"

**(리산데르의 외침에, 아르카인들은 처음으로 '패배'라는 것을 인정하며, 동료들의 시신조차 수습하지 못한 채 필사적으로 도망치기 시작했다.)**

**(황무지에 남은 것은 몬스터들의 기괴한 울음소리와, 바람에 흩날리는 아르카인들의 검은 먼지뿐이었다. 그날, 그들은 영원한 평화를 잃었다. 그리고 그 빈자리에, '공포'라는 새로운 감정을 배웠다.)**

**[SCENE END]**

================================================================================

물론입니다. 1000년 전 이야기, **제2장: 세 갈래의 길**의 대화 대본을 최대한 길고 상세하게, 각 인물의 신념과 고뇌, 그리고 아르카인 사회가 돌이킬 수 없이 분열되는 과정을 극적으로 구성해 드리겠습니다.

---
---

### **📜 아르카디아 전기: 1000년 전 이야기**

## **원초의 분열 (The Primordial Schism)**

### **제 2장: 세 갈래의 길 (The Three Paths)**

**[SCENE START]**

**[2-1. 패배의 귀환, 침묵의 회랑]**

**장소:** 아니마 문디의 심장부, '영원의 회랑'. 세 가문의 문장이 새겨진 거대한 원탁이 있다. 평소라면 영롱한 빛으로 가득했을 회랑은, 지금은 반쯤 빛을 잃고 침울한 기운에 잠겨 있다.
**등장인물:** 리산데르, 엘라라, 보를라그, 그리고 각 가문의 장로들과 살아남은 정예병들.

**(원정에서 돌아온 생존자들은 망연자실한 표정으로 회랑 곳곳에 주저앉아 있다. 그들의 수정 갑옷은 부식되고 금이 갔으며, 몇몇은 동료를 잃은 슬픔에 조용히 눈물만 흘리고 있다. 아르카인 역사상 처음으로 '패배'와 '상실'의 무게가 회랑 전체를 짓누른다.)**

**(리산데르가 무거운 발걸음으로 원탁의 자기 자리에 선다. 그의 얼굴에는 지혜 대신 깊은 고뇌가 서려 있다.)**

**리산데르:**
"모두... 들으시오. 오늘, 우리는 동포를 잃었고, 신념을 잃었으며, 우리의 완전함이 영원하지 않다는 것을 배웠소. 이것은 단순한 패배가 아니오. 우리 존재의 근간을 뒤흔드는... 첫 번째 시련이오."

**(보를라그가 주먹으로 벽을 '쿵' 소리 나게 내리친다. 그의 눈은 핏발이 서 있다.)**

**보를라그:**
"시련? 리산데르, 저것은 시련 따위가 아니다! 학살이다! 모욕이다! 리렌은... 그 젊은 아이는 눈앞에서 먼지가 되었어! 우리의 힘, 우리의 긍지, 그 모든 것이 저주받은 황무지에서 짓밟혔단 말이다!"

**엘라라:** (창백한 얼굴로, 떨리는 목소리지만 단호하게)
"그만하세요, 보를라그. 당신의 분노는 죽은 이들을 돌아오게 하지 못합니다. 오히려... 저 혼돈을 더 기쁘게 할 뿐. 지금 우리에게 필요한 것은 분노가 아니라... 해답입니다. 이 오염을 어떻게 막고, 우리의 순수함을 어떻게 지켜낼 것인지에 대한..."

**(엘라라의 말에 회랑은 더욱 깊은 침묵에 잠긴다. '해답'. 그들에게는 한 번도 필요했던 적이 없는 단어였다.)**

---

**[2-2. 첫 번째 제안: 거부의 길]**

**(엘라라가 원탁의 중심으로 걸어 나온다. 그녀는 아니마 문디의 희미한 빛에 손을 얹고, 모두를 향해 말한다.)**

**엘라라:**
"동포들이여, 우리는 보았습니다. 저 혼돈은 외부의 적입니다. 세상의 법칙을 거스르는 독입니다. 독을 치료하는 방법은 단 하나, 몸에서 완전히 도려내거나, 독이 닿지 않는 곳으로 피하는 것입니다."

**(그녀는 회랑에 모인 모두의 눈을 마주치며, 결연하게 제안한다.)**

**엘라라:**
"우리는 아니마 문디의 가장 깊은 곳, 태초의 빛이 잠들어 있는 '생명의 근원'으로 들어가야 합니다. 그리고 우리 자신을 세계수와 완전히 결속시키는 겁니다. 우리의 영혼을 세계수의 일부로 만들어, 이 위대한 나무의 힘으로 영원한 결계를 펼치는 것입니다. 외부의 모든 오염과 혼돈으로부터 우리를 완벽하게 격리하고, 우리의 순수함을 보존하는 것. 그것만이 우리가 살아남을 유일한 길입니다!"

**한 엘라라 가문의 장로:**
"대사제님의 말씀이 옳습니다! 저 더러운 것들과 섞일 바에야, 차라리 위대한 어머니의 품 안에서 영원한 순수함을 지키겠습니다!"

**(엘라라의 주장에 많은 이들이 고개를 끄덕인다. 공포에 질린 그들에게 '완벽한 격리'는 가장 달콤하고 확실한 구원의 약속처럼 들렸다.)**

---

**[2-3. 두 번째 제안: 투쟁의 길]**

**(그때, 보를라그가 거칠게 끼어든다.)**

**보를라그:**
"격리? 결계? 숨어서 떨자는 말을 길게도 하는군, 엘라라! 그건 생존이 아니라 겁쟁이들의 자기기만일 뿐이다! 네가 말하는 결계 안에서 기도나 하는 동안, 저 혼돈은 밖에서 대지를 전부 집어삼키고 더욱 강해질 것이다! 결국엔 우리를 가둔 결계가 우리의 무덤이 될 거란 말이다!"

**(보를라그는 자신의 도끼를 높이 치켜들며 포효한다.)**

**보를라그:**
"길은 단 하나! 싸우는 것이다! 우리의 힘이 부족했다면, 더 강한 힘을 손에 넣으면 된다! 우리의 마법이 통하지 않았다면, 마법을 초월하는 육체를 만들면 된다! 나는 제안한다! 아니마 문디의 제어되지 않은 힘, 저 깊은 곳에 잠들어 있는 '야성'의 힘을 해방시켜 우리 몸에 주입하는 것이다! 우리의 육신을 살아있는 병기로, 강철보다 단단하고 폭풍보다 빠르게 만드는 것이다! 그리하여 저 혼돈을 힘으로! 오직 압도적인 힘으로 찍어 누르는 것이다! 투쟁만이 우리의 긍지를 되찾을 유일한 길이다!"

**한 보를라그 가문의 전사:**
"그렇다! 도망치지 않는다! 싸워서 되찾는다! 보를라그 님을 따르겠다!"

**(보를라그의 선동적인 연설에, 분노와 복수심에 불타던 아르카인들이 열광적으로 외치기 시작한다. 회랑은 순식간에 두 개의 파벌로 나뉘어 서로를 적대적으로 바라본다.)**

---

**[2-4. 세 번째 제안: 순응의 길]**

**(리산데르는 이 모든 광경을 침통하게 지켜보다가, 조용히 입을 연다. 그의 목소리는 나지막했지만, 회랑의 모든 소음을 잠재울 만큼의 무게를 담고 있었다.)**

**리산데르:**
"모두 그만하시오. 당신들은... 아직도 우리가 '선택'할 수 있다고 믿는 겁니까?"

**(모두의 시선이 그에게 쏠린다.)**

**리산데르:**
"엘라라, 당신의 순수함은 존중하지만, 그것은 현실을 외면한 이상일 뿐이오. 스스로를 가두는 것은 결국 고립과 정체를 의미하오. 보를라그, 당신의 용기는 훌륭하지만, 그것은 어둠을 이해하지 못한 맹목적인 분노일 뿐이오. 더 큰 힘은 더 큰 파멸을 부를 뿐, 그것이 세상의 이치요."

**(리산데르는 원탁을 천천히 돌며, 모두에게 잔인한 진실을 말한다.)**

**리산데르:**
"우리는 인정해야 하오. 세상은... 변했소. 우리가 알던 완벽한 세상은 이미 죽었소. 혼돈은 이제 이 세상의 새로운 법칙이며, 우리는 그 법칙 아래에서 살아가야만 하오. 이것은 선택이 아니라, 운명이오."

**엘라라:** (믿을 수 없다는 듯)
"리산데르! 당신 지금... 무슨 말을 하는 겁니까! 혼돈을... 받아들이자는 겁니까?"

**리산데르:**
"그렇소. 받아들이고, 이해하고, 그 속에서 살아남는 법을 배우는 것이오. 그러기 위해서는... 우리 또한 변해야 하오. 우리의 이 '완벽함'을 포기해야만 하오."

**(그의 말에 회랑 전체가 술렁인다. '완벽함을 포기한다'는 말은 그들에게는 신성모독과도 같았다.)**

**리산데르:**
"나는 제안하오. 아니마 문디와의 마력 연결을 우리 스스로 끊어냅시다. 그 대가로 우리의 긴 수명과 강력한 마력을 잃게 되겠지요. 하지만 우리는 혼돈의 마력에 저항할 수 있는 새로운 육체를 얻고, 어떤 환경에도 적응할 수 있는 강인한 생명력을 얻게 될 것이오! 그것은 퇴보가 아니오. 이 새로운 세상에서 살아남기 위한... 필사적인 '진화'란 말이오!"

---

**[2-5. 돌이킬 수 없는 분열]**

**(리산데르의 제안은 격렬한 반발을 불러일으켰다.)**

**엘라라:** (눈물을 흘리며 절규한다)
"절대 안 됩니다! 그것은 우리 아르카인의 영혼을 죽이는 행위입니다! 아니마 문디의 축복을 스스로 버리는 것은 가장 끔찍한 배신이에요! 나는 결코, 나의 동포들이 마력 없는 불완전한 존재가 되는 것을 볼 수 없습니다!"

**보를라그:** (경멸에 찬 눈으로)
"결국 네놈의 길은 가장 비겁한 길이였군, 리산데르! 힘을 포기하고 적에게 꼬리를 내리자는 것과 뭐가 다른가! 나는 그런 나약한 존재가 되느니, 차라리 저주받은 황무지에서 싸우다 죽겠다!"

**리산데르:** (지친 목소리로, 하지만 단호하게)
"살아남지 못하면, 긍지도 순수함도 아무 의미가 없소! 당신들은 과거의 영광이라는 꿈속에서 허우적대고 있을 뿐이야! 나는... 나는 내 동포들을 살릴 것이오. 어떤 대가를 치르더라도!"

**(더 이상의 대화는 무의미했다. 세 지도자는 서로에게 완전히 등을 돌렸다. 그들의 뒤로, 각자의 신념을 따르는 아르카인들이 모여들었다.)**

**엘라라:**
"우리의 길을 따르는 자들이여, 나를 따라 아니마 문디의 심장으로 갑시다. 그곳에서 영원한 순수함을 지킬 것입니다."

**보를라그:**
"진정한 힘을 갈망하는 자들이여, 나를 따르라! 우리는 피와 투쟁으로 우리의 길을 개척할 것이다!"

**리산데르:**
"그리고... 그럼에도 불구하고 살아남기를 택하는 자들이여. 나에게 오시오. 우리는 고통스럽고 험난하겠지만, 새로운 시대의 첫걸음을 내디딜 것입니다."

**(한때는 하나의 이름으로 불렸던 형제들이, 이제는 세 개의 다른 깃발 아래, 서로를 의심과 적의의 눈으로 바라보고 있었다. 영원의 회랑은 차갑게 식어버렸고, 아니마 문디는 슬픔에 잠겨 빛을 거두었다.)**

**(나레이션 - 고대의 목소리)**
그날, 우리는 형제를 잃었다. 그리고 우리 자신을 잃었다. 하나였던 세계는 세 개로 찢어졌고, 그 상처는 천 년이 지나도록 아물지 않았다. '하나였던 시절'은 끝났다. 그리고 '분열의 시대'가 시작되었다.

**[SCENE END]**

================================================================================

물론입니다. 1000년 전 이야기의 가장 비극적이고 결정적인 순간, **제3장: 대분열의 의식**의 대화 대본을 최대한 길고 상세하게, 각 인물의 마지막 고뇌와 결단, 그리고 세상이 찢어지는 순간의 장엄함을 담아 구성해 드리겠습니다.

---
---

### **📜 아르카디아 전기: 1000년 전 이야기**

## **원초의 분열 (The Primordial Schism)**

### **제 3장: 대분열의 의식 (The Ritual of Schism)**

**[SCENE START]**

**[3-1. 생명의 근원, 마지막 조우]**

**장소:** 아니마 문디의 가장 깊은 성소. 세상의 모든 영맥이 모여드는 거대한 공동(空洞). 중앙에는 거대하고, 살아있는 심장처럼 박동하는 순수한 빛의 결정체, **'생명의 근원(The Core of Life)'**이 부유하며 성소 전체를 밝히고 있다.
**등장인물:** 리산데르, 엘라라, 보를라그, 그리고 그들을 따르는 수백의 아르카인들.

**(공동은 세 방향으로 갈라져 있고, 각 입구 앞에는 고대의 룬이 새겨진 제단이 놓여 있다. 하나는 이성을 상징하는 기하학적 문양, 다른 하나는 자연을 상징하는 덩굴 문양, 마지막 하나는 힘을 상징하는 발톱 문양이 새겨져 있다.)**

**(세 파벌의 아르카인들은 각자의 지도자 뒤에, 서로에게 등을 돌린 채 서 있다. 그들 사이에는 돌이킬 수 없는 침묵의 강이 흐른다. 한때 형제였던 그들은 이제 서로의 얼굴조차 보지 않는다. '생명의 근원'이 내뿜는 성스러운 빛이 그들의 비극적인 결의를 아이러니하게 비추고 있다.)**

**(리산데르가 가장 먼저 입을 연다. 그의 목소리는 슬픔으로 가득 차 있다.)**

**리산데르:**
"엘라라. 보를라그. 우리가 이 성소에 함께 서는 것은... 이것이 마지막이겠군."

**엘라라:** (눈을 감은 채, '생명의 근원'을 향해 기도하듯)
"저는 당신들과 서 있는 것이 아닙니다, 리산데르. 저는 아니마 문디의 곁에 서 있을 뿐. 당신들은 스스로 그 축복을 등졌습니다."

**보를라그:** (자신의 제단에 놓인, 뼈로 만든 거친 의식용 단검을 쥐며)
"마지막이라니 다행이군. 네놈들의 나약한 철학과 비겁한 도피를 더는 듣지 않아도 될 테니. 오늘, 우리는 썩은 살을 도려내고 진정한 전사로 다시 태어날 것이다."

**리산데르:** (고개를 저으며, 엘라라와 보를라그를 차례로 본다. 그의 눈에는 분노가 아닌 깊은 연민이 담겨 있다.)
"정말... 아무것도 기억나지 않는가? 엘라라, 우리가 이 빛 아래에서 처음으로 마법을 배웠던 날을. 보를라그, 우리가 저 세계수의 꼭대기까지 함께 오르며 서로의 등을 밀어주었던 날을. 우리가 '하나'였던 그 모든 시간을... 이 의식과 함께 전부 버릴 셈인가?"

**엘라라:** (눈을 뜬다. 그녀의 눈에는 눈물이 고여 있지만, 의지는 흔들리지 않는다.)
"기억합니다. 그렇기에 더 고통스럽습니다. 하지만 그 기억은 이제 과거의 유산일 뿐. 순수함을 잃은 기억은 오염된 보석과 같습니다. 저는 그 보석을 지키기 위해, 기꺼이 과거의 저를 이 제단에 바칠 겁니다."

**보를라그:**
"기억? 그딴 감상적인 놀음은 패배자들이나 하는 것이다! 나는 미래를 본다! 혼돈을 찢어발기고, 그 피 위에서 포효하는 우리의 미래를! 과거는 그저 발을 묶는 족쇄일 뿐!"

**(리산데르는 더 이상 말을 잇지 못한다. 설득은 끝났다. 남은 것은 돌이킬 수 없는 실행뿐이다. 그는 비장하게 자신의 제단으로 향한다. 그가 쥐고 있는 것은 수정으로 만든, 지극히 논리적이고 차가운 형태의 단검이다.)**

---

**[3-2. 세 개의 맹세, 하나의 종말]**

**(세 지도자는 동시에 각자의 단검으로 자신의 손바닥을 깊게 긋는다. 완벽했던 아르카인의 육체에서 처음으로 스스로 낸 상처. 선홍색 피가 흘러나와 각자의 제단 위로 떨어진다.)**

**(그들은 피 묻은 손을 제단에 얹고, 서로 다른 금단의 주문을 외우기 시작한다. 세 개의 목소리가 불협화음처럼 성소에 울려 퍼진다.)**

**리산데르 (단절의 의식):**
"(낮고 결의에 찬 목소리로)
나는 맹세한다. 영원의 굴레를 끊고, 유한한 생명을 받아들이겠다.
나는 맹세한다. 마력의 축복을 버리고, 필멸의 지혜를 구하겠다.
아니마 문디여, 우리를 놓아주소서. 당신의 아이가 아닌, 대지의 아이로 다시 태어나게 하소서! **아나테마! (Anathema - 단절)**"

**엘라라 (결속의 의식):**
"(흐느끼지만 굳건한 목소리로)
저는 맹세합니다. 저의 모든 것을 바쳐, 당신의 순수함과 하나가 되겠습니다.
저는 맹세합니다. 세상의 더러움에 등을 돌리고, 당신의 숲에서 영원을 지키겠습니다.
아니마 문디여, 우리를 받아주소서. 당신의 심장이 되어, 영원히 당신 곁에 머물게 하소서! **산크투스! (Sanctus - 신성)**"

**보를라그 (야성의 의식):**
"(포효와 같은 목소리로)
나는 맹세한다! 이성의 족쇄를 부수고, 원초의 분노를 받아들이겠다!
나는 맹세한다! 나약한 마법을 버리고, 파괴의 본능을 내 것으로 삼겠다!
아니마 문디여, 우리에게 힘을 다오! 적을 물어뜯을 발톱과, 뼈를 부술 송곳니를! **임페라투스! (Imperatus - 지배)**"

---

**[3-3. 대분열, 새로운 탄생]**

**(세 개의 주문이 정점에 달하자, '생명의 근원'이 격렬하게 반응하기 시작한다. 세 종류의 이질적인 의지가 빛의 심장을 잡아 찢기 시작한다.)**

**(리산데르와 그의 추종자들의 몸에서 빛이 빠져나간다. 그들은 고통에 찬 비명을 지른다. 영원의 시간이 압축되며 찰나의 필멸자로 변하는 고통. 그들의 뾰족했던 귀는 둥글게 변하고, 투명하던 피부는 생기를 잃고 평범한 살갗이 된다. 그들은 최초의 '인간(Human)'이 되어, 처음으로 '추위'와 '약함'을 느끼며 몸을 떤다.)**

**리산데르의 추종자:**
"으... 으아아악! 마력이... 내 안의 빛이 사라지고 있어! 이 공허함은 뭐지...!"

**(엘라라와 그녀의 추종자들의 몸은 오히려 더욱 강렬한 빛에 휩싸인다. 그들의 몸은 더 가늘고 우아해지며, 머리카락은 달빛처럼, 눈은 숲의 이슬처럼 변한다. 하지만 그들의 발밑에서 빛의 덩굴이 자라나 발목을 감싼다. 아름다운 구속. 그들은 최초의 '엘프(Elf)'가 되어, 숲 밖으로 나갈 수 없는 운명을 받아들인다.)**

**엘라라의 추종자:**
"아아... 아니마 문디의 숨결이... 우리 자신이 되고 있어! 이 얼마나 순수한 기쁨인가... 하지만... 움직일 수가 없어..."

**(보를라그와 그의 추종자들에게는 가장 끔찍한 변화가 일어난다. 제단에 스며든 '야성'의 힘이 역류하며 그들의 몸을 찢고 재구성한다. 근육이 비정상적으로 부풀어 오르고, 피부는 거친 녹색으로 변하며, 턱에서는 거대한 송곳니가 돋아난다. 그들의 눈에서는 이성의 빛이 사라지고, 오직 붉은 분노만이 타오른다. 그들은 최초의 '오크(Orc)'가 되어, 고통과 분노가 뒤섞인 짐승의 포효를 내지른다.)**

**보를라그의 추종자:**
"크아아아아아! 힘이다! 힘이 넘쳐흐른다! 죽여라! 부숴라! 크아아악!"

---

**[3-4. 세계의 비명, 저주의 씨앗]**

**[SFX: 귀를 찢는 듯한 날카로운 파열음]**

**(결국, '생명의 근원'은 세 개의 상반된 힘을 견디지 못하고, 거대한 균열과 함께 세 조각으로 산산이 부서진다. 동시에, 아니마 문디 전체가 고통에 찬 비명을 지른다. 그것은 소리가 아닌, 세상 모든 존재의 영혼을 뒤흔드는 정신적인 충격파였다.)**

**(성소의 빛이 완전히 꺼지고, 부서진 '생명의 근원' 조각들만이 위태로운 핏빛 잔광을 내뿜는다. 성소 밖, 알테아의 하늘은 순식간에 핏빛으로 물든다.)**

**(그리고... 제단 위에 흘렀던 세 지도자의 피. 그 피는 부서진 근원의 핏빛 저주와 공명하며 검게 변하더니, 제단을 녹이고 대지의 가장 깊은 곳, 아니마 문디의 뿌리 속으로 스며든다. 분열의 슬픔, 서로에 대한 증오, 잃어버린 과거에 대한 원망이 담긴 그 피는, 천 년간 잠들어 있을 '분열과 증오의 저주'의 씨앗이 되어 깊은 어둠 속에 잠긴다.)**

**(모든 것이 끝났다. 어둠과 침묵 속에서, 세 개의 새로운 종족이 서로를 마주 본다.)**

**리산데르(이제는 인간):** (자신의 짧아진 수명과 사라진 마력을 느끼며, 엘프가 된 엘라라와 오크가 된 보를라그를 본다. 그의 눈에는 더 이상 형제를 보는 따스함이 없다. 오직 경계심과 슬픔뿐이다.)
"...이것이 우리가 선택한 결과인가."

**엘라라(이제는 엘프):** (속세의 존재가 된 인간들과 야만적인 짐승이 된 오크들을 보며, 차가운 경멸과 슬픔이 뒤섞인 표정을 짓는다.)
"스스로 축복을 버린 자들과... 순수함을 더럽힌 자들. 다시는 우리 숲에 발을 들일 생각 마시오."

**보를라그(이제는 오크):** (이성이 마비된 채, 본능에 따라 으르렁거린다. 그는 더 이상 옛 동료를 알아보지 못한다. 그의 눈에 비친 것은 그저 나약한 먹잇감과 거슬리는 이물질뿐이다.)
"크르르르... 약하다... 전부... 약하다!"

**(보를라그가 첫 번째 전투 포효를 내지르자, 그의 뒤에 있던 오크들도 일제히 함성을 지른다. 엘프들은 경멸하며 등을 돌려 숲의 그림자 속으로 사라지고, 인간들은 두려움과 결의가 뒤섞인 눈으로 그들을 노려본다.)**

**(한때 하나였던 그들은, 이제 서로를 이해할 수 없는 완벽한 '타자'가 되었다. 그들은 말없이 각자의 길을 통해 성소를 떠난다. 다시는 한자리에 모일 수 없으리라는 것을 예감하며.)**

**(나레이션 - 고대의 목소리)**
아르카인의 시대는 그렇게 끝났다. 하나의 이름 아래 빛나던 영광은 깨진 거울 조각이 되어 흩어졌다. 그리고 그 자리에는... 인간의 불안, 엘프의 오만, 오크의 분노만이 남아, 다가올 피의 시대를 예고하고 있었다. 천 년의 비극은, 바로 이 순간 시작되었다.

**[SCENE END]**

================================================================================

물론입니다. 마침내 모든 것이 부서지고 새로운 시대가 피로 시작되는 순간, **제4장: 새로운 시대, 첫 번째 전쟁**의 대화 대본을 최대한 길고 상세하게, 각 종족의 변화와 그로 인해 촉발되는 비극을 장엄하게 구성해 드리겠습니다.

---
---

### **📜 아르카디아 전기: 1000년 전 이야기**

## **원초의 분열 (The Primordial Schism)**

### **제 4장: 새로운 시대, 첫 번째 전쟁 (A New Age, The First War)**

**[SCENE START]**

**[4-1. 핏빛 하늘 아래, 세 개의 종족]**

**장소:** 대분열의 의식이 끝난 아니마 문디의 성소 입구. 하늘은 '생명의 근원'이 파괴될 때 흘린 피눈물처럼 온통 붉게 물들어 있다.
**등장인물:** 리산데르(인간), 엘라라(엘프), 보를라그(오크)와 각자의 종족들.

**(세 종족이 마침내 성소 밖으로 모습을 드러낸다. 그들은 핏빛 하늘 아래, 서로를 처음 보는 생물처럼 낯설게 바라본다.)**

**인간 병사 1:** (자신의 손을 내려다보며, 떨리는 목소리로)
"리산데르 님... 내 안의 빛이... 느껴지지 않습니다. 아니마 문디의 노래가 들리지 않아요... 마치... 영혼의 절반이 뜯겨나간 것 같습니다."

**인간 병사 2:** (갑옷도 없이 맨몸에 와닿는 찬 바람에 몸을 떨며)
"춥다... 태어나서 처음으로... 춥다는 감각을 느꼈어. 이게... 우리가 선택한 겁니까? 이 나약함이?"

**리산데르:** (자신의 백발이 된 머리카락과 주름진 손을 보며, 씁쓸하게 대답한다)
"나약함이 아니다. '생존'이다. 우리는 이제 대지의 일부다. 추위도, 허기도, 고통도 전부 우리가 살아있다는 증거다. 익숙해져야 한다. 이것이 우리의 새로운 시작이다."

**(한편, 엘프들은 자신들을 감싸는 숲의 기운에 경탄하면서도, 그 경계를 벗어나려 할 때 느껴지는 미묘한 속박감에 당황한다.)**

**엘프 장로:** (엘라라에게)
"대사제님, 숲의 모든 것이 노래하는 소리가 들립니다. 이 얼마나 순수한 마력입니까! 하지만... 저 숲 너머의 평원을 보려 하면... 마음이 무거워지고 발이 떨어지지 않습니다."

**엘라라:** (핏빛 하늘을 보며, 차가운 목소리로)
"우리는 이제 숲의 수호자이자, 숲 그 자체이니까요. 저 밖의 속세는 이제 우리의 땅이 아닙니다. 저 불완전한 자들과 야만스러운 짐승들이 날뛰는 오염된 땅일 뿐. 우리는 이 신성한 숲을 지키는 것으로 우리의 소명을 다할 것입니다."

**(그녀의 시선이 인간들과 오크들에게 닿는다. 그 눈에는 더 이상 동정이나 연민이 없다. 오직 차가운 경멸과 결별의 의지만이 서려 있다.)**

**(가장 극적인 변화를 겪은 것은 오크들이다. 그들은 이성적인 대화 대신, 본능적인 소리를 내뱉으며 주변을 경계한다.)**

**보를라그:** (주변을 둘러보며, 낮은 목소리로 으르렁거린다)
"크르르르... 약하다... 냄새가... 다르다..."

**오크 전사:**
"피! 싸움! 배고프다!"

**(보를라그의 시선이 자신들을 두려움과 경계의 눈으로 보는 인간들에게 고정된다. 그의 마비된 이성은 그들을 '형제'가 아닌, 자신보다 '약한 존재'로 인식할 뿐이다. 그의 안에서 억눌려왔던 투쟁 본능이, 이제는 통제 불가능한 파괴 본능이 되어 들끓기 시작한다.)**

---

**[4-2. 분열 전쟁의 서막]**

**(보를라그가 갑자기 거대한 도끼를 고쳐 잡고, 가장 가까이 있던 인간들을 향해 한 걸음 내딛는다.)**

**리산데르:** (본능적으로 위험을 감지하고 외친다)
"보를라그! 멈춰라! 대체 무슨 짓인가!"

**(하지만 보를라그에게 리산데르의 말은 더 이상 의미가 없다. 그는 그저 나약한 존재의 외침으로 받아들일 뿐이다.)**

**보를라그:**
"크아아아아아!! (약한 놈들은 없어져야 한다!)"

**(보를라그의 포효를 신호탄으로, 수백의 오크들이 이성을 잃고 인간들에게 달려든다. 알테아 역사상 최초의 전쟁, '분열 전쟁'이 그렇게 시작되었다.)**

**인간 병사들:**
"으아악! 막아라! 대열을 갖춰라!"
"마법이 없어! 검과 창으로 싸워야 한다!"

**(인간들은 필사적으로 저항하지만, 마력을 잃은 그들에게 야성의 힘을 얻은 오크들은 공포 그 자체였다. 오크의 도끼에 인간들의 방패가 종이처럼 찢겨나가고, 비명이 전장을 가득 메운다.)**

**리산데르:** (검을 뽑아 들고 오크와 맞서 싸우며 절규한다)
"이 어리석은 놈! 우리가 싸워야 할 적은 서로가 아니란 말이다!"

**(그때, 전투의 여파가 엘프들이 머무는 숲의 경계까지 번진다. 오크 한 마리가 닥치는 대로 휘두른 도끼에 신성한 고목이 쓰러지자, 엘프들의 태도가 급변한다.)**

**엘라라:** (쓰러진 나무를 보며, 그녀의 얼굴에서 모든 슬픔이 사라지고 얼음 같은 분노가 깃든다)
"...감히. 감히 우리의 신성한 숲을 더럽혀? 저 야만스러운 짐승들이...!"

**엘프 장로:**
"대사제님, 명령을!"

**엘라라:**
"숲을 침범하는 모든 것을 파괴하십시오. 저 오크들도, 그들을 막지 못하는 저 무력한 인간들도, 모두 우리 숲의 적입니다! 숲의 분노를 보여주세요!"

**(엘라라의 명령에, 엘프들이 일제히 활시위를 당긴다. 수백 개의 마력 화살이 비처럼 쏟아져, 오크와 인간을 가리지 않고 전장에 내리꽂힌다. 이제 전쟁은 삼파전이 되었다. 인간은 오크의 야만성과 엘프의 무자비함 사이에서 살아남기 위해 발버둥 쳐야 했다.)**

---

**[4-3. 공멸의 그림자]**

**(전쟁은 몇 달간 계속되었다. 대지는 세 종족이 흘린 피로 물들었고, 아니마 문디의 비명은 그칠 줄을 몰랐다. 한때 형제였던 그들은 서로를 죽고 죽이며 대륙을 세 개의 영역으로 나누었다.)**

**(어느 날, 리산데르는 지친 몸을 이끌고 전장의 언덕 위에서 참혹한 광경을 내려다본다. 그때, 한 정찰병이 공포에 질려 달려온다.)**

**정찰병:**
"폐하! 큰일 났습니다! 재의 황무지... 그곳에서 혼돈의 기운이... 이전보다 몇 배는 더 강해졌습니다! 우리의 전쟁이, 우리의 증오와 절망이... 저것들을 살찌우고 있었습니다!"

**리산데르:** (하늘을 본다. 핏빛 하늘 너머로, 더욱 짙고 거대해진 검은 균열이 보였다)
"...이럴 수가. 우리는... 서로를 죽이며, 우리 모두의 진정한 적을 키우고 있었던 건가. 이 얼마나 끔찍한 희극인가..."

**(리산데르는 결단한다. 이 미친 전쟁을 멈추지 않으면 모두가 공멸할 것이라고.)**

---

**[4-4. 마지못한 동맹]**

**장소:** 세 종족의 경계선에 있는 황량한 평원.

**(리산데르는 각 종족에 '일시적 휴전'과 '삼자회담'을 제안한다. 엘라라와 보를라그는 마지못해 제안을 받아들인다. 세 지도자가 마침내 다시 한자리에 모였다. 하지만 그들의 사이에는 불신과 증오의 강이 흐르고 있었다.)**

**엘라라:** (인간과 오크를 번갈아 보며, 차갑게)
"용건이 무엇이죠, '인간'의 왕? 당신들의 나약함과 저 짐승들의 야만성 때문에 우리 숲의 평화가 깨졌습니다. 더 이상 할 말은 없다고 생각하는데요."

**보를라그:** (여전히 이성은 불완전하지만, 리더로서의 본능은 남아있다. 그는 리산데르를 노려보며 으르렁거린다)
"크르르... 강한 놈이... 나타났다. 황무지... 괴물... 더 커졌다. 사실이냐?"

**리산데르:**
"사실이다, 보를라그. 그리고 엘라라. 우리가 이 소모적인 전쟁을 계속하는 동안, 원초의 혼돈은 우리의 피와 증오를 먹고 자라나, 이제 이 세상 전부를 삼키려 하고 있소. 우리가 서로를 마지막 형제라고 부르며 죽이게 된다면, 그것이야말로 저 혼돈이 바라는 결말이겠지."

**(리산데르는 부서진 '생명의 근원' 조각 하나를 꺼내 보인다.)**

**리산데르:**
"우리는 갈라섰지만, 우리의 뿌리는 여전히 하나요. 이 아니마 문디의 힘을 합칠 수 있다면... 단 한 번이라도 힘을 합칠 수 있다면, 저 혼돈을 파괴하진 못해도 봉인할 수는 있을 것이오. 우리... 마지못해 손을 잡아야만 하오. 살아남기 위해서."

**엘라라:**
"당신들을 어떻게 믿죠? 이 동맹이 끝나는 순간, 당신들의 칼과 저 짐승의 도끼가 다시 우리를 향하지 않는다고 누가 장담합니까?"

**리산데르:**
"장담할 수 없소. 나 역시 당신들을 믿지 않소. 하지만 선택의 여지가 없지. 함께 싸우거나, 각자 죽거나. 둘 중 하나요."

**(긴 침묵 끝에, 엘라라가 마지못해 고개를 끄덕인다. 보를라그 역시 '더 큰 적'을 향한 투쟁 본능에 고개를 끄덕인다.)**

---

**[4-5. 생명의 쐐기, 그리고 영원한 상처]**

**(세 종족은 마지못해 힘을 합친다. 그들은 아니마 문디의 가장 단단하고 순수한 가지를 꺾어 창을 만들었다.)**

*   **엘프**들은 자신들의 순수한 마력을 불어넣어, 창에 혼돈을 봉인할 수 있는 법칙을 새겼다.
*   **인간**들은 자신들의 지혜와 전략으로, 거대한 혼돈의 핵을 꿰뚫을 방법을 찾아냈다.
*   **오크**들은 자신들의 압도적인 힘으로, 그 창을 들고 최전선에 서서 길을 열었다.

**(마침내, 거대해진 '원초의 혼돈' 앞에서 세 종족의 연합군이 최후의 결전을 벌인다. 보를라그가 혼돈의 방어막을 부수고, 리산데르의 지휘 아래 엘라라가 마력으로 길을 열자, 선택된 영웅들이 그 틈으로 뛰어들어 아니마 문디의 가지로 만든 최초의 **'생명의 쐐기(Vita Spiculum)'**를 혼돈의 심장에 박아 넣는다.)**

**[SFX: 시공간이 찢어지는 굉음과 함께, 혼돈이 시공의 틈새로 빨려 들어간다.]**

**(혼돈은 봉인되었다. 하지만 승리의 기쁨은 없었다. 대륙은 상처 입었고, 셀 수 없는 이들이 죽었다. 세 지도자는 폐허가 된 대지 위에서 서로를 마주 본다.)**

**리산데르:**
"...끝났다."

**엘라라:** (차가운 목소리로)
"네. 우리의 '동맹'도 끝났습니다. 다시는 우리 숲에 접근하지 마십시오. 외부의 모든 것은 이제 우리의 적입니다."

**(엘라라는 미련 없이 돌아서, 남은 엘프들을 이끌고 숲의 장막 속으로 사라진다.)**

**보를라그:** (리산데르를 한번 노려본 뒤, 땅에 침을 뱉는다)
"크륵... 다음은... 너다."

**(보를라그 역시 남은 오크들을 이끌고 황야로 사라진다.)**

**(리산데르는 폐허 위에 홀로 서서, 핏빛에서 겨우 본래의 색을 찾아가는 하늘을 본다. 그는 승리했지만, 모든 것을 잃었다.)**

**(나레이션 - 고대의 목소리)**
우리는 가장 큰 위협을 봉인했다. 하지만 그 대가는 너무나 컸다. 우리는 서로를 용서할 수 없었고, 서로를 이해할 수도 없었다. 대륙에 그어진 경계선보다 더 깊은 상처가, 우리의 마음에 새겨졌다. '분열 전쟁'은 그렇게 끝났지만, 천 년에 걸친 불신과 증오의 역사는... 이제 막 시작되었을 뿐이었다.

**[SCENE END]**

================================================================================

물론입니다. 모든 비극의 대서사시, 그 천 년 후의 이야기를 담은 **에필로그: 천 년의 저주**의 대화 대본을 최대한 길고 상세하게, 시간의 흐름과 잊힌 역사의 무게, 그리고 새로운 비극의 서막이 느껴지도록 구성해 드리겠습니다.

---
---

### **📜 아르카디아 전기: 1000년 전 이야기**

## **에필로그: 천 년의 저주 (Epilogue: A Thousand-Year Curse)**

**[SCENE START]**

**[1. 시간의 먼지 (Decades Later)]**

**(나레이션 - 고대의 목소리, 지치고 슬픔에 잠긴 톤으로)**
시간은 상처를 치유하지 못했다. 그저 상처 위에 먼지가 쌓이게 할 뿐이었다. 분열 전쟁이 끝나고 수십 년이 흘렀다. 한때 아르카인이었던 세 지도자는, 이제 각자의 종족을 이끄는 늙고 지친 왕과 여왕, 그리고 족장이 되었다.

**(장면 전환: 세 개의 다른 장소)**

**1-1. 인간의 왕국, '테라노바'의 왕성 발코니.**
**(백발의 리산데르가 새로 지어진 도시의 야경을 내려다본다. 도시는 활기차지만, 그 빛은 마력의 영롱함이 아닌, 필멸자들이 피운 수많은 횃불과 등불의 빛이다. 그의 곁에는 젊은 장군이 서 있다.)**

**젊은 장군:**
"폐하, 또 밤을 지새우시는군요. 우리의 왕국은 날로 번성하고 있습니다. 폐하의 지혜 덕분입니다."

**리산데르:** (쓴웃음을 지으며)
"번성이라... 그래, 우리는 살아남았지. 자식을 낳고, 땅을 일구고, 성벽을 쌓았지. 하지만... 가끔 꿈을 꾸네, 장군. 우리가 하늘을 날고, 손짓 한 번으로 별을 빚어내던 시절의 꿈을... 그리고 꿈에서 깨면, 이 늙고 병든 육체와 곧 다가올 죽음의 그림자만이 날 반기지. 이것이... 내가 선택한 '생존'의 대가일세."

**젊은 장군:**
"폐하... 그건 그저 전설이 아닙니까?"

**리산데르:**
"그래... 이제는 그저 전설이 되었지. 아무도 기억하지 못하는..."

**(리산데르의 눈은 도시의 불빛이 아닌, 저 멀리 어둠에 잠긴 거대한 숲을 향하고 있다.)**

**1-2. 엘프의 숲, '실바누스'의 달빛 샘.**
**(영원한 젊음을 유지하고 있지만, 눈빛에는 천 년의 고독이 서린 엘라라가 물 위에 비친 자신의 모습을 본다. 주변에는 어떤 소음도 없이, 오직 숲의 정령들이 속삭이는 소리만 들린다.)**

**정령의 속삭임:**
<여왕이시여... 숲 밖에서... 필멸자들이 또 영역을 넓힙니다... 시끄러운... 소리가... 들립니다...>

**엘라라:** (샘물을 향해 나지막이)
"내버려 두어라. 벌레들이 꿈틀대는 것까지 신경 쓸 필요는 없다. 그들이 감히 이 신성한 경계를 넘지 않는 한... 우리는 우리의 완벽한 조화 속에서 영원할 것이니. 순수함이야말로 우리의 가장 강한 갑옷이다."

**(하지만 그녀의 손이 미세하게 떨리고 있다. 영원한 고립이 과연 승리인지, 그녀 자신도 더는 확신하지 못하는 듯하다.)**

**1-3. 오크의 황야, '보르' 부족의 화톳불 앞.**
**(수많은 상처로 뒤덮인 늙은 족장, 보를라그가 고기를 뜯고 있다. 그의 주변에는 수많은 오크들과, 늑대나 곰의 특징을 지닌 새로운 '수인'들이 섞여 있다. 그의 이성은 어느 정도 돌아왔지만, 말은 여전히 거칠고 짧다.)**

**젊은 늑대 수인:**
"족장님! 서쪽의 인간들이 또 사냥터를 침범했다!"

**보를라그:** (고기를 씹으며)
"크륵... 약한 놈들. 하지만... 수가 많다. 숲의 뾰족귀들도... 거슬린다."

**젊은 늑대 수인:**
"어떻게 할까? 싸울까?"

**보를라그:** (화톳불을 보며, 아주 잠시 옛 기억이 스친다)
"싸운다... 늘... 싸운다... 하지만... 왜 싸우는지... 가끔... 잊어버린다... 그냥... 이 분노가... 끓어오른다..."

**(그는 가슴을 치며 으르렁거린다. 야성의 힘은 그에게 생존을 주었지만, 영원히 지워지지 않을 분노의 낙인을 함께 새겼다.)**

---

**[2. 잊힌 기원 (Centuries Later)]**

**(나레이션 - 고대의 목소리)**
수백 년이 흘렀다. 첫 세대는 모두 죽어 흙으로 돌아갔다. 인간들은 대륙 전역으로 퍼져나가 제국을 건설했고, 엘프는 신화 속 존재가 되었으며, 오크는 황야의 수많은 수인 부족으로 분화했다. 그들은 모두 자신들의 기원을 잊었다. '아르카인'이라는 이름은, 이제 그 누구도 기억하지 못하는 먼지 쌓인 단어가 되었다. 그들은 서로를 '타자'로만 인식하며, 끝없는 반목과 작은 전쟁을 반복했다.

---

**[3. 10년 전, 마왕의 출현]**

**장소:** 대마법사 크로노스의 탑. 폭풍우가 몰아치는 밤.
**등장인물:** 대마법사 크로노스, 젊은 마법사.

**(탑의 창문 밖으로, 검은 마기가 소용돌이치며 도시를 위협하고 있다. 마왕 '아자젤'이 강림한 것이다.)**

**젊은 마법사:**
"스승님! 마왕의 군세가 성벽을 무너뜨리고 있습니다! 막을 수가 없습니다! 저것은... 원초의 혼돈이 남긴 파편입니다!"

**크로노스:** (고대의 양피지를 펼치며, 결의에 찬 눈으로)
"방법은 단 하나뿐이다! 천 년 전, 고대의 존재들이 혼돈을 봉인했다는 '생명의 쐐기'... 그 원리를 재현하는 것이다!"

**젊은 마법사:**
"하지만 스승님! 그건 인간과 엘프, 그리고 수인들의 힘을 모두 합쳐야만 가능했던 금단의 유물입니다! 지금 우리에게 그런 힘이 어디 있습니까!"

**크로노스:** (자신의 가슴에 손을 얹는다. 그의 심장이 빛나기 시작한다.)
"나의 모든 마력과 생명을 바쳐서라도, 그 힘을 억지로 끌어낼 수밖에 없다! 이 세상이 혼돈에 삼켜지게 둘 수는 없다!"

**(크로노스는 금단의 주문을 외우고, 그의 생명을 대가로 한 '생명의 쐐기'가 허공에 구현된다. 그는 마지막 힘을 다해 쐐기를 마왕 아자젤에게 날린다. 쐐기는 마왕의 심장을 꿰뚫고, 마왕을 시공간의 틈새에 다시 한번 봉인한다.)**

**크로노스:** (재가 되어 사라지며, 마지막 말을 남긴다)
"...봉인은... 불완전하다... 언젠가... 진짜가... 나타나야만..."

---

**[4. 저주의 각성 (The Present Day)]**

**(나레이션 - 고대의 목소리)**
크로노스의 희생으로 세상은 다시 한번 구해진 듯 보였다. 하지만 그의 예언대로, 불완전한 봉인은 새로운 재앙의 씨앗이 되었다. 봉인의 약해진 틈새로, 아주 조금씩 새어 나온 혼돈의 마력... 그것은 대지를 오염시키기엔 미미한 양이었다. 하지만 그 마력은... 천 년 동안 대지 깊숙한 곳에 잠들어 있던 것을 깨우기엔 충분했다.

**(장면 전환: 인간과 수인의 경계 지역, 한적한 마을의 저녁.)**

**(마을의 땅바닥 틈새에서, 붉고 끈적한 안개가 스멀스멀 피어오르기 시작한다. '피의 안개'다.)**

**마을 주민 1:** (밭을 갈다가, 코를 킁킁거리며)
"이... 이 비릿한 냄새는 뭐지? 꼭 피 냄새 같군."

**마을 주민 2:** (옆집 이웃을 보며, 갑자기 눈에 적의를 드러낸다)
"어이, 김. 자네... 어제 내 밭 경계를 살짝 넘지 않았나? 네놈, 도둑놈이었군!"

**마을 주민 1:**
"뭐, 뭐라고? 이 영감탱이가 노망이 났나!"

**(피의 안개가 짙어지자, 평화롭던 마을 사람들이 서로를 향해 이유 없는 분노와 증오를 터뜨리기 시작한다. 그들의 눈이 충혈되고, 손에는 농기구가 무기처럼 들려 있다. 그들의 마음속 깊은 곳에 잠들어 있던 '분열과 증오의 저주'가 피의 안개에 의해 깨어난 것이다.)**

**순찰 중인 병사:** (안개를 보며 공포에 질려 외친다)
"이건... 이건 고대의 저주다! '피의 안개'가 돌아왔어! 모두 정신 차려! 서로 싸우지 마!"

**(하지만 그의 외침은 증오에 찬 함성에 묻혀버린다. 마을은 순식간에 아비규환의 지옥으로 변한다.)**

---

**[5. 새로운 영웅의 서막]**

**(나레이션 - 고대의 목소리)**
천 년 전, 세 지도자가 흘렸던 슬픔과 증오의 피가 저주가 되어 깨어났다. 피의 안개는 대륙 전역으로 퍼져나가며, 모든 종족의 마음에 분열의 불씨를 지피기 시작했다. 역사는 반복되는 듯했다. 비극은 또 다른 비극을 낳고, 세상은 다시 한번 공멸의 위기 앞에 섰다.

**(장면 전환: 한적한 시골 마을의 훈련장. 한 젊은이가 홀로 검을 휘두르고 있다. 그는 인간이지만, 그 움직임은 엘프처럼 우아하고, 그 기세는 수인처럼 맹렬하다. 그의 이름은 **카인 레오하트**.)**

**(카인이 숨을 고르며 훈련을 멈추고, 저 멀리 지평선에 피어오르는 붉은 안개를 바라본다. 그는 아직 아무것도 모른다. 자신의 몸에 흐르는, 세 종족의 힘과 가능성을 모두 품은 피의 비밀을. 자신이 천 년 전 시작된 비극의 마지막 장을 끝내고, 갈라진 세상을 다시 하나로 묶을 '통합의 열쇠'라는 것을.)**

**(나레이션 - 고대의 목소리, 마지막 희망을 담아)**
하지만 어둠이 가장 깊을 때, 별은 가장 밝게 빛나는 법. 이제, 천 년의 저주를 끊어낼 새로운 이야기가 시작될 차례였다. 그의 운명의 톱니바퀴가... 이제 막 움직이기 시작했다.

**[SCENE END]**



## 📊 버전 히스토리

### 🆕 Version 1.8.0 (2025-06-21) - Current
**"Complete Foundation Update"**

#### ✅ 새로 구현된 기능
- **🎓 TutorialManager 시스템**
  - 6단계 완전 가이드 튜토리얼
  - UI 하이라이트 및 단계별 진행
  - 플레이어 진행 상황 저장
  - 건너뛰기 및 재시작 기능

- **🔊 SoundManager 시스템**  
  - BGM/SFX 완전 분리 관리
  - 오디오 소스 풀링 (10개 동시 재생)
  - 볼륨 조절 및 음소거 지원
  - 게임 상황별 자동 사운드 재생
  - 페이드 인/아웃 효과

- **📊 CharacterCSVDatabase 시스템**
  - 18개 완전한 샘플 캐릭터 (3종족 × 6개)
  - 22개 필드 완전한 CSV 구조
  - Unity 에디터 통합 툴
  - CSV ↔ ScriptableObject 양방향 변환

- **🎮 상속 기반 캐릭터 행동 시스템**
  - CharacterBehaviorBase 추상 클래스
  - MeleeAttackBehavior, RangedAttackBehavior, SupportBehavior
  - CharacterBehaviorManager로 통합 관리
  - 인스펙터 체크박스 개별 제어

#### 📚 문서화 완료
- **개발자 Unity 구현 가이드**: 완전 초보자용 단계별 가이드
- **플레이어 게임플레이 완전 가이드**: 초급부터 마스터까지
- **CSV 시스템 사용법**: 에디터 툴 사용 방법
- **확장성 가이드**: AI 생성, 외부 데이터 연동, 커뮤니티 모딩

### 🔧 Version 1.7.0 (2025-06-18)
**"Advanced Systems Integration"**

#### ✅ 구현 완료
- **RecycleManager**: 종족 변환 시스템 (골드 50)
- **RaceSynergyManager**: 3/5/7/9명 시너지 버프
- **CharacterUpgradeManager**: 인게임 전 업그레이드 (최대 30레벨)
- **InGameEnhanceManager**: 실시간 강화 버튼 (종족별)
- **WaveRewardManager**: 5웨이브 보상 선택 시스템
- **AutoMergeManager**: 주사위 버튼 자동 합성

#### 🏗️ 아키텍처 개선
- RouteWaypointManager: 3라인 웨이포인트 시스템
- BossMonster: 3페이즈 보스 시스템
- AI 학습 시스템: 패턴 인식 및 동적 난이도

### 🎯 Version 1.6.0 (2025-06-15)
**"Core Gameplay Foundation"**

#### ✅ 핵심 시스템
- **플레이어 캐릭터 소환 제한**: 최대 50마리
- **타워 스택 시스템**: 같은 캐릭터 3개까지 스택
- **향상된 성 전투 시스템**: 성의 자동 공격 및 방어 버프
- **웨이브 스포너**: 20웨이브 시스템 + 보스 웨이브

#### 🎮 게임플레이
- **3라인 시스템**: 왼쪽/중앙/오른쪽 경로
- **캐릭터 드래그 이동**: 라인 간 이동 가능
- **합성 시스템**: 1성×3→2성, 2성×3→3성
- **종족 시스템**: Human/Orc/Elf + 덱 구성 (3+3+3+1)

### 🏗️ Version 1.5.0 (2025-06-10)
**"Data Management System"**

#### ✅ 데이터 시스템
- **CSV 동기화**: ScriptableObject ↔ CSV 양방향
- **CharacterDatabase**: 캐릭터 데이터 구조화
- **가챠 시스템**: 단일/10연차 + 200칸 인벤토리
- **저장/불러오기**: Easy Save 3 연동

#### 🎨 UI 시스템
- **LobbySceneManager**: 100개 스테이지 관리
- **DeckPanelManager**: 10칸 덱 구성 UI
- **DrawPanelManager**: 가챠 확률 시스템

### 🔧 Version 1.4.0 (2025-06-05)
**"Combat & AI System"**

#### ✅ 전투 시스템
- **Character 클래스**: 컴포넌트 기반 아키텍처
- **CharacterCombat**: 타겟팅 및 데미지 계산
- **Monster 클래스**: 웨이포인트 추적 + 성 공격
- **IDamageable**: 통합 데미지 인터페이스

#### 🤖 AI 시스템
- **AIPlayer**: 플레이어와 동일한 행동 가능
- **AIBrain**: 가중치 기반 의사결정
- **학습 시스템**: 플레이 패턴 분석 및 개선

### 🎯 Version 1.3.0 (2025-06-01)
**"Resource & Management"**

#### ✅ 자원 관리
- **SummonManager**: 미네랄 30 랜덤 소환
- **MergeManager**: 타일 기반 합성 감지
- **PlacementManager**: 캐릭터 배치 + 50마리 제한
- **TileManager**: 그리드 기반 배치 시스템

### 🏠 Version 1.2.0 (2025-05-25)
**"Core Architecture"**

#### ✅ 게임 아키텍처
- **GameManager**: 게임 상태 + 20웨이브 관리
- **Canvas → World 2D**: 좌표계 전환 완료
- **싱글톤 패턴**: 모든 매니저 클래스 적용
- **컴포넌트 시스템**: 모듈화된 캐릭터 구조

### 🌱 Version 1.1.0 (2025-05-20)
**"Project Setup"**

#### ✅ 프로젝트 기반
- **Unity 6.2 Beta**: 최신 Unity 버전 적용
- **폴더 구조**: Scripts/pjy 모듈화 구조
- **기본 씬 설정**: GameScene + LobbyScene
- **Git 저장소**: 버전 관리 시작

### 🛠️ Version 1.8.3 (2025-06-21)
**"Stability & Performance Enhancement"**

#### ✅ 안정성 개선
- **SummonManager 로직 수정**: 
  - FindCharacterIndex() 메서드의 unreachable code 제거
  - 적 데이터베이스 검색 로직 정상화

- **전투 타겟 검증 강화**: 
  - CharacterCombat.IsTargetValid() 메서드 추가
  - 사망한 타겟에 대한 무효 공격 방지
  - 메모리 누수 방지를 위한 무효 참조 제거

- **리소스 로딩 안정성**: 
  - 프리팹 로딩 실패 시 graceful fallback 처리 강화
  - null 참조 예외 방지 시스템 개선

#### 🔧 상세 수정 내용
- **SummonManager.cs:518-531** - 적 데이터베이스 검색 로직 수정 (early return 제거)
- **CharacterCombat.cs:419-433** - IsTargetValid() 메서드 추가로 사망한 타겟 공격 방지  
- **CharacterCombat.cs:442-447** - Attack() 메서드에 타겟 유효성 검증 추가

#### 🎯 게임플레이 안정성
- **전투 시스템**: 예외 상황 처리 강화로 크래시 방지
- **타겟팅 시스템**: 무효 타겟 자동 제거로 성능 개선
- **메모리 관리**: 참조 누수 방지로 장시간 플레이 안정성 확보

### 🛠️ Version 1.8.2 (2025-06-21)
**"Critical System Fixes & Combat Enhancement"**

#### ✅ 핵심 시스템 수정
- **SummonManager 초기화 안정성 강화**:
  - 의존성 매니저 대기 로직 추가
  - Fallback 자원 로딩 시스템 구현
  - 5단계 프리팹 로딩 시스템 (spawnPrefab → motionPrefab → Resources 경로들 → 기본 프리팹)

- **캐릭터-몬스터 전투 시스템 완성**:
  - 몬스터가 캐릭터 우선 공격하도록 수정
  - 3단계 타겟 우선순위: 캐릭터 → 중간성 → 최종성
  - CharacterCombat의 완전한 타겟팅 시스템과 연동

- **UI-World 좌표계 통합 문제 해결**:
  - AutoMergeManager의 좌표계 혼재 문제 수정
  - 안전한 캐릭터 생성 시스템 구현
  - World Space와 UI Space 자동 감지 및 처리

#### 📋 시뮬레이션 검증 완료
- **3가지 핵심 시나리오 검증**:
  1. 게임 시작 → 캐릭터 소환 (완전 동작)
  2. 웨이브 스폰 → 전투 (캐릭터↔몬스터 상호 공격)
  3. 캐릭터 합성 (좌표계 문제 해결)

### 🔧 Version 1.8.1 (2025-06-21)
**"System Optimization & Stability"**

#### ✅ 코드 품질 개선
- **CharacterData 중복 필드 정리**: 
  - `range`, `health`, `maxHealth`, `tribe` 필드를 프로퍼티로 변환
  - Obsolete 속성으로 하위 호환성 유지
  - 데이터 일관성 향상

- **CharacterDatabase 초기화 버그 수정**:
  - 코루틴 기반 → 이벤트 기반 초기화로 전환
  - GameManager.OnGameManagerReady 이벤트 구독
  - 타이밍 문제 해결 및 안정성 향상

- **CSV 동기화 시스템 안정성 강화**:
  - 안전한 파싱 메서드 추가 (SafeParseInt, SafeParseFloat, SafeParseEnum)
  - CSV 파싱 오류 내성 향상
  - 기본값 지원으로 데이터 무결성 보장

#### 📋 시뮬레이션 검증 완료
- **핵심 게임플레이 루프**: 100% 구현 검증
- **캐릭터 소환-배치-전투-합성**: 완전한 사이클 확인
- **AI 플레이어 시스템**: 정상 동작 검증
- **웨이브 스폰과 성 공격**: 연결성 확인
- **CSV 데이터와 게임플레이**: 연동 검증

### 🎉 Version 1.0.0 (2025-05-15)
**"Project Genesis"**

#### ✅ 프로젝트 시작
- **프로젝트 생성**: "Twelve" 게임 컨셉 확정
- **기획 문서**: Tower Defense + Auto-Battler 하이브리드
- **기술 스택**: Unity 6.2 Beta + C# + Easy Save 3
- **목표 설정**: 모바일 실시간 PvP 게임

---
