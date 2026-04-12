using SBR_Game.UI;

namespace SBR_Game
{
    internal static class Program
    {
        private static MainMenuForm _menuForm = null!;
        private static fMainWindow _gameForm = null!;

        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            _menuForm = new MainMenuForm();
            _gameForm = new fMainWindow();

            _menuForm.PlayRequested += OnPlayRequested;
            _gameForm.FormClosed += OnGameFormClosed;

            Application.Run(_menuForm);
        }

        private static void OnPlayRequested()
        {
            _menuForm.Hide();
            _gameForm.StartGame();
        }

        private static void OnGameFormClosed(object? sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        public static void ShowMainMenu()
        {
            _menuForm.Show();
            _menuForm.BringToFront();
        }
    }
}