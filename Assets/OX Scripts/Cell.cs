using UnityEngine;

public class Cell : MonoBehaviour
{
    [SerializeField] private int row;
    [SerializeField] private int col;

    // 원래 Character occupant -> oxCharacter occupant
    private oxCharacter occupant;
    private TeamType occupantTeam;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    public int Row => row;
    public int Col => col;

    // Occupant를 oxCharacter로 반환
    public oxCharacter Occupant => occupant;
    public TeamType OccupantTeam => occupantTeam;

    public void Init(int r, int c)
    {
        row = r;
        col = c;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    public void SetOccupant(oxCharacter newCharacter, TeamType team)
    {
        occupant = newCharacter;
        occupantTeam = team;
    }

    public bool IsEmpty()
    {
        return occupant == null;
    }

    public void ClearCell()
    {
        occupant = null;
    }

    private void OnMouseEnter()
    {
        if (spriteRenderer == null) return;
        if (!IsEmpty()) spriteRenderer.color = Color.red;
    }

    private void OnMouseExit()
    {
        if (spriteRenderer == null) return;
        spriteRenderer.color = originalColor;
    }
}
