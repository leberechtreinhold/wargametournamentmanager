using EasyLocalization.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;

// Note the namespace! This is because we add WPF markups, and they are easier
// to use in the local namespace
// When localizing enums in WPF, we can use a converter to get the value to the
// translated one, but to convert back, we need to use something that holds
// both values, because culture may have changed. Therefore we use a LocalizedText
// object, and use a markup extension to make it transparent.
// Based on https://www.meziantou.net/wpf-and-enumerations.htm
namespace WargameTournamentManager
{
    public class LocalizedEnumText
    {
        public Enum Value { get; set; }
        public string Text { get; set; }
        public string TranslatedText { get; set; }

        public LocalizedEnumText(Enum value)
        {
            Value = value;
            var attribute = value.GetType().GetRuntimeField(value.ToString()).
                GetCustomAttributes(typeof(LocalizationTextAttribute), false).
                SingleOrDefault() as LocalizationTextAttribute;
            if (attribute != null)
            {
                Text = attribute.LocalizationText;
                TranslatedText = LocalizationManager.Instance.GetValue(Text);

            }
            else
            {
                Text = value.ToString();
                TranslatedText = Text;
            }
        }
    }

    [MarkupExtensionReturnType(typeof(IEnumerable<LocalizedEnumText>))]
    public class EnumExtension : MarkupExtension
    {
        public EnumExtension()
        {
        }

        public EnumExtension(Type enumType)
        {
            this.EnumType = enumType;
        }

        [ConstructorArgument("enumType")]
        public Type EnumType { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (EnumType == null)
                throw new InvalidOperationException("The enum type is not set");

            return Enum.GetValues(this.EnumType).Cast<Enum>().Select(value => new LocalizedEnumText(value));
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class LocalizationTextAttribute : Attribute
    {
        public LocalizationTextAttribute(string localizationText)
        {
            LocalizationText = localizationText;
        }

        public string LocalizationText { get; set; }
    }

    public class LocalizationTextAttributeBasedObjectDataProvider : ObjectDataProvider
    {
        public object GetEnumValues(Enum enumObj)
        {
            var attribute = enumObj.GetType().GetRuntimeField(enumObj.ToString()).
                GetCustomAttributes(typeof(LocalizationTextAttribute), false).
                SingleOrDefault() as LocalizationTextAttribute;
            return attribute == null ? enumObj.ToString() : LocalizationManager.Instance.GetValue(attribute.LocalizationText);
        }

        public List<object> GetListOfAttributes(Type type)
        {
            return Enum.GetValues(type).OfType<Enum>().Select(GetEnumValues).ToList();
        }
    }

}
