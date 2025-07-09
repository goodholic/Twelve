using UnityEngine;
using System.Collections.Generic;
using CharacterData = GuildMaster.Data.CharacterData;

namespace GuildMaster.Battle
{
    // CharacterData 클래스에 누락된 프로퍼티 추가
    public partial class CharacterData
    {
        // UI 관련
        private Sprite _buttonIcon;
        public Sprite buttonIcon
        {
            get => _buttonIcon ?? iconSprite;
            set => _buttonIcon = value;
        }
        
        // 레벨 관련
        private int _level = 1;
        public int level
        {
            get => _level;
            set => _level = value;
        }
        
        // 공격력
        private int _attackPower;
        public int attackPower
        {
            get => _attackPower > 0 ? _attackPower : baseAttack;
            set => _attackPower = value;
        }
        
        // 별 등급
        private int _star = 1;
        public int star
        {
            get => _star;
            set => _star = value;
        }
        
        // 캐릭터 인덱스
        public int characterIndex { get; set; }
        
        // 종족
        private string _race = "Human";
        public string race
        {
            get => _race;
            set => _race = value;
        }
        
        // 비용
        private int _cost = 1;
        public int cost
        {
            get => _cost;
            set => _cost = value;
        }
        
        // 경험치
        private int _currentExp = 0;
        public int currentExp
        {
            get => _currentExp;
            set => _currentExp = value;
        }
        
        // 다음 레벨까지 필요 경험치
        private int _expToNextLevel = 100;
        public int expToNextLevel
        {
            get => _expToNextLevel;
            set => _expToNextLevel = value;
        }
        
        // 체력
        private int _health;
        public int health
        {
            get => _health > 0 ? _health : baseHP;
            set => _health = value;
        }
        
        // 최대 체력
        private int _maxHP;
        public int maxHP
        {
            get => _maxHP > 0 ? _maxHP : baseHP;
            set => _maxHP = value;
        }
        
        // 이동 속도
        private float _moveSpeed = 5f;
        public float moveSpeed
        {
            get => _moveSpeed;
            set => _moveSpeed = value;
        }
        
        // 공격 속도
        private int _attackSpeed = 1;
        public int attackSpeed
        {
            get => _attackSpeed;
            set => _attackSpeed = value;
        }
        
        // 공격 범위
        private float _attackRange = 1.5f;
        public float attackRange
        {
            get => _attackRange;
            set => _attackRange = value;
        }
        
        // 레어도 (별칭)
        public int starLevel => star;
        
        // 캐릭터 ID (호환성)
        public string characterID
        {
            get => id;
            set => id = value;
        }
        
        // 캐릭터 이름 (호환성)
        public string characterName
        {
            get => name;
            set => name = value;
        }
        
        // 프리팹
        private GameObject _spawnPrefab;
        public GameObject spawnPrefab
        {
            get => _spawnPrefab ?? modelPrefab;
            set => _spawnPrefab = value;
        }
        
        // 모션 프리팹
        public GameObject motionPrefab { get; set; }
        
        // 초기 별 등급
        public int initialStar { get; set; } = 1;
        
        // 범위 타입
        public string rangeType { get; set; } = "Melee";
        
        // 범위 공격 여부
        public bool isAreaAttack { get; set; } = false;
        
        // 버프/서포트 여부
        public bool isBuffSupport { get; set; } = false;
        
        // 프리 슬롯 전용
        public bool isFreeSlotOnly { get; set; } = false;
        
        // 앞면 스프라이트
        public Sprite frontSprite { get; set; }
        
        // 뒷면 스프라이트
        public Sprite backSprite { get; set; }
        
        // 범위 공격 반경
        public float areaAttackRadius { get; set; } = 0f;
    }
}