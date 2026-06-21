using System.IO;

namespace FlytLauncher;

public class MainForm : Form
{
    // --- Настройки сервера ---
    private const string ServerIp = "5.199.138.96";
    private const string ServerPort = "2306";

    // Папка с игрой лежит рядом с .exe лаунчера
    private static readonly string GameFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SAMP");
    private static readonly string GtaExePath = Path.Combine(GameFolder, "gta_sa.exe");
    private static readonly string SampDllPath = Path.Combine(GameFolder, "samp.dll");
    private static readonly string SampCfgPath = Path.Combine(GameFolder, "samp.cfg");

    // Файл с сохранённым ником — рядом с .exe лаунчера, не зависит от папки game
    private static readonly string NicknameFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nickname.txt");

    private TextBox _nicknameBox = null!;
    private Button _playButton = null!;
    private Panel _statusDot = null!;
    private Label _statusText = null!;

    public MainForm()
    {
        InitializeUi();
        LoadSavedNickname();
        CheckInstallation();
    }

    private void InitializeUi()
    {
        Text = "FlytRP Launcher";
        ClientSize = new Size(440, 320);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(18, 18, 22);

        // --- Верхняя полоса (имитация заголовка окна, т.к. рамка убрана) ---
        var topBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = Color.FromArgb(14, 14, 17)
        };
        topBar.MouseDown += (_, e) => DragWindow();

        var closeButton = new Label
        {
            Text = "\u2715",
            ForeColor = Color.FromArgb(150, 150, 155),
            Font = new Font("Segoe UI", 11),
            AutoSize = false,
            Size = new Size(36, 36),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand
        };
        closeButton.Click += (_, _) => Close();
        closeButton.MouseEnter += (_, _) => closeButton.ForeColor = Color.FromArgb(230, 80, 80);
        closeButton.MouseLeave += (_, _) => closeButton.ForeColor = Color.FromArgb(150, 150, 155);
        topBar.Controls.Add(closeButton);

        // --- Лого / заголовок ---
        var title = new Label
        {
            Text = "FLYT",
            Font = new Font("Segoe UI", 26, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(40, 56)
        };

        var titleAccent = new Label
        {
            Text = "RP",
            Font = new Font("Segoe UI", 26, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 200, 255),
            AutoSize = true,
            Location = new Point(132, 56)
        };

        var subtitle = new Label
        {
            Text = "ROLEPLAY SERVER",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(110, 110, 118),
            AutoSize = true,
            Location = new Point(40, 100)
        };

        // --- Поле никнейма ---
        var nicknameLabel = new Label
        {
            Text = "НИКНЕЙМ",
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = Color.FromArgb(110, 110, 118),
            AutoSize = true,
            Location = new Point(40, 144)
        };

        _nicknameBox = new TextBox
        {
            Location = new Point(40, 164),
            Width = 360,
            Height = 40,
            MaxLength = 24,
            Font = new Font("Segoe UI", 12),
            BackColor = Color.FromArgb(28, 28, 33),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        _nicknameBox.TextChanged += OnNicknameTextChanged;

        // --- Статус установки (точка + текст) ---
        var statusColor = Color.FromArgb(120, 120, 125);
        _statusDot = new Panel
        {
            Size = new Size(8, 8),
            Location = new Point(40, 222),
            BackColor = Color.FromArgb(18, 18, 22),
            Tag = statusColor
        };
        _statusDot.Paint += (_, e) =>
        {
            using var brush = new SolidBrush((Color)_statusDot.Tag!);
            e.Graphics.FillEllipse(brush, 0, 0, _statusDot.Width, _statusDot.Height);
        };

        _statusText = new Label
        {
            Text = "Проверка установки...",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(150, 150, 155),
            AutoSize = true,
            Location = new Point(56, 220)
        };

        // --- Кнопка "Играть" ---
        _playButton = new Button
        {
            Text = "ИГРАТЬ",
            Location = new Point(40, 250),
            Size = new Size(360, 46),
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 180, 230),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Enabled = false,
            Cursor = Cursors.Hand
        };
        _playButton.FlatAppearance.BorderSize = 0;
        _playButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 200, 255);
        _playButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 150, 195);
        _playButton.Click += OnPlayClicked;

        Controls.Add(topBar);
        Controls.Add(title);
        Controls.Add(titleAccent);
        Controls.Add(subtitle);
        Controls.Add(nicknameLabel);
        Controls.Add(_nicknameBox);
        Controls.Add(_statusDot);
        Controls.Add(_statusText);
        Controls.Add(_playButton);
    }

    // --- DragWindow / WinAPI для перетаскивания окна без рамки ---
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HT_CAPTION = 0x2;

    private void DragWindow()
    {
        ReleaseCapture();
        SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
    }

    private void LoadSavedNickname()
    {
        try
        {
            if (File.Exists(NicknameFilePath))
            {
                string saved = File.ReadAllText(NicknameFilePath).Trim();
                if (!string.IsNullOrWhiteSpace(saved))
                {
                    _nicknameBox.Text = saved;
                }
            }
        }
        catch
        {
            // Если файл повреждён или недоступен — просто игнорируем, поле останется пустым
        }
    }

    private void OnNicknameTextChanged(object? sender, EventArgs e)
    {
        SaveNicknameToDisk(_nicknameBox.Text.Trim());
    }

    private void SaveNicknameToDisk(string nickname)
    {
        try
        {
            File.WriteAllText(NicknameFilePath, nickname);
        }
        catch
        {
            // Сохранение ника не критично для работы лаунчера — ошибку молча игнорируем
        }
    }

    private void CheckInstallation()
    {
        bool gtaFound = File.Exists(GtaExePath);
        bool sampFound = File.Exists(SampDllPath);

        if (gtaFound && sampFound)
        {
            _statusText.Text = "Установка найдена";
            SetStatusDotColor(Color.FromArgb(80, 220, 100));
            _playButton.Enabled = true;
        }
        else
        {
            _statusText.Text = "Не найдено: SAMP/gta_sa.exe или SAMP/samp.dll";
            SetStatusDotColor(Color.FromArgb(230, 80, 80));
            _playButton.Enabled = false;
        }
    }

    private void SetStatusDotColor(Color color)
    {
        _statusDot.Tag = color;
        _statusDot.Invalidate();
    }

    private void OnPlayClicked(object? sender, EventArgs e)
    {
        string nickname = _nicknameBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(nickname))
        {
            MessageBox.Show("Введите никнейм перед входом в игру.", "FlytRP Launcher",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (nickname.Length < 3)
        {
            MessageBox.Show("Никнейм должен содержать минимум 3 символа.", "FlytRP Launcher",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            WritePlayerNameToConfig(nickname);
            LaunchGame();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось запустить игру:\n{ex.Message}", "FlytRP Launcher",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // SAMP читает никнейм игрока из файла samp.cfg (ключ "gamemode" не трогаем,
    // нужный нам параметр для логина — playername в реестре или quick-connect ник).
    // Самый надёжный кросс-версийный способ - прокинуть ник через samp.cfg "name=".
    private void WritePlayerNameToConfig(string nickname)
    {
        var lines = new List<string>();

        if (File.Exists(SampCfgPath))
        {
            lines.AddRange(File.ReadAllLines(SampCfgPath));
        }

        bool nameWritten = false;
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].StartsWith("name ", StringComparison.OrdinalIgnoreCase))
            {
                lines[i] = $"name {nickname}";
                nameWritten = true;
                break;
            }
        }

        if (!nameWritten)
        {
            lines.Add($"name {nickname}");
        }

        File.WriteAllLines(SampCfgPath, lines);
    }

    private void LaunchGame()
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = GtaExePath,
            Arguments = $"-c {ServerIp}:{ServerPort}",
            WorkingDirectory = GameFolder,
            UseShellExecute = true
        };

        System.Diagnostics.Process.Start(psi);
        Close();
    }
}
