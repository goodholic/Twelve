@echo off
echo 🎮 Unity 로그를 터미널에 출력하며 실행합니다...

REM Unity 설치 경로 (버전에 맞게 수정하세요)
set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\2022.3.50f1\Editor\Unity.exe"

REM 프로젝트 경로 (2단계 상위 디렉토리)
set PROJECT_PATH="%~dp0..\.."

REM 로그 파일 경로
set LOG_FILE="%~dp0..\..\Logs\unity-editor.log"

REM 로그 디렉토리 생성
if not exist "%~dp0..\..\Logs" mkdir "%~dp0..\..\Logs"

echo 📁 프로젝트 경로: %PROJECT_PATH%
echo 📄 로그 파일: %LOG_FILE%
echo.

echo ⚡ Unity 에디터를 시작합니다...
echo 💡 Unity가 실행되는 동안 로그 파일을 모니터링하려면 새 터미널에서 다음 명령어를 실행하세요:
echo Get-Content -Path %LOG_FILE% -Wait -Tail 20
echo.

REM Unity 실행 (로그 파일에 출력)
%UNITY_PATH% -projectPath %PROJECT_PATH% -logFile %LOG_FILE%

echo.
echo ✅ Unity가 종료되었습니다.
pause 