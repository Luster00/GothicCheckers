namespace GothicCheckers;

// Главное окно игры и пользовательский интерфейс.
public sealed class MainForm : Form
{
    // Экземпляр игрового движка, содержащий состояние партии.
    private readonly GameEngine game = new();
    // Экземпляр искусственного интеллекта.
    private readonly ComputerPlayer computer = new();
    // Массив кнопок, визуально представляющих клетки доски.
    private readonly Button[,] cells = new Button[8, 8];
    // Координаты шашки, выбранной игроком.
    private Pos? selected;
    // Список ходов, доступных для выбранной шашки.
    private List<Move> selectedMoves = new();
    private bool winnerShown;

    private readonly Label status = new()
    {
        Dock = DockStyle.Top,
        Height = 46,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI", 13, FontStyle.Bold),
        ForeColor = Color.White
    };

    private readonly Label help = new()
    {
        Dock = DockStyle.Bottom,
        Height = 70,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI", 9),
        ForeColor = Color.Gainsboro,
        BackColor = Color.FromArgb(25, 25, 30),
        Text = "Вы играете белыми. После взятия необходимо продолжать брать шашки той же фигурой, пока взятия возможны."
    };

    public MainForm()
    {
        Text = "Готические шашки — человек против компьютера";
        MinimumSize = new Size(860, 900);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(24, 24, 28);

        var menu = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(10), BackColor = Color.FromArgb(35,35,42) };
        var newGame = new Button { Text = "Новая игра", AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        newGame.Click += (_, _) => { game.NewGame(); selected = null; selectedMoves.Clear(); winnerShown = false; RefreshBoard(); };
        var rules = new Button { Text = "Правила", AutoSize = true, Font = new Font("Segoe UI", 10) };
        rules.Click += (_, _) => ShowRules();
        menu.Controls.Add(newGame); menu.Controls.Add(rules);

        var board = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 8, RowCount = 8,
            Padding = new Padding(16), BackColor = Color.FromArgb(24,24,28)
        };
        for (int i=0;i<8;i++) { board.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,12.5f)); board.RowStyles.Add(new RowStyle(SizeType.Percent,12.5f)); }

        for (int r=0;r<8;r++)
        for (int c=0;c<8;c++)
        {
            int rr=r, cc=c;
            var b = new Button { Dock=DockStyle.Fill, Margin=Padding.Empty, FlatStyle=FlatStyle.Flat,
                Font=new Font("Segoe UI Symbol", 48, FontStyle.Bold), TabStop=false, UseVisualStyleBackColor=false };
            b.FlatAppearance.BorderSize=0;
            b.Click += (_,_) => CellClick(new Pos(rr,cc));
            cells[r,c]=b; board.Controls.Add(b,c,r);
        }

        Controls.Add(board); Controls.Add(help); Controls.Add(status); Controls.Add(menu);
        game.NewGame(); RefreshBoard();
    }

    // Обработка нажатия игрока на клетку игрового поля.
    private void CellClick(Pos p)
    {
        if (game.GameOver || game.IsComputerTurn) return;

        if (selected.HasValue)
        {
            var move = selectedMoves.FirstOrDefault(m => m.To == p);
            if (move is not null)
            {
                if (!game.TryMove(selected.Value, p, out var error)) MessageBox.Show(error);
                selected = game.IsForcedPiece(p) ? p : null;
                selectedMoves = selected.HasValue ? game.GetMovesForPiece(selected.Value) : new List<Move>();
                RefreshBoard();
                if (!game.GameOver && game.IsComputerTurn) ComputerTurn();
                return;
            }
        }

        if (game.Board[p.Row,p.Col]?.Color == game.CurrentPlayer)
        {
            var moves=game.GetMovesForPiece(p);
            if (moves.Count>0) { selected=p; selectedMoves=moves; RefreshBoard(); return; }
        }
        selected=null; selectedMoves.Clear(); RefreshBoard();
    }

    // Автоматическое выполнение хода компьютера после завершения хода человека.
    private void ComputerTurn()
    {
        while (!game.GameOver && game.IsComputerTurn)
        {
            status.Text="Компьютер анализирует позицию..."; status.ForeColor=Color.Orange;
            Application.DoEvents();
            var move=computer.ChooseMove(game);
            if (move is null) break;
            Thread.Sleep(180);
            game.TryMove(move.From, move.To, out _);
            RefreshBoard();
            Application.DoEvents();
        }
        selected=null; selectedMoves.Clear(); RefreshBoard();
    }

    // Перерисовка доски, шашек, подсказок и текущего статуса игры.
    private void RefreshBoard()
    {
        for(int r=0;r<8;r++) for(int c=0;c<8;c++)
        {
            var b=cells[r,c]; bool dark=(r+c)%2==1;
            b.BackColor=dark ? Color.FromArgb(62,62,72) : Color.FromArgb(220,214,198);
            var p=game.Board[r,c];
            b.Text=p switch
            {
                null=>"",
                {Color:PlayerColor.White,IsKing:false}=>"●",
                {Color:PlayerColor.Black,IsKing:false}=>"●",
                {Color:PlayerColor.White,IsKing:true}=>"♛",
                {Color:PlayerColor.Black,IsKing:true}=>"♛"
            };
            // Высокий контраст: белые — почти белые, чёрные — яркий бордовый.
            if (p?.Color==PlayerColor.White) b.ForeColor=Color.White;
            else if (p?.Color==PlayerColor.Black) b.ForeColor=Color.FromArgb(150,20,35);
            else b.ForeColor=Color.Black;

            var pos=new Pos(r,c);
            if(selected==pos) b.BackColor=Color.Gold;
            else if(selectedMoves.Any(m=>m.To==pos)) b.BackColor=selectedMoves.Any(m=>m.To==pos && m.IsCapture) ? Color.FromArgb(210,95,95) : Color.FromArgb(145,205,145);
        }

        if(game.GameOver)
        {
            status.Text=$"Победа: {(game.Winner==PlayerColor.White?"вы":"компьютер")}";
            status.ForeColor=Color.Gold;
            if(!winnerShown){ winnerShown=true; BeginInvoke(()=>MessageBox.Show(status.Text,"Готические шашки")); }
        }
        else if(selected.HasValue && game.IsForcedPiece(selected.Value))
        {
            status.Text="Обязательное продолжение: этой шашкой нужно взять ещё."; status.ForeColor=Color.Orange;
        }
        else
        {
            status.Text=game.CurrentPlayer==PlayerColor.White ? "Ваш ход — белые" : "Ход компьютера — чёрные";
            status.ForeColor=Color.White;
        }
    }

    // Отображение краткого описания правил игры.
    private void ShowRules() => MessageBox.Show(
        "ГОТИЧЕСКИЕ ШАШКИ\n\n"+
        "• Доска 8×8, по 16 шашек у каждого игрока.\n"+
        "• Простая шашка ходит по диагонали вперёд. Дамка — по диагонали на любое расстояние.\n"+
        "• Простая шашка бьёт вперёд по диагонали, а также прямо, влево и вправо.\n"+
        "• Дамка бьёт во всех 8 направлениях.\n"+
        "• Взятие можно не начинать, но если вы начали серию взятий — её нельзя прерывать.\n"+
        "• Если после взятия той же шашкой доступно ещё одно взятие, игра ОБЯЗЫВАЕТ продолжить ход.\n"+
        "• Вы играете белыми против компьютера.",
        "Правила готических шашек",MessageBoxButtons.OK,MessageBoxIcon.Information);
}
