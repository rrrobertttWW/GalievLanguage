using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace GalievLanguageSchool
{
    public partial class ClientPage : Page
    {
        int CountRecords;
        int CountPage;
        int CurrentPage = 0;
        int ObjectsOnPage = 10;

        List<Client> CurrentPageList = new List<Client>();
        List<Client> FilteredClients; // Отфильтрованные и отсортированные клиенты
        List<Client> AllClients; // Все клиенты

        string searchText = "";
        int sortIndex = 0;
        int filterIndex = 0;

        public ClientPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                // Загружаем всех клиентов
                AllClients = ГалиевLanguageEntities.GetContext().Client.ToList();
                if (AllClients == null)
                    AllClients = new List<Client>();

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
                AllClients = new List<Client>();
            }
        }

        private void ApplyFilters()
        {
            if (AllClients == null) return;

            // Начинаем со всех клиентов
            FilteredClients = AllClients.ToList();

            // Применяем поиск
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string search = searchText.ToLower();

                // Очищаем поисковый запрос для телефона (убираем все лишние символы)
                string cleanedSearchPhone = search
                    .Replace("+", "")
                    .Replace("(", "")
                    .Replace(")", "")
                    .Replace("-", "")
                    .Replace(" ", "")
                    .Replace("8", "7");

                FilteredClients = FilteredClients.Where(c =>
                    // Поиск по ФИО
                    (c.FIO != null && c.FIO.ToLower().Contains(search)) ||

                    // Поиск по email
                    (c.Email != null && c.Email.ToLower().Contains(search)) ||

                    // Поиск по телефону (с очисткой от форматирования)
                    (c.Phone != null && c.Phone
                        .Replace("+", "")
                        .Replace("(", "")
                        .Replace(")", "")
                        .Replace("-", "")
                        .Replace(" ", "")
                        .Replace("8", "7")
                        .Contains(cleanedSearchPhone))
                ).ToList();
            }

            // Применяем фильтр по полу с проверкой на null
            if (filterIndex == 1) // Мужской
            {
                FilteredClients = FilteredClients.Where(c =>
                    c.Gender != null &&
                    c.Gender.Code != null &&
                    c.Gender.Code.ToLower() == "м").ToList();
            }
            else if (filterIndex == 2) // Женский
            {
                FilteredClients = FilteredClients.Where(c =>
                    c.Gender != null &&
                    c.Gender.Code != null &&
                    c.Gender.Code.ToLower() == "ж").ToList();
            }

            // Применяем сортировку
            switch (sortIndex)
            {
                case 1: // По фамилии (А-Я)
                    FilteredClients = FilteredClients.OrderBy(c => c.FIO).ToList();
                    break;
                case 2: // По фамилии (Я-А)
                    FilteredClients = FilteredClients.OrderByDescending(c => c.FIO).ToList();
                    break;
                case 3: // По дате последнего посещения (возр.) - сначала более старые
                    FilteredClients = FilteredClients
                        .OrderBy(c => c.ClientService
                            .Where(cs => cs.ClientID == c.ID)
                            .OrderByDescending(cs => cs.StartTime)
                            .Select(cs => (DateTime?)cs.StartTime)
                            .FirstOrDefault())
                        .ToList();
                    break;
                case 4: // По дате последнего посещения (убыв.) - сначала более новые
                    FilteredClients = FilteredClients
                        .OrderByDescending(c => c.ClientService
                            .Where(cs => cs.ClientID == c.ID)
                            .OrderByDescending(cs => cs.StartTime)
                            .Select(cs => (DateTime?)cs.StartTime)
                            .FirstOrDefault())
                        .ToList();
                    break;
                case 5: // По посещениям (возр.)
                    FilteredClients = FilteredClients.OrderBy(c => c.TotalVisits).ToList();
                    break;
                case 6: // По посещениям (убыв.)
                    FilteredClients = FilteredClients.OrderByDescending(c => c.TotalVisits).ToList();
                    break;
                default: // Без сортировки
                    FilteredClients = FilteredClients.OrderBy(c => c.ID).ToList();
                    break;
            }

            UpdatePagination();
        }

        private void UpdatePagination()
        {
            // Сбрасываем на первую страницу
            CurrentPage = 0;
            ChangePage(0, 0);
        }

        private void ChangePage(int direction, int? selectedPage)
        {
            if (FilteredClients == null || FilteredClients.Count == 0)
            {
                ClientListView.ItemsSource = null;
                TBCount.Text = "0";
                TBAllRecords.Text = " из 0";
                PageListBox.Items.Clear();
                return;
            }

            CurrentPageList.Clear();
            CountRecords = FilteredClients.Count;

            // Получаем актуальное значение ObjectsOnPage из ComboBox
            if (ObjOnPage.SelectedItem is ComboBoxItem selectedItem)
            {
                string content = selectedItem.Content.ToString();
                if (content == "Все")
                    ObjectsOnPage = CountRecords;
                else if (int.TryParse(content, out int count))
                    ObjectsOnPage = count;
            }

            // Вычисляем количество страниц
            CountPage = (int)Math.Ceiling((double)CountRecords / ObjectsOnPage);
            if (CountPage == 0) CountPage = 1;

            if (selectedPage.HasValue)
            {
                if (selectedPage >= 0 && selectedPage < CountPage)
                {
                    CurrentPage = selectedPage.Value;
                }
            }
            else
            {
                switch (direction)
                {
                    case 1: // Влево
                        if (CurrentPage > 0)
                            CurrentPage--;
                        break;
                    case 2: // Вправо
                        if (CurrentPage < CountPage - 1)
                            CurrentPage++;
                        break;
                }
            }

            FillCurrentPage();
            UpdatePageUI();
        }

        private void FillCurrentPage()
        {
            CurrentPageList.Clear();
            int start = CurrentPage * ObjectsOnPage;
            int end = Math.Min(start + ObjectsOnPage, CountRecords);

            for (int i = start; i < end; i++)
            {
                CurrentPageList.Add(FilteredClients[i]);
            }
        }

        private void UpdatePageUI()
        {
            // Обновляем список страниц
            PageListBox.Items.Clear();
            for (int i = 1; i <= CountPage; i++)
            {
                PageListBox.Items.Add(i);
            }

            if (PageListBox.Items.Count > 0 && CurrentPage < PageListBox.Items.Count)
                PageListBox.SelectedIndex = CurrentPage;

            // Обновляем список клиентов
            ClientListView.ItemsSource = CurrentPageList;
            ClientListView.Items.Refresh();

            // Обновляем информацию о записях
            int startRecord = CurrentPage * ObjectsOnPage + 1;
            int endRecord = Math.Min((CurrentPage + 1) * ObjectsOnPage, CountRecords);

            if (CountRecords > 0)
                TBCount.Text = $"{startRecord}-{endRecord}";
            else
                TBCount.Text = "0";

            TBAllRecords.Text = $" из {CountRecords}";
        }

        private void ObjOnPage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilteredClients != null && ObjOnPage.SelectedItem != null)
            {
                try
                {
                    // Получаем выбранный элемент как ComboBoxItem
                    var selectedItem = ObjOnPage.SelectedItem as ComboBoxItem;
                    if (selectedItem != null)
                    {
                        string content = selectedItem.Content.ToString();

                        // Определяем количество записей на странице
                        if (content == "Все")
                        {
                            ObjectsOnPage = FilteredClients.Count;
                        }
                        else
                        {
                            if (int.TryParse(content, out int count))
                            {
                                ObjectsOnPage = count;
                            }
                        }

                        // Сбрасываем на первую страницу и обновляем отображение
                        CurrentPage = 0;
                        ChangePage(0, 0);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при изменении количества записей: {ex.Message}");
                }
            }
        }

        private void RightDirButton_Click(object sender, RoutedEventArgs e)
        {
            ChangePage(2, null);
        }

        private void PageListBox_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (PageListBox.SelectedItem != null)
            {
                ChangePage(0, Convert.ToInt32(PageListBox.SelectedItem.ToString()) - 1);
            }
        }

        private void LeftDirButton_Click(object sender, RoutedEventArgs e)
        {
            ChangePage(1, null);
        }

        private void DelBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var currentClient = (sender as Button).DataContext as Client;
                if (currentClient == null) return;

                var currentClientService = ГалиевLanguageEntities.GetContext().ClientService
                    .Where(p => p.ClientID == currentClient.ID).ToList();

                if (currentClientService.Count != 0)
                {
                    MessageBox.Show("Невозможно выполнить удаление: у клиента есть посещения");
                }
                else
                {
                    if (MessageBox.Show("Вы хотите удалить клиента?", "Внимание!",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        ГалиевLanguageEntities.GetContext().Client.Remove(currentClient);
                        ГалиевLanguageEntities.GetContext().SaveChanges();

                        // Перезагружаем данные после удаления
                        LoadData();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}");
            }
        }

        private void TBSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            searchText = TBSearch.Text;
            ApplyFilters();
        }

        private void CBSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CBSort.SelectedItem is ComboBoxItem selectedItem)
            {
                sortIndex = CBSort.SelectedIndex;
                ApplyFilters();
            }
        }

        private void CBFilt_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CBFilt.SelectedItem is ComboBoxItem selectedItem)
            {
                filterIndex = CBFilt.SelectedIndex;
                ApplyFilters();
            }
        }

      

        private void UpdBtn_Click(object sender, RoutedEventArgs e)
        {
            var currentClient = (sender as Button).DataContext as Client;
            if (currentClient != null)
            {
                Manager.MainFrame.Navigate(new AddPage(currentClient));
            }
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new AddPage(null));
        }

    }
}