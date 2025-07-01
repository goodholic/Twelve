using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace GuildMaster.Dialogue
{
    [System.Serializable]
    public class DialogueData
    {
        public int ID { get; set; }
        public string CharacterName { get; set; }
        public string DialogueText { get; set; }
        public string Position { get; set; } // "Left" or "Right"
        public string Expression { get; set; } // "Normal", "Happy", "Sad", "Angry", etc.
        public string Effect { get; set; } // Special effects like "Shake", "FadeIn", etc.
        public float Duration { get; set; } // Auto advance duration (0 = manual)
        public string NextID { get; set; } // Next dialogue ID or choices
        public string Background { get; set; } // Background image name
        public string BGM { get; set; } // Background music
        public string SFX { get; set; } // Sound effect
    }
    
    [System.Serializable]
    public class CharacterSprite
    {
        public string characterName;
        public GameObject characterPrefab;
        public Sprite normalExpression;
        public Sprite happyExpression;
        public Sprite sadExpression;
        public Sprite angryExpression;
        public Sprite surprisedExpression;
        public Sprite embarrassedExpression;
        
        public Sprite GetExpression(string expression)
        {
            return expression?.ToLower() switch
            {
                "happy" => happyExpression,
                "sad" => sadExpression,
                "angry" => angryExpression,
                "surprised" => surprisedExpression,
                "embarrassed" => embarrassedExpression,
                _ => normalExpression
            };
        }
    }
    
    public class DialogueSystem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private GameObject namePanel;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private GameObject autoPlayIndicator;
        
        [Header("Character Positions")]
        [SerializeField] private Transform leftCharacterPosition;
        [SerializeField] private Transform rightCharacterPosition;
        [SerializeField] private float characterFadeTime = 0.5f;
        
        [Header("Character Database")]
        [SerializeField] private List<CharacterSprite> characterDatabase = new List<CharacterSprite>();
        private Dictionary<string, CharacterSprite> characterLookup;
        
        [Header("Dialogue Settings")]
        [SerializeField] private float textSpeed = 0.05f;
        [SerializeField] private bool autoPlay = false;
        [SerializeField] private float autoPlayDelay = 2f;
        
        [Header("Effects")]
        [SerializeField] private Animator dialoguePanelAnimator;
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;
        
        // Current dialogue state
        private Dictionary<int, DialogueData> dialogueDatabase;
        private DialogueData currentDialogue;
        private Queue<DialogueData> dialogueQueue;
        private bool isTyping = false;
        private bool canProceed = false;
        private Coroutine typingCoroutine;
        
        // Character management
        private GameObject leftCharacter;
        private GameObject rightCharacter;
        private Image leftCharacterImage;
        private Image rightCharacterImage;
        
        // Events
        public event Action<DialogueData> OnDialogueStart;
        public event Action<DialogueData> OnDialogueEnd;
        public event Action OnAllDialoguesComplete;
        
        void Awake()
        {
            // Create character lookup dictionary
            characterLookup = new Dictionary<string, CharacterSprite>();
            foreach (var character in characterDatabase)
            {
                if (!string.IsNullOrEmpty(character.characterName))
                {
                    characterLookup[character.characterName.ToLower()] = character;
                }
            }
            
            // Setup buttons
            if (nextButton != null)
                nextButton.onClick.AddListener(OnNextButtonClicked);
            if (skipButton != null)
                skipButton.onClick.AddListener(SkipDialogue);
                
            dialogueQueue = new Queue<DialogueData>();
            dialogueDatabase = new Dictionary<int, DialogueData>();
            
            // Hide dialogue panel initially
            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);
        }
        
        public void LoadDialogueFromCSV(string csvContent)
        {
            dialogueDatabase.Clear();
            
            string[] lines = csvContent.Split('\n');
            
            // Skip header if exists
            int startIndex = 0;
            if (lines.Length > 0 && lines[0].Contains("ID"))
                startIndex = 1;
                
            for (int i = startIndex; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                DialogueData dialogue = ParseCSVLine(lines[i]);
                if (dialogue != null && dialogue.ID > 0)
                {
                    dialogueDatabase[dialogue.ID] = dialogue;
                }
            }
            
            Debug.Log($"Loaded {dialogueDatabase.Count} dialogue entries");
        }
        
        DialogueData ParseCSVLine(string csvLine)
        {
            try
            {
                // Simple CSV parser - handles basic comma separation
                // For production, use a proper CSV parser that handles quotes and escapes
                string[] values = SplitCSVLine(csvLine);
                
                if (values.Length < 5) return null;
                
                return new DialogueData
                {
                    ID = int.Parse(values[0]),
                    CharacterName = values[1].Trim(),
                    DialogueText = values[2].Trim(),
                    Position = values[3].Trim(),
                    Expression = values.Length > 4 ? values[4].Trim() : "Normal",
                    Effect = values.Length > 5 ? values[5].Trim() : "",
                    Duration = values.Length > 6 ? float.Parse(values[6]) : 0f,
                    NextID = values.Length > 7 ? values[7].Trim() : "",
                    Background = values.Length > 8 ? values[8].Trim() : "",
                    BGM = values.Length > 9 ? values[9].Trim() : "",
                    SFX = values.Length > 10 ? values[10].Trim() : ""
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing CSV line: {csvLine}\n{e.Message}");
                return null;
            }
        }
        
        string[] SplitCSVLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            string currentField = "";
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField);
                    currentField = "";
                }
                else
                {
                    currentField += c;
                }
            }
            
            result.Add(currentField); // Add last field
            return result.ToArray();
        }
        
        public void StartDialogue(int startID)
        {
            if (!dialogueDatabase.ContainsKey(startID))
            {
                Debug.LogError($"Dialogue ID {startID} not found!");
                return;
            }
            
            dialogueQueue.Clear();
            EnqueueDialogueChain(startID);
            
            if (dialoguePanel != null)
                dialoguePanel.SetActive(true);
                
            DisplayNextDialogue();
        }
        
        void EnqueueDialogueChain(int startID)
        {
            HashSet<int> visited = new HashSet<int>();
            int currentID = startID;
            
            while (dialogueDatabase.ContainsKey(currentID) && !visited.Contains(currentID))
            {
                visited.Add(currentID);
                dialogueQueue.Enqueue(dialogueDatabase[currentID]);
                
                string nextID = dialogueDatabase[currentID].NextID;
                if (int.TryParse(nextID, out int next))
                {
                    currentID = next;
                }
                else
                {
                    break; // No more dialogues or choices
                }
            }
        }
        
        void DisplayNextDialogue()
        {
            if (dialogueQueue.Count == 0)
            {
                EndDialogue();
                return;
            }
            
            currentDialogue = dialogueQueue.Dequeue();
            OnDialogueStart?.Invoke(currentDialogue);
            
            // Update character name
            if (characterNameText != null)
            {
                characterNameText.text = currentDialogue.CharacterName;
                if (namePanel != null)
                    namePanel.SetActive(!string.IsNullOrEmpty(currentDialogue.CharacterName));
            }
            
            // Update character sprites
            UpdateCharacterSprite(currentDialogue.CharacterName, currentDialogue.Position, currentDialogue.Expression);
            
            // Apply effects
            ApplyDialogueEffects(currentDialogue);
            
            // Start typing
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeDialogue(currentDialogue.DialogueText));
            
            // Handle auto-play
            if (autoPlay && currentDialogue.Duration > 0)
            {
                StartCoroutine(AutoAdvance(currentDialogue.Duration));
            }
        }
        
        void UpdateCharacterSprite(string characterName, string position, string expression)
        {
            if (string.IsNullOrEmpty(characterName)) return;
            
            string charKey = characterName.ToLower();
            if (!characterLookup.ContainsKey(charKey)) return;
            
            CharacterSprite charData = characterLookup[charKey];
            GameObject targetChar = null;
            Transform targetPos = null;
            
            if (position.ToLower() == "left")
            {
                targetChar = leftCharacter;
                targetPos = leftCharacterPosition;
            }
            else if (position.ToLower() == "right")
            {
                targetChar = rightCharacter;
                targetPos = rightCharacterPosition;
            }
            
            if (targetPos == null) return;
            
            // Create or update character
            if (targetChar == null || targetChar.name != characterName)
            {
                if (targetChar != null)
                    Destroy(targetChar);
                    
                if (charData.characterPrefab != null)
                {
                    targetChar = Instantiate(charData.characterPrefab, targetPos);
                    targetChar.name = characterName;
                }
                else
                {
                    targetChar = new GameObject(characterName);
                    targetChar.transform.SetParent(targetPos);
                    targetChar.transform.localPosition = Vector3.zero;
                    targetChar.transform.localScale = Vector3.one;
                    
                    Image img = targetChar.AddComponent<Image>();
                    img.sprite = charData.GetExpression(expression);
                }
                
                if (position.ToLower() == "left")
                {
                    leftCharacter = targetChar;
                    leftCharacterImage = targetChar.GetComponent<Image>();
                }
                else
                {
                    rightCharacter = targetChar;
                    rightCharacterImage = targetChar.GetComponent<Image>();
                }
                
                // Fade in
                StartCoroutine(FadeCharacter(targetChar, 0f, 1f));
            }
            else
            {
                // Update expression
                Image img = targetChar.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = charData.GetExpression(expression);
                }
            }
            
            // Dim non-speaking character
            if (leftCharacterImage != null)
            {
                Color c = leftCharacterImage.color;
                c.a = (position.ToLower() == "left") ? 1f : 0.5f;
                leftCharacterImage.color = c;
            }
            if (rightCharacterImage != null)
            {
                Color c = rightCharacterImage.color;
                c.a = (position.ToLower() == "right") ? 1f : 0.5f;
                rightCharacterImage.color = c;
            }
        }
        
        IEnumerator TypeDialogue(string text)
        {
            isTyping = true;
            canProceed = false;
            dialogueText.text = "";
            
            foreach (char c in text)
            {
                dialogueText.text += c;
                
                if (c != ' ')
                {
                    PlayTypeSound();
                    yield return new WaitForSeconds(textSpeed);
                }
            }
            
            isTyping = false;
            canProceed = true;
        }
        
        IEnumerator FadeCharacter(GameObject character, float from, float to)
        {
            Image img = character.GetComponent<Image>();
            if (img == null) yield break;
            
            float elapsed = 0f;
            Color color = img.color;
            
            while (elapsed < characterFadeTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / characterFadeTime;
                color.a = Mathf.Lerp(from, to, t);
                img.color = color;
                yield return null;
            }
            
            color.a = to;
            img.color = color;
        }
        
        void ApplyDialogueEffects(DialogueData dialogue)
        {
            // Background
            if (!string.IsNullOrEmpty(dialogue.Background))
            {
                // TODO: Change background
            }
            
            // BGM
            if (!string.IsNullOrEmpty(dialogue.BGM))
            {
                PlayBGM(dialogue.BGM);
            }
            
            // SFX
            if (!string.IsNullOrEmpty(dialogue.SFX))
            {
                PlaySFX(dialogue.SFX);
            }
            
            // Special effects
            if (!string.IsNullOrEmpty(dialogue.Effect))
            {
                switch (dialogue.Effect.ToLower())
                {
                    case "shake":
                        if (dialoguePanelAnimator != null)
                            dialoguePanelAnimator.SetTrigger("Shake");
                        break;
                    case "flash":
                        if (dialoguePanelAnimator != null)
                            dialoguePanelAnimator.SetTrigger("Flash");
                        break;
                }
            }
        }
        
        void OnNextButtonClicked()
        {
            if (isTyping)
            {
                // Complete typing instantly
                if (typingCoroutine != null)
                {
                    StopCoroutine(typingCoroutine);
                    dialogueText.text = currentDialogue.DialogueText;
                    isTyping = false;
                    canProceed = true;
                }
            }
            else if (canProceed)
            {
                DisplayNextDialogue();
            }
        }
        
        void SkipDialogue()
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);
                
            dialogueQueue.Clear();
            EndDialogue();
        }
        
        IEnumerator AutoAdvance(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (canProceed && autoPlay)
            {
                DisplayNextDialogue();
            }
        }
        
        void EndDialogue()
        {
            OnDialogueEnd?.Invoke(currentDialogue);
            OnAllDialoguesComplete?.Invoke();
            
            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);
                
            // Clean up characters
            if (leftCharacter != null)
            {
                StartCoroutine(FadeCharacter(leftCharacter, 1f, 0f));
                Destroy(leftCharacter, characterFadeTime);
                leftCharacter = null;
            }
            if (rightCharacter != null)
            {
                StartCoroutine(FadeCharacter(rightCharacter, 1f, 0f));
                Destroy(rightCharacter, characterFadeTime);
                rightCharacter = null;
            }
        }
        
        void PlayBGM(string bgmName)
        {
            if (bgmSource == null) return;
            
            // TODO: Load BGM from Resources or addressables
            AudioClip clip = Resources.Load<AudioClip>($"BGM/{bgmName}");
            if (clip != null)
            {
                bgmSource.clip = clip;
                bgmSource.Play();
            }
        }
        
        void PlaySFX(string sfxName)
        {
            if (sfxSource == null) return;
            
            // TODO: Load SFX from Resources or addressables
            AudioClip clip = Resources.Load<AudioClip>($"SFX/{sfxName}");
            if (clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }
        
        void PlayTypeSound()
        {
            // TODO: Play typing sound effect
        }
        
        // Public methods for external control
        public void SetAutoPlay(bool enabled)
        {
            autoPlay = enabled;
            if (autoPlayIndicator != null)
                autoPlayIndicator.SetActive(enabled);
        }
        
        public void SetTextSpeed(float speed)
        {
            textSpeed = Mathf.Clamp(speed, 0.01f, 0.2f);
        }
        
        public bool IsDialogueActive()
        {
            return dialoguePanel != null && dialoguePanel.activeSelf;
        }
        
        public void AddCharacterToDatabase(string name, GameObject prefab)
        {
            CharacterSprite newChar = new CharacterSprite
            {
                characterName = name,
                characterPrefab = prefab
            };
            
            characterDatabase.Add(newChar);
            characterLookup[name.ToLower()] = newChar;
        }
    }
}