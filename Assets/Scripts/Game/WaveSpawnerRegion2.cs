using UnityEngine;

public class WaveSpawnerRegion2 : WaveSpawner
{
    [Header("지역 2 전용 설정")]
    public float region2SpawnMultiplier = 1.5f;
    public bool useSpecialEnemies = true;
    
    protected override void Start()
    {
        // 지역 2 전용 초기화
        if (region2SpawnMultiplier > 1f)
        {
            enemiesPerWave = Mathf.RoundToInt(enemiesPerWave * region2SpawnMultiplier);
        }
        
        base.Start();
    }
    
    protected override void SpawnEnemy()
    {
        if (useSpecialEnemies && Random.Range(0f, 1f) < 0.3f)
        {
            // 30% 확률로 특별한 적 스폰
            SpawnSpecialEnemy();
        }
        else
        {
            base.SpawnEnemy();
        }
    }
    
    private void SpawnSpecialEnemy()
    {
        // 특별한 적 스폰 로직
        Debug.Log("지역 2 특별한 적 스폰!");
        base.SpawnEnemy(); // 기본 스폰으로 대체
    }
} 