#!/usr/bin/env node

const { Server } = require('@modelcontextprotocol/sdk/server/index.js');
const { StdioServerTransport } = require('@modelcontextprotocol/sdk/server/stdio.js');
const { CallToolRequestSchema, ListToolsRequestSchema } = require('@modelcontextprotocol/sdk/types.js');
const fs = require('fs');
const path = require('path');

// Unity 프로젝트 경로 설정
const UNITY_PROJECT_PATH = process.env.UNITY_PROJECT_PATH || path.resolve(__dirname, '../..');
const UNITY_ASSETS_PATH = path.join(UNITY_PROJECT_PATH, 'Assets');

class UnityMCPServer {
  constructor() {
    this.server = new Server({
      name: 'unity-game-server',
      version: '1.0.0',
    }, {
      capabilities: {
        tools: {},
        resources: {},
      },
    });

    this.setupToolHandlers();
  }

  setupToolHandlers() {
    // 도구 목록 제공
    this.server.setRequestHandler(ListToolsRequestSchema, async () => {
      return {
        tools: [
          {
            name: 'read_unity_script',
            description: 'Unity C# 스크립트 파일을 읽습니다',
            inputSchema: {
              type: 'object',
              properties: {
                scriptPath: {
                  type: 'string',
                  description: 'Assets 폴더 기준 상대 경로 (예: Scripts/GameManager.cs)',
                },
              },
              required: ['scriptPath'],
            },
          },
          {
            name: 'list_unity_scripts',
            description: 'Unity 프로젝트의 모든 C# 스크립트를 나열합니다',
            inputSchema: {
              type: 'object',
              properties: {
                folder: {
                  type: 'string',
                  description: '검색할 폴더 (예: Scripts, Editor)',
                  default: '',
                },
              },
            },
          },
          {
            name: 'write_unity_script',
            description: 'Unity C# 스크립트 파일을 생성하거나 수정합니다',
            inputSchema: {
              type: 'object',
              properties: {
                scriptPath: {
                  type: 'string',
                  description: 'Assets 폴더 기준 상대 경로',
                },
                content: {
                  type: 'string',
                  description: '스크립트 내용',
                },
              },
              required: ['scriptPath', 'content'],
            },
          },
          {
            name: 'analyze_unity_project',
            description: 'Unity 프로젝트 구조를 분석합니다',
            inputSchema: {
              type: 'object',
              properties: {},
            },
          },
          {
            name: 'find_unity_references',
            description: 'Unity 프로젝트에서 특정 클래스나 메서드의 참조를 찾습니다',
            inputSchema: {
              type: 'object',
              properties: {
                searchTerm: {
                  type: 'string',
                  description: '검색할 클래스명, 메서드명, 또는 변수명',
                },
              },
              required: ['searchTerm'],
            },
          },
          {
            name: 'get_game_state',
            description: '현재 Unity 게임 상태를 가져옵니다',
            inputSchema: {
              type: 'object',
              properties: {},
            },
          },
        ],
      };
    });

    // 도구 호출 처리
    this.server.setRequestHandler(CallToolRequestSchema, async (request) => {
      const { name, arguments: args } = request.params;

      try {
        switch (name) {
          case 'read_unity_script':
            return await this.readUnityScript(args.scriptPath);
          
          case 'list_unity_scripts':
            return await this.listUnityScripts(args.folder || '');
          
          case 'write_unity_script':
            return await this.writeUnityScript(args.scriptPath, args.content);
          
          case 'analyze_unity_project':
            return await this.analyzeUnityProject();
          
          case 'find_unity_references':
            return await this.findUnityReferences(args.searchTerm);
          
          case 'get_game_state':
            return await this.getGameState();
          
          default:
            throw new Error(`알 수 없는 도구: ${name}`);
        }
      } catch (error) {
        return {
          content: [
            {
              type: 'text',
              text: `❌ 오류 발생: ${error.message}`,
            },
          ],
          isError: true,
        };
      }
    });
  }

  async readUnityScript(scriptPath) {
    const fullPath = path.join(UNITY_ASSETS_PATH, scriptPath);
    
    if (!fs.existsSync(fullPath)) {
      throw new Error(`파일을 찾을 수 없습니다: ${scriptPath}`);
    }

    const content = fs.readFileSync(fullPath, 'utf8');
    const stats = fs.statSync(fullPath);
    
    return {
      content: [
        {
          type: 'text',
          text: `📄 **${scriptPath}**\n크기: ${stats.size} bytes | 수정일: ${stats.mtime.toLocaleString()}\n\n\`\`\`csharp\n${content}\n\`\`\``,
        },
      ],
    };
  }

  async listUnityScripts(folder) {
    const searchPath = folder ? path.join(UNITY_ASSETS_PATH, folder) : UNITY_ASSETS_PATH;
    
    if (!fs.existsSync(searchPath)) {
      throw new Error(`폴더를 찾을 수 없습니다: ${folder}`);
    }

    const scripts = this.findCSharpFiles(searchPath);
    const scriptInfo = scripts.map(script => {
      const relativePath = path.relative(UNITY_ASSETS_PATH, script).replace(/\\/g, '/');
      const stats = fs.statSync(script);
      return {
        path: relativePath,
        size: stats.size,
        modified: stats.mtime,
      };
    });

    // 크기순으로 정렬
    scriptInfo.sort((a, b) => b.size - a.size);

    let result = `🗂️ **Unity C# 스크립트 목록** (${scriptInfo.length}개)\n\n`;
    
    scriptInfo.forEach(info => {
      result += `• **${info.path}** (${info.size} bytes)\n`;
    });

    return {
      content: [
        {
          type: 'text',
          text: result,
        },
      ],
    };
  }

  async writeUnityScript(scriptPath, content) {
    const fullPath = path.join(UNITY_ASSETS_PATH, scriptPath);
    const directory = path.dirname(fullPath);

    // 디렉토리가 없으면 생성
    if (!fs.existsSync(directory)) {
      fs.mkdirSync(directory, { recursive: true });
    }

    fs.writeFileSync(fullPath, content, 'utf8');

    return {
      content: [
        {
          type: 'text',
          text: `✅ **스크립트가 저장되었습니다:** ${scriptPath}\n\n💡 Unity에서 스크립트가 컴파일될 때까지 기다려주세요.`,
        },
      ],
    };
  }

  async analyzeUnityProject() {
    const scripts = this.findCSharpFiles(UNITY_ASSETS_PATH);
    
    // 폴더별 스크립트 분류
    const scriptsByFolder = {};
    let totalLines = 0;
    let totalSize = 0;

    scripts.forEach(script => {
      const relativePath = path.relative(UNITY_ASSETS_PATH, script);
      const folder = path.dirname(relativePath);
      const stats = fs.statSync(script);
      
      if (!scriptsByFolder[folder]) {
        scriptsByFolder[folder] = { files: [], size: 0, lines: 0 };
      }
      
      // 라인 수 계산
      try {
        const content = fs.readFileSync(script, 'utf8');
        const lines = content.split('\n').length;
        scriptsByFolder[folder].lines += lines;
        totalLines += lines;
      } catch (e) {
        // 파일 읽기 실패시 무시
      }
      
      scriptsByFolder[folder].files.push(path.basename(script));
      scriptsByFolder[folder].size += stats.size;
      totalSize += stats.size;
    });

    let report = `🎮 **Unity 프로젝트 분석 보고서**\n\n`;
    report += `📊 **전체 통계:**\n`;
    report += `• 총 C# 스크립트: **${scripts.length}개**\n`;
    report += `• 총 코드 라인: **${totalLines.toLocaleString()}줄**\n`;
    report += `• 총 파일 크기: **${(totalSize / 1024).toFixed(1)}KB**\n\n`;
    
    report += `📁 **폴더별 분석:**\n`;
    
    // 크기 순으로 정렬
    const sortedFolders = Object.entries(scriptsByFolder)
      .sort(([,a], [,b]) => b.size - a.size);
    
    sortedFolders.forEach(([folder, info]) => {
      const folderName = folder === '.' ? 'Assets 루트' : folder;
      report += `\n**${folderName}** (${info.files.length}개 파일, ${info.lines}줄)\n`;
      
      // 주요 파일들만 표시
      info.files.slice(0, 5).forEach(file => {
        report += `  • ${file}\n`;
      });
      
      if (info.files.length > 5) {
        report += `  • ... 그 외 ${info.files.length - 5}개 파일\n`;
      }
    });

    // 대용량 파일 찾기
    const largeFiles = scripts
      .map(script => ({
        path: path.relative(UNITY_ASSETS_PATH, script),
        size: fs.statSync(script).size,
      }))
      .filter(file => file.size > 10000) // 10KB 이상
      .sort((a, b) => b.size - a.size)
      .slice(0, 5);

    if (largeFiles.length > 0) {
      report += `\n📈 **대용량 스크립트 파일:**\n`;
      largeFiles.forEach(file => {
        report += `• **${file.path}** (${(file.size / 1024).toFixed(1)}KB)\n`;
      });
    }

    return {
      content: [
        {
          type: 'text',
          text: report,
        },
      ],
    };
  }

  async findUnityReferences(searchTerm) {
    const scripts = this.findCSharpFiles(UNITY_ASSETS_PATH);
    const references = [];

    scripts.forEach(scriptPath => {
      try {
        const content = fs.readFileSync(scriptPath, 'utf8');
        const lines = content.split('\n');
        
        lines.forEach((line, index) => {
          if (line.toLowerCase().includes(searchTerm.toLowerCase())) {
            const relativePath = path.relative(UNITY_ASSETS_PATH, scriptPath);
            references.push({
              file: relativePath,
              line: index + 1,
              content: line.trim(),
            });
          }
        });
      } catch (error) {
        // 파일 읽기 오류 무시
      }
    });

    let report = `🔍 **"${searchTerm}" 참조 검색 결과**\n\n`;
    
    if (references.length === 0) {
      report += `❌ 참조를 찾을 수 없습니다.`;
    } else {
      report += `✅ 총 **${references.length}개의 참조**를 찾았습니다:\n\n`;
      
      // 파일별로 그룹화
      const groupedRefs = {};
      references.forEach(ref => {
        if (!groupedRefs[ref.file]) groupedRefs[ref.file] = [];
        groupedRefs[ref.file].push(ref);
      });
      
      Object.entries(groupedRefs).forEach(([file, refs]) => {
        report += `📄 **${file}** (${refs.length}개 참조)\n`;
        refs.forEach(ref => {
          report += `   \`${ref.line}:\` ${ref.content}\n`;
        });
        report += `\n`;
      });
    }

    return {
      content: [
        {
          type: 'text',
          text: report,
        },
      ],
    };
  }

  async getGameState() {
    // Unity 프로젝트의 현재 상태 정보 수집
    const projectSettings = this.readProjectSettings();
    const sceneFiles = this.findSceneFiles();
    
    let report = `🎮 **Unity 게임 상태 정보**\n\n`;
    
    if (projectSettings) {
      report += `🏷️ **프로젝트 정보:**\n`;
      report += `• 프로젝트명: ${projectSettings.productName || 'Unknown'}\n`;
      report += `• 버전: ${projectSettings.bundleVersion || 'Unknown'}\n`;
      report += `• Unity 버전: ${projectSettings.unityVersion || 'Unknown'}\n\n`;
    }
    
    report += `🎬 **씬 파일:**\n`;
    sceneFiles.forEach(scene => {
      const sceneName = path.basename(scene, '.unity');
      report += `• ${sceneName}\n`;
    });

    const scripts = this.findCSharpFiles(UNITY_ASSETS_PATH);
    const managerScripts = scripts.filter(script => 
      path.basename(script).toLowerCase().includes('manager')
    );
    
    if (managerScripts.length > 0) {
      report += `\n🎯 **매니저 스크립트:**\n`;
      managerScripts.forEach(script => {
        const relativePath = path.relative(UNITY_ASSETS_PATH, script);
        report += `• ${relativePath}\n`;
      });
    }

    return {
      content: [
        {
          type: 'text',
          text: report,
        },
      ],
    };
  }

  readProjectSettings() {
    try {
      const settingsPath = path.join(UNITY_PROJECT_PATH, 'ProjectSettings', 'ProjectSettings.asset');
      if (fs.existsSync(settingsPath)) {
        const content = fs.readFileSync(settingsPath, 'utf8');
        // Unity 설정 파일에서 기본 정보 추출 (간단한 파싱)
        const productNameMatch = content.match(/productName:\s*(.+)/);
        const bundleVersionMatch = content.match(/bundleVersion:\s*(.+)/);
        
        return {
          productName: productNameMatch ? productNameMatch[1].trim() : null,
          bundleVersion: bundleVersionMatch ? bundleVersionMatch[1].trim() : null,
        };
      }
    } catch (error) {
      // 설정 파일 읽기 실패시 무시
    }
    return null;
  }

  findSceneFiles() {
    const scenesPath = path.join(UNITY_ASSETS_PATH, 'Scenes');
    if (!fs.existsSync(scenesPath)) return [];
    
    return this.findFilesByExtension(scenesPath, '.unity');
  }

  findFilesByExtension(directory, extension) {
    const files = [];
    
    if (!fs.existsSync(directory)) return files;

    const items = fs.readdirSync(directory);
    
    items.forEach(item => {
      const fullPath = path.join(directory, item);
      const stat = fs.statSync(fullPath);
      
      if (stat.isDirectory()) {
        files.push(...this.findFilesByExtension(fullPath, extension));
      } else if (item.endsWith(extension)) {
        files.push(fullPath);
      }
    });

    return files;
  }

  findCSharpFiles(directory) {
    return this.findFilesByExtension(directory, '.cs');
  }

  async run() {
    const transport = new StdioServerTransport();
    await this.server.connect(transport);
    console.error('🎮 Unity MCP 서버가 시작되었습니다!');
  }
}

// 서버 실행
if (require.main === module) {
  const server = new UnityMCPServer();
  server.run().catch(console.error);
}

module.exports = UnityMCPServer; 