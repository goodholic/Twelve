using UnityEngine;
using System.Collections.Generic;

namespace GuildMaster.Battle
{
    /// <summary>
    /// 캐릭터의 기본 데이터를 담는 ScriptableObject
    /// CSV 데이터로부터 생성되며, 전투에서 사용됨
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterData", menuName = "GuildMaster/Battle/CharacterData", order = 0)]
    public class CharacterData : ScriptableObject
    {
        [Header("기본 정보")]
        public string ID;
        public string Name;
        public JobClass jobClass;
        public int Level = 1;
        public Rarity rarity = Rarity.Common;
        
        [Header("기본 스탯")]
        public int HP = 100;
        public int MP = 50;
        public int Attack = 10;
        public int Defense = 5;
        public int MagicPower = 5;
        public int Speed = 10;
        
        [Header("전투 스탯")]
        public float CritRate = 0.1f;
        public float CritDamage = 1.5f;
        public float Accuracy = 0.95f;
        public float Evasion = 0.05f;
        
        [Header("스킬")]
        public List<string> skillIDs = new List<string>();
        
        [Header("설명")]
        [TextArea(3, 5)]
        public string Description;
        
        [Header("비주얼")]
        public Sprite sprite;
        public Sprite portrait;
        public GameObject modelPrefab;
        
        [Header("사운드")]
        public AudioClip attackSound;
        public AudioClip hitSound;
        public AudioClip deathSound;
        
        /// <summary>
        /// CSV 데이터로 초기화
        /// </summary>
        public void InitializeFromCSV(string csvLine)
        {
            string[] values = csvLine.Split(',');
            
            if (values.Length >= 15)
            {
                ID = values[0];
                Name = values[1];
                
                // JobClass 파싱
                if (System.Enum.TryParse<JobClass>(values[2], out JobClass parsedJob))
                    jobClass = parsedJob;
                    
                Level = int.Parse(values[3]);
                
                // Rarity 파싱
                if (System.Enum.TryParse<Rarity>(values[4], out Rarity parsedRarity))
                    rarity = parsedRarity;
                    
                HP = int.Parse(values[5]);
                MP = int.Parse(values[6]);
                Attack = int.Parse(values[7]);
                Defense = int.Parse(values[8]);
                MagicPower = int.Parse(values[9]);
                Speed = int.Parse(values[10]);
                
                CritRate = float.Parse(values[11]);
                CritDamage = float.Parse(values[12]);
                Accuracy = float.Parse(values[13]);
                Evasion = float.Parse(values[14]);
                
                // 스킬 ID 파싱 (15, 16, 17번 인덱스)
                skillIDs.Clear();
                if (values.Length > 15 && !string.IsNullOrEmpty(values[15]))
                    skillIDs.Add(values[15]);
                if (values.Length > 16 && !string.IsNullOrEmpty(values[16]))
                    skillIDs.Add(values[16]);
                if (values.Length > 17 && !string.IsNullOrEmpty(values[17]))
                    skillIDs.Add(values[17]);
                
                // 설명 (따옴표 제거)
                if (values.Length > 18)
                {
                    Description = values[18].Trim('"');
                }
            }
        }
        
        /// <summary>
        /// 전투력 계산 (간단한 공식)
        /// </summary>
        public int GetCombatPower()
        {
            int basePower = HP + (Attack * 5) + (Defense * 3) + (MagicPower * 4) + (Speed * 2);
            float rarityMultiplier = 1f + ((int)rarity * 0.2f);
            return Mathf.RoundToInt(basePower * rarityMultiplier);
        }
        
        /// <summary>
        /// 희귀도에 따른 색상
        /// </summary>
        public Color GetRarityColor()
        {
            switch (rarity)
            {
                case Rarity.Common:
                    return Color.gray;
                case Rarity.Uncommon:
                    return Color.green;
                case Rarity.Rare:
                    return new Color(0.2f, 0.6f, 1f); // 파란색
                case Rarity.Epic:
                    return new Color(0.6f, 0.2f, 0.8f); // 보라색
                case Rarity.Legendary:
                    return new Color(1f, 0.6f, 0f); // 주황색
                default:
                    return Color.white;
            }
        }
        
        /// <summary>
        /// 직업 아이콘 경로
        /// </summary>
        public string GetJobIconPath()
        {
            return $"Icons/Jobs/{jobClass}";
        }
        
        /// <summary>
        /// 캐릭터 정보 문자열
        /// </summary>
        public string GetInfoText()
        {
            return $"{Name} Lv.{Level}\n" +
                   $"{jobClass} ({rarity})\n" +
                   $"HP: {HP} ATK: {Attack}\n" +
                   $"DEF: {Defense} SPD: {Speed}";
        }
        
        /// <summary>
        /// 복사본 생성
        /// </summary>
        public CharacterData Clone()
        {
            CharacterData clone = CreateInstance<CharacterData>();
            
            clone.ID = ID;
            clone.Name = Name;
            clone.jobClass = jobClass;
            clone.Level = Level;
            clone.rarity = rarity;
            
            clone.HP = HP;
            clone.MP = MP;
            clone.Attack = Attack;
            clone.Defense = Defense;
            clone.MagicPower = MagicPower;
            clone.Speed = Speed;
            
            clone.CritRate = CritRate;
            clone.CritDamage = CritDamage;
            clone.Accuracy = Accuracy;
            clone.Evasion = Evasion;
            
            clone.skillIDs = new List<string>(skillIDs);
            clone.Description = Description;
            
            clone.sprite = sprite;
            clone.portrait = portrait;
            clone.modelPrefab = modelPrefab;
            
            clone.attackSound = attackSound;
            clone.hitSound = hitSound;
            clone.deathSound = deathSound;
            
            return clone;
        }
    }
}