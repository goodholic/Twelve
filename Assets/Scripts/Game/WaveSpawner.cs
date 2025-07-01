using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveSpawner : MonoBehaviour
{
    [Header("웨이브 설정")]
    public List<GameObject> enemyPrefabs = new List<GameObject>();
    public Transform[] spawnPoints;
    public float spawnInterval = 2f;
    public int enemiesPerWave = 5;
    
    [Header("웨이브 진행")]
    public int currentWave = 1;
    public int maxWaves = 10;
    public bool isSpawning = false;
    
    private int enemiesSpawned = 0;
    private int enemiesRemaining = 0;
    
    public System.Action OnWaveComplete;
    public System.Action OnAllWavesComplete;
    
    protected virtual void Start()
    {
        StartNextWave();
    }
    
    public void StartNextWave()
    {
        if (currentWave > maxWaves)
        {
            OnAllWavesComplete?.Invoke();
            return;
        }
        
        enemiesSpawned = 0;
        enemiesRemaining = enemiesPerWave;
        isSpawning = true;
        
        StartCoroutine(SpawnWave());
    }
    
    private IEnumerator SpawnWave()
    {
        while (enemiesSpawned < enemiesPerWave)
        {
            SpawnEnemy();
            enemiesSpawned++;
            
            yield return new WaitForSeconds(spawnInterval);
        }
        
        isSpawning = false;
    }
    
    protected virtual void SpawnEnemy()
    {
        if (enemyPrefabs.Count == 0 || spawnPoints.Length == 0) return;
        
        GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        
        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        
        // 적이 죽었을 때 호출될 이벤트 등록
        var enemyComponent = enemy.GetComponent<Enemy>();
        if (enemyComponent != null)
        {
            enemyComponent.OnDeath += OnEnemyDeath;
        }
    }
    
    private void OnEnemyDeath()
    {
        enemiesRemaining--;
        
        if (enemiesRemaining <= 0 && !isSpawning)
        {
            OnWaveComplete?.Invoke();
            currentWave++;
            
            // 다음 웨이브 시작
            Invoke(nameof(StartNextWave), 3f);
        }
    }
}

// 기본 Enemy 클래스
public class Enemy : MonoBehaviour
{
    public System.Action OnDeath;
    
    public void Die()
    {
        OnDeath?.Invoke();
        Destroy(gameObject);
    }
} 