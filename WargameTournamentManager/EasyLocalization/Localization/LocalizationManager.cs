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

using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using EasyLocalization.Annotations;
using EasyLocalization.Readers;

namespace EasyLocalization.Localization
{
    public class LocalizationManager : INotifyPropertyChanged
    {

        #region Singleton

        private static LocalizationManager _instance;

        public static LocalizationManager Instance => _instance ?? (_instance = new LocalizationManager());

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Fields

        private readonly Dictionary<CultureInfo, Dictionary<string, LocalizationEntry>> _languageEntries = new Dictionary<CultureInfo, Dictionary<string, LocalizationEntry>>();
        private CultureInfo _currentCulture;

        #endregion

        #region Properties

        public CultureInfo CurrentCulture
        {
            get => _currentCulture;
            set
            {
                if (_currentCulture?.Equals(value) == true)
                    return;

                if (!_languageEntries.ContainsKey(value))
                    return;

                _currentCulture = value;
                OnPropertyChanged();
            }
        }

        public List<CultureInfo> AvailableCultures => _languageEntries.Keys.ToList();

        #endregion

        #region Public Methods

        public void AddCulture(CultureInfo culture, FileReader reader, bool choose = false)
        {
            if (_languageEntries.ContainsKey(culture))
            {
                // If the culture is already registered, re-set its values
                _languageEntries[culture] = reader.GetEntries();
            }
            else
            {
                _languageEntries.Add(culture, reader.GetEntries());
            }

            if (choose)
            {
                CurrentCulture = culture;
            }
        }

        public string GetValue(string key, bool nullWhenUnfound = true)
        {
            if (_languageEntries == null || CurrentCulture == null)
                return key;

            var entries = _languageEntries[CurrentCulture];

            if (key == null || !entries.ContainsKey(key))
                return nullWhenUnfound ? null : key;

            return entries[key].Value;
        }

        public string GetValue(string key, int count, bool nullWhenUnfound = true)
        {
            if (_languageEntries == null || CurrentCulture == null)
                return key;

            var entries = _languageEntries[CurrentCulture];

            if (key == null || !entries.ContainsKey(key))
                return nullWhenUnfound ? null : key;

            var entry = entries[key];
            return count == 0 ? entry.ZeroValue : count == 1 ? entry.Value : string.Format(entry.PluralValue, count);
        }

        #endregion


    }
}
