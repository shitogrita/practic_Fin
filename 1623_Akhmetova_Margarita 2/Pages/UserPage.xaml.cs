using _1623_Akhmetova_Margarita;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Main.Pages
{
    public partial class UserPage : Page
    {
        public UserPage()
        {
            InitializeComponent();

            // Шаг 12: стартовые значения
            CmbSorting.SelectedIndex = 0;
            CheckUserOnly.IsChecked = false;

            // первичная загрузка
            UpdateUsers();
        }

        private void UpdateUsers()
        {
            // загружаем всех пользователей
            var currentUsers = Entities.GetContext().User.ToList();

            // поиск по ФИО без учета регистра; защита от NULL в FIO
            var search = (TextBoxSearch.Text ?? "").ToLower();
            currentUsers = currentUsers
                .Where(x => (x.FIO ?? "").ToLower().Contains(search))
                .ToList();

            // фильтр: только роль "Пользователь"
            if (CheckUserOnly.IsChecked == true)
                currentUsers = currentUsers
                    .Where(x => (x.Role ?? "").Contains("Пользователь"))
                    .ToList();

            // сортировка по ФИО
            if (CmbSorting.SelectedIndex == 0)
                ListUser.ItemsSource = currentUsers.OrderBy(x => x.FIO).ToList();
            else
                ListUser.ItemsSource = currentUsers.OrderByDescending(x => x.FIO).ToList();
        }

        // TextBox: поиск
        private void TextBoxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateUsers();
        }

        // ComboBox: сортировка
        private void CmbSorting_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateUsers();
        }

        // CheckBox: фильтрация
        private void CheckUserOnly_Checked(object sender, RoutedEventArgs e)
        {
            UpdateUsers();
        }

        private void CheckUserOnly_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateUsers();
        }

        // Кнопка: очистка фильтра
        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            TextBoxSearch.Text = "";
            CheckUserOnly.IsChecked = false;
            CmbSorting.SelectedIndex = 0;

            UpdateUsers();
        }
    }
}
