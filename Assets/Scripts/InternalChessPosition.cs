/*
Algorithm:
This class stores the full chess position and generates legal moves.
It creates pseudo-legal moves for each piece, applies them on a copy,
and keeps only the moves that do not leave the moving side in check.
This supports normal moves, captures, castling, en passant, and promotion.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public sealed class InternalChessPosition : IThirdPartyChessPosition, ILoadableFen
{
    private enum PieceType
    {
        None,
        Pawn,
        Knight,
        Bishop,
        Rook,
        Queen,
        King
    }

    private struct Piece
    {
        public PieceType Type;
        public ThirdPartySide Side;

        public bool IsEmpty => Type == PieceType.None;

        public Piece(PieceType type, ThirdPartySide side)
        {
            Type = type;
            Side = side;
        }

        public static Piece Empty => new Piece(PieceType.None, ThirdPartySide.White);
    }

    private Piece[,] board = new Piece[8, 8];

    public ThirdPartySide SideToMove { get; private set; } = ThirdPartySide.White;

    private bool whiteCastleKingSide = true;
    private bool whiteCastleQueenSide = true;
    private bool blackCastleKingSide = true;
    private bool blackCastleQueenSide = true;

    private int? enPassantRank;
    private int? enPassantFile;

    private int halfmoveClock = 0;
    private int fullmoveNumber = 1;

    public IEnumerable<ThirdPartyMove> GetLegalMoves()
    {
        List<ThirdPartyMove> pseudo = GeneratePseudoLegalMoves(SideToMove);
        List<ThirdPartyMove> legal = new List<ThirdPartyMove>();

        foreach (ThirdPartyMove move in pseudo)
        {
            InternalChessPosition copy = Clone();
            copy.ApplyMoveInternal(move);

            ThirdPartySide movingSide = SideToMove;
            if (!copy.IsInCheck(movingSide))
                legal.Add(move);
        }

        return legal;
    }

    public void ApplyMove(string uci)
    {
        ThirdPartyMove move = ParseUci(uci);

        ThirdPartyMove matchingMove = GetLegalMoves().FirstOrDefault(m => m.ToUci() == move.ToUci());
        if (matchingMove == null)
            throw new InvalidOperationException($"Illegal move: {uci}");

        ApplyMoveInternal(matchingMove);
    }

    public string GetFen()
    {
        StringBuilder sb = new StringBuilder();

        for (int rank = 7; rank >= 0; rank--)
        {
            int empty = 0;
            for (int file = 0; file < 8; file++)
            {
                Piece piece = board[rank, file];
                if (piece.IsEmpty)
                {
                    empty++;
                    continue;
                }

                if (empty > 0)
                {
                    sb.Append(empty);
                    empty = 0;
                }

                sb.Append(ToFenChar(piece));
            }

            if (empty > 0)
                sb.Append(empty);

            if (rank > 0)
                sb.Append('/');
        }

        sb.Append(SideToMove == ThirdPartySide.White ? " w " : " b ");

        string castling = "";
        if (whiteCastleKingSide) castling += "K";
        if (whiteCastleQueenSide) castling += "Q";
        if (blackCastleKingSide) castling += "k";
        if (blackCastleQueenSide) castling += "q";
        sb.Append(string.IsNullOrEmpty(castling) ? "-" : castling);
        sb.Append(' ');

        if (enPassantRank.HasValue && enPassantFile.HasValue)
            sb.Append(ToSquare(enPassantRank.Value, enPassantFile.Value));
        else
            sb.Append('-');

        sb.Append($" {halfmoveClock} {fullmoveNumber}");

        return sb.ToString();
    }

    public void LoadFen(string fen)
    {
        string[] parts = fen.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
            throw new ArgumentException("Invalid FEN.");

        board = new Piece[8, 8];

        string[] ranks = parts[0].Split('/');
        if (ranks.Length != 8)
            throw new ArgumentException("Invalid FEN board.");

        for (int fenRank = 0; fenRank < 8; fenRank++)
        {
            int boardRank = 7 - fenRank;
            int file = 0;

            foreach (char c in ranks[fenRank])
            {
                if (char.IsDigit(c))
                {
                    file += c - '0';
                    continue;
                }

                board[boardRank, file] = FromFenChar(c);
                file++;
            }
        }

        SideToMove = parts[1] == "w" ? ThirdPartySide.White : ThirdPartySide.Black;

        string castle = parts[2];
        whiteCastleKingSide = castle.Contains('K');
        whiteCastleQueenSide = castle.Contains('Q');
        blackCastleKingSide = castle.Contains('k');
        blackCastleQueenSide = castle.Contains('q');

        if (parts[3] == "-")
        {
            enPassantRank = null;
            enPassantFile = null;
        }
        else
        {
            ParseSquare(parts[3], out int r, out int f);
            enPassantRank = r;
            enPassantFile = f;
        }

        halfmoveClock = parts.Length > 4 ? int.Parse(parts[4]) : 0;
        fullmoveNumber = parts.Length > 5 ? int.Parse(parts[5]) : 1;
    }

    public bool IsInCheck(ThirdPartySide side)
    {
        FindKing(side, out int kingRank, out int kingFile);
        return IsSquareAttacked(kingRank, kingFile, OpponentOf(side));
    }

    public bool IsCheckmate()
    {
        return IsInCheck(SideToMove) && !GetLegalMoves().Any();
    }

    public bool IsStalemate()
    {
        return !IsInCheck(SideToMove) && !GetLegalMoves().Any();
    }

    public bool IsDraw()
    {
        return IsStalemate() || halfmoveClock >= 100;
    }

    public bool IsOccupied(int rank, int file)
    {
        return !board[rank, file].IsEmpty;
    }

    public static InternalChessPosition CreateStartingPosition()
    {
        InternalChessPosition position = new InternalChessPosition();
        position.LoadFen("rn1qkbnr/pppbpppp/8/3p4/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        position.LoadFen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        return position;
    }

    public static InternalChessPosition CreateEmpty()
    {
        InternalChessPosition position = new InternalChessPosition();
        position.LoadFen("8/8/8/8/8/8/8/8 w - - 0 1");
        return position;
    }

    private InternalChessPosition Clone()
    {
        InternalChessPosition copy = new InternalChessPosition();
        copy.board = (Piece[,])board.Clone();
        copy.SideToMove = SideToMove;
        copy.whiteCastleKingSide = whiteCastleKingSide;
        copy.whiteCastleQueenSide = whiteCastleQueenSide;
        copy.blackCastleKingSide = blackCastleKingSide;
        copy.blackCastleQueenSide = blackCastleQueenSide;
        copy.enPassantRank = enPassantRank;
        copy.enPassantFile = enPassantFile;
        copy.halfmoveClock = halfmoveClock;
        copy.fullmoveNumber = fullmoveNumber;
        return copy;
    }

    private List<ThirdPartyMove> GeneratePseudoLegalMoves(ThirdPartySide side)
    {
        List<ThirdPartyMove> moves = new List<ThirdPartyMove>();

        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                Piece piece = board[rank, file];
                if (piece.IsEmpty || piece.Side != side)
                    continue;

                switch (piece.Type)
                {
                    case PieceType.Pawn:
                        GeneratePawnMoves(rank, file, side, moves);
                        break;
                    case PieceType.Knight:
                        GenerateKnightMoves(rank, file, side, moves);
                        break;
                    case PieceType.Bishop:
                        GenerateSlidingMoves(rank, file, side, moves, new (int, int)[] { (1, 1), (1, -1), (-1, 1), (-1, -1) });
                        break;
                    case PieceType.Rook:
                        GenerateSlidingMoves(rank, file, side, moves, new (int, int)[] { (1, 0), (-1, 0), (0, 1), (0, -1) });
                        break;
                    case PieceType.Queen:
                        GenerateSlidingMoves(rank, file, side, moves, new (int, int)[] { (1, 1), (1, -1), (-1, 1), (-1, -1), (1, 0), (-1, 0), (0, 1), (0, -1) });
                        break;
                    case PieceType.King:
                        GenerateKingMoves(rank, file, side, moves);
                        break;
                }
            }
        }

        return moves;
    }

    private void GeneratePawnMoves(int rank, int file, ThirdPartySide side, List<ThirdPartyMove> moves)
    {
        int dir = side == ThirdPartySide.White ? 1 : -1;
        int startRank = side == ThirdPartySide.White ? 1 : 6;
        int promotionRank = side == ThirdPartySide.White ? 7 : 0;

        int oneStepRank = rank + dir;
        if (InBounds(oneStepRank, file) && board[oneStepRank, file].IsEmpty)
        {
            AddPawnAdvance(rank, file, oneStepRank, file, promotionRank, false, false, moves);

            int twoStepRank = rank + dir * 2;
            if (rank == startRank && board[twoStepRank, file].IsEmpty)
            {
                moves.Add(new ThirdPartyMove
                {
                    FromRank = rank,
                    FromFile = file,
                    ToRank = twoStepRank,
                    ToFile = file
                });
            }
        }

        for (int df = -1; df <= 1; df += 2)
        {
            int targetFile = file + df;
            int targetRank = rank + dir;
            if (!InBounds(targetRank, targetFile))
                continue;

            Piece target = board[targetRank, targetFile];
            if (!target.IsEmpty && target.Side != side)
            {
                AddPawnAdvance(rank, file, targetRank, targetFile, promotionRank, true, false, moves);
            }

            if (enPassantRank == targetRank && enPassantFile == targetFile)
            {
                moves.Add(new ThirdPartyMove
                {
                    FromRank = rank,
                    FromFile = file,
                    ToRank = targetRank,
                    ToFile = targetFile,
                    IsCapture = true,
                    IsEnPassant = true
                });
            }
        }
    }

    private void AddPawnAdvance(int fromRank, int fromFile, int toRank, int toFile, int promotionRank, bool isCapture, bool isEnPassant, List<ThirdPartyMove> moves)
    {
        if (toRank == promotionRank)
        {
            foreach (ThirdPartyPromotionPiece promo in new[]
            {
                ThirdPartyPromotionPiece.Queen,
                ThirdPartyPromotionPiece.Rook,
                ThirdPartyPromotionPiece.Bishop,
                ThirdPartyPromotionPiece.Knight
            })
            {
                moves.Add(new ThirdPartyMove
                {
                    FromRank = fromRank,
                    FromFile = fromFile,
                    ToRank = toRank,
                    ToFile = toFile,
                    IsCapture = isCapture,
                    IsEnPassant = isEnPassant,
                    IsPromotion = true,
                    PromotionPiece = promo
                });
            }
        }
        else
        {
            moves.Add(new ThirdPartyMove
            {
                FromRank = fromRank,
                FromFile = fromFile,
                ToRank = toRank,
                ToFile = toFile,
                IsCapture = isCapture,
                IsEnPassant = isEnPassant
            });
        }
    }

    private void GenerateKnightMoves(int rank, int file, ThirdPartySide side, List<ThirdPartyMove> moves)
    {
        (int dr, int df)[] offsets =
        {
            (2, 1), (2, -1), (-2, 1), (-2, -1),
            (1, 2), (1, -2), (-1, 2), (-1, -2)
        };

        foreach (var (dr, df) in offsets)
        {
            int tr = rank + dr;
            int tf = file + df;
            if (!InBounds(tr, tf))
                continue;

            Piece target = board[tr, tf];
            if (target.IsEmpty || target.Side != side)
            {
                moves.Add(new ThirdPartyMove
                {
                    FromRank = rank,
                    FromFile = file,
                    ToRank = tr,
                    ToFile = tf,
                    IsCapture = !target.IsEmpty
                });
            }
        }
    }

    private void GenerateSlidingMoves(int rank, int file, ThirdPartySide side, List<ThirdPartyMove> moves, (int dr, int df)[] directions)
    {
        foreach (var (dr, df) in directions)
        {
            int tr = rank + dr;
            int tf = file + df;

            while (InBounds(tr, tf))
            {
                Piece target = board[tr, tf];

                if (target.IsEmpty)
                {
                    moves.Add(new ThirdPartyMove
                    {
                        FromRank = rank,
                        FromFile = file,
                        ToRank = tr,
                        ToFile = tf
                    });
                }
                else
                {
                    if (target.Side != side)
                    {
                        moves.Add(new ThirdPartyMove
                        {
                            FromRank = rank,
                            FromFile = file,
                            ToRank = tr,
                            ToFile = tf,
                            IsCapture = true
                        });
                    }
                    break;
                }

                tr += dr;
                tf += df;
            }
        }
    }

    private void GenerateKingMoves(int rank, int file, ThirdPartySide side, List<ThirdPartyMove> moves)
    {
        for (int dr = -1; dr <= 1; dr++)
        {
            for (int df = -1; df <= 1; df++)
            {
                if (dr == 0 && df == 0)
                    continue;

                int tr = rank + dr;
                int tf = file + df;
                if (!InBounds(tr, tf))
                    continue;

                Piece target = board[tr, tf];
                if (target.IsEmpty || target.Side != side)
                {
                    moves.Add(new ThirdPartyMove
                    {
                        FromRank = rank,
                        FromFile = file,
                        ToRank = tr,
                        ToFile = tf,
                        IsCapture = !target.IsEmpty
                    });
                }
            }
        }

        if (side == ThirdPartySide.White && rank == 0 && file == 4)
        {
            if (whiteCastleKingSide &&
                board[0, 5].IsEmpty &&
                board[0, 6].IsEmpty &&
                !IsSquareAttacked(0, 4, ThirdPartySide.Black) &&
                !IsSquareAttacked(0, 5, ThirdPartySide.Black) &&
                !IsSquareAttacked(0, 6, ThirdPartySide.Black))
            {
                moves.Add(new ThirdPartyMove
                {
                    FromRank = 0,
                    FromFile = 4,
                    ToRank = 0,
                    ToFile = 6,
                    IsCastleKingSide = true
                });
            }

            if (whiteCastleQueenSide &&
                board[0, 1].IsEmpty &&
                board[0, 2].IsEmpty &&
                board[0, 3].IsEmpty &&
                !IsSquareAttacked(0, 4, ThirdPartySide.Black) &&
                !IsSquareAttacked(0, 3, ThirdPartySide.Black) &&
                !IsSquareAttacked(0, 2, ThirdPartySide.Black))
            {
                moves.Add(new ThirdPartyMove
                {
                    FromRank = 0,
                    FromFile = 4,
                    ToRank = 0,
                    ToFile = 2,
                    IsCastleQueenSide = true
                });
            }
        }

        if (side == ThirdPartySide.Black && rank == 7 && file == 4)
        {
            if (blackCastleKingSide &&
                board[7, 5].IsEmpty &&
                board[7, 6].IsEmpty &&
                !IsSquareAttacked(7, 4, ThirdPartySide.White) &&
                !IsSquareAttacked(7, 5, ThirdPartySide.White) &&
                !IsSquareAttacked(7, 6, ThirdPartySide.White))
            {
                moves.Add(new ThirdPartyMove
                {
                    FromRank = 7,
                    FromFile = 4,
                    ToRank = 7,
                    ToFile = 6,
                    IsCastleKingSide = true
                });
            }

            if (blackCastleQueenSide &&
                board[7, 1].IsEmpty &&
                board[7, 2].IsEmpty &&
                board[7, 3].IsEmpty &&
                !IsSquareAttacked(7, 4, ThirdPartySide.White) &&
                !IsSquareAttacked(7, 3, ThirdPartySide.White) &&
                !IsSquareAttacked(7, 2, ThirdPartySide.White))
            {
                moves.Add(new ThirdPartyMove
                {
                    FromRank = 7,
                    FromFile = 4,
                    ToRank = 7,
                    ToFile = 2,
                    IsCastleQueenSide = true
                });
            }
        }
    }

    private void ApplyMoveInternal(ThirdPartyMove move)
    {
        Piece moving = board[move.FromRank, move.FromFile];
        Piece target = board[move.ToRank, move.ToFile];

        bool isPawnMove = moving.Type == PieceType.Pawn;
        bool isCapture = move.IsCapture || !target.IsEmpty;

        halfmoveClock = (isPawnMove || isCapture) ? 0 : halfmoveClock + 1;

        if (SideToMove == ThirdPartySide.Black)
            fullmoveNumber++;

        UpdateCastlingRightsBeforeMove(move, moving, target);

        board[move.FromRank, move.FromFile] = Piece.Empty;

        if (move.IsEnPassant)
        {
            int capturedPawnRank = moving.Side == ThirdPartySide.White ? move.ToRank - 1 : move.ToRank + 1;
            board[capturedPawnRank, move.ToFile] = Piece.Empty;
        }

        if (move.IsCastleKingSide)
        {
            board[move.ToRank, move.ToFile] = moving;
            board[move.ToRank, 5] = board[move.ToRank, 7];
            board[move.ToRank, 7] = Piece.Empty;
        }
        else if (move.IsCastleQueenSide)
        {
            board[move.ToRank, move.ToFile] = moving;
            board[move.ToRank, 3] = board[move.ToRank, 0];
            board[move.ToRank, 0] = Piece.Empty;
        }
        else
        {
            board[move.ToRank, move.ToFile] = moving;

            if (move.IsPromotion)
            {
                board[move.ToRank, move.ToFile] = new Piece(ToInternalPromotion(move.PromotionPiece), moving.Side);
            }
        }

        enPassantRank = null;
        enPassantFile = null;

        if (moving.Type == PieceType.Pawn && Math.Abs(move.ToRank - move.FromRank) == 2)
        {
            enPassantRank = (move.ToRank + move.FromRank) / 2;
            enPassantFile = move.FromFile;
        }

        SideToMove = OpponentOf(SideToMove);
    }

    private void UpdateCastlingRightsBeforeMove(ThirdPartyMove move, Piece moving, Piece captured)
    {
        if (moving.Type == PieceType.King)
        {
            if (moving.Side == ThirdPartySide.White)
            {
                whiteCastleKingSide = false;
                whiteCastleQueenSide = false;
            }
            else
            {
                blackCastleKingSide = false;
                blackCastleQueenSide = false;
            }
        }

        if (moving.Type == PieceType.Rook)
        {
            if (moving.Side == ThirdPartySide.White)
            {
                if (move.FromRank == 0 && move.FromFile == 0) whiteCastleQueenSide = false;
                if (move.FromRank == 0 && move.FromFile == 7) whiteCastleKingSide = false;
            }
            else
            {
                if (move.FromRank == 7 && move.FromFile == 0) blackCastleQueenSide = false;
                if (move.FromRank == 7 && move.FromFile == 7) blackCastleKingSide = false;
            }
        }

        if (!captured.IsEmpty && captured.Type == PieceType.Rook)
        {
            if (captured.Side == ThirdPartySide.White)
            {
                if (move.ToRank == 0 && move.ToFile == 0) whiteCastleQueenSide = false;
                if (move.ToRank == 0 && move.ToFile == 7) whiteCastleKingSide = false;
            }
            else
            {
                if (move.ToRank == 7 && move.ToFile == 0) blackCastleQueenSide = false;
                if (move.ToRank == 7 && move.ToFile == 7) blackCastleKingSide = false;
            }
        }
    }

    private bool IsSquareAttacked(int rank, int file, ThirdPartySide bySide)
    {
        int pawnDir = bySide == ThirdPartySide.White ? 1 : -1;
        int pawnSourceRank = rank - pawnDir;
        foreach (int df in new[] { -1, 1 })
        {
            int pf = file + df;
            if (InBounds(pawnSourceRank, pf))
            {
                Piece p = board[pawnSourceRank, pf];
                if (!p.IsEmpty && p.Side == bySide && p.Type == PieceType.Pawn)
                    return true;
            }
        }

        (int dr, int df)[] knightOffsets =
        {
            (2, 1), (2, -1), (-2, 1), (-2, -1),
            (1, 2), (1, -2), (-1, 2), (-1, -2)
        };

        foreach (var (dr, df) in knightOffsets)
        {
            int tr = rank + dr;
            int tf = file + df;
            if (InBounds(tr, tf))
            {
                Piece p = board[tr, tf];
                if (!p.IsEmpty && p.Side == bySide && p.Type == PieceType.Knight)
                    return true;
            }
        }

        if (AttackedBySliding(rank, file, bySide, new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }, PieceType.Rook, PieceType.Queen))
            return true;

        if (AttackedBySliding(rank, file, bySide, new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) }, PieceType.Bishop, PieceType.Queen))
            return true;

        for (int dr = -1; dr <= 1; dr++)
        {
            for (int df = -1; df <= 1; df++)
            {
                if (dr == 0 && df == 0)
                    continue;

                int tr = rank + dr;
                int tf = file + df;
                if (!InBounds(tr, tf))
                    continue;

                Piece p = board[tr, tf];
                if (!p.IsEmpty && p.Side == bySide && p.Type == PieceType.King)
                    return true;
            }
        }

        return false;
    }

    private bool AttackedBySliding(int rank, int file, ThirdPartySide bySide, (int dr, int df)[] dirs, params PieceType[] validTypes)
    {
        foreach (var (dr, df) in dirs)
        {
            int tr = rank + dr;
            int tf = file + df;

            while (InBounds(tr, tf))
            {
                Piece p = board[tr, tf];
                if (!p.IsEmpty)
                {
                    if (p.Side == bySide && validTypes.Contains(p.Type))
                        return true;
                    break;
                }

                tr += dr;
                tf += df;
            }
        }

        return false;
    }

    private void FindKing(ThirdPartySide side, out int rank, out int file)
    {
        for (rank = 0; rank < 8; rank++)
        {
            for (file = 0; file < 8; file++)
            {
                Piece p = board[rank, file];
                if (!p.IsEmpty && p.Side == side && p.Type == PieceType.King)
                    return;
            }
        }

        throw new InvalidOperationException("King not found.");
    }

    private static bool InBounds(int rank, int file)
    {
        return rank >= 0 && rank < 8 && file >= 0 && file < 8;
    }

    private static ThirdPartySide OpponentOf(ThirdPartySide side)
    {
        return side == ThirdPartySide.White ? ThirdPartySide.Black : ThirdPartySide.White;
    }

    private static PieceType ToInternalPromotion(ThirdPartyPromotionPiece promotion)
    {
        return promotion switch
        {
            ThirdPartyPromotionPiece.Queen => PieceType.Queen,
            ThirdPartyPromotionPiece.Rook => PieceType.Rook,
            ThirdPartyPromotionPiece.Bishop => PieceType.Bishop,
            ThirdPartyPromotionPiece.Knight => PieceType.Knight,
            _ => PieceType.Queen
        };
    }

    private static char ToFenChar(Piece piece)
    {
        char c = piece.Type switch
        {
            PieceType.Pawn => 'p',
            PieceType.Knight => 'n',
            PieceType.Bishop => 'b',
            PieceType.Rook => 'r',
            PieceType.Queen => 'q',
            PieceType.King => 'k',
            _ => ' '
        };

        return piece.Side == ThirdPartySide.White ? char.ToUpper(c) : c;
    }

    private static Piece FromFenChar(char c)
    {
        ThirdPartySide side = char.IsUpper(c) ? ThirdPartySide.White : ThirdPartySide.Black;
        char lower = char.ToLower(c);

        PieceType type = lower switch
        {
            'p' => PieceType.Pawn,
            'n' => PieceType.Knight,
            'b' => PieceType.Bishop,
            'r' => PieceType.Rook,
            'q' => PieceType.Queen,
            'k' => PieceType.King,
            _ => PieceType.None
        };

        return new Piece(type, side);
    }

    private static ThirdPartyMove ParseUci(string uci)
    {
        ParseSquare(uci.Substring(0, 2), out int fr, out int ff);
        ParseSquare(uci.Substring(2, 2), out int tr, out int tf);

        ThirdPartyMove move = new ThirdPartyMove
        {
            FromRank = fr,
            FromFile = ff,
            ToRank = tr,
            ToFile = tf
        };

        if (uci.Length == 5)
        {
            move.IsPromotion = true;
            move.PromotionPiece = uci[4] switch
            {
                'q' => ThirdPartyPromotionPiece.Queen,
                'r' => ThirdPartyPromotionPiece.Rook,
                'b' => ThirdPartyPromotionPiece.Bishop,
                'n' => ThirdPartyPromotionPiece.Knight,
                _ => ThirdPartyPromotionPiece.Queen
            };
        }

        return move;
    }

    public void FindKingSquare(ThirdPartySide side, out int rank, out int file)
    {
        FindKing(side, out rank, out file);
    }


    private static void ParseSquare(string text, out int rank, out int file)
    {
        file = text[0] - 'a';
        rank = text[1] - '1';
    }

    private static string ToSquare(int rank, int file)
    {
        return $"{(char)('a' + file)}{(char)('1' + rank)}";
    }
}
