using System;
using Windows.UI.Xaml.Data;

namespace Mail.Converters
{
    internal class FileSizeToDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is ulong SizeRaw)
            {
                if (SizeRaw > 0)
                {
                    switch ((short)Math.Log(SizeRaw, 1024))
                    {
                        case 0:
                            {
                                return $"{SizeRaw:##.##} B";
                            }
                        case 1:
                            {
                                return $"{SizeRaw / 1024d:##.##} KB";
                            }
                        case 2:
                            {
                                return $"{SizeRaw / 1048576d:##.##} MB";
                            }
                        case 3:
                            {
                                return $"{SizeRaw / 1073741824d:##.##} GB";
                            }
                        case 4:
                            {
                                return $"{SizeRaw / 1099511627776d:##.##} TB";
                            }
                        case 5:
                            {
                                return $"{SizeRaw / 1125899906842624d:##.##} PB";
                            }
                        case 6:
                            {
                                return $"{SizeRaw / 1152921504606846976d:##.##} EB";
                            }
                        default:
                            {
                                throw new ArgumentOutOfRangeException(nameof(SizeRaw), $"Argument is too large");
                            }
                    }
                }
                else
                {
                    return "0 KB";
                }
            }

            throw new NotSupportedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
