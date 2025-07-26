using UnityEngine;
using System.Collections.Generic;

public class BattleSystem : MonoBehaviour
{
    private static BattleSystem instance;
    public static BattleSystem Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<BattleSystem>();
            return instance;
        }
    }

    void Awake()
    {
        instance = this;
    }

    // 배치 시 전투 시뮬레이션
    public BattleResult SimulateBattle(CharacterData attacker, Character defender)
    {
        BattleResult result = new BattleResult();

        if (defender == null)
        {
            // 빈 타일이면 공격자만 생존
            result.attackerSurvives = true;
            result.defenderSurvives = false;
            return result;
        }

        // 같은 팀이면 배치 불가
        if ((attacker == GameManager.Instance.xTeamPool.Find(c => c == attacker) && defender.team == GameManager.Team.X) ||
            (attacker == GameManager.Instance.oTeamPool.Find(c => c == attacker) && defender.team == GameManager.Team.O))
        {
            result.canPlace = false;
            return result;
        }

        // 전투 계산 (공격자가 선공)
        int defenderHP = defender.currentHP;
        defenderHP -= attacker.attackPower;

        if (defenderHP <= 0)
        {
            // 방어자 사망
            result.defenderSurvives = false;
            result.attackerSurvives = true;
        }
        else
        {
            // 방어자 생존, 반격
            int attackerHP = attacker.hp;
            attackerHP -= defender.characterData.attackPower;

            if (attackerHP <= 0)
            {
                // 공격자 사망
                result.attackerSurvives = false;
                result.defenderSurvives = true;
            }
            else
            {
                // 둘 다 생존 - 배치 불가
                result.attackerSurvives = true;
                result.defenderSurvives = true;
                result.canPlace = false;
            }
        }

        return result;
    }

    // 캐릭터 배치 후 공격 범위 처리
    public void ProcessCharacterAttack(Character character)
    {
        List<Vector2Int> attackPositions = character.characterData.GetAttackPositions();
        
        foreach (Vector2Int offset in attackPositions)
        {
            int targetX = character.x + offset.x;
            int targetY = character.y + offset.y;
            int targetBoard = character.boardIndex;

            // 건너편 보드 공격 처리
            if (AttackPatternManager.IsCrossBoardAttack(character.characterData.attackPattern))
            {
                targetBoard = 1 - character.boardIndex;
                // 건너편 보드의 같은 좌표 공격
                targetX = character.x;
                targetY = character.y;
            }

            // 유효한 좌표인지 확인
            if (IsValidPosition(targetBoard, targetX, targetY))
            {
                Character target = GameManager.Instance.boardState[targetBoard, targetX, targetY];
                if (target != null && target.team != character.team)
                {
                    // 데미지 처리
                    DamageCharacter(target, character.characterData.attackPower);
                }
            }
        }
    }

    // 캐릭터에게 데미지 주기
    void DamageCharacter(Character target, int damage)
    {
        target.currentHP -= damage;
        
        if (target.currentHP <= 0)
        {
            // 캐릭터 제거
            RemoveCharacter(target);
        }
        else
        {
            // HP 표시 업데이트 (UI 구현 필요)
            UpdateCharacterHPDisplay(target);
        }
    }

    // 캐릭터 제거
    void RemoveCharacter(Character character)
    {
        GameManager.Instance.boardState[character.boardIndex, character.x, character.y] = null;
        
        // 시각적 제거
        if (character.gameObject != null)
            Destroy(character.gameObject);
    }

    // HP 표시 업데이트
    void UpdateCharacterHPDisplay(Character character)
    {
        // UI 구현 필요
        Debug.Log($"{character.characterData.characterName} HP: {character.currentHP}");
    }

    // 유효한 위치인지 확인
    bool IsValidPosition(int boardIndex, int x, int y)
    {
        return boardIndex >= 0 && boardIndex < 2 &&
               x >= 0 && x < GameManager.BOARD_WIDTH &&
               y >= 0 && y < GameManager.BOARD_HEIGHT;
    }

    // 전투 미리보기 (마우스 호버 시)
    public PreviewResult PreviewBattle(CharacterData attacker, int boardIndex, int x, int y)
    {
        PreviewResult preview = new PreviewResult();
        Character defender = GameManager.Instance.boardState[boardIndex, x, y];

        if (defender == null)
        {
            preview.canPlace = true;
            preview.willKillDefender = false;
            preview.willDie = false;
        }
        else
        {
            BattleResult result = SimulateBattle(attacker, defender);
            preview.canPlace = result.canPlace;
            preview.willKillDefender = !result.defenderSurvives;
            preview.willDie = !result.attackerSurvives;
        }

        return preview;
    }
}

// 전투 결과
public class BattleResult
{
    public bool canPlace = true;
    public bool attackerSurvives = true;
    public bool defenderSurvives = false;
}

// 미리보기 결과
public class PreviewResult
{
    public bool canPlace;
    public bool willKillDefender;
    public bool willDie;
}