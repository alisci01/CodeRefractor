using System.Collections.Generic;
using System.Linq;

namespace CodeRefractor.RuntimeBase.Analyze
{
    public class FieldNameTable
    {
        readonly Dictionary<string, string> InvalidNames = new Dictionary<string, string>();
        int _countedField;

        public static FieldNameTable Instance { get { return StaticInstance; } }

        static readonly FieldNameTable StaticInstance = new FieldNameTable();

        public string GetFieldName(string name)
        {
            if (!ContainsInvalidCharacters(name))
                return name;
            string result;
            if (InvalidNames.TryGetValue(name, out result))
                return result;
            var formattedName = string.Format("AutoNamed_{0}", _countedField);
            _countedField++;
            InvalidNames[name] = formattedName;
            return formattedName;
        }

        public static bool ContainsInvalidCharacters(string text)
        {
            var notToFind = new[] { "<", ">" };
            var count = notToFind.Count(text.Contains);
            return count != 0;
        }
    }
}