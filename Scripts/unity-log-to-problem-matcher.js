#!/usr/bin/env node

const fs = require('fs');
const path = require('path');
const readline = require('readline');

// Unity 로그 파일 경로
const logFilePath = path.join(__dirname, '..', 'Logs', 'unity-console.log');

// 로그 타입별 severity 매핑
const severityMap = {
    'ERROR': 'error',
    'EXCEPTION': 'error',
    'WARNING': 'warning',
    'INFO': 'info'
};

// Unity 로그 라인 파싱
function parseLogLine(line) {
    // Unity 로그 형식: [2025-01-05 12:34:56] [ERROR] ScriptName.cs(123): Error message
    const logPattern = /^\[(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})\] \[(\w+)\] (.+)$/;
    const match = line.match(logPattern);
    
    if (!match) return null;
    
    const [, timestamp, logType, content] = match;
    
    // 파일 경로와 라인 번호 추출
    const filePattern = /^(.+?)\((\d+)\): (.+)$/;
    const fileMatch = content.match(filePattern);
    
    let file = 'UnityEngine';
    let lineNumber = 1;
    let message = content;
    
    if (fileMatch) {
        file = fileMatch[1];
        lineNumber = parseInt(fileMatch[2], 10);
        message = fileMatch[3];
    } else {
        // 스택 트레이스에서 파일 정보 추출 시도
        const stackPattern = /at .+ in (.+):(\d+)/;
        const stackMatch = content.match(stackPattern);
        if (stackMatch) {
            file = stackMatch[1];
            lineNumber = parseInt(stackMatch[2], 10);
        }
    }
    
    // 파일 경로를 절대 경로로 변환
    if (!path.isAbsolute(file)) {
        // Assets 폴더 내의 스크립트인 경우
        if (file.includes('.cs')) {
            const possiblePath = path.join(__dirname, '..', 'Assets', file);
            if (fs.existsSync(possiblePath)) {
                file = possiblePath;
            } else {
                // Assets 폴더 내에서 재귀적으로 검색
                file = findFileInAssets(file) || file;
            }
        }
    }
    
    return {
        file: file,
        line: lineNumber,
        column: 1,
        severity: severityMap[logType] || 'info',
        message: `[${logType}] ${message}`,
        source: 'Unity'
    };
}

// Assets 폴더에서 파일 찾기
function findFileInAssets(fileName) {
    const assetsPath = path.join(__dirname, '..', 'Assets');
    
    function searchDir(dir) {
        const files = fs.readdirSync(dir);
        for (const file of files) {
            const fullPath = path.join(dir, file);
            const stat = fs.statSync(fullPath);
            
            if (stat.isDirectory()) {
                const found = searchDir(fullPath);
                if (found) return found;
            } else if (file === fileName || fullPath.endsWith(fileName)) {
                return fullPath;
            }
        }
        return null;
    }
    
    return searchDir(assetsPath);
}

// VS Code 문제 형식으로 출력
function outputProblem(problem) {
    // VS Code problem matcher 형식
    console.log(`${problem.file}:${problem.line}:${problem.column}: ${problem.severity}: ${problem.message}`);
}

// 로그 파일 모니터링
function watchLogFile() {
    if (!fs.existsSync(logFilePath)) {
        console.error(`Unity log file not found: ${logFilePath}`);
        console.error('Make sure Unity Editor is running and logs are being generated.');
        process.exit(1);
    }
    
    // 기존 로그 읽기
    const fileStream = fs.createReadStream(logFilePath);
    const rl = readline.createInterface({
        input: fileStream,
        crlfDelay: Infinity
    });
    
    rl.on('line', (line) => {
        const problem = parseLogLine(line);
        if (problem && (problem.severity === 'error' || problem.severity === 'warning')) {
            outputProblem(problem);
        }
    });
    
    rl.on('close', () => {
        // 파일 변경 감지
        let lastSize = fs.statSync(logFilePath).size;
        
        fs.watchFile(logFilePath, { interval: 100 }, (curr, prev) => {
            if (curr.size > lastSize) {
                const stream = fs.createReadStream(logFilePath, {
                    start: lastSize,
                    end: curr.size
                });
                
                const rl = readline.createInterface({
                    input: stream,
                    crlfDelay: Infinity
                });
                
                rl.on('line', (line) => {
                    const problem = parseLogLine(line);
                    if (problem && (problem.severity === 'error' || problem.severity === 'warning')) {
                        outputProblem(problem);
                    }
                });
                
                lastSize = curr.size;
            }
        });
    });
}

// 메인 실행
if (require.main === module) {
    console.error('Starting Unity log monitoring for VS Code Problems panel...');
    watchLogFile();
}