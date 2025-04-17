using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CardDatabase", menuName = "MyGame/Card Database")]
public class CardDatabase : ScriptableObject
{
    public List<CardData> cardList = new List<CardData>();
}
