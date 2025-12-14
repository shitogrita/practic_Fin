using _1623_Akhmetova_Margarita;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace YourNamespace
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();

            var db = new Entities();
            var count = db.User.Count();
            MessageBox.Show($"Пользователей в БД: {count}");

            // Таймер для обновления даты/времени
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (_, __) => TbDateTime.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            _timer.Start();

            // Пример: стартовая страница (позже замените на страницу авторизации)
            // MainFrame.Navigate(new AuthPage());
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.CanGoBack)
                MainFrame.GoBack();
            else
                MessageBox.Show("Переход назад невозможен: отсутствует история навигации.",
                    "Навигация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            TryCloseWithConfirmation();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Подтверждение при закрытии окна стандартными средствами (крестик и т.п.)
            if (!TryCloseWithConfirmation())
            {
                e.Cancel = true;
                return;
            }

            base.OnClosing(e);
        }

        private bool TryCloseWithConfirmation()
        {
            var result = MessageBox.Show(
                "Вы действительно хотите выйти?",
                "Подтверждение выхода",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Корректная остановка таймера (не обязательно, но корректно)
                _timer?.Stop();
                Application.Current.Shutdown();
                return true;
            }

            return false;
        }
        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            var page = e.Content as Page;
            if (page != null)
            {
                Title = $"Ахметова | {page.Title}"; // фамилию можно заменить
                BtnBack.Visibility = (page.Title == "Авторизация")
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }
        }
    }
}
