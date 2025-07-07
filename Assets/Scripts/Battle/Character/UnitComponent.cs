using UnityEngine;
using GuildMaster.Battle;
using GuildMaster.Data;

namespace GuildMaster.Battle
{
    [AddComponentMenu("GuildMaster/Battle/UnitStatus Component")]
    public class UnitComponent : MonoBehaviour
    {
        [System.NonSerialized]
        private UnitStatus _unitData;
        
        public UnitStatus UnitData
        {
            get => _unitData;
            set => _unitData = value;
        }
        
        // UnitStatus 데이터를 설정하는 메서드
        public void SetUnit(UnitStatus unit)
        {
            _unitData = unit;
            if (_unitData != null)
            {
                _unitData.transform = transform;
                _unitData.gameObject = gameObject;
            }
        }
        
        // 새로운 UnitStatus을 생성하고 설정하는 메서드
        public void CreateUnit(string name, int level, JobClass jobClass, Rarity rank = Rarity.Common)
        {
            _unitData = new UnitStatus(name, level, jobClass, rank);
            _unitData.transform = transform;
            _unitData.gameObject = gameObject;
        }
        
        // UnitStatus 데이터에 쉽게 접근할 수 있도록 하는 프로퍼티들
        public string UnitName => _unitData?.unitName ?? "";
        public int Level => _unitData?.level ?? 0;
        public JobClass JobClass => _unitData?.jobClass ?? JobClass.None;
        public bool IsAlive => _unitData?.isAlive ?? false;
        public float CurrentHP => _unitData?.currentHP ?? 0f;
        public float MaxHP => _unitData?.maxHP ?? 0f;
        
        // Unity 이벤트 메서드들
        void Awake()
        {
            if (_unitData == null)
            {
                _unitData = new UnitStatus("Default", 1, JobClass.None);
                _unitData.transform = transform;
                _unitData.gameObject = gameObject;
            }
        }
        
        void OnDestroy()
        {
            _unitData = null;
        }
    }
}