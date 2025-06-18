# 📝 Twelve 프로젝트 구현 노트

## 🔄 주요 변경사항 (2025년 1월)

### 1. AI 시스템 통합 ✨
- **PlayerController.cs**: 플레이어/AI 통합 컨트롤러 추가
  - `isAI` 플래그로 AI/인간 플레이어 구분
  - 동일한 코드베이스 공유, AI 전용 동작은 AIBehavior 컴포넌트에서 처리
- **AIBehavior.cs**: AI 전략 시스템 구현
  - 5가지 전략: Rush, Turtle, Economy, Counter, Balanced
  - 4단계 난이도: Easy, Normal, Hard, Expert
- **GameManager.cs**: 플레이어 관리 시스템 추가
  - RegisterPlayer/UnregisterPlayer 메서드
  - AI/인간 플레이어 자동 생성 로직

### 2. 성능 최적화 🚀
- **Gizmos 조건부 컴파일**: 모든 OnDrawGizmos를 `#if UNITY_EDITOR`로 감싸 빌드 크기 감소
- **오브젝트 풀링 개선**: 
  - ObjectPooler에 최대 크기 제한 추가
  - Bullet 클래스에 IPooledObject 구현
  - 메모리 관리 메서드 추가 (ClearPool, ClearAllPools)
- **Update → Coroutine 전환**: CharacterCombat의 타겟 검색을 코루틴으로 변경

### 3. 안정성 개선 🛡️
- **예외 처리 추가**: 
  - 주요 Start/Awake 메서드에 try-catch 블록
  - null 체크 강화
- **메모리 누수 방지**:
  - OnDestroy에서 이벤트 구독 해제
  - 리스트/딕셔너리 정리
  - 코루틴 중지 로직

### 4. Unity 호환성 개선 🎮
- **프리팹 호환성**: Character의 컴포넌트 자동 추가를 런타임에서만 수행
- **Resources.Load 안전성**: CharacterData의 spawnPrefab 직접 참조 우선 사용

## 🎯 6가지 핵심 기능 구현 상태

1. **✅ 같은 캐릭터 3개까지 한 타일에 배치**
   - `Tile.cs`: UpdateCharacterPositions()에서 위치/크기 자동 조정
   - 1개: 100%, 2개: 80%, 3개: 70% 크기

2. **✅ 몬스터의 성 공격 시스템**
   - `Monster.cs`: FindAndAttackCastle(), OnReachEnd() 구현
   - MiddleCastle(HP 500), FinalCastle(HP 1000) 공격

3. **✅ 50마리 소환 제한**
   - `PlacementManager.cs`: maxPlayerCharacters = 50
   - CanSummonCharacter()에서 제한 체크

4. **✅ 공격 대상 시스템**
   - AttackTargetType enum: Character, Monster, Both, CastleOnly, All
   - 몬스터 → 성, 성 → 몬스터/캐릭터, 캐릭터 → 성/캐릭터

5. **✅ 3라인 웨이포인트 시스템**
   - `WaypointManager.cs`: 좌/중/우 라우트별 경로 관리
   - RouteType enum: Left, Center, Right

6. **✅ AI 플레이어 통합**
   - isAI 플래그로 구분
   - 동일 GameObject, 다른 동작은 AIBehavior 컴포넌트로 처리

## ⚠️ 주의사항

### 빌드 시 확인사항
1. **Resources 폴더 구조**: 
   - `Resources/Prefabs/Characters/` 경로에 캐릭터 프리팹 배치 필요
   - 또는 CharacterData의 spawnPrefab 직접 할당

2. **레이어 설정**:
   - "Characters" 정렬 레이어 필요
   - "Projectile" 레이어 필요

3. **태그 설정**:
   - "Castle" 태그 필요 (성 오브젝트용)

### 성능 고려사항
1. **오브젝트 풀 설정**:
   - Bullet: 초기 크기 50-100 권장
   - Effect: 초기 크기 20-30 권장

2. **AI 설정**:
   - AI 의사결정 간격: 기본 2초 (aiDecisionInterval)
   - 동시 AI 플레이어는 2-4명 권장

## 🔧 디버깅 도구

### AITestSetup 사용법
1. GameScene에 빈 GameObject 생성
2. AITestSetup 컴포넌트 추가
3. 단축키:
   - F1: 플레이어 상태 확인
   - F2: AI 난이도 변경
   - F3: 모든 플레이어에게 50 미네랄 추가

### 오브젝트 풀 상태 확인
```csharp
// 콘솔에서 실행
ObjectPooler.Instance.PrintPoolStatus();
```

## 📚 확장 가능한 부분

1. **AI 전략 추가**: AIBehavior.AIStrategy enum에 새 전략 추가
2. **공격 타입 추가**: AttackTargetType enum 확장
3. **난이도 조정**: AIBehavior.difficultyParameters 수정

## 📝 복잡한 로직 주석 추가 (2025년 1월)

### 주요 메서드 설명 추가
1. **MergeManager.ExecuteMerge()**
   - 3개 캐릭터 합성의 전체 프로세스 설명
   - 데이터베이스 선택 로직
   - 스탯 배율 계산 방식
   - 컴포넌트 복사 및 정리 과정

2. **PlacementManager.SummonCharacterOnTile()**
   - 50마리 제한 체크
   - 같은 캐릭터 3개까지 배치 규칙
   - Missing Component 자동 복구 메커니즘
   - UI 프리팹의 월드 공간 변환 처리

3. **CharacterCombat.FindTarget()**
   - AttackTargetType별 타겟팅 로직
   - 거리 기반 우선순위 시스템
   - 다중 타겟 타입 처리 방식

4. **WaveSpawner.SpawnMonster()**
   - 챕터별 몬스터 선택 로직
   - 3라인 경로 할당 시스템
   - 이벤트 기반 상태 추적

## 🐛 알려진 이슈

1. **트레일 렌더러**: Bullet 풀링 시 트레일이 남을 수 있음
   - 해결: OnObjectSpawn()에서 trailRenderer.Clear() 호출

2. **동시 병합**: 여러 캐릭터가 동시에 병합될 때 간헐적 오류
   - 해결: AutoMergeManager의 재귀 방지 로직 추가

---
최종 업데이트: 2025년 1월
작성자: Claude Code Assistant