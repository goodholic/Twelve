# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Twelve is a Unity-based tower defense/auto-battler hybrid mobile game featuring:
- Wave-based combat with 20 waves per stage
- Character placement and merging mechanics
- Star-based character progression (1-3 stars)
- Multiple regions (Region 1 vs Region 2 gameplay)
- Castle defense mechanics
- Gacha/summoning system for character collection

## Key Development Commands

### Unity Editor Commands
- **Open Project**: Use Unity Hub to open the project (Unity 2021.3 or later recommended)
- **Play Mode**: Press Play button in Unity Editor to test in GameScene
- **Scene Navigation**: 
  - LobbyScene: Main menu and character management
  - GameScene: Core gameplay

### Data Management Commands
```bash
# Export Unity data to CSV
python export_unity_data_to_excel.py
# or
run_export.bat
```

### Unity Menu Tools
- **Tools > Data Export Manager**: Create and export game data
- **Tools > CSV Data Sync Manager**: Bidirectional sync between CSV and ScriptableObjects
- **Tools > Generate Sample Data**: Generate test data for characters
- **Tools > Generate Ally Sample Data**: Generate test data for ally characters

## High-Level Architecture

### Core Game Systems

#### Game Flow
1. **LobbyScene**: Player manages characters, summons new ones, accesses shop
2. **GameScene**: Wave-based combat with character placement on grid
3. **Result**: Victory/defeat based on castle health and wave completion

#### Character System Architecture
```
Character.cs (Base class)
├── CharacterStats (Stats management)
├── CharacterCombat (Attack/damage logic)
├── CharacterMovement (Pathfinding)
├── CharacterJump (Jump mechanics)
└── CharacterVisual (Sprite/animation)
```

#### Data Flow
```
CSV Files (Assets/CSV/)
    ↕️ (CSV Data Sync Manager)
ScriptableObjects (Assets/Prefabs/Data/)
    → Runtime Character Instances
```

#### Manager Architecture
- **GameManager**: Central game state, wave progression, victory conditions
- **WaveSpawner/WaveSpawnerRegion2**: Monster spawning logic
- **PlacementManager**: Character placement on tiles
- **SummonManager**: Character summoning/gacha logic
- **MergeManager/AutoMergeManager**: Character merging mechanics
- **TileManager**: Grid management for character placement
- **RouteManager**: Path management for character/monster movement

### Key Data Files
- **CharacterDatabase.asset**: All ally characters
- **opponentCharacterDatabase.asset**: All enemy characters
- **StarMergeDatabase.asset**: Ally 2-3 star merge data
- **OPStarMergeDatabase 1.asset**: Enemy 2-3 star merge data
- **NewItemDatabase.asset**: Item data

### Network Architecture (In Development)
- Uses Unity Netcode for GameObjects
- NetworkManager prefab configured but currently disabled
- Multiplayer features planned but not yet implemented

## Common Development Tasks

### Adding New Characters
1. Open **Tools > Data Export Manager**
2. Create new character with appropriate star level
3. Set stats (Attack, HP, Range, Speed, etc.)
4. Export to CSV using the manager
5. Characters automatically available in summon pool

### Modifying Character Stats
1. Open **Tools > CSV Data Sync Manager**
2. Enable Auto Sync
3. Edit CSV files in Assets/CSV/
4. Changes auto-import to Unity
5. Test in Play mode

### Testing Wave Spawning
1. Open GameScene
2. Locate WaveSpawner in hierarchy
3. Modify wave configurations in inspector
4. Test specific waves using debug controls

### Debugging Character Placement
1. Enable TileManager debug visualization
2. Use PlacementManager debug logs
3. Check tile occupancy states
4. Verify character spawn positions

## Key Code Locations

### Core Gameplay
- `Assets/Scripts/GameManager.cs:42` - Main game loop
- `Assets/Scripts/WaveSpawner.cs:89` - Wave spawning logic
- `Assets/Scripts/Character.cs:156` - Character base behavior
- `Assets/Scripts/Monster.cs:78` - Enemy behavior

### UI Management
- `Assets/OX UI Scripts/LobbySceneManager.cs:34` - Lobby UI logic
- `Assets/OX UI Scripts/DrawPanelManager.cs:67` - Gacha UI
- `Assets/OX UI Scripts/CharacterPanelManager.cs:45` - Character collection UI

### Data Management
- `Assets/Scripts/CharacterDatabase.cs:23` - Character data structure
- `Assets/Editor/CSVDataSyncEditor.cs:156` - CSV sync implementation
- `Assets/Editor/DataExportEditor.cs:89` - Data export logic

## Important Notes

### Performance Considerations
- Object pooling implemented for bullets and effects
- Character limit per tile prevents overcrowding
- Wave spawning uses coroutines for performance

### Save System
- Uses Easy Save 3 for persistence
- Player data saved locally
- Character collection persistent between sessions

### Localization
- Localization system prepared but not fully implemented
- Text assets in Resources/Localizations/

### Known Limitations
- Networking code present but disabled
- Some UI elements placeholder
- Balance testing ongoing for character stats

## Testing Checklist

Before committing changes:
1. Verify both LobbyScene and GameScene load correctly
2. Test character summoning and placement
3. Verify wave spawning works properly
4. Check merge functionality
5. Ensure CSV sync works bidirectionally
6. Test on target resolution (1080x1920)