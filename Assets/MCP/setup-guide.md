# 🎮 Unity MCP 서버 설정 완료!

축하합니다! Unity MCP 서버가 성공적으로 설치되었습니다.

## 🔧 다음 단계

### 1. Cursor AI에서 Unity MCP 사용하기

**Cursor 설정 파일 위치:**
- Windows: `%APPDATA%\Cursor\User\globalStorage\cursor.mcp\config.json`

**설정 내용:**
```json
{
  "mcpServers": {
    "unity-game-server": {
      "command": "node",
      "args": ["./Assets/MCP/unity-mcp-server.js"],
      "cwd": "C:/Users/super/OneDrive/문서/GitHub/Twelve",
      "env": {
        "UNITY_PROJECT_PATH": ".",
        "NODE_ENV": "development"
      }
    }
  }
}
```

### 2. Claude Desktop에서 Unity MCP 사용하기

**Claude Desktop 설정 파일 위치:**
- Windows: `%APPDATA%\Claude\claude_desktop_config.json`

**설정 내용:**
```json
{
  "mcpServers": {
    "unity-game-server": {
      "command": "node",
      "args": ["unity-mcp-server.js"],
      "cwd": "C:/Users/super/OneDrive/문서/GitHub/Twelve/Assets/MCP",
      "env": {
        "UNITY_PROJECT_PATH": "../.."
      }
    }
  }
}
```

## 💡 사용법

설정 완료 후 Cursor AI나 Claude Desktop에서 다음과 같이 요청하세요:

- "Unity 프로젝트를 분석해주세요"
- "GameManager 스크립트를 보여주세요"  
- "Scripts 폴더의 모든 파일을 나열해주세요"
- "CharacterData 클래스의 참조를 찾아주세요"

## 🚨 중요사항

1. **Cursor나 Claude Desktop을 재시작**해야 MCP 설정이 적용됩니다.
2. Node.js 버전 18 이상이 필요합니다.
3. Unity 프로젝트 경로에 한글이 포함되어 있어도 정상 작동합니다.

---

🎉 **이제 AI와 Unity 프로젝트가 직접 소통할 수 있습니다!** 