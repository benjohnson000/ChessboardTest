using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UNITY ONLY!!!
/// Meant to simulate chess board, useful for fetching boardspaces
/// </summary>
public class SIM_ChessBoard : benjohnson.SIM_Singleton<SIM_ChessBoard>
{
    private SIM_BoardSpace[,] boardSpaces = new SIM_BoardSpace[8, 8];

    public SIM_BoardSpace GetBoardSpace(int rankID, int fileID) => boardSpaces[rankID, fileID];

    protected override void Awake()
    {
        base.Awake();
        SpawnBoardSpaces();
        SpawnPieces();
    }

    private void SpawnBoardSpaces()
    {
        Vector2 offset = new Vector2(-3.5f, -3.5f) + (Vector2)transform.position;

        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                // file = horizontal, rank = vertical
                // 7-rank makes rank 0 appear at the top and rank 7 at the bottom
                Vector2 pos = new Vector2(file, 7 - rank) + offset;

                SIM_BoardSpace bspace = SIM_BoardSpace.Create(pos);
                boardSpaces[rank, file] = bspace;
                bspace.transform.parent = transform;
            }
        }
    }

    public void SpawnPieces()
    {
        List<string> whitePieces = new List<string>()
        {
            "--------",
            "--------",
            "--------",
            "--------",
            "--------",
            "--------",
            "PPPPPPPP",
            "RNBQKBNR"
        };

        List<string> blackPieces = new List<string>()
        {
            "RNBQKBNR",
            "PPPPPPPP",
            "--------",
            "--------",
            "--------",
            "--------",
            "--------",
            "--------"
        };

        SpawnPiecesHelper("W", whitePieces);
        SpawnPiecesHelper("B", blackPieces);
    }

    private void SpawnPiecesHelper(string color, List<string> map)
    {
        for (int rank = 0; rank < map.Count; rank++)
        {
            for (int file = 0; file < map[rank].Length; file++)
            {
                char c = map[rank][file];
                if (c != '-')
                {
                    SIM_ChessPiece.Create(
                        GetBoardSpace(rank, file).transform.position,
                        $"{color} {c}"
                    );
                }
            }
        }
    }
}