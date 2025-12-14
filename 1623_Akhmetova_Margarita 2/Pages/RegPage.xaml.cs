using _1623_Akhmetova_Margarita;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Security.Cryptography;
using System.Text;



namespace Main.Pages
{
    public partial class RegPage : Page
    {
        public RegPage()
        {
            InitializeComponent();
        }

        private void BtnBackToAuth_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            // 5.1 Проверка заполнения
            if (string.IsNullOrEmpty(TextBoxLogin.Text) ||
                string.IsNullOrEmpty(PasswordBox.Password) ||
                string.IsNullOrEmpty(PasswordBoxRepeat.Password) ||
                string.IsNullOrEmpty(TextBoxFIO.Text))
            {
                MessageBox.Show("Заполните все обязательные поля: логин, пароль, подтверждение пароля и ФИО.",
                    "Регистрация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var login = TextBoxLogin.Text.Trim();
            var fio = TextBoxFIO.Text.Trim();
            var pass = PasswordBox.Password;
            var pass2 = PasswordBoxRepeat.Password;

            // 5.3 Проверки пароля:
            // 1) ≥ 6 символов
            if (pass.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать 6 или более символов.",
                    "Регистрация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2) только английская раскладка (интерпретация: только латинские буквы и цифры)
            if (!Regex.IsMatch(pass, @"^[A-Za-z0-9]+$"))
            {
                MessageBox.Show("Пароль должен содержать только английские буквы и цифры (без пробелов и русских символов).",
                    "Регистрация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3) хотя бы одна цифра
            if (!Regex.IsMatch(pass, @"\d"))
            {
                MessageBox.Show("Пароль должен содержать хотя бы одну цифру.",
                    "Регистрация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 5.4 Совпадение паролей
            if (pass != pass2)
            {
                MessageBox.Show("Пароли не совпадают.",
                    "Регистрация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Роль из ComboBox
            var roleItem = CmbRole.SelectedItem as ComboBoxItem;
            var role = roleItem?.Content?.ToString() ?? "Пользователь";

            // 5.2 Проверка уникальности логина + 5.5 запись в БД
            using (var db = new Entities())
            {
                bool exists = db.User.Any(u => u.Login == login);
                if (exists)
                {
                    MessageBox.Show("Пользователь с таким логином уже существует.",
                        "Регистрация", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var userObject = new User
                {
                    FIO = fio,
                    Login = login,
                    Password = GetHash(pass),
                    Role = role,
                    Photo = null
                };

                db.User.Add(userObject);
                db.SaveChanges();
            }

            // 5.6 Сообщение об успехе
            MessageBox.Show("Регистрация выполнена успешно.",
                "Регистрация", MessageBoxButton.OK, MessageBoxImage.Information);

            // 5.7 Очистка полей
            TextBoxLogin.Clear();
            PasswordBox.Clear();
            PasswordBoxRepeat.Clear();
            TextBoxFIO.Clear();
            CmbRole.SelectedIndex = 1;
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
