using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace GalievLanguageSchool
{
    public partial class AddPage : Page
    {
        private Client _client;          // Редактируемый клиент (null для добавления)
        private bool _isEdit;            // Флаг редактирования
        private string _selectedPhotoPath; // Выбранный путь к фото (временный)

        public AddPage(Client client)
        {
            InitializeComponent();
            _client = client;
            _isEdit = client != null;

            if (_isEdit)
            {
                TbID.Visibility = Visibility.Visible;
                TbID.Text = client.ID.ToString();
                LoadClientData();
                Title = "Редактирование клиента";
            }
            else
            {
                Title = "Добавление клиента";
            }
        }

        private void LoadClientData()
        {
            TbLastName.Text = _client.LastName;
            TbFirstName.Text = _client.FirstName;
            TbMiddleName.Text = _client.Patronymic;

            TbEmail.Text = _client.Email;
            TbPhone.Text = _client.Phone;
            DpBirthday.SelectedDate = _client.Birthday;

            // Установка пола
            if (_client.GenderCode == "м")
                RbMale.IsChecked = true;
            else if (_client.GenderCode == "ж")
                RbFemale.IsChecked = true;

            // Загрузка фото - формируем полный путь из имени файла
            if (!string.IsNullOrEmpty(_client.PhotoPath))
            {
                // Склеиваем путь к exe-файлу и относительный путь из базы
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _client.PhotoPath);

                if (File.Exists(fullPath))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(fullPath);
                    // Важно: OnLoad позволяет приложению "отпустить" файл, чтобы его можно было перезаписать
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    ImgPhoto.Source = bitmap;
                    _selectedPhotoPath = fullPath;
                }
            }
        }

        private void BtnSelectPhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                _selectedPhotoPath = openFileDialog.FileName;
                ImgPhoto.Source = new BitmapImage(new Uri(_selectedPhotoPath, UriKind.Absolute));
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields())
                return;

            var context = ГалиевLanguageEntities.GetContext();

            if (_isEdit)
            {
                var clientToUpdate = context.Client.Find(_client.ID);
                if (clientToUpdate == null)
                {
                    MessageBox.Show("Клиент не найден в базе данных.");
                    return;
                }
                UpdateClient(clientToUpdate);
                context.SaveChanges();
                MessageBox.Show("Клиент успешно обновлен.");
            }
            else
            {
                var newClient = new Client();
                UpdateClient(newClient);
                newClient.RegistrationDate = DateTime.Now;
                // TotalVisits и LastVisitDate вычисляются автоматически в модели
                context.Client.Add(newClient);
                context.SaveChanges();
                MessageBox.Show("Клиент успешно добавлен.");
            }

            NavigationService.Navigate(new ClientPage());
        }

        private void UpdateClient(Client client)
        {
            client.LastName = TbLastName.Text.Trim();
            client.FirstName = TbFirstName.Text.Trim();
            client.Patronymic = TbMiddleName.Text.Trim();

            client.Email = TbEmail.Text.Trim();
            client.Phone = TbPhone.Text.Trim();
            client.Birthday = DpBirthday.SelectedDate;

            if (RbMale.IsChecked == true)
                client.GenderCode = "м";
            else if (RbFemale.IsChecked == true)
                client.GenderCode = "ж";
            else
                client.GenderCode = null;

            // Сохранение фотографии - сохраняем ТОЛЬКО ИМЯ ФАЙЛА
            if (!string.IsNullOrEmpty(_selectedPhotoPath))
            {
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                // 1. Создаем папку "Клиенты" в bin/Debug (или там, где запущено приложение)
                string folderName = "Клиенты";
                string targetDir = Path.Combine(appDir, folderName);

                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                // 2. Генерируем имя файла
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(_selectedPhotoPath);
                string destPath = Path.Combine(targetDir, fileName);

                // 3. Копируем файл
                File.Copy(_selectedPhotoPath, destPath, true);

                // 4. Сохраняем в БД относительный путь: "Клиенты\имя_файла.jpg"
                client.PhotoPath = Path.Combine(folderName, fileName);
            }
        }

        private bool ValidateFields()
        {
            string lastName = TbLastName.Text.Trim();
            string firstName = TbFirstName.Text.Trim();
            string middleName = TbMiddleName.Text.Trim();
            string email = TbEmail.Text.Trim();
            string phone = TbPhone.Text.Trim();

            // ФИО обязательны
            if (string.IsNullOrWhiteSpace(firstName))
            {
                MessageBox.Show("Фамилия обязательна для заполнения.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                TbLastName.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(lastName))
            {
                MessageBox.Show("Имя обязательно для заполнения.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                TbFirstName.Focus();
                return false;
            }

            // Проверка допустимых символов (только буквы, пробел, дефис)
            if (!IsValidFIO(firstName))
            {
                MessageBox.Show("Фамилия может содержать только буквы, пробел и дефис.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                TbLastName.Focus();
                return false;
            }
            if (!IsValidFIO(lastName))
            {
                MessageBox.Show("Имя может содержать только буквы, пробел и дефис.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                TbFirstName.Focus();
                return false;
            }
            if (!string.IsNullOrWhiteSpace(middleName) && !IsValidFIO(middleName))
            {
                MessageBox.Show("Отчество может содержать только буквы, пробел и дефис.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                TbMiddleName.Focus();
                return false;
            }

            // Длина каждого поля не более 50
            if (firstName.Length > 50)
            {
                MessageBox.Show("Фамилия не может быть длиннее 50 символов.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                TbLastName.Focus();
                return false;
            }
            if (lastName.Length > 50)
            {
                MessageBox.Show("Имя не может быть длиннее 50 символов.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                TbFirstName.Focus();
                return false;
            }
            if (middleName.Length > 50)
            {
                MessageBox.Show("Отчество не может быть длиннее 50 символов.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                TbMiddleName.Focus();
                return false;
            }

            // Проверка email
            if (!IsValidEmail(email))
            {
                MessageBox.Show("Введите корректный email адрес.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                TbEmail.Focus();
                return false;
            }

            // Проверка телефона
            if (!IsValidPhone(phone))
            {
                TbPhone.Focus();
                return false;
            }

            // Проверка даты рождения
            if (!DpBirthday.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите дату рождения.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                DpBirthday.Focus();
                return false;
            }

            if (DpBirthday.SelectedDate.Value > DateTime.Now)
            {
                MessageBox.Show("Дата рождения не может быть в будущем.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                DpBirthday.Focus();
                return false;
            }

            // Пол обязателен
            if (RbMale.IsChecked != true && RbFemale.IsChecked != true)
            {
                MessageBox.Show("Выберите пол.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private bool IsValidFIO(string text)
        {
            // Разрешаем буквы (латиница, кириллица), пробел, дефис
            return Regex.IsMatch(text, @"^[a-zA-Zа-яА-ЯёЁ\s-]+$");
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            // Строгая проверка email через регулярное выражение
            // Требует наличие @ и точки в доменной части
            string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

            if (!Regex.IsMatch(email, pattern))
            {
                MessageBox.Show("Введите корректный email адрес.\n" +
                               "Пример правильного формата: user@mail.ru или name@domain.com\n\n" +
                               "Требования:\n" +
                               "• Должен быть символ @\n" +
                               "• Доменная часть должна содержать точку (например, .ru, .com)\n" +
                               "• Допустимы латинские буквы, цифры и символы: . _ % + -",
                               "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Дополнительная проверка через MailAddress на случай других edge-cases
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("Номер телефона обязателен для заполнения.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверяем наличие недопустимых символов
            if (!Regex.IsMatch(phone, @"^[\d+\-\(\)\s]+$"))
            {
                // Находим недопустимые символы для информативного сообщения
                var invalidChars = phone.Where(c => !char.IsDigit(c) && c != '+' && c != '-' && c != '(' && c != ')' && c != ' ')
                                        .Distinct()
                                        .Select(c => $"'{c}'");

                string invalidCharsList = string.Join(", ", invalidChars);

                MessageBox.Show($"Телефон содержит недопустимые символы: {invalidCharsList}.\n\n" +
                               "Разрешены только цифры и символы: + - ( ) пробел.\n" +
                               "Пример правильного формата: +7 (999) 123-45-67",
                               "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Считаем только цифры
            int digitCount = phone.Count(char.IsDigit);

            // Проверяем количество цифр
            if (digitCount < 11)
            {
                MessageBox.Show($"В номере телефона недостаточно цифр.\n" +
                               $"Сейчас цифр: {digitCount}, требуется ровно 11 цифр.\n\n" +
                               $"Пример правильного формата: +7 (999) 123-45-67",
                               "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (digitCount > 11)
            {
                MessageBox.Show($"В номере телефона слишком много цифр.\n" +
                               $"Сейчас цифр: {digitCount}, требуется ровно 11 цифр.\n\n" +
                               $"Пример правильного формата: +7 (999) 123-45-67",
                               "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}