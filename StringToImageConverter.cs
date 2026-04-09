using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace GalievLanguageSchool
{
    public class StringToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string fileName = value as string;
            if (string.IsNullOrEmpty(fileName))
                return GetDefaultImage();

            // Строим полный путь к папке Photos
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Photos", fileName);
            if (File.Exists(fullPath))
                return new BitmapImage(new Uri(fullPath));

            return GetDefaultImage();
        }

        private BitmapImage GetDefaultImage()
        {
            string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Photos", "default.png");
            if (File.Exists(defaultPath))
                return new BitmapImage(new Uri(defaultPath));
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}