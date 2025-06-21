using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

namespace pjy.Managers
{
    /// <summary>
    /// 종족 시너지 시스템 매니저
    /// 같은 종족이 많을수록 해당 종족 캐릭터들에게 버프 제공
    /// </summary>
    public class RaceSynergyManager : MonoBehaviour
    {
        [System.Serializable]
        public class RaceSynergy
        {
            public CharacterRace race;
            public int count;
            public float attackBonus;
            public float healthBonus;
            public float attackSpeedBonus;
        }
        
        [System.Serializable]
        public class SynergyLevel
        {
            public int requiredCount;
            public float attackBonusPercent;
            public float healthBonusPercent;
            public float attackSpeedBonusPercent;
            public string description;
        }
        
        [Header("시너지 레벨 설정")]
        [SerializeField] private List<SynergyLevel> synergyLevels = new List<SynergyLevel>()
        {
            new SynergyLevel { requiredCount = 3, attackBonusPercent = 5f, healthBonusPercent = 5f, attackSpeedBonusPercent = 0f, description = "3명: 공격력/체력 +5%" },
            new SynergyLevel { requiredCount = 5, attackBonusPercent = 10f, healthBonusPercent = 10f, attackSpeedBonusPercent = 5f, description = "5명: 공격력/체력 +10%, 공격속도 +5%" },
            new SynergyLevel { requiredCount = 7, attackBonusPercent = 15f, healthBonusPercent = 15f, attackSpeedBonusPercent = 10f, description = "7명: 공격력/체력 +15%, 공격속도 +10%" },
            new SynergyLevel { requiredCount = 9, attackBonusPercent = 20f, healthBonusPercent = 20f, attackSpeedBonusPercent = 15f, description = "9명: 공격력/체력 +20%, 공격속도 +15%" }
        };
        
        [Header("현재 시너지 상태")]
        [SerializeField] private List<RaceSynergy> currentSynergies = new List<RaceSynergy>();
        
        [Header("UI 요소")]
        [SerializeField] private GameObject synergyUIPanel;
        [SerializeField] private Transform synergyUIContainer;
        [SerializeField] private GameObject synergyUIItemPrefab;
        
        [Header("시너지 효과 표시")]
        [SerializeField] private GameObject synergyEffectPrefab;
        [SerializeField] private Color humanSynergyColor = Color.blue;
        [SerializeField] private Color orcSynergyColor = Color.red;
        [SerializeField] private Color elfSynergyColor = Color.green;
        
        private Dictionary<CharacterRace, List<Character>> raceCharacterMap = new Dictionary<CharacterRace, List<Character>>();
        private Dictionary<CharacterRace, int> previousSynergyLevels = new Dictionary<CharacterRace, int>();
        
        // 싱글톤 인스턴스
        private static RaceSynergyManager instance;
        public static RaceSynergyManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<RaceSynergyManager>();
                }
                return instance;
            }
        }
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            
            // 종족별 리스트 초기화
            raceCharacterMap[CharacterRace.Human] = new List<Character>();
            raceCharacterMap[CharacterRace.Orc] = new List<Character>();
            raceCharacterMap[CharacterRace.Elf] = new List<Character>();
            
            previousSynergyLevels[CharacterRace.Human] = 0;
            previousSynergyLevels[CharacterRace.Orc] = 0;
            previousSynergyLevels[CharacterRace.Elf] = 0;
        }
        
        private void Start()
        {
            // 초기 시너지 상태 설정
            InitializeSynergies();
        }
        
        /// <summary>
        /// 시너지 상태 초기화
        /// </summary>
        private void InitializeSynergies()
        {
            currentSynergies.Clear();
            currentSynergies.Add(new RaceSynergy { race = CharacterRace.Human, count = 0, attackBonus = 0, healthBonus = 0, attackSpeedBonus = 0 });
            currentSynergies.Add(new RaceSynergy { race = CharacterRace.Orc, count = 0, attackBonus = 0, healthBonus = 0, attackSpeedBonus = 0 });
            currentSynergies.Add(new RaceSynergy { race = CharacterRace.Elf, count = 0, attackBonus = 0, healthBonus = 0, attackSpeedBonus = 0 });
        }
        
        /// <summary>
        /// 캐릭터가 필드에 추가될 때 호출
        /// </summary>
        public void OnCharacterSpawned(Character character)
        {
            if (character == null || character.characterData == null) return;
            
            CharacterRace race = character.characterData.race;
            if (!raceCharacterMap.ContainsKey(race)) return;
            
            // 캐릭터를 종족별 리스트에 추가
            if (!raceCharacterMap[race].Contains(character))
            {
                raceCharacterMap[race].Add(character);
                Debug.Log($"[RaceSynergyManager] {race} 캐릭터 추가됨. 현재 수: {raceCharacterMap[race].Count}");
                
                // 시너지 업데이트
                UpdateSynergy();
            }
        }
        
        /// <summary>
        /// 캐릭터가 필드에서 제거될 때 호출
        /// </summary>
        public void OnCharacterRemoved(Character character)
        {
            if (character == null || character.characterData == null) return;
            
            CharacterRace race = character.characterData.race;
            if (!raceCharacterMap.ContainsKey(race)) return;
            
            // 캐릭터를 종족별 리스트에서 제거
            if (raceCharacterMap[race].Remove(character))
            {
                Debug.Log($"[RaceSynergyManager] {race} 캐릭터 제거됨. 현재 수: {raceCharacterMap[race].Count}");
                
                // 시너지 업데이트
                UpdateSynergy();
            }
        }
        
        /// <summary>
        /// 시너지 업데이트
        /// </summary>
        public void UpdateSynergy()
        {
            foreach (var synergy in currentSynergies)
            {
                CharacterRace race = synergy.race;
                int count = raceCharacterMap[race].Count;
                
                // 이전 시너지 레벨 저장
                int previousLevel = GetSynergyLevel(synergy.count);
                
                // 현재 캐릭터 수 업데이트
                synergy.count = count;
                
                // 시너지 레벨 계산
                SynergyLevel currentLevel = GetCurrentSynergyLevel(count);
                
                if (currentLevel != null)
                {
                    synergy.attackBonus = currentLevel.attackBonusPercent;
                    synergy.healthBonus = currentLevel.healthBonusPercent;
                    synergy.attackSpeedBonus = currentLevel.attackSpeedBonusPercent;
                }
                else
                {
                    synergy.attackBonus = 0;
                    synergy.healthBonus = 0;
                    synergy.attackSpeedBonus = 0;
                }
                
                // 해당 종족의 모든 캐릭터에게 버프 적용
                ApplyRaceSynergyToCharacters(race, synergy);
                
                // 시너지 레벨이 변경되었을 때 효과 표시
                int newLevel = GetSynergyLevel(count);
                if (previousLevel != newLevel && newLevel > 0)
                {
                    ShowSynergyLevelUpEffect(race, newLevel);
                }
                previousSynergyLevels[race] = newLevel;
            }
            
            // UI 업데이트
            UpdateSynergyUI();
        }
        
        /// <summary>
        /// 현재 카운트에 해당하는 시너지 레벨 가져오기
        /// </summary>
        private SynergyLevel GetCurrentSynergyLevel(int count)
        {
            SynergyLevel bestLevel = null;
            
            foreach (var level in synergyLevels)
            {
                if (count >= level.requiredCount)
                {
                    bestLevel = level;
                }
            }
            
            return bestLevel;
        }
        
        /// <summary>
        /// 시너지 레벨 번호 가져오기 (1, 2, 3, 4...)
        /// </summary>
        private int GetSynergyLevel(int count)
        {
            int level = 0;
            foreach (var synergyLevel in synergyLevels)
            {
                if (count >= synergyLevel.requiredCount)
                {
                    level++;
                }
            }
            return level;
        }
        
        /// <summary>
        /// 특정 종족의 모든 캐릭터에게 시너지 버프 적용
        /// </summary>
        private void ApplyRaceSynergyToCharacters(CharacterRace race, RaceSynergy synergy)
        {
            if (!raceCharacterMap.ContainsKey(race)) return;
            
            foreach (var character in raceCharacterMap[race])
            {
                if (character != null)
                {
                    character.ApplyRaceSynergy(synergy.attackBonus, synergy.healthBonus, synergy.attackSpeedBonus);
                }
            }
        }
        
        /// <summary>
        /// 시너지 레벨업 효과 표시
        /// </summary>
        private void ShowSynergyLevelUpEffect(CharacterRace race, int level)
        {
            // 모든 해당 종족 캐릭터에게 이펙트 표시
            if (!raceCharacterMap.ContainsKey(race)) return;
            
            Color effectColor = GetRaceColor(race);
            
            foreach (var character in raceCharacterMap[race])
            {
                if (character != null)
                {
                    // 시너지 이펙트 생성
                    if (synergyEffectPrefab != null)
                    {
                        GameObject effect = Instantiate(synergyEffectPrefab, character.transform.position, Quaternion.identity);
                        effect.transform.SetParent(character.transform);
                        
                        // 색상 설정
                        ParticleSystem ps = effect.GetComponent<ParticleSystem>();
                        if (ps != null)
                        {
                            var main = ps.main;
                            main.startColor = effectColor;
                        }
                        
                        // 일정 시간 후 제거
                        Destroy(effect, 2f);
                    }
                    
                    // 캐릭터 시각적 효과 (잠시 밝게)
                    StartCoroutine(FlashCharacter(character, effectColor));
                }
            }
            
            // 시너지 달성 메시지
            string message = $"{race} 시너지 레벨 {level} 달성!";
            ShowSynergyMessage(message, effectColor);
        }
        
        /// <summary>
        /// 캐릭터 플래시 효과
        /// </summary>
        private IEnumerator FlashCharacter(Character character, Color color)
        {
            SpriteRenderer sr = character.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color originalColor = sr.color;
                
                // 밝게
                sr.color = Color.Lerp(originalColor, color, 0.5f);
                yield return new WaitForSeconds(0.3f);
                
                // 원래대로
                sr.color = originalColor;
            }
        }
        
        /// <summary>
        /// 종족별 색상 가져오기
        /// </summary>
        private Color GetRaceColor(CharacterRace race)
        {
            switch (race)
            {
                case CharacterRace.Human:
                    return humanSynergyColor;
                case CharacterRace.Orc:
                    return orcSynergyColor;
                case CharacterRace.Elf:
                    return elfSynergyColor;
                default:
                    return Color.white;
            }
        }
        
        /// <summary>
        /// 시너지 메시지 표시
        /// </summary>
        private void ShowSynergyMessage(string message, Color color)
        {
            // 임시 메시지 표시
            GameObject messageObj = new GameObject("SynergyMessage");
            messageObj.transform.SetParent(transform, false);
            
            TextMeshProUGUI messageText = messageObj.AddComponent<TextMeshProUGUI>();
            messageText.text = message;
            messageText.fontSize = 32;
            messageText.color = color;
            messageText.alignment = TextAlignmentOptions.Center;
            
            RectTransform rect = messageObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.7f);
            rect.anchorMax = new Vector2(0.5f, 0.7f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(500, 60);
            
            // 페이드 아웃 애니메이션
            StartCoroutine(FadeOutMessage(messageText));
        }
        
        private IEnumerator FadeOutMessage(TextMeshProUGUI text)
        {
            float duration = 2f;
            float elapsed = 0f;
            Color startColor = text.color;
            
            yield return new WaitForSeconds(1f); // 1초 대기
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                
                // 위로 이동
                text.rectTransform.anchoredPosition += Vector2.up * 30f * Time.deltaTime;
                
                yield return null;
            }
            
            Destroy(text.gameObject);
        }
        
        /// <summary>
        /// 시너지 UI 업데이트
        /// </summary>
        private void UpdateSynergyUI()
        {
            if (synergyUIPanel == null || synergyUIContainer == null) return;
            
            // 기존 UI 아이템 제거
            foreach (Transform child in synergyUIContainer)
            {
                Destroy(child.gameObject);
            }
            
            // 각 종족별 시너지 상태 표시
            foreach (var synergy in currentSynergies)
            {
                if (synergy.count > 0)
                {
                    CreateSynergyUIItem(synergy);
                }
            }
        }
        
        /// <summary>
        /// 시너지 UI 아이템 생성
        /// </summary>
        private void CreateSynergyUIItem(RaceSynergy synergy)
        {
            if (synergyUIItemPrefab == null) return;
            
            GameObject item = Instantiate(synergyUIItemPrefab, synergyUIContainer);
            
            // 종족 아이콘
            Image raceIcon = item.transform.Find("RaceIcon")?.GetComponent<Image>();
            if (raceIcon != null)
            {
                raceIcon.color = GetRaceColor(synergy.race);
            }
            
            // 종족 이름과 수
            TextMeshProUGUI raceText = item.transform.Find("RaceText")?.GetComponent<TextMeshProUGUI>();
            if (raceText != null)
            {
                raceText.text = $"{synergy.race} ({synergy.count})";
            }
            
            // 보너스 표시
            TextMeshProUGUI bonusText = item.transform.Find("BonusText")?.GetComponent<TextMeshProUGUI>();
            if (bonusText != null)
            {
                string bonusStr = "";
                if (synergy.attackBonus > 0) bonusStr += $"공격력 +{synergy.attackBonus}%\n";
                if (synergy.healthBonus > 0) bonusStr += $"체력 +{synergy.healthBonus}%\n";
                if (synergy.attackSpeedBonus > 0) bonusStr += $"공격속도 +{synergy.attackSpeedBonus}%";
                
                bonusText.text = bonusStr;
            }
            
            // 시너지 레벨 표시
            int level = GetSynergyLevel(synergy.count);
            Transform[] levelStars = item.transform.Find("LevelStars")?.GetComponentsInChildren<Transform>();
            if (levelStars != null)
            {
                for (int i = 1; i <= levelStars.Length - 1 && i <= level; i++)
                {
                    levelStars[i].gameObject.SetActive(true);
                }
            }
        }
        
        /// <summary>
        /// 현재 시너지 정보 가져오기
        /// </summary>
        public RaceSynergy GetRaceSynergy(CharacterRace race)
        {
            return currentSynergies.FirstOrDefault(s => s.race == race);
        }
        
        /// <summary>
        /// 디버그용: 모든 시너지 정보 출력
        /// </summary>
        [ContextMenu("Debug Print All Synergies")]
        public void DebugPrintAllSynergies()
        {
            Debug.Log("=== 현재 시너지 상태 ===");
            foreach (var synergy in currentSynergies)
            {
                Debug.Log($"{synergy.race}: {synergy.count}명, 공격력+{synergy.attackBonus}%, 체력+{synergy.healthBonus}%, 공격속도+{synergy.attackSpeedBonus}%");
            }
        }
        
        /// <summary>
        /// 특정 플레이어의 종족별 캐릭터 수 가져오기 (AI에서 사용)
        /// </summary>
        public (int humanCount, int orcCount, int elfCount) GetRaceCountsForPlayer(int playerID)
        {
            // 임시로 현재 종족별 캐릭터 수 반환
            int humanCount = raceCharacterMap.ContainsKey(CharacterRace.Human) ? raceCharacterMap[CharacterRace.Human].Count : 0;
            int orcCount = raceCharacterMap.ContainsKey(CharacterRace.Orc) ? raceCharacterMap[CharacterRace.Orc].Count : 0;
            int elfCount = raceCharacterMap.ContainsKey(CharacterRace.Elf) ? raceCharacterMap[CharacterRace.Elf].Count : 0;
            
            return (humanCount, orcCount, elfCount);
        }
    }
}