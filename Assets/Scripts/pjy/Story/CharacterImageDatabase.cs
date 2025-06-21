using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace pjy.Story
{
    /// <summary>
    /// 캐릭터 이미지 데이터
    /// </summary>
    [System.Serializable]
    public class CharacterImageData
    {
        [Header("캐릭터 정보")]
        public string characterName;
        public CharacterEmotion emotion = CharacterEmotion.Normal;
        
        [Header("이미지")]
        public Sprite characterSprite;
        
        [Header("설정")]
        public Vector2 imageSize = Vector2.one;
        public Vector2 imageOffset = Vector2.zero;
        public Color imageColor = Color.white;
        
        [Header("애니메이션")]
        public bool hasAnimation = false;
        public float animationDuration = 0.3f;
        public AnimationType animationType = AnimationType.FadeIn;
    }

    /// <summary>
    /// 애니메이션 타입
    /// </summary>
    public enum AnimationType
    {
        None,
        FadeIn,
        SlideFromLeft,
        SlideFromRight,
        ScaleUp,
        Bounce
    }

    /// <summary>
    /// 캐릭터 이미지 데이터베이스
    /// - 스토리 대화용 캐릭터 이미지 관리
    /// - 감정별 스프라이트 지원
    /// - 애니메이션 설정 지원
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterImageDatabase", menuName = "pjy/Story/CharacterImageDatabase")]
    public class CharacterImageDatabase : ScriptableObject
    {
        [Header("캐릭터 이미지 데이터")]
        [SerializeField] private List<CharacterImageData> characterImages = new List<CharacterImageData>();
        
        [Header("기본 설정")]
        [SerializeField] private Sprite defaultCharacterSprite;
        [SerializeField] private Color defaultImageColor = Color.white;

        /// <summary>
        /// 캐릭터 이미지 가져오기
        /// </summary>
        public Sprite GetCharacterImage(string characterName, CharacterEmotion emotion = CharacterEmotion.Normal)
        {
            var imageData = characterImages.FirstOrDefault(img => 
                img.characterName.Equals(characterName, System.StringComparison.OrdinalIgnoreCase) &&
                img.emotion == emotion
            );

            if (imageData != null && imageData.characterSprite != null)
            {
                return imageData.characterSprite;
            }

            // 감정이 일치하지 않으면 기본 감정(Normal) 시도
            if (emotion != CharacterEmotion.Normal)
            {
                var normalImageData = characterImages.FirstOrDefault(img => 
                    img.characterName.Equals(characterName, System.StringComparison.OrdinalIgnoreCase) &&
                    img.emotion == CharacterEmotion.Normal
                );

                if (normalImageData != null && normalImageData.characterSprite != null)
                {
                    return normalImageData.characterSprite;
                }
            }

            // 캐릭터의 첫 번째 이미지 반환
            var firstImage = characterImages.FirstOrDefault(img => 
                img.characterName.Equals(characterName, System.StringComparison.OrdinalIgnoreCase)
            );

            if (firstImage != null && firstImage.characterSprite != null)
            {
                return firstImage.characterSprite;
            }

            // 기본 이미지 반환
            return defaultCharacterSprite;
        }

        /// <summary>
        /// 캐릭터 이미지 데이터 가져오기
        /// </summary>
        public CharacterImageData GetCharacterImageData(string characterName, CharacterEmotion emotion = CharacterEmotion.Normal)
        {
            return characterImages.FirstOrDefault(img => 
                img.characterName.Equals(characterName, System.StringComparison.OrdinalIgnoreCase) &&
                img.emotion == emotion
            );
        }

        /// <summary>
        /// 캐릭터의 모든 감정 이미지 가져오기
        /// </summary>
        public List<CharacterImageData> GetCharacterAllEmotions(string characterName)
        {
            return characterImages.Where(img => 
                img.characterName.Equals(characterName, System.StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        /// <summary>
        /// 등록된 모든 캐릭터 이름 가져오기
        /// </summary>
        public List<string> GetAllCharacterNames()
        {
            return characterImages
                .Select(img => img.characterName)
                .Distinct(System.StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// 특정 캐릭터의 모든 감정 가져오기
        /// </summary>
        public List<CharacterEmotion> GetCharacterEmotions(string characterName)
        {
            return characterImages
                .Where(img => img.characterName.Equals(characterName, System.StringComparison.OrdinalIgnoreCase))
                .Select(img => img.emotion)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// 캐릭터 이미지 추가 (에디터용)
        /// </summary>
        public void AddCharacterImage(CharacterImageData imageData)
        {
            if (imageData != null && !characterImages.Contains(imageData))
            {
                characterImages.Add(imageData);
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        /// <summary>
        /// 캐릭터 이미지 제거 (에디터용)
        /// </summary>
        public void RemoveCharacterImage(CharacterImageData imageData)
        {
            if (characterImages.Contains(imageData))
            {
                characterImages.Remove(imageData);
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        /// <summary>
        /// 캐릭터 이미지 존재 여부 확인
        /// </summary>
        public bool HasCharacterImage(string characterName, CharacterEmotion emotion = CharacterEmotion.Normal)
        {
            return characterImages.Any(img => 
                img.characterName.Equals(characterName, System.StringComparison.OrdinalIgnoreCase) &&
                img.emotion == emotion
            );
        }

        /// <summary>
        /// 캐릭터 존재 여부 확인
        /// </summary>
        public bool HasCharacter(string characterName)
        {
            return characterImages.Any(img => 
                img.characterName.Equals(characterName, System.StringComparison.OrdinalIgnoreCase)
            );
        }

        /// <summary>
        /// 이미지 개수 가져오기
        /// </summary>
        public int GetImageCount()
        {
            return characterImages.Count;
        }

        /// <summary>
        /// 캐릭터 개수 가져오기
        /// </summary>
        public int GetCharacterCount()
        {
            return GetAllCharacterNames().Count;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 캐릭터 이미지 데이터베이스 검증
        /// </summary>
        [UnityEditor.MenuItem("Tools/Story/Validate Character Image Database")]
        public static void ValidateCharacterImageDatabase()
        {
            CharacterImageDatabase[] databases = Resources.FindObjectsOfTypeAll<CharacterImageDatabase>();
            
            foreach (var db in databases)
            {
                Debug.Log($"[CharacterImageDatabase] 검증 시작: {db.name}");
                
                // 누락된 스프라이트 체크
                var missingSprites = db.characterImages.Where(img => img.characterSprite == null);
                foreach (var missing in missingSprites)
                {
                    Debug.LogWarning($"[CharacterImageDatabase] 누락된 스프라이트: {missing.characterName} - {missing.emotion}");
                }
                
                // 중복 체크
                var duplicates = db.characterImages
                    .GroupBy(img => new { img.characterName, img.emotion })
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);
                
                foreach (var duplicate in duplicates)
                {
                    Debug.LogWarning($"[CharacterImageDatabase] 중복된 이미지: {duplicate.characterName} - {duplicate.emotion}");
                }
                
                Debug.Log($"[CharacterImageDatabase] 검증 완료. 총 이미지: {db.GetImageCount()}, 캐릭터 수: {db.GetCharacterCount()}");
            }
        }
#endif
    }
} 