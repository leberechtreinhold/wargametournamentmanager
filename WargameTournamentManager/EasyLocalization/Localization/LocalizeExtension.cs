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
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace EasyLocalization.Localization
{
    public class LocalizeExtension : MarkupExtension
    {

        #region Properties

        public string Key { get; set; }

        public Binding KeySource { get; set; }

        public Binding CountSource { get; set; }

        #endregion

        public LocalizeExtension() { }

        public LocalizeExtension(string key)
        {
            Key = key;
        }

        public LocalizeExtension(string key, Binding countSource)
        {
            Key = key;
            CountSource = countSource;
        }

        public LocalizeExtension(Binding keySource, Binding countSource)
        {
            KeySource = keySource;
            CountSource = countSource;
        }

        #region Public Methods

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var provideValueTarget = serviceProvider as IProvideValueTarget;
            var targetObject = provideValueTarget?.TargetObject as FrameworkElement;
            var targetProperty = provideValueTarget?.TargetProperty as DependencyProperty;
            string alternativeKey = $"{targetObject?.Name}_{targetProperty?.Name}";

            var multiBinding = new MultiBinding
            {
                Converter = new LocalizationConverter(Key, alternativeKey),
                NotifyOnSourceUpdated = true
            };

            multiBinding.Bindings.Add(new Binding
            {
                Source = LocalizationManager.Instance,
                Path = new PropertyPath("CurrentCulture")
            });

            if (KeySource != null)
            {
                multiBinding.Bindings.Add(KeySource);
            }

            if (CountSource != null)
            {
                multiBinding.Bindings.Add(CountSource);
            }

            return multiBinding.ProvideValue(serviceProvider);
        }

        #endregion

    }
}