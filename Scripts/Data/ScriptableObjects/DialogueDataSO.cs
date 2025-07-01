using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuildMaster.Data
{
    [CreateAssetMenu(fileName = "DialogueData", menuName = "GuildMaster/Data/Dialogue Data")]
    public class DialogueDataSO : ScriptableObject
    {
        public string dialogueName;
        public List<DialogueEntry> dialogues = new List<DialogueEntry>();
        
        [System.Serializable]
        public class DialogueEntry
        {
            public int id;
            public string characterName;
            public string dialogueText;
            public string position;
            public string expression;
            public string effect;
            public float duration;
            public int nextId;
            public string background;
            public string bgm;
            public string sfx;
        }
        
        public void Initialize()
        {
            // 초기화 로직이 필요한 경우
        }
        
        public DialogueEntry GetDialogue(int id)
        {
            return dialogues.Find(d => d.id == id);
        }
        
        public List<DialogueEntry> GetAllDialogues()
        {
            return new List<DialogueEntry>(dialogues);
        }
    }
} 