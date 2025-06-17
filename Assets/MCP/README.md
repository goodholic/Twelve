# Unity MCP 서버 설정 가이드

이 가이드는 Cursor AI와 Claude Desktop에서 Unity 프로젝트와 연결할 수 있는 MCP(Model Context Protocol) 서버를 설정하는 방법을 설명합니다.

## 🚀 빠른 시작

### 1. 의존성 설치

먼저 Node.js가 설치되어 있는지 확인하세요 (버전 18 이상 권장).

```bash
# Unity 프로젝트 루트에서 실행
cd Assets/MCP
npm install
```

### 2. MCP 서버 테스트

```bash
# MCP 서버 직접 실행 테스트
node unity-mcp-server.js
```

## 🎯 Cursor AI 설정

### 방법 1: Cursor 설정 파일 직접 수정

1. Cursor의 설정 파일을 엽니다:
   - **Windows**: `%APPDATA%\Cursor\User\globalStorage\cursor.mcp\config.json`
   - **macOS**: `~/Library/Application Support/Cursor/User/globalStorage/cursor.mcp/config.json`
   - **Linux**: `~/.config/Cursor/User/globalStorage/cursor.mcp/config.json`

2. 다음 내용을 추가하거나 기존 내용과 병합합니다:

```json
{
  "mcpServers": {
    "unity-game-server": {
      "command": "node",
      "args": ["./Assets/MCP/unity-mcp-server.js"],
      "cwd": "YOUR_UNITY_PROJECT_PATH",
      "env": {
        "UNITY_PROJECT_PATH": ".",
        "NODE_ENV": "development"
      }
    }
  }
}
```

`YOUR_UNITY_PROJECT_PATH`를 실제 Unity 프로젝트 경로로 변경하세요.

### 방법 2: 프로젝트별 설정 (권장)

Unity 프로젝트 루트에 `.cursor-mcp.json` 파일을 생성하고 다음 내용을 추가합니다:

```json
{
  "mcpServers": {
    "unity-game-server": {
      "command": "node",
      "args": ["./Assets/MCP/unity-mcp-server.js"],
      "cwd": ".",
      "env": {
        "UNITY_PROJECT_PATH": ".",
        "NODE_ENV": "development"
      }
    }
  }
}
```

## 🤖 Claude Desktop 설정

1. Claude Desktop의 설정 파일을 엽니다:
   - **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
   - **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
   - **Linux**: `~/.config/claude/claude_desktop_config.json`

2. 다음 내용을 추가하거나 기존 내용과 병합합니다:

```json
{
  "mcpServers": {
    "unity-game-server": {
      "command": "node",
      "args": ["unity-mcp-server.js"],
      "cwd": "YOUR_UNITY_PROJECT_PATH/Assets/MCP",
      "env": {
        "UNITY_PROJECT_PATH": "../.."
      }
    }
  }
}
```

`YOUR_UNITY_PROJECT_PATH`를 실제 Unity 프로젝트의 절대 경로로 변경하세요.

## 🛠️ 사용 가능한 MCP 도구들

MCP 서버가 성공적으로 연결되면 다음 도구들을 사용할 수 있습니다:

### 📄 `read_unity_script`
Unity C# 스크립트 파일을 읽습니다.
```
매개변수: scriptPath (예: "Scripts/GameManager.cs")
```

### 📋 `list_unity_scripts`
Unity 프로젝트의 모든 C# 스크립트를 나열합니다.
```
매개변수: folder (선택사항, 예: "Scripts")
```

### ✏️ `write_unity_script`
Unity C# 스크립트 파일을 생성하거나 수정합니다.
```
매개변수: 
- scriptPath: 파일 경로
- content: 스크립트 내용
```

### 🔍 `analyze_unity_project`
Unity 프로젝트 구조를 분석합니다.

### 🔎 `find_unity_references`
특정 클래스나 메서드의 참조를 찾습니다.
```
매개변수: searchTerm (검색할 용어)
```

### 🎮 `get_game_state`
현재 Unity 게임 상태를 가져옵니다.

## 💡 사용 예시

Cursor AI나 Claude Desktop에서 다음과 같이 요청할 수 있습니다:

```
"Unity 프로젝트의 GameManager 스크립트를 보여주세요"
"Scripts 폴더의 모든 C# 파일을 나열해주세요"
"Unity 프로젝트를 분석해주세요"
"CharacterData 클래스가 어디서 사용되는지 찾아주세요"
```

## 🔧 문제 해결

### MCP 서버가 연결되지 않는 경우

1. Node.js가 올바르게 설치되었는지 확인
2. 의존성이 설치되었는지 확인: `npm install`
3. Unity 프로젝트 경로가 올바른지 확인
4. Cursor/Claude를 재시작

### 권한 오류가 발생하는 경우

1. 스크립트에 실행 권한 부여:
   ```bash
   chmod +x Assets/MCP/unity-mcp-server.js
   ```

2. Unity 프로젝트 폴더에 읽기/쓰기 권한이 있는지 확인

## 📝 로그 확인

MCP 서버의 로그는 Cursor나 Claude Desktop의 개발자 도구에서 확인할 수 있습니다.

---

🎮 **이제 Cursor AI와 Claude Desktop에서 Unity 프로젝트와 직접 소통할 수 있습니다!** 