/* MIT License

Copyright(c) 2019 zHaytam

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. */

using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace EasyLocalization.Localization
{
    public class LocalizationConverter : IMultiValueConverter
    {

        #region Fields

        private string _key;
        private string _alternativeKey;

        #endregion

        public LocalizationConverter(string key, string alternativeKey)
        {
            _key = key;
            _alternativeKey = alternativeKey;
        }

        #region Public Methods

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            object key;
            int count;

            switch (values.Length)
            {
                default:
                    // (1) CultureInfo
                    return LocalizationManager.Instance.GetValue(_key) ?? LocalizationManager.Instance.GetValue(_alternativeKey, false);
                case 2:
                    // (2) CultureInfo + KeySource or CultureInfo + CountSource
                    key = values.FirstOrDefault(v => v is string);

                    if (key == null)
                    {
                        // CultureInfo + CountSource
                        count = System.Convert.ToInt32(values.First(v => !(v is CultureInfo)));
                        return LocalizationManager.Instance.GetValue(_key, count) ?? LocalizationManager.Instance.GetValue(_alternativeKey, count, false);
                    }
                    else
                    {
                        // CultureInfo + KeySource
                        return LocalizationManager.Instance.GetValue(key.ToString()) ?? LocalizationManager.Instance.GetValue(_alternativeKey, false);
                    }
                case 3:
                    // (3) CultureInfo + KeySource + CountSource
                    key = values.First(v => v is string);
                    count = System.Convert.ToInt32(values.First(v => v != key && !(v is CultureInfo)));
                    return LocalizationManager.Instance.GetValue(key.ToString(), count) ?? LocalizationManager.Instance.GetValue(_alternativeKey, count, false);
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}
