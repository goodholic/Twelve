using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GuildMaster.Data;

namespace GuildMaster.Core
{
    public class SaveManager : MonoBehaviour
    {
        private static SaveManager _instance;
        public static SaveManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SaveManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SaveManager");
                        _instance = go.AddComponent<SaveManager>();
                    }
                }
                return _instance;
            }
        }
        
        [Header("Save Settings")]
        [SerializeField] private string saveDirectory = "Saves";
        [SerializeField] private string saveFileExtension = ".sav";
        [SerializeField] private int maxSaveSlots = 3;
        [SerializeField] private float autoSaveInterval = 300f; // 5분
        [SerializeField] private bool enableCompression = true;
        [SerializeField] private bool enableEncryption = false;
        
        private string savePath;
        private Coroutine autoSaveCoroutine;
        private float lastSaveTime;
        
        // 암호화 키 (실제 게임에서는 더 안전한 방법 사용)
        private const string ENCRYPTION_KEY = "GuildMaster2025SecretKey";

        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            Initialize();
        }
        
        public void Initialize()
        {
            // 저장 경로 설정
            savePath = Path.Combine(Application.persistentDataPath, saveDirectory);
            
            // 디렉토리 생성
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
            
            Debug.Log($"Save path: {savePath}");
        }
        
        public void StartAutoSave()
        {
            if (autoSaveCoroutine != null)
            {
                StopCoroutine(autoSaveCoroutine);
            }
            
            autoSaveCoroutine = StartCoroutine(AutoSaveRoutine());
        }
        
        public void StopAutoSave()
        {
            if (autoSaveCoroutine != null)
            {
                StopCoroutine(autoSaveCoroutine);
                autoSaveCoroutine = null;
            }
        }
        
        IEnumerator AutoSaveRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(autoSaveInterval);
                
                if (Time.time - lastSaveTime >= autoSaveInterval)
                {
                    AutoSave();
                }
            }
        }

        public bool SaveGame(int slotIndex, bool isAutoSave = false)
        {
            try
            {
                SaveData saveData = CreateSaveData(slotIndex, isAutoSave);
                string fileName = GetSaveFileName(slotIndex, isAutoSave);
                string fullPath = Path.Combine(savePath, fileName);
                
                // JSON으로 직렬화
                string json = JsonUtility.ToJson(saveData, true);
                
                // 압축
                if (enableCompression)
                {
                    json = CompressString(json);
                }
                
                // 암호화
                if (enableEncryption)
                {
                    json = EncryptString(json);
                }
                
                // 파일 저장
                File.WriteAllText(fullPath, json);
                
                // 스크린샷 저장
                SaveScreenshot(slotIndex);
                
                lastSaveTime = Time.time;
                
                Debug.Log($"Game saved to slot {slotIndex}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save game: {e.Message}");
                return false;
            }
        }

        public SaveData LoadGame(int slotIndex, bool isAutoSave = false)
        {
            try
            {
                string fileName = GetSaveFileName(slotIndex, isAutoSave);
                string fullPath = Path.Combine(savePath, fileName);
                
                if (!File.Exists(fullPath))
                {
                    Debug.LogWarning($"Save file not found: {fullPath}");
                    return null;
                }
                
                // 파일 읽기
                string json = File.ReadAllText(fullPath);
                
                // 복호화
                if (enableEncryption)
                {
                    json = DecryptString(json);
                }
                
                // 압축 해제
                if (enableCompression)
                {
                    json = DecompressString(json);
                }
                
                // 역직렬화
                SaveData saveData = JsonUtility.FromJson<SaveData>(json);
                
                Debug.Log($"Game loaded from slot {slotIndex}");
                return saveData;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load game: {e.Message}");
                return null;
            }
        }
        
        public void DeleteSave(int slotIndex)
        {
            try
            {
                // 일반 세이브 삭제
                string fileName = GetSaveFileName(slotIndex, false);
                string fullPath = Path.Combine(savePath, fileName);
                
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
                
                // 스크린샷 삭제
                string screenshotPath = GetScreenshotPath(slotIndex);
                if (File.Exists(screenshotPath))
                {
                    File.Delete(screenshotPath);
                }
                
                Debug.Log($"Save slot {slotIndex} deleted");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete save: {e.Message}");
            }
        }
        
        public void AutoSave()
        {
            SaveGame(-1, true); // -1은 자동 저장 슬롯
            Debug.Log("Auto-save completed");
        }
        
        public bool HasAutoSave()
        {
            string fileName = GetSaveFileName(-1, true);
            string fullPath = Path.Combine(savePath, fileName);
            return File.Exists(fullPath);
        }
        
        public SaveData[] GetAllSaveData()
        {
            List<SaveData> saves = new List<SaveData>();
            
            for (int i = 0; i < maxSaveSlots; i++)
            {
                SaveData data = LoadGame(i, false);
                if (data != null)
                {
                    saves.Add(data);
                }
            }
            
            return saves.ToArray();
        }
        
        SaveData CreateSaveData(int slotIndex, bool isAutoSave)
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null) return null;
            
            SaveData saveData = new SaveData
            {
                slotIndex = slotIndex,
                isAutoSave = isAutoSave,
                saveTime = DateTime.Now,
                gameVersion = Application.version,
                
                // 플레이어 데이터
                playerName = PlayerPrefs.GetString("PlayerName", "Player"),
                guildName = gameManager.GuildManager?.GetGuildName() ?? "Guild",
                guildLevel = gameManager.GuildManager?.GetGuildLevel() ?? 1,
                totalPlayTime = Time.time,
                
                // 자원 데이터
                gold = gameManager.ResourceManager?.GetGold() ?? 0,
                wood = gameManager.ResourceManager?.GetWood() ?? 0,
                stone = gameManager.ResourceManager?.GetStone() ?? 0,
                manaStone = gameManager.ResourceManager?.GetManaStone() ?? 0,
                reputation = gameManager.ResourceManager?.GetReputation() ?? 0,
                
                // 게임 진행 상태
                currentChapter = 1, // 기본값으로 설정
                completedQuests = new List<string>(), // TODO: 퀘스트 시스템에서 가져오기
                unlockedCharacters = new List<string>(), // TODO: 캐릭터 시스템에서 가져오기
                
                // 모험가 데이터
                adventurers = SaveAdventurerData(),
                
                // 부대 편성 데이터
                squads = SaveSquadData(),
                
                // 스토리 진행 데이터
                storyProgress = SaveStoryProgress(),
                
                // 스크린샷 경로
                screenshotPath = GetScreenshotPath(slotIndex)
            };
            
            return saveData;
        }

        List<AdventurerSaveData> SaveAdventurerData()
        {
            var adventurers = new List<AdventurerSaveData>();
            var guildManager = GameManager.Instance?.GuildManager;
            
            if (guildManager != null)
            {
                foreach (var unit in guildManager.GetAdventurers())
                {
                    adventurers.Add(new AdventurerSaveData
                    {
                        id = unit.unitId,
                        unitId = unit.unitId,
                        characterId = unit.characterId,
                        name = unit.unitName,
                        level = unit.level,
                        experience = unit.experience,
                        jobClass = unit.jobClass,
                        rarity = unit.rarity,
                        awakenLevel = unit.awakenLevel,
                        equipmentIds = new List<string>() // TODO: 장비 시스템에서 가져오기
                    });
                }
            }
            
            return adventurers;
        }
        
        List<SquadSaveData> SaveSquadData()
        {
            var squads = new List<SquadSaveData>();
            var battleManager = GameManager.Instance?.BattleManager;
            
            if (battleManager != null)
            {
                // TODO: BattleManager에서 부대 데이터 가져오기
            }
            
            return squads;
        }
        
        StoryProgressData SaveStoryProgress()
        {
            var storyProgress = new StoryProgressData();
            var storyManager = GameManager.Instance?.StoryManager;
            
            if (storyManager != null)
            {
                // TODO: StoryManager에서 스토리 진행 데이터 가져오기
            }
            
            return storyProgress;
        }
        
        string GetSaveFileName(int slotIndex, bool isAutoSave)
        {
            if (isAutoSave)
            {
                return $"autosave{saveFileExtension}";
            }
            else
            {
                return $"save_{slotIndex}{saveFileExtension}";
            }
        }
        
        string GetScreenshotPath(int slotIndex)
        {
            return Path.Combine(savePath, $"screenshot_{slotIndex}.png");
        }
        
        void SaveScreenshot(int slotIndex)
        {
            StartCoroutine(CaptureScreenshot(slotIndex));
        }
        
        IEnumerator CaptureScreenshot(int slotIndex)
        {
            yield return new WaitForEndOfFrame();
            
            int width = 320;
            int height = 180;
            
            RenderTexture rt = new RenderTexture(width, height, 24);
            Camera.main.targetTexture = rt;
            
            Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            Camera.main.Render();
            
            RenderTexture.active = rt;
            screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenshot.Apply();
            
            Camera.main.targetTexture = null;
            RenderTexture.active = null;
            Destroy(rt);
            
            byte[] bytes = screenshot.EncodeToPNG();
            string path = GetScreenshotPath(slotIndex);
            File.WriteAllBytes(path, bytes);
            
            Destroy(screenshot);
        }

        // 압축 메서드 (간단한 구현)
        string CompressString(string text)
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(text);
            var ms = new MemoryStream();
            
            using (var zip = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Compress, true))
            {
                zip.Write(buffer, 0, buffer.Length);
            }
            
            ms.Position = 0;
            byte[] compressed = ms.ToArray();
            return Convert.ToBase64String(compressed);
        }
        
        string DecompressString(string compressedText)
        {
            byte[] compressed = Convert.FromBase64String(compressedText);
            
            using (var ms = new MemoryStream(compressed))
            using (var zip = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Decompress))
            using (var reader = new StreamReader(zip))
            {
                return reader.ReadToEnd();
            }
        }
        
        // 암호화 메서드 (간단한 XOR 암호화)
        string EncryptString(string text)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(text);
            byte[] key = System.Text.Encoding.UTF8.GetBytes(ENCRYPTION_KEY);
            
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(data[i] ^ key[i % key.Length]);
            }
            
            return Convert.ToBase64String(data);
        }
        
        string DecryptString(string encryptedText)
        {
            return EncryptString(encryptedText); // XOR은 대칭 암호화
        }

    }
}