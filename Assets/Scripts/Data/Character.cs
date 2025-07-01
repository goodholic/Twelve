using UnityEngine;

namespace GuildMaster.Data
{
    public class Character : MonoBehaviour
    {
        [Header("캐릭터 속성")]
        public bool isHero = false;
        public float attackPower = 10f;
        public float attackRange = 1.5f;
        public Canvas hpBarCanvas;
        
        [Header("UI 참조")]
        private Transform bulletPanel;
        
        /// <summary>
        /// 총알 패널 설정
        /// </summary>
        public void SetBulletPanel(Transform panel)
        {
            bulletPanel = panel;
        }
        
        /// <summary>
        /// 총알 패널 가져오기
        /// </summary>
        public Transform GetBulletPanel()
        {
            return bulletPanel;
        }
    }
} 