using _1623_Akhmetova_Margarita;
using Main.Pages;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Security.Cryptography;
using System.Text;


namespace Main.Pages
{
    public partial class AuthPage : Page
    {
        public AuthPage()
        {
            InitializeComponent();
        }

        private void BtnEnter_Click(object sender, RoutedEventArgs e)
        {
            var login = TbLogin.Text?.Trim();
            var password = PbPassword.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Необходимо заполнить логин и пароль.",
                    "Валидация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var db = new Entities())
            {
                var user = db.User.FirstOrDefault(u => u.Login == login && u.Password == password);

                if (user == null)
                {
                    MessageBox.Show("Учетная запись не найдена либо пароль неверен.",
                        "Авторизация", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MessageBox.Show("Авторизация выполнена успешно.",
                    "Авторизация", MessageBoxButton.OK, MessageBoxImage.Information);

                // Переход в зависимости от роли (значения должны совпадать с БД)
                if (user.Role == "Администратор")
                {
                    NavigationService?.Navigate(new AdminPage());
                }
                else if (user.Role == "Пользователь")
                {
                    NavigationService?.Navigate(new UserPage());
                }
                else
                {
                    MessageBox.Show("Роль пользователя не распознана.",
                        "Авторизация", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void BtnToRegister_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new RegPage());
        }

        public static string GetHash(string password)
        {
            using (var hash = SHA1.Create())
            {
                return string.Concat(
                    hash.ComputeHash(Encoding.UTF8.GetBytes(password))
                        .Select(x => x.ToString("X2"))
                );
            }
        }


    }
}
