# 🎮 Twelve 프로젝트 메뉴 시스템 가이드

## 📋 메뉴 정리 완료 보고서

**이전 프로젝트명들**(TacticalTileGame, GuildMaster)이 혼재되었던 Unity 상단 메뉴를 **"Twelve" 프로젝트명에 맞게 통일**했습니다!

## 🎯 새로운 메뉴 구조

```
🎮 Twelve/
├── 📖 About Twelve Project          # 프로젝트 정보
├── 🎮 Twelve Project Hub           # 통합 도구 허브 (창)
├── ⚡ Quick Tools/                 # 빠른 도구들
│   ├── MOV 투명배경 설정 가이드      # MOV 투명배경 설정 단계별 안내
│   └── Clean Project              # 프로젝트 정리
├── 📊 Data Management/             # 데이터 관리 시스템 (통합)
│   ├── Import Character Data       # 캐릭터 데이터 가져오기
│   ├── Export Character Data       # 캐릭터 데이터 내보내기
│   ├── CSV Data Converter         # CSV 데이터 변환기
│   ├── CSV 임포터                  # CSV 파일 임포트 도구
│   ├── 빠른 캐릭터 임포트            # 캐릭터 데이터 빠른 임포트
│   ├── 빠른 대화 임포트             # 대화 데이터 빠른 임포트
│   ├── 빠른 건물 임포트             # 건물 데이터 빠른 임포트
│   ├── 빠른 스킬 임포트             # 스킬 데이터 빠른 임포트
│   ├── CSV → SO 변환기             # CSV를 ScriptableObject로 변환
│   ├── 데이터 내보내기 관리자        # 데이터 내보내기 관리 도구
│   ├── 빠른 데이터 내보내기 (Ctrl+Shift+E) # 빠른 데이터 내보내기
│   ├── CSV 데이터 동기화 관리자      # CSV 데이터 동기화 도구
│   ├── 빠른 CSV 동기화 (Ctrl+Shift+S) # 빠른 CSV 동기화
│   ├── CSV 데이터 생성              # CSV 데이터 생성 도구
│   ├── Full System Sync           # 전체 시스템 동기화
│   └── Validate System            # 시스템 검증
├── 🎬 Animation Tools/             # 애니메이션 도구들
│   └── MOV 투명배경 설정 도구
├── 🛠️ Development Tools/           # 개발 도구들
│   ├── Attack Pattern Visualizer  # 공격 패턴 시각화 도구
│   ├── Create Default Characters  # 기본 캐릭터 생성
│   ├── Assign Character Pools     # 캐릭터 풀 할당
│   ├── Missing Prefab Fixer      # 누락된 프리팹 수정
│   ├── Fix Serialization Issues  # 직렬화 문제 수정
│   ├── Force Refresh Scene       # 씬 강제 새로고침
│   ├── Clear Selection and Reload Inspectors # 선택 해제 및 인스펙터 재로드
│   ├── 인스펙터 정리 실행           # 네트워크 컴포넌트 플래그 정리
│   ├── 자동 인스펙터 정리 활성화     # 자동 정리 토글
│   └── 시작 시 자동 수정 수동 실행   # 시작 시 오브젝트 수정
└── ❓ Help/                       # 도움말 및 지원
    ├── Menu Structure             # 메뉴 구조 설명
    └── Troubleshooting           # 문제 해결 가이드
```

## 🔄 변경사항 요약

### ✅ **정리된 메뉴들**

| 이전 메뉴 | 새 메뉴 | 상태 |
|-----------|---------|------|
| `TacticalTileGame/CSV Data Converter` | `Twelve/📊 Data Management/CSV Data Converter` | ✅ 변경완료 |
| `Tools/CSV System/*` | `Twelve/📊 Data Management/*` | ✅ 변경완료 |
| `Tools/Fusion/*` | `Twelve/🛠️ Development Tools/*` | ✅ 변경완료 |
| `Tools/Cleanup/*` | `Twelve/🛠️ Development Tools/*` | ✅ 변경완료 |
| `Tools/Video Character/*` | `Twelve/🎬 Animation Tools/*` | ✅ 변경완료 (MOV 전용) |
| `GuildMaster/*` | `Twelve/📊 Data Management/*` | ✅ 변경완료 (한국어 메뉴명) |
| `Tools/GuildMaster/*` | `Twelve/📊 Data Management/*` | ✅ 변경완료 (한국어 메뉴명) |

### 🔄 **중복 제거 완료**

- ❌ **제거됨**: `Quick Tools/Create Test Character` (Data Management/Import Character Data와 중복)
- 🔄 **통합됨**: `CSV System` + `Data Tools` → `Data Management`
- 🔄 **통합됨**: `GuildMaster/*` → `Twelve/📊 Data Management/*`

### 🆕 **새로 추가된 메뉴들**

- ✨ **Twelve Project Hub** - 통합 도구 허브 창
- ✨ **Quick Tools** - 자주 사용하는 도구들 모음
- ✨ **Data Management** - CSV/데이터 도구들 통합
- ✨ **Development Tools** - 개발/디버깅 도구들 통합
- ✨ **Help** - 메뉴 구조 설명 및 문제 해결 가이드

### 🔒 **그대로 유지된 메뉴들**

- 🔌 **Tools/Pixel Crushers/** - 외부 플러그인 (그대로 유지)
- 🔌 **Tools/Easy Save 3/** - 외부 플러그인 (그대로 유지)

## 🚀 주요 기능

### 1. **🎮 Twelve Project Hub**
- **통합 도구 창**: 모든 주요 기능을 한 곳에서 접근
- **GUI 버튼 인터페이스**: 클릭 한 번으로 도구 실행
- **프로젝트 정보 표시**: 개발 현황 한눈에 파악

### 2. **⚡ Quick Tools**
- **MOV 투명배경 설정 가이드**: 투명배경 MOV 파일 설정 단계별 안내 (투명배경 전용)
- **Clean Project**: 프로젝트 정리 및 새로고침

### 3. **📊 Data Management** (통합됨!)
- **Import Character Data**: CSV에서 캐릭터 데이터 가져오기 (게임용)
- **Export Character Data**: 캐릭터 데이터를 CSV로 내보내기
- **CSV Data Converter**: CSV 파일을 ScriptableObject로 변환
- **CSV 임포터**: 통합 CSV 파일 임포트 도구
- **빠른 캐릭터 임포트**: 캐릭터 데이터만 빠르게 임포트
- **빠른 대화 임포트**: 대화 데이터만 빠르게 임포트
- **빠른 건물 임포트**: 건물 데이터만 빠르게 임포트
- **빠른 스킬 임포트**: 스킬 데이터만 빠르게 임포트
- **CSV → SO 변환기**: CSV를 ScriptableObject로 변환하는 도구
- **데이터 내보내기 관리자**: 데이터 내보내기 관리 도구
- **빠른 데이터 내보내기**: 단축키로 빠른 데이터 내보내기 (Ctrl+Shift+E)
- **CSV 데이터 동기화 관리자**: CSV 데이터 동기화 도구
- **빠른 CSV 동기화**: 단축키로 빠른 CSV 동기화 (Ctrl+Shift+S)
- **CSV 데이터 생성**: CSV 데이터 생성 도구
- **Full System Sync**: 전체 데이터 시스템 동기화
- **Validate System**: 데이터 시스템 유효성 검사

### 4. **🛠️ Development Tools**
- **Attack Pattern Visualizer**: 캐릭터 공격 패턴 시각화 도구
- **Create Default Characters**: 기본 캐릭터 세트 자동 생성
- **Assign Character Pools**: 캐릭터 풀 할당 도구
- **Missing Prefab Fixer**: 누락된 프리팹 참조 수정 도구
- **Fix Serialization Issues**: Unity 직렬화 문제 해결
- **Force Refresh Scene**: 씬 강제 새로고침 (문제 해결용)
- **Clear Selection and Reload Inspectors**: 선택 해제 및 인스펙터 재로드
- **인스펙터 정리 실행**: 네트워크 컴포넌트 플래그 정리
- **자동 인스펙터 정리 활성화**: 자동 플래그 정리 토글
- **시작 시 자동 수정 수동 실행**: 프로젝트 시작 시 오브젝트 자동 수정

### 5. **❓ Help System**
- **Menu Structure**: 전체 메뉴 구조 설명
- **Troubleshooting**: 자주 발생하는 문제들의 해결책

## 📖 사용법

### **기본 사용법**
1. Unity 상단 메뉴에서 **`Twelve`** 클릭
2. 원하는 기능 선택
3. Console에서 결과 확인

### **허브 창 사용법**
1. **`Twelve → 🎮 Twelve Project Hub`** 클릭
2. **GUI 창**에서 버튼으로 기능 실행
3. **실시간 피드백** 확인

### **빠른 접근**
- **캐릭터 데이터 생성**: `Twelve → 📊 Data Management → Import Character Data`
- **빠른 캐릭터 임포트**: `Twelve → 📊 Data Management → 빠른 캐릭터 임포트`
- **빠른 데이터 내보내기**: `Ctrl+Shift+E` 또는 `Twelve → 📊 Data Management → 빠른 데이터 내보내기`
- **빠른 CSV 동기화**: `Ctrl+Shift+S` 또는 `Twelve → 📊 Data Management → 빠른 CSV 동기화`
- **MOV 투명배경 설정**: `Twelve → 🎬 Animation Tools → MOV 투명배경 설정 도구`
- **MOV 설정 가이드**: `Twelve → ⚡ Quick Tools → MOV 투명배경 설정 가이드`
- **개발 도구**: `Twelve → 🛠️ Development Tools`
- **문제 해결**: `Twelve → ❓ Help → Troubleshooting`

## 🔧 개발자 정보

### **네임스페이스 정리**
- **새 네임스페이스**: `Twelve.Editor`
- **기존 네임스페이스**: `GuildMaster.*`, `TacticalTileGame.*` (유지)
- **메뉴만 통일**, 코드 구조는 안전하게 유지

### **파일 위치**
- **메인 도구**: `Assets/Editor/TwelveProjectTools.cs`
- **가이드 문서**: `Assets/Editor/TWELVE_MENU_GUIDE.md`

## 🎊 완성!

이제 **Twelve 프로젝트에 맞는 일관된 메뉴 시스템**이 완성되었습니다!

### ✅ **혜택**
- 🎯 **일관된 네이밍**: 모든 도구가 "Twelve" 브랜드로 통일
- 🚀 **향상된 접근성**: 직관적인 카테고리 분류
- 📋 **체계적 정리**: 기능별로 논리적 그룹핑
- 💡 **도움말 시스템**: 내장된 가이드와 문제 해결

### 🎮 **시작하기**
Unity 메뉴에서 **`Twelve → 🎮 Twelve Project Hub`**를 클릭해보세요!

---

> 📝 **마지막 업데이트**: 2024-12-25  
> 🔄 **버전**: v1.0  
> 👨‍💻 **정리자**: Twelve 프로젝트 팀 