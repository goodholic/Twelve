# 🎮 Twelve 프로젝트 도구 가이드 v3.0

## 📋 새로운 메뉴 구조 (역할 분할 완료!)

**🎯 Twelve vs Tools 메뉴 역할 분할**
- **Twelve 메뉴**: 게임 프로젝트 전용 도구들 (캐릭터, 배틀, PNG, 로비씬 등)
- **Tools 메뉴**: Unity 에디터 전용 도구들 (Inspector, Scene, Serialization 등)

### 🏠 Twelve 메뉴 (게임 프로젝트 전용)
```
Twelve/
├── 🏠 도구 관리자 ⭐ (모든 게임 도구의 중앙 허브)
├── 🎮 Twelve Project Hub (기존 프로젝트 도구)
├── 📖 About Twelve Project
├── 🖼️ PNG 도구/ (이미지 처리)
├── 👥 캐릭터 생성/ (캐릭터 생성 도구들)
├── 📊 Data Management/ (CSV 데이터 관리)
├── ⚡ Quick Tools/ (캐릭터 DB, 배틀씬)
├── 🔧 로비씬 문제해결/ (로비씬 전용)
└── ❓ Help/
```

### 🛠️ Tools 메뉴 (Unity 에디터 전용)
```
Tools/
├── Unity Editor/
│   ├── Fix Serialization Issues
│   └── Clear Selection and Reload Inspectors
├── Unity Scene/
│   └── Force Refresh Scene
├── Unity Inspector/
│   ├── 인스펙터 정리 실행
│   └── 자동 인스펙터 정리 활성화
└── Fusion/
    └── 시작 시 자동 수정 수동 실행
```

---

## 🏠 통합 도구 관리자 ⭐

**메뉴 위치**: `Twelve → 🏠 도구 관리자`

**🎯 가장 중요!** 모든 Twelve 프로젝트 도구들을 한 곳에서 관리하는 중앙 허브입니다.
게임 개발에 필요한 모든 도구를 여기서 처리할 수 있습니다!

### 🔧 문제 해결 탭

#### 📋 Missing Script 해결
- **🔍 Deep Missing Script Scanner**: 가장 강력한 Missing Script 자동 수정 도구
  - 씬과 프리팹의 모든 Missing Script 감지 및 수정
  - 99%의 Missing Script 문제 해결 가능
  - ✅ **TwelveToolsManager에서만 접근 (중복 제거)**

- **🧹 Third Party Cleaner**: Feel, Layer Lab 등 써드파티 에셋 정리
  - 써드파티와 핵심 게임 스크립트 구분하여 처리
  - 불필요한 써드파티 Missing Script 제거
  - ✅ **TwelveToolsManager에서만 접근 (중복 제거)**

#### 🖼️ Canvas 문제 해결
- **🚨 MMTouchControls Remover**: Canvas 비활성화 문제 해결
  - Canvas를 비활성화시키는 MMTouchControls 컴포넌트 제거
  - Feel 에셋의 MMTouchControls 자동 감지 및 제거
  - ✅ **TwelveToolsManager에서만 접근 (중복 제거)**

#### 🏠 로비 씬 수정
- **📹 Fix Lobby Scene Cameras**: 로비 씬 카메라 문제 해결
  - 로비 씬의 카메라 설정 자동 수정
  - 파란 화면 문제 해결
  - ✅ **TwelveToolsManager에서만 접근 (중복 제거)**

### 🎨 에셋 관리 탭

#### 🖼️ 이미지 처리 도구
- **🎬 PNG Sequence Tools**: PNG 시퀀스 애니메이션 생성
- **📏 PNG Resize Tools**: PNG 이미지 리사이즈 (메뉴 방식)

#### 👥 캐릭터 관리
- **➕ Character Creator**: 새 캐릭터 데이터 생성
  - ✅ **TwelveToolsManager에서만 접근 (중복 제거)**
- **✏️ Character Data Editor**: 기존 캐릭터 데이터 편집 (Inspector 기반)

#### 🚀 빠른 캐릭터 생성
- **🎬 PNG 시퀀스 캐릭터**: PNG 애니메이션용 캐릭터 데이터 생성
- **🧪 테스트 캐릭터**: 테스트용 샘플 캐릭터 데이터 생성  
- **⚡ 빠른 캐릭터 생성**: 모든 기본 캐릭터 일괄 생성

### 📊 프로젝트 유틸 탭
- **🎮 Twelve Project Tools**: 기존 프로젝트 유틸리티 모음
- **🔧 Unity Editor Tools 열기**: Tools 메뉴의 Unity 에디터 도구들 바로가기

---

## 🖼️ PNG 도구 (통합 완료!)

**메뉴 위치**: `Twelve → 🖼️ PNG 도구/`

모든 PNG 관련 도구들이 하나의 카테고리로 통합되었습니다!

### 📁 PNG 시퀀스 도구
- **PNG 시퀀스 자동 설정**: 자동 애니메이션 생성

### 📂 PNG 임포트 설정
- **📁 Video 폴더 PNG 설정 적용**: Video 폴더 일괄 적용
- **📂 폴더 선택하여 PNG 설정 적용**: 원하는 폴더 선택
- **🔍 Video 폴더 PNG 설정 확인**: 설정 상태 확인

### 📱 PNG 최적화
- **📱 모바일 최적화 (720x540)**: 모바일용 최적화
- **💻 PC 중간 품질 (960x720)**: PC용 중간 품질
- **🎮 균형 최적화 (840x630)**: 크로스 플랫폼 균형
- **📊 현재 크기 분석**: 현재 PNG 크기 분석
- **💾 원본 백업 생성**: 안전한 원본 백업

---

## 🔧 로비씬 문제해결 (정리 완료!)

**메뉴 위치**: `Twelve → 🔧 로비씬 문제해결/`

로비씬 관련 모든 도구들이 하나의 카테고리로 정리되었습니다!

### 📹 카메라 관련
- **카메라 배경색 수정 (파란 화면 해결)**: 파란 화면 문제 즉시 해결
- **모든 씬의 카메라 배경색 확인**: 전체 프로젝트 카메라 상태 점검
- **수동 카메라 수정 방법 안내**: 수동 수정 가이드

### 🖼️ Canvas 관련  
- **Canvas 활성화 문제 해결**: Canvas 비활성화 문제 해결

---

## ⚡ Quick Tools (핵심만 보존!)

**메뉴 위치**: `Twelve → ⚡ Quick Tools/`

게임 개발에 자주 사용하는 핵심 도구들만 남겨두었습니다!

### 🎮 캐릭터 관련
- **🎮 배틀용 캐릭터 DB 생성**: 배틀용 캐릭터 데이터베이스 생성
- **📊 캐릭터 DB 상태 확인**: 캐릭터 DB 상태 점검
- **📁 DB를 Resources로 복사**: Resources 폴더로 DB 복사
- **🔍 캐릭터 DB 상세 분석**: 상세한 캐릭터 분석

### 🏟️ 배틀씬 관련
- **📋 배틀씬 완전 설정 가이드**: 배틀씬 설정 가이드
- **🔧 배틀씬 자동 설정**: 배틀씬 자동 구성
- **🚑 배틀 씬 에러 해결**: 배틀씬 문제 해결
- **🛡️ 안전한 배틀씬 설정**: 안전한 배틀씬 구성

---

## 🛠️ Tools 메뉴 (Unity 에디터 전용)

**메뉴 위치**: `Tools → Unity 카테고리들`

Unity 에디터 관련 도구들이 Tools 메뉴로 완전히 이동되었습니다!

### 🔧 Unity Editor
- **Fix Serialization Issues**: 직렬화 문제 해결
  - 모든 씬의 더티 플래그 정리
  - 프리팹 연결 상태 확인
  - 에셋 데이터베이스 새로고침
- **Clear Selection and Reload Inspectors**: 선택 초기화 및 Inspector 재로드

### 🎬 Unity Scene
- **Force Refresh Scene**: 씬 강제 새로고침
  - 현재 씬을 다시 로드하여 상태 초기화
  - 직렬화 문제 해결

### 🔍 Unity Inspector
- **인스펙터 정리 실행**: Inspector 수동 정리
- **자동 인스펙터 정리 활성화**: 자동 Inspector 정리 토글

### ⚡ Fusion
- **시작 시 자동 수정 수동 실행**: Photon Fusion 관련 자동 수정

---

## 👥 캐릭터 생성 (정리 완료!)

**메뉴 위치**: `Twelve → 👥 캐릭터 생성/`

이미지에서 보이던 Tools 메뉴의 캐릭터 생성 도구들이 모두 Twelve 메뉴로 이동되었습니다!

### 🎬 PNG 시퀀스 관련
- **PNG 시퀀스 캐릭터 데이터 생성**: PNG 애니메이션용 캐릭터 생성
- **테스트 캐릭터 데이터 생성**: PNG 시퀀스 테스트용 캐릭터 생성

### ⚡ 빠른 생성
- **빠른 캐릭터 생성**: 모든 기본 캐릭터 일괄 생성

### ❌ 정리된 항목들
- ~~Tools/Create PNG Sequence Character Data~~ → **Twelve/👥 캐릭터 생성/** 으로 이동
- ~~Tools/Create Test Character Data~~ → **Twelve/👥 캐릭터 생성/** 으로 이동
- ~~Tools/Quick Create Characters~~ → **Twelve/👥 캐릭터 생성/** 으로 이동
- ~~Tools/Generate Characters from CSV~~ → **Twelve/📊 Data Management/** 와 통합

---

## 📊 Data Management (통합 완료!)

**메뉴 위치**: `Twelve → 📊 Data Management/`

CSV 관련 도구들이 모두 통합되어 중복이 제거되었습니다!

### 📁 CSV 데이터 관리
- **CSV 데이터 생성**: 새로운 CSV 데이터 생성
- **CSV 임포터**: CSV 파일에서 데이터 임포트
- **CSV → SO 변환기**: CSV를 ScriptableObject로 변환
- **CSV에서 캐릭터 생성**: CSV 파일에서 캐릭터 데이터 생성 (통합됨!)

### 🔄 동기화 도구
- **CSV 데이터 동기화 관리자**: 데이터 동기화 관리
- **빠른 CSV 동기화**: 빠른 동기화 실행

### 📤 내보내기
- **데이터 내보내기 관리자**: 데이터 내보내기 관리
- **빠른 데이터 내보내기**: 빠른 내보내기 실행

---

## 📖 사용 권장 순서

### 🆕 새 사용자
1. **`Twelve → 🏠 도구 관리자`** 열기 (게임 개발 도구)
2. **문제 해결 탭**에서 Missing Script 정리
3. **에셋 관리 탭**에서 캐릭터/PNG 작업
4. 필요시 **`Tools → Unity 카테고리들`** 사용 (에디터 도구)

### 🔧 문제 해결시
**게임 관련 문제**: Twelve 메뉴 사용
1. **🔍 Deep Missing Script Scanner** (가장 우선)
2. **🧹 Third Party Cleaner** (써드파티 정리)
3. **🚨 MMTouchControls Remover** (Canvas 문제)
4. **📹 Fix Lobby Scene Cameras** (로비씬 문제)

**Unity 에디터 문제**: Tools 메뉴 사용
1. **Tools → Unity Editor → Fix Serialization Issues**
2. **Tools → Unity Scene → Force Refresh Scene**
3. **Tools → Unity Inspector → 인스펙터 정리**

### 🎨 에셋 작업시
1. **➕ Character Creator** (캐릭터 생성)
2. **🖼️ PNG 도구** (이미지 처리)
3. **⚡ Quick Tools** (빠른 작업)

---

## 🎉 개선 사항 v3.0

### ✅ 완전한 역할 분할
```
이전: 모든 도구가 Twelve 메뉴에 섞여있음
Twelve/
├── 게임 도구들 + Unity 에디터 도구들 (혼재)
└── Development Tools/ (Unity + 게임 도구 혼합)

현재: 명확한 역할 분할
Twelve/ (게임 프로젝트 전용)
├── 🏠 도구 관리자 (게임 도구 중앙 허브)
├── 🖼️ PNG 도구/ (게임 에셋)
├── ⚡ Quick Tools/ (게임 기능)
├── 🔧 로비씬 문제해결/ (게임 씬)
└── ❓ Help/

Tools/ (Unity 에디터 전용)
├── Unity Editor/ (에디터 기능)
├── Unity Scene/ (씬 관리)
├── Unity Inspector/ (Inspector 관리)
└── Fusion/ (네트워킹)
```

### ✅ 중복 완전 제거
- **이전**: 같은 도구가 여러 곳에 분산
- **현재**: TwelveToolsManager 하나로 게임 도구 통합, Tools 메뉴로 에디터 도구 분할

### ✅ 카테고리 명확화
- **게임 관련**: Twelve 메뉴에서 관리
- **Unity 에디터 관련**: Tools 메뉴에서 관리
- **더 이상 혼동 없음**: 각 도구의 역할이 명확

### ✅ 접근성 향상
- **게임 개발자**: Twelve 메뉴만 사용하면 됨
- **Unity 전문가**: Tools 메뉴로 에디터 도구 직접 접근
- **TwelveToolsManager**: 게임 도구들의 원스톱 액세스

---

## 💡 팁

### 🚀 효율적인 작업 흐름
1. **게임 작업**: `Twelve → 🏠 도구 관리자`부터 시작
2. **Unity 에디터 문제**: `Tools → Unity 카테고리들` 직접 사용
3. **각 메뉴의 역할을 명확히 구분**

### 🎯 어떤 메뉴를 써야 할까?
- **캐릭터, 배틀, PNG, 로비씬 작업**: **Twelve 메뉴**
- **Inspector, Scene, Serialization 문제**: **Tools 메뉴**
- **모르겠으면**: **Twelve → 🏠 도구 관리자**에서 시작

**🎉 이제 Twelve 메뉴와 Tools 메뉴가 명확하게 분할되어 더욱 체계적으로 작업할 수 있습니다!** 