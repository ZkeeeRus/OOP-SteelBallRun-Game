using SBR_Game.Audio;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Resources;
using System.Windows.Forms;

namespace SBR_Game
{
    public class MainMenuForm : Form
    {
        public event Action? PlayRequested;

        private readonly Button _btnPlay = new();
        private readonly Button _btnQuit = new();
        private readonly Label _lblTitle = new();
        private readonly Label _lblSubtitle = new();

        private AudioManager _audioManager = null!;

        public MainMenuForm()
        {
            InitializeComponent();

            Text = "Steel Ball Run";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            ClientSize = new Size(1280, 720);
            BackColor = Color.FromArgb(10, 10, 20);
            StartPosition = FormStartPosition.CenterScreen;

            BuildTitle();
            BuildButtons();

            string content = Path.Combine(AppContext.BaseDirectory, "Content");

            _audioManager = new AudioManager();
            _audioManager.Initialize(content);
            _audioManager.PlayMainMenuMusic();
        }


        private void BuildTitle()
        {
            _lblTitle.Text = "Скачки";
            _lblTitle.Font = new Font("Segoe UI", 96, FontStyle.Bold, GraphicsUnit.Pixel);
            _lblTitle.ForeColor = Color.FromArgb(64, 196, 255);
            _lblTitle.AutoSize = true;
            _lblTitle.BackColor = Color.Transparent;
            _lblTitle.Location = new Point(0, 0);
            Controls.Add(_lblTitle);

            _lblSubtitle.Text = "Steel Ball Run";
            _lblSubtitle.Font = new Font("Segoe UI", 22, FontStyle.Regular, GraphicsUnit.Pixel);
            _lblSubtitle.ForeColor = Color.FromArgb(160, 220, 255);
            _lblSubtitle.AutoSize = true;
            _lblSubtitle.BackColor = Color.Transparent;
            Controls.Add(_lblSubtitle);
        }

        private void BuildButtons()
        {
            StyleButton(_btnPlay, "ИГРАТЬ", Color.FromArgb(64, 196, 255));
            StyleButton(_btnQuit, "ВЫЙТИ", Color.FromArgb(255, 80, 60));

            _btnPlay.Click += (_, _) =>
            {
                _audioManager.StopMusic();
                PlayRequested?.Invoke();
            };
            _btnQuit.Click += (_, _) => Application.Exit();

            Controls.Add(_btnPlay);
            Controls.Add(_btnQuit);
        }

        private static void StyleButton(Button btn, string text, Color accent)
        {
            btn.Text = text;
            btn.Font = new Font("Segoe UI", 16, FontStyle.Bold, GraphicsUnit.Pixel);
            btn.ForeColor = accent;
            btn.BackColor = Color.FromArgb(25, 30, 45);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = accent;
            btn.FlatAppearance.BorderSize = 2;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, accent.R, accent.G, accent.B);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(70, accent.R, accent.G, accent.B);
            btn.Size = new Size(220, 54);
            btn.Cursor = Cursors.Hand;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            RepositionControls();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            RepositionControls();
        }

        private void RepositionControls()
        {
            int w = ClientSize.Width;
            int h = ClientSize.Height;

            _lblTitle.Location = new Point(
                (w - _lblTitle.PreferredWidth) / 2,
                (int)(h * 0.22f));

            _lblSubtitle.Location = new Point(
                (w - _lblSubtitle.PreferredWidth) / 2,
                _lblTitle.Bottom + 4);


            int btnY = (int)(h * 0.56f);
            _btnPlay.Location = new Point((w - _btnPlay.Width) / 2, btnY);
            _btnQuit.Location = new Point((w - _btnQuit.Width) / 2, btnY + _btnPlay.Height + 18);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainMenuForm));
            SuspendLayout();

            ResumeLayout(false);

        }


        protected override void OnPaintBackground(PaintEventArgs e)
        {

            using var brush = new LinearGradientBrush(
                ClientRectangle,
                Color.FromArgb(12, 18, 38),
                Color.FromArgb(5, 5, 12),
                LinearGradientMode.Vertical);
            e.Graphics.FillRectangle(brush, ClientRectangle);


            using var linePen = new Pen(Color.FromArgb(14, 255, 255, 255));
            for (int y = 0; y < ClientSize.Height; y += 4)
                e.Graphics.DrawLine(linePen, 0, y, ClientSize.Width, y);
        }

        private bool _gameDisposed;
        protected override void OnFormClosed(FormClosedEventArgs e)
        {

            if (_gameDisposed) { base.OnFormClosed(e); return; }
            _gameDisposed = true;

            _audioManager?.Dispose();

            base.OnFormClosed(e);
        }
    }
}
