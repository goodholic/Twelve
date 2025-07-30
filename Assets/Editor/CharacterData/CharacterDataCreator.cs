using UnityEngine;
using UnityEditor;

public class CharacterDataCreator
{
    [MenuItem("Twelve/👥 캐릭터 생성/테스트 캐릭터 데이터 생성")]
    public static void CreateTestCharacterData()
    {
        // CharacterData ScriptableObject 생성
        CharacterData newCharacterData = ScriptableObject.CreateInstance<CharacterData>();
        
        // 기본값 설정
        newCharacterData.characterName = "Test PNG Sequence Character";
        newCharacterData.animationType = AnimationType.PNGSequence;
        newCharacterData.hp = 100;
        newCharacterData.attackPower = 50;
        newCharacterData.pngSequenceScale = 1.0f;
        newCharacterData.loopPNGSequences = true;
        
        // Asset으로 저장
        string path = "Assets/TestVideoCharacterData.asset";
        AssetDatabase.CreateAsset(newCharacterData, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // 생성된 asset 선택
        Selection.activeObject = newCharacterData;
        EditorGUIUtility.PingObject(newCharacterData);
        
        Debug.Log($"✅ Test CharacterData 생성 완료: {path}");
        Debug.Log("💡 Inspector에서 PNG 시퀀스 자동 설정 도구를 사용하거나 Idle/Attack PNG Sequence 필드에 PNG 파일들을 할당하세요!");
    }
    
    [MenuItem("Twelve/👥 캐릭터 생성/PNG 시퀀스 캐릭터 데이터 생성")]
    public static void CreatePNGSequenceCharacterData()
    {
        // 새로운 CharacterData 생성
        CharacterData characterData = ScriptableObject.CreateInstance<CharacterData>();
        
        // PNG 시퀀스 애니메이션으로 설정
        characterData.animationType = AnimationType.PNGSequence;
        characterData.characterName = "New PNG Sequence Character";
        
        // 기본값들
        characterData.hp = 100;
        characterData.maxHP = 100;
        characterData.attackPower = 50;
        characterData.pngSequenceScale = 1.0f;
        characterData.loopPNGSequences = true;
        
        // 파일 저장 다이얼로그
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Character Data", 
            "NewVideoCharacter", 
            "asset", 
            "새로운 동영상 캐릭터 데이터를 생성합니다.");
            
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(characterData, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // 생성된 asset 선택하고 Inspector 포커스
            Selection.activeObject = characterData;
            EditorGUIUtility.PingObject(characterData);
            
            Debug.Log($"✅ MOV CharacterData 생성: {path}");
            Debug.Log("🎬 Inspector에서 'MOV 투명배경 동영상 할당' 섹션을 확인하세요!");
            Debug.Log("📁 Idle Video: 대기 상태 투명배경 MOV 파일");
            Debug.Log("⚔️ Attack Video: 공격 상태 투명배경 MOV 파일");
        }
    }
} 