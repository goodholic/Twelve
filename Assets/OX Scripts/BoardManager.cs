using UnityEngine;
using System.Collections.Generic;

public enum TeamType
{
    X,
    O
}

public class BoardManager : MonoBehaviour
{
    [Header("5x5 셀 (직접 에디터에서 세팅)")]
    [SerializeField] private List<Cell> allCells = new List<Cell>();

    private int rows = 5;
    private int cols = 5;

    public bool IsBoardFull()
    {
        foreach (var cell in allCells)
        {
            if (cell.IsEmpty()) return false;
        }
        return true;
    }

    /// <summary>
    /// 해당 (row,col)을 점유하고 있는 oxCharacter를 반환
    /// </summary>
    public oxCharacter GetOccupant(int row, int col)
    {
        Cell cell = GetCell(row, col);
        if (cell == null) return null;
        return cell.Occupant;
    }

    /// <summary>
    /// 해당 (row,col)에 있는 캐릭터의 팀을 반환. 비어있으면 null
    /// </summary>
    public TeamType? GetOccupantTeam(int row, int col)
    {
        Cell cell = GetCell(row, col);
        if (cell == null || cell.IsEmpty()) return null;
        return cell.OccupantTeam;
    }

    /// <summary>
    /// (row,col)에 oxCharacter를 배치 (팀 지정)
    /// </summary>
    public bool PlaceCharacter(int row, int col, oxCharacter prefab, TeamType team)
    {
        Cell cell = GetCell(row, col);
        if (cell == null) return false;
        if (!cell.IsEmpty()) return false;

        oxCharacter newChar = Instantiate(prefab, cell.transform.position, Quaternion.identity);
        newChar.gameObject.name = $"Character_{team}_{row}_{col}";
        cell.SetOccupant(newChar, team);
        return true;
    }

    /// <summary>
    /// (row,col)에 있는 캐릭터를 제거(파괴)
    /// </summary>
    public void RemoveCharacter(int row, int col)
    {
        Cell cell = GetCell(row, col);
        if (cell == null || cell.IsEmpty()) return;
        Destroy(cell.Occupant.gameObject);
        cell.ClearCell();
    }

    /// <summary>
    /// 지정된 팀이 5연속을 만들었는지 검사 (가로,세로,대각)
    /// </summary>
    public bool CheckWin(TeamType team)
    {
        // 가로
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols - 4; c++)
            {
                if (IsLine(team, r, c, 0, 1)) return true;
            }
        }
        // 세로
        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows - 4; r++)
            {
                if (IsLine(team, r, c, 1, 0)) return true;
            }
        }
        // 대각 ↘
        for (int r = 0; r < rows - 4; r++)
        {
            for (int c = 0; c < cols - 4; c++)
            {
                if (IsLine(team, r, c, 1, 1)) return true;
            }
        }
        // 대각 ↙
        for (int r = 0; r < rows - 4; r++)
        {
            for (int c = 4; c < cols; c++)
            {
                if (IsLine(team, r, c, 1, -1)) return true;
            }
        }
        return false;
    }

    private bool IsLine(TeamType team, int startRow, int startCol, int stepRow, int stepCol)
    {
        for (int i = 0; i < 5; i++)
        {
            int rr = startRow + stepRow * i;
            int cc = startCol + stepCol * i;

            TeamType? occupantTeam = GetOccupantTeam(rr, cc);
            if (occupantTeam == null) return false;
            if (occupantTeam.Value != team) return false;
        }
        return true;
    }

    private Cell GetCell(int row, int col)
    {
        if (row < 0 || row >= rows) return null;
        if (col < 0 || col >= cols) return null;

        int index = row * cols + col;
        if (index < 0 || index >= allCells.Count) return null;
        return allCells[index];
    }
}
