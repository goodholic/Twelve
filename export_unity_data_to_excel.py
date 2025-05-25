import pandas as pd
import re
from pathlib import Path

def parse_yaml_value(line):
    """YAML 라인에서 값을 추출"""
    if ':' in line:
        return line.split(':', 1)[1].strip()
    return line.strip()

def parse_character_data(lines, start_idx):
    """캐릭터 데이터 파싱"""
    character = {}
    i = start_idx
    
    while i < len(lines):
        line = lines[i].strip()
        
        if 'characterName:' in line:
            character['이름'] = parse_yaml_value(line).strip('"')
        elif 'initialStar:' in line:
            character['초기 별'] = int(parse_yaml_value(line))
        elif 'race:' in line and 'CharacterRace' not in line:
            race_val = int(parse_yaml_value(line))
            character['종족'] = ['Human', 'Orc', 'Elf'][race_val] if race_val < 3 else 'Unknown'
        elif 'attackPower:' in line:
            character['공격력'] = float(parse_yaml_value(line))
        elif 'attackSpeed:' in line:
            character['공격속도'] = float(parse_yaml_value(line))
        elif 'attackRange:' in line:
            character['공격범위'] = float(parse_yaml_value(line))
        elif 'maxHP:' in line:
            character['최대 HP'] = float(parse_yaml_value(line))
        elif 'moveSpeed:' in line:
            character['이동속도'] = float(parse_yaml_value(line))
        elif 'rangeType:' in line:
            range_val = int(parse_yaml_value(line))
            character['공격 타입'] = ['Melee', 'Ranged', 'LongRange'][range_val] if range_val < 3 else 'Unknown'
        elif 'isAreaAttack:' in line:
            character['광역공격'] = '예' if parse_yaml_value(line) == '1' else '아니오'
        elif 'cost:' in line:
            character['비용'] = int(parse_yaml_value(line))
        elif 'weight:' in line:
            character['가중치'] = float(parse_yaml_value(line))
            break  # weight가 나오면 이 캐릭터 데이터 끝
            
        i += 1
    
    return character, i

def parse_character_database(file_path):
    """CharacterDatabase 파일 파싱"""
    with open(file_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    characters = []
    i = 0
    
    while i < len(lines):
        line = lines[i].strip()
        
        if '- characterName:' in line:
            character, next_idx = parse_character_data(lines, i)
            if character:
                characters.append(character)
            i = next_idx
        else:
            i += 1
    
    return characters

def parse_star_merge_database(file_path):
    """StarMergeDatabase 파일 파싱"""
    with open(file_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    two_star_characters = []
    three_star_characters = []
    current_pool = None
    
    i = 0
    while i < len(lines):
        line = lines[i].strip()
        
        if 'twoStarPools:' in line:
            current_pool = 'two_star'
        elif 'threeStarPools:' in line:
            current_pool = 'three_star'
        elif '- characterData:' in line and current_pool:
            character, next_idx = parse_character_data(lines, i + 1)
            if character:
                if current_pool == 'two_star':
                    two_star_characters.append(character)
                else:
                    three_star_characters.append(character)
            i = next_idx
        else:
            i += 1
    
    return two_star_characters, three_star_characters

def parse_item_database(file_path):
    """ItemDatabase 파일 파싱"""
    with open(file_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    items = []
    i = 0
    
    while i < len(lines):
        line = lines[i].strip()
        
        if '- itemName:' in line:
            item = {}
            item['아이템명'] = parse_yaml_value(line).strip('"')
            
            # 다음 줄들에서 추가 정보 파싱
            j = i + 1
            while j < len(lines) and lines[j].strip().startswith(' '):
                sub_line = lines[j].strip()
                if 'itemIcon:' in sub_line:
                    item['아이콘'] = '있음'
                elif 'description:' in sub_line:
                    item['설명'] = parse_yaml_value(sub_line).strip('"')
                elif 'effectType:' in sub_line:
                    effect_type = int(parse_yaml_value(sub_line))
                    item['효과 타입'] = ['공격력', 'HP', '사거리'][effect_type] if effect_type < 3 else 'Unknown'
                elif 'effectValue:' in sub_line:
                    item['효과 값'] = int(parse_yaml_value(sub_line))
                j += 1
            
            items.append(item)
            i = j - 1
        
        i += 1
    
    return items

def export_to_excel():
    """모든 데이터를 엑셀 파일로 내보내기"""
    
    try:
        # 1. 캐릭터 데이터베이스 파싱
        print("1. 아군 캐릭터 데이터 파싱 중...")
        ally_characters = parse_character_database('Assets/Prefabs/Data/CharacterDatabase.asset')
        
        print("2. 적 캐릭터 데이터 파싱 중...")
        enemy_characters = parse_character_database('Assets/Prefabs/Data/opponentCharacterDatabase.asset')
        
        # 2. 2성/3성 캐릭터 데이터 파싱
        print("3. 2성/3성 캐릭터 데이터 파싱 중...")
        two_star_chars, three_star_chars = parse_star_merge_database('Assets/Prefabs/Data/StarMergeDatabase.asset')
        
        # 3. 아이템 데이터 파싱
        print("4. 아이템 데이터 파싱 중...")
        items = parse_item_database('Assets/Prefabs/Data/NewItemDatabase.asset')
        
        # 데이터프레임 생성
        df_ally = pd.DataFrame(ally_characters)
        df_enemy = pd.DataFrame(enemy_characters)
        df_two_star = pd.DataFrame(two_star_chars)
        df_three_star = pd.DataFrame(three_star_chars)
        df_items = pd.DataFrame(items)
        
        # 엑셀 파일로 저장
        with pd.ExcelWriter('Unity_Game_Data.xlsx', engine='openpyxl') as writer:
            if len(df_ally) > 0:
                df_ally.to_excel(writer, sheet_name='아군 캐릭터', index=False)
            if len(df_enemy) > 0:
                df_enemy.to_excel(writer, sheet_name='적 캐릭터', index=False)
            if len(df_two_star) > 0:
                df_two_star.to_excel(writer, sheet_name='2성 캐릭터', index=False)
            if len(df_three_star) > 0:
                df_three_star.to_excel(writer, sheet_name='3성 캐릭터', index=False)
            if len(df_items) > 0:
                df_items.to_excel(writer, sheet_name='아이템', index=False)
        
        print("\n엑셀 파일이 생성되었습니다: Unity_Game_Data.xlsx")
        print(f"- 아군 캐릭터: {len(ally_characters)}개")
        print(f"- 적 캐릭터: {len(enemy_characters)}개")
        print(f"- 2성 캐릭터: {len(two_star_chars)}개")
        print(f"- 3성 캐릭터: {len(three_star_chars)}개")
        print(f"- 아이템: {len(items)}개")
        
    except Exception as e:
        print(f"오류 발생: {e}")
        import traceback
        traceback.print_exc()

if __name__ == "__main__":
    export_to_excel() 