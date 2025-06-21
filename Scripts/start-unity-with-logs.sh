#!/bin/bash

# Start Unity Editor and monitor logs in split terminals

# Colors
GREEN='\033[0;32m'
CYAN='\033[0;36m'
YELLOW='\033[0;33m'
NC='\033[0m'

# Unity paths
UNITY_HUB="/mnt/c/Program Files/Unity/Hub/Editor"
PROJECT_PATH="/mnt/c/Users/super/OneDrive/문서/GitHub/Twelve"
LOG_MONITOR_SCRIPT="./Scripts/monitor-unity-logs.sh"

# Find Unity version
if [ -d "$UNITY_HUB" ]; then
    UNITY_VERSION=$(ls "$UNITY_HUB" | grep -E "^[0-9]+\." | head -1)
    if [ -z "$UNITY_VERSION" ]; then
        echo -e "${YELLOW}⚠️  No Unity Editor version found in Hub${NC}"
        exit 1
    fi
else
    echo -e "${YELLOW}⚠️  Unity Hub not found at expected location${NC}"
    exit 1
fi

UNITY_EXE="$UNITY_HUB/$UNITY_VERSION/Editor/Unity.exe"

echo -e "${GREEN}🎮 Starting Unity Editor and Log Monitoring${NC}"
echo -e "${CYAN}Unity Version: $UNITY_VERSION${NC}"
echo -e "${CYAN}Project: $PROJECT_PATH${NC}"
echo "============================================================"

# Start Unity in background via Windows
echo -e "${CYAN}Starting Unity Editor...${NC}"
cmd.exe /c start "" "$(wslpath -w "$UNITY_EXE")" -projectPath "$(wslpath -w "$PROJECT_PATH")" 2>/dev/null &

# Wait a bit for Unity to start
echo -e "${YELLOW}Waiting for Unity to initialize...${NC}"
sleep 5

# Start log monitoring
echo -e "${GREEN}Starting log monitoring...${NC}"
exec $LOG_MONITOR_SCRIPT "$@"