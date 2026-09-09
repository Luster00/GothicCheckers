namespace GothicCheckers;

// Класс искусственного интеллекта, управляющего чёрными шашками.
public sealed class ComputerPlayer
{
    // Глубина просмотра дерева ходов алгоритмом Minimax.
    private const int SearchDepth = 5;

    // Выбор наиболее выгодного хода из всех доступных компьютеру.
    public Move? ChooseMove(GameEngine game)
    {
        var moves = game.GetAllLegalMoves();
        if (moves.Count == 0) return null;

        Move? best = null;
        int bestScore = int.MinValue;
        int alpha = int.MinValue + 1;
        int beta = int.MaxValue;

        foreach (var move in OrderMoves(game, moves))
        {
            var next = game.CloneForSearch();
            next.TryMove(move.From, move.To, out _);
            int score = Minimax(next, SearchDepth - 1, alpha, beta);
            if (score > bestScore)
            {
                bestScore = score;
                best = move;
            }
            alpha = Math.Max(alpha, bestScore);
        }
        return best;
    }

    // Рекурсивный алгоритм Minimax с Alpha-Beta отсечением.
    private int Minimax(GameEngine state, int depth, int alpha, int beta)
    {
        if (depth <= 0 || state.GameOver) return Evaluate(state);
        var moves = state.GetAllLegalMoves();
        if (moves.Count == 0) return Evaluate(state);

        bool maximizing = state.CurrentPlayer == PlayerColor.Black;
        if (maximizing)
        {
            int value = int.MinValue + 1;
            foreach (var move in OrderMoves(state, moves))
            {
                var next = state.CloneForSearch();
                next.TryMove(move.From, move.To, out _);
                value = Math.Max(value, Minimax(next, depth - 1, alpha, beta));
                alpha = Math.Max(alpha, value);
                if (alpha >= beta) break;
            }
            return value;
        }
        else
        {
            int value = int.MaxValue;
            foreach (var move in OrderMoves(state, moves))
            {
                var next = state.CloneForSearch();
                next.TryMove(move.From, move.To, out _);
                value = Math.Min(value, Minimax(next, depth - 1, alpha, beta));
                beta = Math.Min(beta, value);
                if (alpha >= beta) break;
            }
            return value;
        }
    }

    // Сортировка ходов: перспективные ходы рассматриваются раньше.
    private static IEnumerable<Move> OrderMoves(GameEngine state, IEnumerable<Move> moves) =>
        moves.OrderByDescending(m => m.IsCapture)
             .ThenByDescending(m => state.Board[m.From.Row, m.From.Col]?.IsKing ?? false);

    // Эвристическая оценка позиции с точки зрения компьютера.
    private static int Evaluate(GameEngine state)
    {
        if (state.GameOver)
            return state.Winner == PlayerColor.Black ? 100000 : -100000;

        int score = 0;
        for (int r = 0; r < 8; r++)
        for (int c = 0; c < 8; c++)
        {
            var p = state.Board[r,c];
            if (p is null) continue;
            int value = p.IsKing ? 190 : 100;
            // Поощрение продвижения обычных шашек и контроля центра.
            int advance = p.Color == PlayerColor.Black ? r : 7-r;
            value += p.IsKing ? 0 : advance * 5;
            if (r is >= 2 and <= 5 && c is >= 2 and <= 5) value += 8;
            score += p.Color == PlayerColor.Black ? value : -value;
        }

        // Небольшая оценка мобильности.
        score += (state.GetAllLegalMoves().Count) * (state.CurrentPlayer == PlayerColor.Black ? 2 : -2);
        return score;
    }
}
