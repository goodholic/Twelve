using System;
using UnityEngine;

// ===== [수정] 유니티 에디터 전용 using은 클래스/namespace 밖에 위치해야 함 =====
#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
#endif
// ==========================================================================

[CreateAssetMenu(fileName = "StarMergeDatabase", menuName = "MyGame/StarMerge Database")]
public class StarMergeDatabaseObject : ScriptableObject
{
    /// <summary>
    /// 2성 후보 목록(종족별).
    /// </summary>
    public RaceStarPool[] twoStarPools;

    /// <summary>
    /// 3성 후보 목록(종족별).
    /// </summary>
    public RaceStarPool[] threeStarPools;

    /// <summary>
    /// 1성 후보 목록(종족별) - 기본 소환용
    /// </summary>
    public RaceStarPool[] oneStarPools;

    /// <summary>
    /// raceType에 해당하는 1성 풀에서 무작위 CharacterData를 하나 반환.
    /// </summary>
    public CharacterData GetRandom1Star(RaceType raceType)
    {
        RaceStarPool pool = FindPool(oneStarPools, raceType);
        if (pool == null || pool.possibleCharacters == null || pool.possibleCharacters.Length == 0)
        {
            Debug.LogWarning($"[StarMergeDatabase] 1성 풀에 '{raceType}' 종족 정보가 없거나 비어있음!");
            return null;
        }
        return GetRandomCharacterFromPool(pool);
    }

    /// <summary>
    /// raceType에 해당하는 2성 풀에서 무작위 CharacterData를 하나 반환.
    /// raceType이 없으면 null 반환.
    /// </summary>
    public CharacterData GetRandom2Star(RaceType raceType)
    {
        RaceStarPool pool = FindPool(twoStarPools, raceType);
        if (pool == null || pool.possibleCharacters == null || pool.possibleCharacters.Length == 0)
        {
            Debug.LogWarning($"[StarMergeDatabase] 2성 풀에 '{raceType}' 종족 정보가 없거나 비어있음!");
            return null;
        }
        return GetRandomCharacterFromPool(pool);
    }

    /// <summary>
    /// raceType에 해당하는 3성 풀에서 무작위 CharacterData를 하나 반환
    /// </summary>
    public CharacterData GetRandom3Star(RaceType raceType)
    {
        RaceStarPool pool = FindPool(threeStarPools, raceType);
        if (pool == null || pool.possibleCharacters == null || pool.possibleCharacters.Length == 0)
        {
            Debug.LogWarning($"[StarMergeDatabase] 3성 풀에 '{raceType}' 종족 정보가 없거나 비어있음!");
            return null;
        }
        return GetRandomCharacterFromPool(pool);
    }

    /// <summary>
    /// 무작위 종족의 특정 등급 캐릭터 반환 (5웨이브 보상용)
    /// </summary>
    public CharacterData GetRandomCharacterAnyRace(CharacterStar star)
    {
        RaceStarPool[] targetPools = null;
        
        switch (star)
        {
            case CharacterStar.OneStar:
                targetPools = oneStarPools;
                break;
            case CharacterStar.TwoStar:
                targetPools = twoStarPools;
                break;
            case CharacterStar.ThreeStar:
                targetPools = threeStarPools;
                break;
            default:
                Debug.LogWarning($"[StarMergeDatabase] 지원하지 않는 등급: {star}");
                return null;
        }

        if (targetPools == null || targetPools.Length == 0)
        {
            Debug.LogWarning($"[StarMergeDatabase] {star} 풀이 비어있음!");
            return null;
        }

        // 무작위 종족 선택
        RaceStarPool randomPool = targetPools[UnityEngine.Random.Range(0, targetPools.Length)];
        return GetRandomCharacterFromPool(randomPool);
    }

    /// <summary>
    /// 5웨이브 보상용 - 랜덤 2성 캐릭터 3개 반환
    /// </summary>
    public CharacterData[] GetWaveRewardCandidates()
    {
        CharacterData[] candidates = new CharacterData[3];
        
        for (int i = 0; i < 3; i++)
        {
            candidates[i] = GetRandomCharacterAnyRace(CharacterStar.TwoStar);
            
            // 중복 방지 (간단한 재시도 로직)
            int retryCount = 0;
            while (i > 0 && retryCount < 10)
            {
                bool isDuplicate = false;
                for (int j = 0; j < i; j++)
                {
                    if (candidates[i] == candidates[j])
                    {
                        isDuplicate = true;
                        break;
                    }
                }
                
                if (isDuplicate)
                {
                    candidates[i] = GetRandomCharacterAnyRace(CharacterStar.TwoStar);
                    retryCount++;
                }
                else
                {
                    break;
                }
            }
        }
        
        return candidates;
    }

    /// <summary>
    /// 주어진 배열에서 raceType에 해당하는 RaceStarPool을 찾는다.
    /// (없으면 null)
    /// </summary>
    private RaceStarPool FindPool(RaceStarPool[] pools, RaceType raceType)
    {
        if (pools == null) return null;
        foreach (var p in pools)
        {
            if (p.race == raceType) return p;
        }
        return null;
    }

    /// <summary>
    /// pool 내의 possibleCharacters에서 확률에 따라 1개 추출.
    /// </summary>
    private CharacterData GetRandomCharacterFromPool(RaceStarPool pool)
    {
        // 총 가중치 합
        float totalWeight = 0f;
        foreach (var c in pool.possibleCharacters)
        {
            totalWeight += c.weight;
        }

        if (totalWeight <= 0f)
        {
            Debug.LogWarning($"[StarMergeDatabase] {pool.race} 풀 totalWeight=0 => 반환 못 함");
            return null;
        }

        // 0..totalWeight 범위에서 랜덤
        float r = UnityEngine.Random.Range(0f, totalWeight);
        float accum = 0f;
        foreach (var c in pool.possibleCharacters)
        {
            accum += c.weight;
            if (r <= accum)
            {
                return c.characterData;
            }
        }

        // (이론상 여기 도달 안 함)
        return null;
    }

#if UNITY_EDITOR
    //----------------------------------------------
    //=== 아래부터 "더미값 입력" 기능 추가 부분 ===
    //----------------------------------------------

    /// <summary>
    /// 1성 데이터 추가 예시
    /// </summary>
    [ContextMenu("Add Dummy 1Star From Tuple (예시)")]
    public void AddDummyOneStarFromTuple_Example()
    {
        string example = "(human, human, 초보검사, 15, 1.2, 1.0, 200, false, Melee)";
        AddDummyOneStarFromTuple(example);
        Debug.Log($"[StarMergeDatabase] 예시로 {example} 를 파싱하여 1성 풀에 추가 완료!");
    }

    /// <summary>
    /// 1성 캐릭터 추가
    /// </summary>
    public void AddDummyOneStarFromTuple(string tupleLine)
    {
        if (string.IsNullOrWhiteSpace(tupleLine))
        {
            Debug.LogWarning("[StarMergeDatabase] 입력이 비어있습니다.(1성)");
            return;
        }

        // 파싱 로직은 2성/3성과 동일
        string trimmed = tupleLine.Trim();
        if (trimmed.StartsWith("(") && trimmed.EndsWith(")"))
        {
            trimmed = trimmed.Substring(1, trimmed.Length - 2);
        }
        string[] tokens = trimmed.Split(',');
        if (tokens.Length < 9)
        {
            Debug.LogWarning($"[StarMergeDatabase] 토큰 수가 9개 미만(1성) => '{tupleLine}'");
            return;
        }

        // 필드 파싱
        string poolRaceStr = tokens[0].Trim();
        string charRaceStr = tokens[1].Trim();
        string nameStr     = tokens[2].Trim();

        string atkStr      = tokens[3].Trim();
        string aspdStr     = tokens[4].Trim();
        string rangeStr    = tokens[5].Trim();
        string maxHpStr    = tokens[6].Trim();
        string areaStr     = tokens[7].Trim();
        string rTypeStr    = tokens[8].Trim();

        float atkVal   = ParseFloatSafe(atkStr, 10f);
        float aspdVal  = ParseFloatSafe(aspdStr, 1f);
        float rngVal   = ParseFloatSafe(rangeStr, 1.5f);
        float maxHPVal = ParseFloatSafe(maxHpStr, 100f);

        bool areaAttack = ParseBoolSafe(areaStr);
        RangeType rangeTypeVal = ParseRangeTypeSafe(rTypeStr, RangeType.Melee);

        RaceType poolRace = ParseRaceType(poolRaceStr, RaceType.Human);
        RaceType charRace = ParseRaceType(charRaceStr, RaceType.Human);

        // 1성 풀 찾기/생성
        RaceStarPool targetPool = FindPool(oneStarPools, poolRace);
        if (targetPool == null)
        {
            int oldSize = (oneStarPools == null) ? 0 : oneStarPools.Length;
            Array.Resize(ref oneStarPools, oldSize + 1);
            RaceStarPool newPool = new RaceStarPool();
            newPool.race = poolRace;
            newPool.possibleCharacters = new WeightedCharacter[0];
            oneStarPools[oldSize] = newPool;
            targetPool = newPool;
        }

        // CharacterData 생성
        CharacterData newCD = new CharacterData();
        newCD.characterName = nameStr;
        newCD.race          = (CharacterRace)charRace;
        newCD.attackPower   = atkVal;
        newCD.attackSpeed   = aspdVal;
        newCD.attackRange   = rngVal;
        newCD.maxHP         = maxHPVal;
        newCD.isAreaAttack  = areaAttack;
        newCD.rangeType     = rangeTypeVal;
        newCD.initialStar   = CharacterStar.OneStar;

        // WeightedCharacter 생성
        WeightedCharacter wc = new WeightedCharacter();
        wc.characterData = newCD;
        wc.weight = 1f;

        // 풀에 추가
        int oldLen = (targetPool.possibleCharacters == null) ? 0 : targetPool.possibleCharacters.Length;
        Array.Resize(ref targetPool.possibleCharacters, oldLen + 1);
        targetPool.possibleCharacters[oldLen] = wc;

        // 저장
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[StarMergeDatabase] 1성 풀({poolRace})에 '{nameStr}'(종족={charRace},atk={atkVal}) 추가 완료!");
    }

    /// <summary>
    /// 예: (human, human, 꽃의 정령, 40, 1.0, 3.5, 320, true, Ranged)
    ///     => 2성 풀에 새 WeightedCharacter + CharacterData 추가
    /// </summary>
    [ContextMenu("Add Dummy 2Star From Tuple (예시)")]
    public void AddDummyTwoStarFromTuple_Example()
    {
        // 예시용으로 하드코딩된 입력
        string example = "(human, human, 꽃의 정령, 40, 1.0, 3.5, 320, true, Ranged)";
        AddDummyTwoStarFromTuple(example);
        Debug.Log($"[StarMergeDatabase] 예시로 {example} 를 파싱하여 2성 풀에 추가 완료!");
    }

    /// <summary>
    /// "(human, human, 꽃의 정령, 40, 1.0, 3.5, 320, true, Ranged)" 형태를 파싱하여
    /// twoStarPools 중 적절한 RaceStarPool에 WeightedCharacter를 1개 추가한다.
    /// </summary>
    /// <param name="tupleLine">"(풀종족, 캐릭터종족, 이름, atk, aspd, range, maxHP, isArea, rangeType)"</param>
    public void AddDummyTwoStarFromTuple(string tupleLine)
    {
        if (string.IsNullOrWhiteSpace(tupleLine))
        {
            Debug.LogWarning("[StarMergeDatabase] 입력이 비어있습니다.");
            return;
        }

        // 1) 괄호 제거, 쉼표 분리
        string trimmed = tupleLine.Trim();
        if (trimmed.StartsWith("(") && trimmed.EndsWith(")"))
        {
            trimmed = trimmed.Substring(1, trimmed.Length - 2);
        }
        string[] tokens = trimmed.Split(',');
        if (tokens.Length < 9)
        {
            Debug.LogWarning($"[StarMergeDatabase] 토큰 수가 9개 미만 => '{tupleLine}'");
            return;
        }

        // 2) 개별 필드 파싱
        // (0) poolRace, (1) charRace, (2) name, (3) atk, (4) aspd, (5) range, (6) maxHP, (7) isArea, (8) rangeType
        string poolRaceStr = tokens[0].Trim();
        string charRaceStr = tokens[1].Trim();
        string nameStr     = tokens[2].Trim();

        string atkStr      = tokens[3].Trim();
        string aspdStr     = tokens[4].Trim();
        string rangeStr    = tokens[5].Trim();
        string maxHpStr    = tokens[6].Trim();
        string areaStr     = tokens[7].Trim();
        string rTypeStr    = tokens[8].Trim();

        // float 파싱
        float atkVal   = ParseFloatSafe(atkStr, 10f);
        float aspdVal  = ParseFloatSafe(aspdStr, 1f);
        float rngVal   = ParseFloatSafe(rangeStr, 1.5f);
        float maxHPVal = ParseFloatSafe(maxHpStr, 100f);

        bool areaAttack = ParseBoolSafe(areaStr);
        RangeType rangeTypeVal = ParseRangeTypeSafe(rTypeStr, RangeType.Melee);

        // 종족 파싱
        RaceType poolRace = ParseRaceType(poolRaceStr, RaceType.Human);
        RaceType charRace = ParseRaceType(charRaceStr, RaceType.Human);

        // 3) 해당 풀(race) 찾기 (twoStarPools)
        RaceStarPool targetPool = FindPool(twoStarPools, poolRace);
        if (targetPool == null)
        {
            // 없으면 새로 만든다
            int oldSize = (twoStarPools == null) ? 0 : twoStarPools.Length;
            Array.Resize(ref twoStarPools, oldSize + 1);
            RaceStarPool newPool = new RaceStarPool();
            newPool.race = poolRace;
            newPool.possibleCharacters = new WeightedCharacter[0];
            twoStarPools[oldSize] = newPool;
            targetPool = newPool;
        }

        // 4) 새 CharacterData 생성
        CharacterData newCD = new CharacterData();
        newCD.characterName = nameStr;
        newCD.race          = (CharacterRace)charRace;
        newCD.attackPower   = atkVal;
        newCD.attackSpeed   = aspdVal;
        newCD.attackRange   = rngVal;
        newCD.maxHP         = maxHPVal;
        newCD.isAreaAttack  = areaAttack;
        newCD.rangeType     = rangeTypeVal;
        // 2성 캐릭터임을 표시
        newCD.initialStar   = CharacterStar.TwoStar;

        // 5) WeightedCharacter 생성(기본 weight=1)
        WeightedCharacter wc = new WeightedCharacter();
        wc.characterData = newCD;
        wc.weight = 1f;

        // 6) targetPool에 추가
        int oldLen = (targetPool.possibleCharacters == null) ? 0 : targetPool.possibleCharacters.Length;
        Array.Resize(ref targetPool.possibleCharacters, oldLen + 1);
        targetPool.possibleCharacters[oldLen] = wc;

        // 7) ScriptableObject에 변경 반영
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[StarMergeDatabase] 2성 풀({poolRace})에 '{nameStr}'(종족={charRace},atk={atkVal}) 추가 완료!");
    }

    // ---------------------------------------------------------
    // === 3성 버전(아래) ===
    // ---------------------------------------------------------
    /// <summary>
    /// 예: (human, elf, 암흑 정령, 38, 1.3, 3.2, 350, true, Ranged)
    ///     => 3성 풀에 새 WeightedCharacter + CharacterData 추가
    /// </summary>
    [ContextMenu("Add Dummy 3Star From Tuple (예시)")]
    public void AddDummyThreeStarFromTuple_Example()
    {
        // 예시용으로 하드코딩된 입력
        string example = "(human, elf, 암흑 정령, 38, 1.3, 3.2, 350, true, Ranged)";
        AddDummyThreeStarFromTuple(example);
        Debug.Log($"[StarMergeDatabase] 예시로 {example} 를 파싱하여 3성 풀에 추가 완료!");
    }

    /// <summary>
    /// "(human, elf, 암흑 정령, 38, 1.3, 3.2, 350, true, Ranged)" 형태를 파싱하여
    /// threeStarPools 중 적절한 RaceStarPool에 WeightedCharacter를 1개 추가한다.
    /// </summary>
    /// <param name="tupleLine">"(풀종족, 캐릭터종족, 이름, atk, aspd, range, maxHP, isArea, rangeType)"</param>
    public void AddDummyThreeStarFromTuple(string tupleLine)
    {
        if (string.IsNullOrWhiteSpace(tupleLine))
        {
            Debug.LogWarning("[StarMergeDatabase] 입력이 비어있습니다.(3성)");
            return;
        }

        // 1) 괄호 제거, 쉼표 분리
        string trimmed = tupleLine.Trim();
        if (trimmed.StartsWith("(") && trimmed.EndsWith(")"))
        {
            trimmed = trimmed.Substring(1, trimmed.Length - 2);
        }
        string[] tokens = trimmed.Split(',');
        if (tokens.Length < 9)
        {
            Debug.LogWarning($"[StarMergeDatabase] 토큰 수가 9개 미만(3성) => '{tupleLine}'");
            return;
        }

        // 2) 개별 필드 파싱
        string poolRaceStr = tokens[0].Trim();
        string charRaceStr = tokens[1].Trim();
        string nameStr     = tokens[2].Trim();

        string atkStr      = tokens[3].Trim();
        string aspdStr     = tokens[4].Trim();
        string rangeStr    = tokens[5].Trim();
        string maxHpStr    = tokens[6].Trim();
        string areaStr     = tokens[7].Trim();
        string rTypeStr    = tokens[8].Trim();

        float atkVal   = ParseFloatSafe(atkStr, 10f);
        float aspdVal  = ParseFloatSafe(aspdStr, 1f);
        float rngVal   = ParseFloatSafe(rangeStr, 1.5f);
        float maxHPVal = ParseFloatSafe(maxHpStr, 100f);

        bool areaAttack = ParseBoolSafe(areaStr);
        RangeType rangeTypeVal = ParseRangeTypeSafe(rTypeStr, RangeType.Melee);

        RaceType poolRace = ParseRaceType(poolRaceStr, RaceType.Human);
        RaceType charRace = ParseRaceType(charRaceStr, RaceType.Human);

        // 3) 해당 풀(race) 찾기 (threeStarPools)
        RaceStarPool targetPool = FindPool(threeStarPools, poolRace);
        if (targetPool == null)
        {
            // 없으면 새로 만든다
            int oldSize = (threeStarPools == null) ? 0 : threeStarPools.Length;
            Array.Resize(ref threeStarPools, oldSize + 1);
            RaceStarPool newPool = new RaceStarPool();
            newPool.race = poolRace;
            newPool.possibleCharacters = new WeightedCharacter[0];
            threeStarPools[oldSize] = newPool;
            targetPool = newPool;
        }

        // 4) 새 CharacterData 생성
        CharacterData newCD = new CharacterData();
        newCD.characterName = nameStr;
        newCD.race          = (CharacterRace)charRace;
        newCD.attackPower   = atkVal;
        newCD.attackSpeed   = aspdVal;
        newCD.attackRange   = rngVal;
        newCD.maxHP         = maxHPVal;
        newCD.isAreaAttack  = areaAttack;
        newCD.rangeType     = rangeTypeVal;
        // 3성 캐릭터임을 표시
        newCD.initialStar   = CharacterStar.ThreeStar;

        // 5) WeightedCharacter 생성(기본 weight=1)
        WeightedCharacter wc = new WeightedCharacter();
        wc.characterData = newCD;
        wc.weight = 1f;

        // 6) targetPool에 추가
        int oldLen = (targetPool.possibleCharacters == null) ? 0 : targetPool.possibleCharacters.Length;
        Array.Resize(ref targetPool.possibleCharacters, oldLen + 1);
        targetPool.possibleCharacters[oldLen] = wc;

        // 7) 저장
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[StarMergeDatabase] 3성 풀({poolRace})에 '{nameStr}'(종족={charRace},atk={atkVal}) 추가 완료!");
    }

    // ---------------------------------------------------------
    // === (새로 추가) 여러 줄을 한꺼번에 처리하는 메서드들 ===
    // ---------------------------------------------------------
    /// <summary>
    /// 여러 줄(라인별 1개의 튜플)을 한꺼번에 1성 풀에 추가.
    /// </summary>
    [ContextMenu("Add Dummy 1Stars From Tuples (Multiline)")]
    public void AddDummyOneStarFromTuples(string multiline)
    {
        if (string.IsNullOrEmpty(multiline))
        {
            Debug.LogWarning("[StarMergeDatabase] multiline 입력이 비어있습니다. (1성)");
            return;
        }

        string[] lines = multiline.Split('\n');
        int count = 0;
        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            AddDummyOneStarFromTuple(trimmed);
            count++;
        }

        Debug.Log($"[StarMergeDatabase] 1성 튜플 {count}줄 처리 완료!");
    }

    /// <summary>
    /// 여러 줄(라인별 1개의 튜플)을 한꺼번에 2성 풀에 추가.
    /// 각 줄은 "(human, human, 낭만검객, 20, 1.5, ...)" 형식
    /// </summary>
    [ContextMenu("Add Dummy 2Stars From Tuples (Multiline)")]
    public void AddDummyTwoStarFromTuples(string multiline)
    {
        if (string.IsNullOrEmpty(multiline))
        {
            Debug.LogWarning("[StarMergeDatabase] multiline 입력이 비어있습니다. (2성)");
            return;
        }

        string[] lines = multiline.Split('\n');
        int count = 0;
        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            AddDummyTwoStarFromTuple(trimmed);
            count++;
        }

        Debug.Log($"[StarMergeDatabase] 2성 튜플 {count}줄 처리 완료!");
    }

    /// <summary>
    /// 여러 줄(라인별 1개의 튜플)을 한꺼번에 3성 풀에 추가.
    /// 각 줄은 "(human, orc, 염라공주, 48, 1.0, 4.0, 360, true, LongRange)" 형식
    /// </summary>
    [ContextMenu("Add Dummy 3Stars From Tuples (Multiline)")]
    public void AddDummyThreeStarFromTuples(string multiline)
    {
        if (string.IsNullOrEmpty(multiline))
        {
            Debug.LogWarning("[StarMergeDatabase] multiline 입력이 비어있습니다. (3성)");
            return;
        }

        string[] lines = multiline.Split('\n');
        int count = 0;
        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            AddDummyThreeStarFromTuple(trimmed);
            count++;
        }

        Debug.Log($"[StarMergeDatabase] 3성 튜플 {count}줄 처리 완료!");
    }

    // ---------------------------------------------------------
    // === (추가) 모든 샘플 데이터를 한 번에 입력하는 예시 메서드
    // ---------------------------------------------------------
    [ContextMenu("Add All Sample Data")]
    public void AddAllSampleData()
    {
        // ===== (0) 1성 다량 튜플 =====
        string oneStarData = @"
(human, human, 초보검사, 15, 1.2, 1.0, 200, false, Melee)
(human, human, 견습마법사, 18, 0.9, 2.5, 150, false, Ranged)
(human, human, 신참궁수, 20, 1.1, 3.0, 180, false, Ranged)
(human, orc, 오크전사, 18, 1.0, 1.1, 250, false, Melee)
(human, orc, 오크주술사, 22, 0.8, 2.8, 160, true, Ranged)
(human, elf, 엘프궁수, 17, 1.3, 3.2, 170, false, Ranged)
(human, elf, 엘프드루이드, 19, 0.95, 2.5, 190, true, Ranged)

(orc, human, 초보검사, 15, 1.2, 1.0, 200, false, Melee)
(orc, human, 견습마법사, 18, 0.9, 2.5, 150, false, Ranged)
(orc, human, 신참궁수, 20, 1.1, 3.0, 180, false, Ranged)
(orc, orc, 오크전사, 18, 1.0, 1.1, 250, false, Melee)
(orc, orc, 오크주술사, 22, 0.8, 2.8, 160, true, Ranged)
(orc, elf, 엘프궁수, 17, 1.3, 3.2, 170, false, Ranged)
(orc, elf, 엘프드루이드, 19, 0.95, 2.5, 190, true, Ranged)

(elf, human, 초보검사, 15, 1.2, 1.0, 200, false, Melee)
(elf, human, 견습마법사, 18, 0.9, 2.5, 150, false, Ranged)
(elf, human, 신참궁수, 20, 1.1, 3.0, 180, false, Ranged)
(elf, orc, 오크전사, 18, 1.0, 1.1, 250, false, Melee)
(elf, orc, 오크주술사, 22, 0.8, 2.8, 160, true, Ranged)
(elf, elf, 엘프궁수, 17, 1.3, 3.2, 170, false, Ranged)
(elf, elf, 엘프드루이드, 19, 0.95, 2.5, 190, true, Ranged)
";
        AddDummyOneStarFromTuples(oneStarData);

        // ===== (1) 2성 다량 튜플 =====
        string twoStarData = @"
(human, human, 낭만검객, 20, 1.5, 1.2, 320, false, Melee)
(human, human, 남자 위치, 26, 1.1, 3.2, 180, true, Ranged)
(human, human, 캐논슈터, 34, 0.85, 3.8, 220, true, Ranged)
(human, human, 방패기사, 13, 0.95, 1.1, 480, false, Melee)
(human, orc, 오크법사, 30, 1.0, 3.3, 180, true, Ranged)
(human, orc, 폭격기, 38, 0.7, 3.8, 230, true, Ranged)
(human, orc, 완드스타, 28, 1.1, 3.0, 170, false, Ranged)
(human, elf, 눈물바다, 19, 1.7, 1.2, 250, false, Melee)
(human, elf, 홀릭, 24, 1.2, 2.5, 200, false, Ranged)
(human, elf, 환각법사, 32, 0.9, 3.5, 180, true, Ranged)

(orc, human, 낭만검객, 20, 1.5, 1.2, 320, false, Melee)
(orc, human, 남자 위치, 26, 1.1, 3.2, 180, true, Ranged)
(orc, human, 캐논슈터, 34, 0.85, 3.8, 220, true, Ranged)
(orc, human, 방패기사, 13, 0.95, 1.1, 480, false, Melee)
(orc, orc, 오크법사, 30, 1.0, 3.3, 180, true, Ranged)
(orc, orc, 폭격기, 38, 0.7, 3.8, 230, true, Ranged)
(orc, orc, 완드스타, 28, 1.1, 3.0, 170, false, Ranged)
(orc, elf, 눈물바다, 19, 1.7, 1.2, 250, false, Melee)
(orc, elf, 홀릭, 24, 1.2, 2.5, 200, false, Ranged)
(orc, elf, 환각법사, 32, 0.9, 3.5, 180, true, Ranged)

(elf, human, 낭만검객, 20, 1.5, 1.2, 320, false, Melee)
(elf, human, 남자 위치, 26, 1.1, 3.2, 180, true, Ranged)
(elf, human, 캐논슈터, 34, 0.85, 3.8, 220, true, Ranged)
(elf, human, 방패기사, 13, 0.95, 1.1, 480, false, Melee)
(elf, orc, 오크법사, 30, 1.0, 3.3, 180, true, Ranged)
(elf, orc, 폭격기, 38, 0.7, 3.8, 230, true, Ranged)
(elf, orc, 완드스타, 28, 1.1, 3.0, 170, false, Ranged)
(elf, elf, 눈물바다, 19, 1.7, 1.2, 250, false, Melee)
(elf, elf, 홀릭, 24, 1.2, 2.5, 200, false, Ranged)
(elf, elf, 환각법사, 32, 0.9, 3.5, 180, true, Ranged)
";
        AddDummyTwoStarFromTuples(twoStarData);

        // ===== (2) 3성 다량 튜플 =====
        string threeStarData = @"
(human, human, 다크 엔젤, 40, 1.6, 2.0, 400, true, Melee)
(human, human, 미치광대, 32, 2.0, 1.5, 280, false, Melee)
(human, human, 인형공주, 30, 1.2, 3.0, 350, true, Ranged)
(human, orc, 예언자, 45, 1.3, 3.5, 300, true, Ranged)
(human, orc, 염라공주, 48, 1.0, 4.0, 360, true, LongRange)
(human, orc, 흑공주, 42, 1.5, 2.8, 330, false, Ranged)
(human, elf, 큐피트, 36, 1.8, 3.8, 280, false, LongRange)
(human, elf, 꽃의 정령, 40, 1.0, 3.5, 320, true, Ranged)
(human, elf, 암흑 정령, 38, 1.3, 3.2, 350, true, Ranged)

(orc, human, 다크 엔젤, 40, 1.6, 2.0, 400, true, Melee)
(orc, human, 미치광대, 32, 2.0, 1.5, 280, false, Melee)
(orc, human, 인형공주, 30, 1.2, 3.0, 350, true, Ranged)
(orc, orc, 예언자, 45, 1.3, 3.5, 300, true, Ranged)
(orc, orc, 염라공주, 48, 1.0, 4.0, 360, true, LongRange)
(orc, orc, 흑공주, 42, 1.5, 2.8, 330, false, Ranged)
(orc, elf, 큐피트, 36, 1.8, 3.8, 280, false, LongRange)
(orc, elf, 꽃의 정령, 40, 1.0, 3.5, 320, true, Ranged)
(orc, elf, 암흑 정령, 38, 1.3, 3.2, 350, true, Ranged)

(elf, human, 다크 엔젤, 40, 1.6, 2.0, 400, true, Melee)
(elf, human, 미치광대, 32, 2.0, 1.5, 280, false, Melee)
(elf, human, 인형공주, 30, 1.2, 3.0, 350, true, Ranged)
(elf, orc, 예언자, 45, 1.3, 3.5, 300, true, Ranged)
(elf, orc, 염라공주, 48, 1.0, 4.0, 360, true, LongRange)
(elf, orc, 흑공주, 42, 1.5, 2.8, 330, false, Ranged)
(elf, elf, 큐피트, 36, 1.8, 3.8, 280, false, LongRange)
(elf, elf, 꽃의 정령, 40, 1.0, 3.5, 320, true, Ranged)
(elf, elf, 암흑 정령, 38, 1.3, 3.2, 350, true, Ranged)
";
        AddDummyThreeStarFromTuples(threeStarData);

        Debug.Log("[StarMergeDatabase] 모든 샘플 1성/2성/3성 데이터 추가 완료!");
    }

    //===============================================================
    //=== 아래 유틸 메서드: String → float/bool/RangeType/RaceType
    //===============================================================
    private float ParseFloatSafe(string s, float defaultVal)
    {
        if (float.TryParse(s, out float f))
            return f;
        return defaultVal;
    }

    private bool ParseBoolSafe(string s)
    {
        if (bool.TryParse(s, out bool b))
            return b;
        return false;
    }

    private RangeType ParseRangeTypeSafe(string s, RangeType defaultVal)
    {
        try
        {
            return (RangeType)Enum.Parse(typeof(RangeType), s, true);
        }
        catch
        {
            return defaultVal;
        }
    }

    private RaceType ParseRaceType(string s, RaceType defaultVal)
    {
        if (string.IsNullOrWhiteSpace(s)) return defaultVal;
        s = s.ToLower().Trim();
        if (s.Contains("human")) return RaceType.Human;
        if (s.Contains("orc")) return RaceType.Orc;
        if (s.Contains("elf")) return RaceType.Elf;
        if (s.Contains("undead")) return RaceType.Undead;
        return RaceType.Etc;
    }

#endif
}

[System.Serializable]
public class RaceStarPool
{
    [Tooltip("종족 타입")]
    public RaceType race;
    public WeightedCharacter[] possibleCharacters;
}

[System.Serializable]
public class WeightedCharacter
{
    public CharacterData characterData;
    [Tooltip("이 캐릭터가 뽑힐 확률 가중치 (0보다 커야함)")]
    public float weight = 1f;
}