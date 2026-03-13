/*
Algorithm:
This launches Stockfish as a child process and communicates with it using UCI commands.
It is used on desktop/editor builds where local executables can be started.
It converts the current chess position to FEN, asks Stockfish for a move,
and parses the returned UCI move into a ChessMove.
*/

using System;
using System.Diagnostics;
using System.IO;

public sealed class StockfishEngine : IEngine
{
    private readonly Process process;
    private readonly StreamWriter input;
    private readonly StreamReader output;

    public StockfishEngine(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
            throw new ArgumentException("Stockfish path is null or empty.");

        if (!File.Exists(executablePath))
            throw new FileNotFoundException($"Stockfish executable not found: {executablePath}");

        process = new Process();
        process.StartInfo.FileName = executablePath;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.WorkingDirectory = Path.GetDirectoryName(executablePath);

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to start Stockfish at path: {executablePath}", ex);
        }

        input = process.StandardInput;
        output = process.StandardOutput;

        Send("uci");
        WaitFor("uciok");

        Send("isready");
        WaitFor("readyok");
    }

    public ChessMove GetBestMove(IChessCore chessCore, int thinkTimeMs)
    {
        string fen = chessCore.CurrentFen;

        Send($"position fen {fen}");
        Send($"go movetime {thinkTimeMs}");

        while (true)
        {
            string line = output.ReadLine();
            if (line == null)
                throw new InvalidOperationException("Stockfish stopped responding.");

            if (line.StartsWith("bestmove "))
            {
                string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return ParseUci(parts[1]);
            }
        }
    }

    private ChessMove ParseUci(string uci)
    {
        BoardSquare from = ParseSquare(uci.Substring(0, 2));
        BoardSquare to = ParseSquare(uci.Substring(2, 2));

        PromotionChoice? promotion = null;
        if (uci.Length == 5)
        {
            promotion = uci[4] switch
            {
                'q' => PromotionChoice.Queen,
                'r' => PromotionChoice.Rook,
                'b' => PromotionChoice.Bishop,
                'n' => PromotionChoice.Knight,
                _ => PromotionChoice.Queen
            };
        }

        return new ChessMove(
            from,
            to,
            promotion.HasValue ? MoveSpecialType.Promotion : MoveSpecialType.Normal,
            promotion);
    }

    private BoardSquare ParseSquare(string algebraic)
    {
        int file = algebraic[0] - 'a';
        int rank = algebraic[1] - '1';
        return new BoardSquare(rank, file);
    }

    private void Send(string command)
    {
        input.WriteLine(command);
        input.Flush();
    }

    private void WaitFor(string token)
    {
        while (true)
        {
            string line = output.ReadLine();
            if (line == null)
                throw new InvalidOperationException("Stockfish stopped responding.");

            if (line.Contains(token))
                return;
        }
    }

    public void Dispose()
    {
        try
        {
            Send("quit");
        }
        catch
        {
        }

        if (!process.HasExited)
            process.Kill();

        process.Dispose();
    }
}
