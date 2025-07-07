# Editor Scripts 폴더

이 폴더는 Unity 에디터에서만 실행되는 개발 도구 스크립트들을 포함합니다.

## 스크립트 목록

### 📄 CSVDataGenerator.cs
- **역할**: CSV 데이터 파일 자동 생성
- **기능**: 
  - 캐릭터 데이터 CSV 생성
  - 건물 데이터 CSV 생성
  - 건물 효과 CSV 생성
  - 건물 생산 CSV 생성
  - 스킬 데이터 CSV 생성
- **메뉴**: GuildMaster > Generate CSV Data

### 📄 CSVImporter.cs
- **역할**: CSV 파일을 Unity로 가져오기
- **기능**: CSV 데이터를 ScriptableObject로 변환
- **메뉴**: GuildMaster > Import CSV Data

### 📄 CSVToScriptableObjectConverter.cs
- **역할**: CSV를 ScriptableObject로 변환
- **기능**: CSV 데이터를 Unity 에셋으로 변환
- **사용법**: CSV 파일을 드래그 앤 드롭하여 변환

### 📄 CSVDataSyncManager.cs
- **역할**: CSV 데이터 동기화 관리
- **기능**: 
  - CSV 파일 변경 감지
  - 자동 데이터 업데이트
  - 데이터 일관성 검사

### 📄 DataExportManager.cs
- **역할**: 게임 데이터 내보내기
- **기능**: 
  - 플레이어 데이터 백업
  - 설정 데이터 내보내기
  - 통계 데이터 추출

### 📄 GuildMasterToolbar.cs
- **역할**: GuildMaster 전용 툴바
- **기능**: 
  - 자주 사용하는 도구들에 대한 빠른 접근
  - 개발 워크플로우 최적화

### 📄 SampleDataGenerator.cs
- **역할**: 샘플 데이터 생성
- **기능**: 
  - 테스트용 캐릭터 데이터 생성
  - 샘플 아이템 데이터 생성
  - 개발 및 테스트용 데이터

### 📄 JobLevelDataEditor.cs
- **역할**: 직업 레벨 데이터 에디터
- **기능**: 
  - 직업별 레벨업 데이터 편집
  - 경험치 요구량 설정
  - 스킬 해금 조건 설정

## 사용법

### CSV 데이터 생성
1. Unity 메뉴에서 `GuildMaster > Generate CSV Data` 선택
2. 원하는 데이터 타입 선택
3. CSV 파일이 `Assets/CSV/` 폴더에 생성됨

### 데이터 가져오기
1. CSV 파일을 `Assets/CSV/` 폴더에 배치
2. `GuildMaster > Import CSV Data` 선택
3. ScriptableObject로 변환됨

### 툴바 사용
- Unity 에디터 상단에 GuildMaster 툴바가 표시됨
- 자주 사용하는 기능들에 빠르게 접근 가능

## 주의사항

1. **에디터 전용**: 이 스크립트들은 빌드에 포함되지 않습니다
2. **네임스페이스**: `GuildMaster.Editor` 네임스페이스 사용
3. **의존성**: `UnityEditor` 네임스페이스 필요
4. **컴파일 순서**: 다른 스크립트보다 먼저 컴파일되어야 함

## 확장 방법

새로운 에디터 도구를 추가할 때:
1. `GuildMaster.Editor` 네임스페이스 사용
2. `[MenuItem]` 속성으로 메뉴 항목 추가
3. 적절한 에러 처리 및 로깅 추가
4. README에 문서화 