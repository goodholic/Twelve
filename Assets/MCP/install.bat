@echo off
echo 🎮 Unity MCP 서버 설치 시작...

REM Node.js 설치 확인
node --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ Node.js가 설치되지 않았습니다.
    echo 다음 링크에서 Node.js를 설치해주세요: https://nodejs.org/
    pause
    exit /b 1
)

echo ✅ Node.js 발견: 
node --version

REM 현재 디렉토리를 MCP 폴더로 변경
cd /d "%~dp0"

echo 📦 MCP SDK 설치 중...
npm install @modelcontextprotocol/sdk

if %errorlevel% neq 0 (
    echo ❌ 패키지 설치 실패
    pause
    exit /b 1
)

echo ✅ 설치 완료!
echo.
echo 🔧 다음 단계:
echo 1. Cursor AI 설정: README.md의 "Cursor AI 설정" 섹션 참조
echo 2. Claude Desktop 설정: README.md의 "Claude Desktop 설정" 섹션 참조
echo.
echo 📖 자세한 설정 방법은 README.md 파일을 확인하세요.

pause 