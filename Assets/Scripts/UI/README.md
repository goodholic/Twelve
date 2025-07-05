# UI Scripts 폴더 구조

이 폴더는 게임의 모든 UI 관련 스크립트를 포함합니다.

## 폴더 구조

### 주요 UI 관리자
- `UIManager.cs` - 전체 UI 시스템 관리
- `GameSceneManager.cs` - 씬 전환 및 게임 상태 관리
- `GameStateUI.cs` - 게임 상태 표시 UI
- `LoadingScreen.cs` - 로딩 화면 관리

### 메뉴 및 네비게이션
- `MainMenuUI.cs` - 메인 메뉴 UI
- `LobbySceneManager.cs` - 로비 씬 관리
- `IntroManager.cs` - 인트로 화면 관리
- `SettingsUI.cs` - 설정 화면 UI

### 게임플레이 UI
- `CharacterInventoryManager.cs` - 캐릭터 인벤토리 관리
- `CharacterUpgradeManager.cs` - 캐릭터 업그레이드 UI
- `BattleUIManager.cs` - 전투 UI 관리
- `ResourceUI.cs` - 자원 표시 UI
- `MineralBar.cs` - 미네랄 바 UI

### 가챠 및 드로우 시스템
- `GachaManager.cs` - 가챠 시스템 관리
- `DrawPanelManager.cs` - 드로우 패널 관리
- `DrawResultIconUI.cs` - 드로우 결과 아이콘 UI
- `DeckPanelManager.cs` - 덱 관리 패널

### 상점 및 거래
- `ShopManager.cs` - 상점 시스템 관리
- `ShopUI.cs` - 상점 UI
- `UpgradePanelManager.cs` - 업그레이드 패널 관리

### 기타 UI
- `NotificationUI.cs` - 알림 UI
- `TutorialManager.cs` - 튜토리얼 관리
- `BookPanelManager.cs` - 도감 패널 관리
- `RaceRecycleManager.cs` - 레이스 재활용 관리
- `JobSelectionUI.cs` - 직업 선택 UI
- `GuildMasterUI.cs` - 길드 마스터 UI
- `GameResolutionManager.cs` - 게임 해상도 관리

## 네임스페이스
모든 UI 스크립트는 `GuildMaster.UI` 네임스페이스를 사용합니다.

## 사용법
UI 스크립트들은 `UIManager`를 통해 중앙 집중식으로 관리됩니다.
```csharp
// UI 패널 표시
UIManager.Instance.ShowPanel("MainMenu");

// UI 패널 숨기기
UIManager.Instance.HidePanel("MainMenu");
``` 