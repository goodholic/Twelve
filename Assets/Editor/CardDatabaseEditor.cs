#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(CardDatabase))]
public class CardDatabaseEditor : Editor
{
    private CardDatabase db;
    private string bulkInsertText = "";

    private void OnEnable()
    {
        db = (CardDatabase)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("=== Bulk Insert ===", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "라인별: '카드이름,레벨,경험치,스프라이트경로'\n" +
            "예) 슬라임,1,0,Icons/Slime\n" +
            "    드래곤,5,20,Icons/Dragon\n\n" +
            "스프라이트경로는 Resources.Load<Sprite>(...) 로 로딩",
            MessageType.Info);

        bulkInsertText = EditorGUILayout.TextArea(bulkInsertText, GUILayout.MinHeight(60));

        if (GUILayout.Button("Add from Text"))
        {
            AddFromText(bulkInsertText);
        }
    }

    private void AddFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.LogWarning("입력 텍스트 없음.");
            return;
        }

        var lines = text.Split('\n')
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrEmpty(l))
                        .ToArray();

        int addedCount = 0;
        foreach (var line in lines)
        {
            var tokens = line.Split(',');
            if (tokens.Length < 4)
            {
                Debug.LogWarning($"파싱 실패: {line}");
                continue;
            }

            string name       = tokens[0].Trim();
            int lv            = int.Parse(tokens[1].Trim());
            int exp           = int.Parse(tokens[2].Trim());
            string spritePath = tokens[3].Trim();

            // 예: Resources/Icons/Slime 에 Slime 스프라이트가 있다고 가정
            var sp = Resources.Load<Sprite>(spritePath);

            CardData newCard = new CardData
            {
                cardName   = name,
                level      = lv,
                currentExp = exp,
                cardSprite = sp
            };
            db.cardList.Add(newCard);
            addedCount++;
        }

        EditorUtility.SetDirty(db);
        Debug.Log($"Bulk Insert: {addedCount}개 카드 추가됨");
    }
}
#endif
