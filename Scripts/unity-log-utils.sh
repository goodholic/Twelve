#!/bin/bash

# Unity Log Utilities for WSL
# Various helper functions for Unity log management

# Colors
RED='\033[0;31m'
YELLOW='\033[0;33m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
WHITE='\033[0;37m'
NC='\033[0m'

LOG_DIR="./Logs"
LOG_FILE="$LOG_DIR/unity-console.log"

# Function to show log summary
show_log_summary() {
    echo -e "${CYAN}📊 Unity Log Summary${NC}"
    echo "========================"
    
    if [ -f "$LOG_FILE" ]; then
        local total_lines=$(wc -l < "$LOG_FILE" | tr -d ' ')
        local errors
        local warnings
        local info
        errors=$(grep -E -c "ERROR|EXCEPTION" "$LOG_FILE" 2>/dev/null) || errors=0
        warnings=$(grep -c "WARNING" "$LOG_FILE" 2>/dev/null) || warnings=0
        info=$(grep -c "INFO" "$LOG_FILE" 2>/dev/null) || info=0
        
        echo -e "Total lines: ${WHITE}$total_lines${NC}"
        echo -e "Errors: ${RED}$errors${NC}"
        echo -e "Warnings: ${YELLOW}$warnings${NC}"
        echo -e "Info: ${GREEN}$info${NC}"
        
        # Show last error if any
        if [ $errors -gt 0 ]; then
            echo -e "\n${RED}Last Error:${NC}"
            grep "ERROR\|EXCEPTION" "$LOG_FILE" | tail -1
        fi
    else
        echo -e "${RED}Log file not found: $LOG_FILE${NC}"
    fi
}

# Function to clear logs
clear_logs() {
    echo -e "${YELLOW}⚠️  Clear Unity logs?${NC}"
    read -p "This will backup current logs. Continue? (y/N) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        if [ -f "$LOG_FILE" ]; then
            backup_file="$LOG_FILE.backup.$(date +%Y%m%d_%H%M%S)"
            cp "$LOG_FILE" "$backup_file"
            echo -e "${GREEN}✅ Backup created: $backup_file${NC}"
            > "$LOG_FILE"
            echo -e "${GREEN}✅ Log file cleared${NC}"
        else
            echo -e "${RED}No log file to clear${NC}"
        fi
    fi
}

# Function to search logs
search_logs() {
    local pattern="$1"
    if [ -z "$pattern" ]; then
        echo -e "${RED}Usage: search_logs <pattern>${NC}"
        return 1
    fi
    
    echo -e "${CYAN}🔍 Searching for: $pattern${NC}"
    echo "========================"
    
    if [ -f "$LOG_FILE" ]; then
        grep -n --color=always "$pattern" "$LOG_FILE" | less -R
    else
        echo -e "${RED}Log file not found${NC}"
    fi
}

# Function to export errors
export_errors() {
    local output_file="${1:-unity-errors-$(date +%Y%m%d_%H%M%S).log}"
    
    if [ -f "$LOG_FILE" ]; then
        grep -E "ERROR|EXCEPTION|STACK" "$LOG_FILE" > "$output_file"
        local error_count=$(wc -l < "$output_file")
        echo -e "${GREEN}✅ Exported $error_count error lines to: $output_file${NC}"
    else
        echo -e "${RED}Log file not found${NC}"
    fi
}

# Function to watch for specific patterns
watch_pattern() {
    local pattern="$1"
    if [ -z "$pattern" ]; then
        echo -e "${RED}Usage: watch_pattern <pattern>${NC}"
        return 1
    fi
    
    echo -e "${CYAN}👁️  Watching for pattern: $pattern${NC}"
    echo "========================"
    
    tail -f "$LOG_FILE" | grep --line-buffered --color=always "$pattern"
}

# Main script logic
case "$1" in
    summary)
        show_log_summary
        ;;
    clear)
        clear_logs
        ;;
    search)
        search_logs "$2"
        ;;
    export-errors)
        export_errors "$2"
        ;;
    watch)
        watch_pattern "$2"
        ;;
    *)
        echo "Unity Log Utilities"
        echo "=================="
        echo "Usage: $0 {summary|clear|search|export-errors|watch} [args]"
        echo ""
        echo "Commands:"
        echo "  summary       - Show log statistics"
        echo "  clear         - Clear current log file (with backup)"
        echo "  search <pat>  - Search for pattern in logs"
        echo "  export-errors - Export all errors to separate file"
        echo "  watch <pat>   - Watch for specific pattern in real-time"
        ;;
esac