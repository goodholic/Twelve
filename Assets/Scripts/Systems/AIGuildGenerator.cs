using UnityEngine;
using System.Collections.Generic;
using GuildMaster.Core;
using GuildMaster.Guild;

namespace GuildMaster.Systems
{
    /// <summary>
    /// AI 길드 생성 시스템
    /// </summary>
    public class AIGuildGenerator : MonoBehaviour
    {
        private static AIGuildGenerator _instance;
        public static AIGuildGenerator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AIGuildGenerator>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("AIGuildGenerator");
                        _instance = go.AddComponent<AIGuildGenerator>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("Guild Generation Settings")]
        [SerializeField] private int maxAIGuilds = 10;
        [SerializeField] private List<string> guildNameTemplates = new List<string>();
        [SerializeField] private int minGuildLevel = 1;
        [SerializeField] private int maxGuildLevel = 50;

        private List<AIGuildData> generatedGuilds = new List<AIGuildData>();

        [System.Serializable]
        public class AIGuildData
        {
            public string guildId;
            public string guildName;
            public int guildLevel;
            public int memberCount;
            public int reputation;
            public Vector2 territoryPosition;
            public List<string> memberIds;

            public AIGuildData()
            {
                guildId = System.Guid.NewGuid().ToString();
                memberIds = new List<string>();
            }
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public AIGuildData GenerateRandomGuild()
        {
            var guild = new AIGuildData();
            guild.guildName = GenerateRandomGuildName();
            guild.guildLevel = Random.Range(minGuildLevel, maxGuildLevel + 1);
            guild.memberCount = Random.Range(5, 20);
            guild.reputation = guild.guildLevel * Random.Range(50, 200);
            guild.territoryPosition = new Vector2(Random.Range(-100f, 100f), Random.Range(-100f, 100f));

            // 멤버 생성
            for (int i = 0; i < guild.memberCount; i++)
            {
                guild.memberIds.Add(System.Guid.NewGuid().ToString());
            }

            generatedGuilds.Add(guild);
            return guild;
        }

        public List<AIGuildData> GenerateGuilds(int count)
        {
            var guilds = new List<AIGuildData>();
            for (int i = 0; i < count; i++)
            {
                guilds.Add(GenerateRandomGuild());
            }
            return guilds;
        }

        string GenerateRandomGuildName()
        {
            if (guildNameTemplates.Count == 0)
            {
                // 기본 템플릿들
                guildNameTemplates.AddRange(new string[]
                {
                    "Dragon {0}", "Phoenix {0}", "Shadow {0}", "Golden {0}", "Silver {0}",
                    "Iron {0}", "Storm {0}", "Fire {0}", "Ice {0}", "Lightning {0}"
                });
            }

            string[] suffixes = { "Warriors", "Knights", "Guild", "Brotherhood", "Alliance", "Order", "Legion", "Company" };
            string template = guildNameTemplates[Random.Range(0, guildNameTemplates.Count)];
            string suffix = suffixes[Random.Range(0, suffixes.Length)];

            return string.Format(template, suffix);
        }

        public List<AIGuildData> GetAllGuilds()
        {
            return new List<AIGuildData>(generatedGuilds);
        }

        public AIGuildData GetGuildById(string guildId)
        {
            return generatedGuilds.Find(g => g.guildId == guildId);
        }

        public int Count => generatedGuilds.Count;
    }
} 