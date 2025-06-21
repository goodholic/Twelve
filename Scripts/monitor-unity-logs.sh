#!/bin/bash

# Unity Log Monitoring Script for WSL
# This script monitors Unity console logs in real-time from VS Code terminal

# Colors for output
RED='\033[0;31m'
YELLOW='\033[0;33m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
MAGENTA='\033[0;35m'
WHITE='\033[0;37m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

# Default values
LOG_PATH="./Logs/unity-console.log"
TAIL_LINES=20
ERRORS_ONLY=false

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -f|--file)
            LOG_PATH="$2"
            shift 2
            ;;
        -n|--lines)
            TAIL_LINES="$2"
            shift 2
            ;;
        -e|--errors-only)
            ERRORS_ONLY=true
            shift
            ;;
        -h|--help)
            echo "Usage: $0 [OPTIONS]"
            echo "Options:"
            echo "  -f, --file PATH      Path to Unity log file (default: ./Logs/unity-console.log)"
            echo "  -n, --lines NUM      Number of lines to show initially (default: 20)"
            echo "  -e, --errors-only    Show only errors and warnings"
            echo "  -h, --help           Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Convert relative path to absolute path if needed
if [[ ! "$LOG_PATH" = /* ]]; then
    LOG_PATH="$(pwd)/$LOG_PATH"
fi

echo -e "${GREEN}🎮 Unity Log Monitoring Starting...${NC}"
echo -e "${CYAN}📁 Log file: $LOG_PATH${NC}"

# Check if log file exists
if [ ! -f "$LOG_PATH" ]; then
    echo -e "${RED}❌ Log file not found: $LOG_PATH${NC}"
    echo -e "${YELLOW}💡 Waiting for Unity to create log file...${NC}"
    
    # Wait for log file to be created
    while [ ! -f "$LOG_PATH" ]; do
        sleep 1
    done
    echo -e "${GREEN}✅ Log file created!${NC}"
fi

echo -e "${YELLOW}🔍 Real-time log monitoring active... (Press Ctrl+C to stop)${NC}"
echo "============================================================"

# Function to colorize log output based on log level
colorize_output() {
    local line="$1"
    local timestamp=$(date +"%H:%M:%S")
    
    if [[ "$line" =~ ERROR|EXCEPTION ]]; then
        echo -e "${RED}[$timestamp] $line${NC}"
    elif [[ "$line" =~ WARNING ]]; then
        echo -e "${YELLOW}[$timestamp] $line${NC}"
    elif [[ "$line" =~ ASSERT ]]; then
        echo -e "${MAGENTA}[$timestamp] $line${NC}"
    elif [[ "$line" =~ INFO ]]; then
        echo -e "${WHITE}[$timestamp] $line${NC}"
    elif [[ "$line" =~ STACK ]]; then
        echo -e "${GRAY}[$timestamp] $line${NC}"
    else
        echo -e "${GRAY}[$timestamp] $line${NC}"
    fi
}

# Main monitoring loop
if [ "$ERRORS_ONLY" = true ]; then
    tail -n "$TAIL_LINES" -f "$LOG_PATH" | grep -E "ERROR|WARNING|EXCEPTION|ASSERT|STACK" | while IFS= read -r line; do
        colorize_output "$line"
    done
else
    tail -n "$TAIL_LINES" -f "$LOG_PATH" | while IFS= read -r line; do
        colorize_output "$line"
    done
fi