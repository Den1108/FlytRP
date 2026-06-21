using System.IO;

namespace FlytLauncher;

public class MainForm : Form
{
    // --- Настройки сервера ---
    private const string ServerIp = "5.199.138.96";
    private const string ServerPort = "2306";

    // Папка с игрой лежит рядом с .exe лаунчера
    private static readonly string GameFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "game");
    private static readonly string GtaExePath = Path.Combine(GameFolder, "gta_sa.exe");
    private static readonly string SampDllPath = Path.Combine(GameFolder, "samp.dll");
    private static readonly string SampCfgPath = Path.Combine(GameFolder, "samp.cfg");

    // Файл с сохранённым ником — рядом с .exe лаунчера, не зависит от папки game
    private static readonly string NicknameFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nickname.txt");

    private TextBox _nicknameBox = null!;
    private Button _playButton = null!;
    private Label _statusLabel = null!;

    public MainForm()
    {
        InitializeUi();
        LoadSavedNickname();
        CheckInstallation();
    }

    private void InitializeUi()
    {
        Text = "FlytRP Launcher";
        ClientSize = new Size(420, 220);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(24, 24, 28);

        var title = new Label
        {
            Text = "FlytRP",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 200, 255),
            AutoSize = true,
            Location = new Point(20, 20)
        };

        var nicknameLabel = new Label
        {
            Text = "Никнейм:",
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 80)
        };

        _nicknameBox = new TextBox
        {
            Location = new Point(20, 100),
            Width = 380,
            MaxLength = 24,
            Font = new Font("Segoe UI", 11)
        };
        _nicknameBox.TextChanged += OnNicknameTextChanged;

        _statusLabel = new Label
        {
            Text = "Проверка установки...",
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(20, 135)
        };

        _playButton = new Button
        {
            Text = "Играть",
            Location = new Point(20, 165),
            Size = new Size(380, 40),
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 160, 220),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Enabled = false
        };
        _playButton.FlatAppearance.BorderSize = 0;
        _playButton.Click += OnPlayClicked;

        Controls.Add(title);
        Controls.Add(nicknameLabel);
        Controls.Add(_nicknameBox);
        Controls.Add(_statusLabel);
        Controls.Add(_playButton);
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
            _statusLabel.Text = "Установка найдена. Готово к игре.";
            _statusLabel.ForeColor = Color.FromArgb(80, 220, 100);
            _playButton.Enabled = true;
        }
        else
        {
            _statusLabel.Text = "Не найдено: game/gta_sa.exe или game/samp.dll";
            _statusLabel.ForeColor = Color.FromArgb(230, 80, 80);
            _playButton.Enabled = false;
        }
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
