# Unity Log Monitoring in VS Code (WSL)

This guide explains how to monitor Unity console logs in VS Code terminal on WSL.

## Overview

The Unity project includes an automatic log export system that writes Unity console output to a file that can be monitored from WSL/VS Code terminal.

### Components

1. **UnityLogToTerminal.cs** - Unity Editor script that captures console logs
2. **monitor-unity-logs.sh** - Bash script for real-time log monitoring
3. **unity-log-utils.sh** - Utility functions for log management
4. **VS Code Tasks** - Quick access to monitoring functions

## Setup

### 1. Unity Side (Already Configured)

The `UnityLogToTerminal.cs` script in `Assets/Editor/` automatically:
- Captures all Unity console output (Info, Warnings, Errors)
- Writes to `Logs/unity-console.log`
- Includes timestamps and log levels
- Auto-flushes for real-time updates

### 2. VS Code Terminal Monitoring

#### Quick Start

1. Open VS Code integrated terminal
2. Run the monitoring script:
   ```bash
   ./Scripts/monitor-unity-logs.sh
   ```

#### Using VS Code Tasks

Press `Ctrl+Shift+P` and run "Tasks: Run Task", then select:
- **Monitor Unity Logs** - Watch all logs in real-time
- **Monitor Unity Errors Only** - Filter to show only errors/warnings
- **Unity Log Summary** - Show log statistics
- **Clear Unity Logs** - Clear logs with backup
- **Export Unity Errors** - Export errors to separate file

## Command Line Usage

### Monitor All Logs
```bash
./Scripts/monitor-unity-logs.sh
```

### Monitor Errors Only
```bash
./Scripts/monitor-unity-logs.sh -e
```

### Custom Options
```bash
./Scripts/monitor-unity-logs.sh -f path/to/log -n 50
```
- `-f, --file` - Custom log file path
- `-n, --lines` - Number of initial lines to show
- `-e, --errors-only` - Show only errors and warnings

### Log Utilities
```bash
# Show log summary
./Scripts/unity-log-utils.sh summary

# Search logs
./Scripts/unity-log-utils.sh search "pattern"

# Export errors
./Scripts/unity-log-utils.sh export-errors

# Watch for specific pattern
./Scripts/unity-log-utils.sh watch "MonoBehaviour"

# Clear logs (with backup)
./Scripts/unity-log-utils.sh clear
```

## Log Format

Logs are formatted as:
```
[HH:mm:ss.fff] LEVEL: Message
```

Log levels:
- **INFO** - General information (white)
- **WARNING** - Warnings (yellow)
- **ERROR** - Errors (red)
- **EXCEPTION** - Exceptions (red)
- **ASSERT** - Assertions (magenta)
- **STACK** - Stack traces (gray)

## Tips

1. **Multiple Terminals**: Open multiple terminals to monitor different aspects:
   - One for all logs
   - One for errors only
   - One for specific pattern watching

2. **Log Location**: The log file is located at:
   ```
   /mnt/c/Users/super/OneDrive/문서/GitHub/Twelve/Logs/unity-console.log
   ```

3. **Unity Menu**: In Unity Editor, use:
   - `Unity Log > 로그 파일 열기` - Open log file
   - `Unity Log > 로그 파일 경로 복사` - Copy log path
   - `Unity Log > 터미널 모니터링 명령어 생성` - Generate PowerShell command

4. **Performance**: The logging system has minimal impact on Unity performance due to:
   - Asynchronous writing
   - Auto-flush for real-time updates
   - Efficient string formatting

## Troubleshooting

### Log file not found
- Make sure Unity Editor is running
- Check if the log directory exists: `mkdir -p Logs`
- Verify the UnityLogToTerminal.cs script is in Assets/Editor/

### No real-time updates
- Ensure auto-flush is enabled in UnityLogToTerminal.cs
- Check file permissions on the log file
- Try restarting Unity Editor

### Colors not showing
- Make sure your terminal supports ANSI color codes
- VS Code integrated terminal should work by default
- For Windows Terminal, colors are supported

## Alternative: PowerShell Monitoring

If you prefer PowerShell, the project includes a PowerShell script:
```powershell
# From PowerShell (not WSL)
.\Assets\Scripts\Watch-UnityLogs.ps1
```

This provides similar functionality but runs in PowerShell instead of bash.