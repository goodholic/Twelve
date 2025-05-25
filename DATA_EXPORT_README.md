# Unity 데이터 생성 및 CSV 내보내기 시스템

## 개요
Unity 에디터에서 아이템, 아군 2성/3성 캐릭터, 적 2성/3성 캐릭터 데이터를 생성하고, 이를 CSV 파일로 내보내는 시스템입니다.

## 사용 방법

### 1. Unity 에디터에서 데이터 생성

#### 1-1. 데이터 내보내기 관리자
- Unity 메뉴: `Tools > Data Export Manager`
- 기능:
  - 새로운 아이템 생성
  - 새로운 적 캐릭터 생성 (2성/3성)
  - 새로운 아군 캐릭터 생성 (2성/3성)
  - 새로운 1성 캐릭터 생성 (아군/적)
  - 현재 데이터 개수 확인
  - CSV 파일로 내보내기

#### 1-2. 샘플 데이터 생성기
- Unity 메뉴: `Tools > Generate Sample Data` (적 캐릭터용)
- Unity 메뉴: `Tools > Generate Ally Sample Data` (아군 캐릭터용)
- 기능:
  - 새로운 아이템 10개 자동 생성
  - 아군 1성 캐릭터 10개 자동 생성
  - 적 1성 캐릭터 10개 자동 생성
  - 적 2성 캐릭터 15개 자동 생성
  - 적 3성 캐릭터 10개 자동 생성
  - 아군 2성 캐릭터 15개 자동 생성
  - 아군 3성 캐릭터 10개 자동 생성
  - 모든 샘플 데이터 한번에 생성

### 2. CSV ↔ ScriptableObject 양방향 동기화

#### 2-1. CSV Data Sync Manager 사용
- Unity 메뉴: `Tools > CSV Data Sync Manager`
- 기능:
  - **자동 동기화**: CSV 파일 변경을 2초마다 자동 감지
  - **CSV → ScriptableObject**: CSV 파일의 데이터를 Unity로 가져오기
  - **ScriptableObject → CSV**: Unity 데이터를 CSV로 내보내기
  - **파일 상태 모니터링**: 각 CSV 파일의 마지막 수정 시간 표시

#### 2-2. CSV 파일 가져오기 (CSV → Unity)
1. `Tools > CSV Data Sync Manager` 열기
2. "CSV → ScriptableObject 가져오기" 섹션에서:
   - "모든 CSV 파일 가져오기": 모든 CSV 파일을 한번에 가져오기
   - 개별 가져오기: 특정 데이터만 선택적으로 가져오기
3. CSV 파일 위치: `Assets/CSV/` 폴더

#### 2-3. CSV 파일 내보내기 (Unity → CSV)
1. `Tools > CSV Data Sync Manager` 열기
2. "ScriptableObject → CSV 내보내기" 섹션에서:
   - "모든 데이터를 CSV로 내보내기" 클릭
3. CSV 파일들이 `Assets/CSV/` 폴더에 생성됨

#### 2-4. 자동 동기화 설정
1. "자동 동기화" 토글 활성화
2. CSV 파일이 변경되면 자동으로 감지하고 가져오기 대화상자 표시
3. 파일 변경 체크 주기: 2초

### 3. 기존 내보내기 방법

#### 3-1. Unity 에디터에서 내보내기
1. `Tools > Data Export Manager` 열기
2. "모든 데이터를 CSV로 내보내기" 버튼 클릭
3. 프로젝트 루트 폴더에 CSV 파일들이 생성됨

#### 3-2. Python 스크립트로 내보내기
1. 프로젝트 루트 폴더에서 `run_export.bat` 실행
2. 또는 직접 `python export_unity_data_to_excel.py` 실행

## 생성되는 CSV 파일들

### 아이템 데이터
- **파일명**: `items_new.csv`
- **컬럼**: 아이템명, 효과 타입, 효과 값, 설명, 아이콘, 범위 반경, 최소 별, 최대 별, 데미지 값

### 1성 캐릭터 데이터
- **아군 1성**: `ally_one_star_characters.csv`
- **적 1성**: `enemy_one_star_characters.csv`
- **컬럼**: 이름, 초기 별, 종족, 공격력, 공격속도, 공격범위, 최대 HP, 이동속도, 공격 타입, 광역공격, 비용

### 아군 캐릭터 데이터 (새로운)
- **아군 2성**: `ally_two_star_characters.csv`
- **아군 3성**: `ally_three_star_characters.csv`
- **컬럼**: 이름, 초기 별, 종족, 공격력, 공격속도, 공격범위, 최대 HP, 이동속도, 공격 타입, 광역공격, 비용, 가중치

### 적 캐릭터 데이터
- **적 2성**: `enemy_two_star_characters.csv`
- **적 3성**: `enemy_three_star_characters.csv`
- **컬럼**: 이름, 초기 별, 종족, 공격력, 공격속도, 공격범위, 최대 HP, 이동속도, 공격 타입, 광역공격, 비용, 가중치

### 기존 데이터 (참고용)
- **아군 캐릭터 (전체)**: `ally_characters.csv`
- **적 캐릭터 (전체)**: `enemy_characters.csv`
- **아군 2성 (기존)**: `two_star_characters.csv`
- **아군 3성 (기존)**: `three_star_characters.csv`

## 데이터 구조

### 아이템 효과 타입
- **0**: 공격력 증가
- **1**: HP 증가
- **2**: 사거리 증가
- **3**: 텔레포트 (점프한 적들을 무작위 위치로 이동)
- **4**: 데미지 (점프한 적들에게 데미지)
- **5**: 소환 (2~3성 유닛 랜덤 소환)

### 캐릭터 종족
- **0**: Human (인간)
- **1**: Orc (오크)
- **2**: Elf (엘프)

### 공격 타입
- **0**: Melee (근접)
- **1**: Ranged (원거리)
- **2**: LongRange (장거리)

## 주의사항

1. **데이터 파일 위치**:
   - 아이템: `Assets/Prefabs/Data/NewItemDatabase.asset`
   - 적 2성/3성: `Assets/Prefabs/Data/OPStarMergeDatabase 1.asset`
   - 아군 2성/3성: `Assets/Prefabs/Data/StarMergeDatabase.asset`
   - 아군 전체: `Assets/Prefabs/Data/CharacterDatabase.asset`
   - 적 전체: `Assets/Prefabs/Data/opponentCharacterDatabase.asset`

2. **CSV 파일 위치**: 
   - 동기화용 CSV 파일들은 `Assets/CSV/` 폴더에 저장
   - 기존 내보내기는 프로젝트 루트 폴더에 저장

3. **인코딩**: 모든 CSV 파일은 UTF-8 BOM으로 저장되어 한글이 정상적으로 표시됩니다.

4. **백업**: 데이터 생성 전에 기존 .asset 파일들을 백업해두는 것을 권장합니다.

5. **CSV 편집 시 주의사항**:
   - Excel에서 편집 시 UTF-8 인코딩 유지 필요
   - 콤마(,)가 포함된 텍스트는 큰따옴표("")로 감싸기
   - 숫자 값은 소수점 형식 유지 (예: 1.0, 2.5)

## 샘플 데이터 예시

### 새로운 아이템들
- 화염 검 (공격력 +5)
- 생명의 물약 (HP +50)
- 정밀 조준경 (사거리 +1.5)
- 순간이동 스크롤 (텔레포트 효과)
- 번개 구슬 (데미지 50)
- 소환 부적 (2~3성 유닛 소환)

### 새로운 아군 1성 캐릭터들
- 견습 기사, 신병 궁수, 수습 마법사
- 오크 신병, 오크 투석병, 엘프 정찰병
- 엘프 견습생, 민병대원, 촌락 수비대
- 숲의 수호자 등 10개 캐릭터

### 새로운 적 1성 캐릭터들
- 고블린 전사, 고블린 궁수, 스켈레톤
- 다크 엘프, 오크 약탈자, 좀비
- 임프, 도적, 늑대인간
- 어둠의 마법사 등 10개 캐릭터

### 새로운 적 2성 캐릭터들
- 오크 전사, 엘프 궁수, 인간 기사
- 오크 주술사, 엘프 마법사, 인간 성기사
- 오크 광전사, 엘프 레인저, 인간 십자군
- 기타 15개 캐릭터

### 새로운 적 3성 캐릭터들
- 오크 대족장, 엘프 대마법사, 인간 영웅
- 오크 파괴자, 엘프 현자, 인간 성왕
- 기타 10개 캐릭터

### 새로운 아군 2성 캐릭터들
- 용맹한 기사, 정예 궁수, 성기사
- 오크 전사, 오크 투척병, 오크 주술사
- 엘프 레인저, 엘프 드루이드, 엘프 마법사
- 기타 15개 캐릭터

### 새로운 아군 3성 캐릭터들
- 성왕, 대마법사, 신관, 영웅
- 오크 대족장, 오크 전쟁군주, 오크 대주술사
- 엘프 현자, 엘프 궁신, 엘프 대드루이드
- 기타 10개 캐릭터

## 문제 해결

### Python 스크립트 실행 오류
- pandas 라이브러리 설치: `pip install pandas`
- 파일 경로 확인: Unity 프로젝트 루트에서 실행

### Unity 에디터 오류
- 스크립트 컴파일 오류 확인
- .asset 파일 경로 확인
- Unity 에디터 재시작

### CSV 파일 한글 깨짐
- UTF-8 BOM 인코딩으로 저장됨
- Excel에서 열 때 "데이터 > 텍스트 나누기" 사용
- 또는 메모장에서 UTF-8로 다시 저장 