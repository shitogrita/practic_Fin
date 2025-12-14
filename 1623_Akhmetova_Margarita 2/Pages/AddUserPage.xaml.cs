using _1623_Akhmetova_Margarita;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Main.Pages
{
    public partial class AddUserPage : Page
    {
        private User _currentUser = new User();

        public AddUserPage(User selectedUser)
        {
            InitializeComponent();
            if (selectedUser != null)
                _currentUser = selectedUser;

            DataContext = _currentUser;
        }

        // SHA-1 как в методичке (Шаг 6)
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

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            var errors = new StringBuilder();

            if (string.IsNullOrWhiteSpace(_currentUser.Login))
                errors.AppendLine("Укажите логин!");

            if ((cmbRole.SelectedItem == null) || string.IsNullOrWhiteSpace(cmbRole.Text))
                errors.AppendLine("Выберите роль!");
            else
                _currentUser.Role = cmbRole.Text;

            if (string.IsNullOrWhiteSpace(_currentUser.FIO))
                errors.AppendLine("Укажите Ф.И.О.!");

            // Проверка уникальности логина: важно исключить самого себя при редактировании
            if (!string.IsNullOrWhiteSpace(_currentUser.Login))
            {
                bool exists = Entities.GetContext().User
                    .Any(u => u.Login == _currentUser.Login && u.ID != _currentUser.ID);

                if (exists)
                    errors.AppendLine("Пользователь с таким логином уже существует!");
            }

            // Если ДОБАВЛЕНИЕ (ID==0), пароль обязателен
            if (_currentUser.ID == 0 && string.IsNullOrWhiteSpace(PbPassword.Password))
                errors.AppendLine("Укажите пароль!");

            if (errors.Length > 0)
            {
                MessageBox.Show(errors.ToString(), "Ошибки ввода",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Если пароль введён — обновляем (хешируем). Если пусто — не трогаем.
            if (!string.IsNullOrWhiteSpace(PbPassword.Password))
                _currentUser.Password = GetHash(PbPassword.Password);

            if (_currentUser.ID == 0)
                Entities.GetContext().User.Add(_currentUser);

            try
            {
                Entities.GetContext().SaveChanges();
                MessageBox.Show("Данные успешно сохранены!",
                    "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);

                NavigationService?.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка сохранения",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
