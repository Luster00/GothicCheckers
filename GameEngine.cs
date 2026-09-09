namespace GothicCheckers;

// Центральный класс игровой логики.
public sealed class GameEngine
{
    // Двумерный массив 8×8. В каждой ячейке находится шашка или null.
    public Piece?[,] Board { get; } = new Piece?[8, 8];
    // Игрок, который должен сделать следующий ход.
    public PlayerColor CurrentPlayer { get; private set; } = PlayerColor.White;
    // Флаг завершения партии.
    public bool GameOver { get; private set; }
    // Победитель игры после её окончания.
    public PlayerColor? Winner { get; private set; }

    // Координаты шашки, которая обязана продолжить серию взятий.
    private Pos? forcedPiece;

    // Создание новой партии и расстановка шашек в начальную позицию.
    public void NewGame()
    {
        Array.Clear(Board);
        for (int c = 0; c < 8; c++)
        {
            Board[0, c] = new Piece(PlayerColor.Black);
            Board[1, c] = new Piece(PlayerColor.Black);
            Board[6, c] = new Piece(PlayerColor.White);
            Board[7, c] = new Piece(PlayerColor.White);
        }
        CurrentPlayer = PlayerColor.White;
        GameOver = false;
        Winner = null;
        forcedPiece = null;
    }

    // Полное копирование текущего состояния для алгоритма искусственного интеллекта.
    public GameEngine CloneForSearch()
    {
        var copy = new GameEngine();
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                copy.Board[r, c] = Board[r, c]?.Clone();
        copy.CurrentPlayer = CurrentPlayer;
        copy.GameOver = GameOver;
        copy.Winner = Winner;
        copy.forcedPiece = forcedPiece;
        return copy;
    }

    // Проверка обязательного продолжения серии взятий.
    public bool IsForcedPiece(Pos p) => forcedPiece == p;
    public bool IsComputerTurn => CurrentPlayer == PlayerColor.Black && !GameOver;

    // Получение всех допустимых ходов выбранной шашки.
    public List<Move> GetMovesForPiece(Pos p, bool applyMaxCaptureRule = true)
    {
        if (!p.Inside || Board[p.Row, p.Col] is not Piece piece || piece.Color != CurrentPlayer)
            return new();
        if (forcedPiece.HasValue && forcedPiece.Value != p)
            return new();

        var captures = GetCaptures(p);
        if (forcedPiece.HasValue)
            return captures; // После первого взятия серия обязательна до конца.

        var result = GetQuietMoves(p);
        if (captures.Count > 0)
        {
            // Если игрок выбирает взятие, показываем только начала серий
            // с максимальным количеством взятых фигур.
            int globalMax = GetGlobalMaximumCaptureCount(CurrentPlayer);
            if (applyMaxCaptureRule)
                captures = captures.Where(m => MaxCaptureSequenceFromMove(m) == globalMax).ToList();
            result.AddRange(captures);
        }
        return result;
    }

    // Получение всех доступных ходов игрока, чей сейчас ход.
    public List<Move> GetAllLegalMoves()
    {
        if (forcedPiece.HasValue)
            return GetMovesForPiece(forcedPiece.Value);
        var list = new List<Move>();
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                if (Board[r, c]?.Color == CurrentPlayer)
                    list.AddRange(GetMovesForPiece(new Pos(r, c)));
        return list;
    }

    // Проверка выбранного хода, его выполнение и обработка продолжения серии взятий.
    public bool TryMove(Pos from, Pos to, out string? error)
    {
        error = null;
        if (GameOver) { error = "Игра уже завершена."; return false; }
        var move = GetMovesForPiece(from).FirstOrDefault(m => m.To == to);
        if (move is null) { error = "Этот ход запрещён правилами готических шашек."; return false; }

        Execute(move);
        PromoteIfNeeded(move.To);

        if (move.IsCapture)
        {
            // ВАЖНО: после взятия нельзя завершить ход, если этой же фигурой
            // можно взять ещё хотя бы одну шашку.
            var continuation = GetCaptures(move.To);
            if (continuation.Count > 0)
            {
                forcedPiece = move.To;
                return true;
            }
        }

        EndTurn();
        return true;
    }

    // Непосредственное перемещение шашки и удаление побитой фигуры.
    private void Execute(Move move)
    {
        var piece = Board[move.From.Row, move.From.Col]!;
        Board[move.From.Row, move.From.Col] = null;
        if (move.Captured is Pos cap) Board[cap.Row, cap.Col] = null;
        Board[move.To.Row, move.To.Col] = piece;
    }

    // Превращение простой шашки в дамку при достижении последнего ряда.
    private void PromoteIfNeeded(Pos p)
    {
        var piece = Board[p.Row, p.Col];
        if (piece is null || piece.IsKing) return;
        if ((piece.Color == PlayerColor.White && p.Row == 0) ||
            (piece.Color == PlayerColor.Black && p.Row == 7))
            piece.IsKing = true;
    }

    // Передача хода сопернику и проверка условий окончания игры.
    private void EndTurn()
    {
        forcedPiece = null;
        CurrentPlayer = CurrentPlayer == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
        if (!Board.Cast<Piece?>().Any(x => x?.Color == CurrentPlayer) || GetAllLegalMoves().Count == 0)
        {
            GameOver = true;
            Winner = CurrentPlayer == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
        }
    }

    // Формирование обычных ходов без взятия.
    private List<Move> GetQuietMoves(Pos p)
    {
        var piece = Board[p.Row, p.Col];
        if (piece is null) return new();
        var result = new List<Move>();
        if (!piece.IsKing)
        {
            int dir = piece.Color == PlayerColor.White ? -1 : 1;
            foreach (int dc in new[] { -1, 1 })
            {
                var to = new Pos(p.Row + dir, p.Col + dc);
                if (to.Inside && Board[to.Row, to.Col] is null) result.Add(new Move(p, to));
            }
            return result;
        }
        foreach (var (dr, dc) in DiagonalDirs)
        {
            var q = new Pos(p.Row + dr, p.Col + dc);
            while (q.Inside && Board[q.Row, q.Col] is null)
            {
                result.Add(new Move(p, q));
                q = new Pos(q.Row + dr, q.Col + dc);
            }
        }
        return result;
    }

    // Выбор алгоритма поиска взятий для простой шашки или дамки.
    private List<Move> GetCaptures(Pos p)
    {
        if (!p.Inside || Board[p.Row, p.Col] is not Piece piece) return new();
        return piece.IsKing ? GetKingCaptures(p) : GetManCaptures(p);
    }

    // Поиск всех возможных взятий простой шашки.
    private List<Move> GetManCaptures(Pos p)
    {
        var piece = Board[p.Row, p.Col];
        if (piece is null) return new();
        int dir = piece.Color == PlayerColor.White ? -1 : 1;
        var dirs = new (int dr, int dc)[] { (dir,-1), (dir,1), (dir,0), (0,-1), (0,1) };
        var result = new List<Move>();
        foreach (var (dr, dc) in dirs)
        {
            var enemy = new Pos(p.Row + dr, p.Col + dc);
            var landing = new Pos(p.Row + 2*dr, p.Col + 2*dc);
            if (!enemy.Inside || !landing.Inside) continue;
            var target = Board[enemy.Row, enemy.Col];
            if (target is not null && target.Color != piece.Color && Board[landing.Row, landing.Col] is null)
                result.Add(new Move(p, landing, enemy));
        }
        return result;
    }

    // Поиск всех возможных взятий дамки.
    private List<Move> GetKingCaptures(Pos p)
    {
        var piece = Board[p.Row, p.Col];
        if (piece is null) return new();
        var result = new List<Move>();
        foreach (var (dr, dc) in AllDirs)
        {
            var q = new Pos(p.Row + dr, p.Col + dc);
            while (q.Inside && Board[q.Row, q.Col] is null) q = new Pos(q.Row + dr, q.Col + dc);
            if (!q.Inside) continue;
            var enemy = Board[q.Row, q.Col];
            if (enemy is null || enemy.Color == piece.Color) continue;
            var landing = new Pos(q.Row + dr, q.Col + dc);
            while (landing.Inside && Board[landing.Row, landing.Col] is null)
            {
                result.Add(new Move(p, landing, q));
                landing = new Pos(landing.Row + dr, landing.Col + dc);
            }
        }
        return result;
    }

    // Вычисление максимальной длины серии взятий среди шашек текущего игрока.
    private int GetGlobalMaximumCaptureCount(PlayerColor player)
    {
        int best = 0;
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                if (Board[r, c]?.Color == player)
                    best = Math.Max(best, MaxCaptureSequence(new Pos(r,c)));
        return best;
    }

    // Рекурсивный поиск максимального количества взятий, доступных из позиции.
    private int MaxCaptureSequence(Pos p)
    {
        int best = 0;
        foreach (var m in GetCaptures(p)) best = Math.Max(best, MaxCaptureSequenceFromMove(m));
        return best;
    }

    // Проверка длины серии взятий, начинающейся с конкретного хода.
    private int MaxCaptureSequenceFromMove(Move first)
    {
        var snapshot = Snapshot();
        try
        {
            if (!first.From.Inside || Board[first.From.Row, first.From.Col] is null) return 0;
            ExecuteSimulation(first);
            return 1 + MaxCaptureSequence(first.To);
        }
        finally { Restore(snapshot); }
    }

    // Сохранение временной копии доски перед моделированием хода.
    private Piece?[,] Snapshot()
    {
        var copy = new Piece?[8,8];
        for(int r=0;r<8;r++) for(int c=0;c<8;c++) copy[r,c]=Board[r,c]?.Clone();
        return copy;
    }
    // Восстановление доски после временного анализа хода.
    private void Restore(Piece?[,] snapshot)
    {
        for(int r=0;r<8;r++) for(int c=0;c<8;c++) Board[r,c]=snapshot[r,c];
    }
    // Выполнение хода только в рамках расчёта возможной серии взятий.
    private void ExecuteSimulation(Move move)
    {
        var piece = Board[move.From.Row, move.From.Col];
        if (piece is null) return;
        Board[move.From.Row, move.From.Col] = null;
        if (move.Captured is Pos cap) Board[cap.Row, cap.Col] = null;
        Board[move.To.Row, move.To.Col] = piece;
        PromoteIfNeeded(move.To);
    }

    // Четыре диагональных направления.
    private static readonly (int dr,int dc)[] DiagonalDirs = { (-1,-1),(-1,1),(1,-1),(1,1) };
    // Восемь направлений для движения и взятия дамки.
    private static readonly (int dr,int dc)[] AllDirs = { (-1,-1),(-1,0),(-1,1),(0,-1),(0,1),(1,-1),(1,0),(1,1) };
}
