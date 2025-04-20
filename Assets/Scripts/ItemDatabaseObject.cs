// Assets/Scripts/ItemDatabaseObject.cs

using UnityEngine;

[CreateAssetMenu(fileName = "NewItemDatabase", menuName = "MyGame/Item Database (ScriptableObject)")]
public class ItemDatabaseObject : ScriptableObject
{
    [Header("Item Data List (.asset)")]
    public ItemData[] items;
}
