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
using System.IO;
using EasyLocalization.Localization;

namespace EasyLocalization.Readers
{
    public class CharSeperatedFileReader : FileReader
    {

        #region Fields

        private readonly char _separator;

        #endregion

        public CharSeperatedFileReader(string path, char separator = ';') : base(path)
        {
            _separator = separator;
        }

        #region Public Methods

        internal override Dictionary<string, LocalizationEntry> GetEntries()
        {
            var entries = new Dictionary<string, LocalizationEntry>();

            using (StreamReader sr = File.OpenText(Path))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    var splitted = line.Split(_separator);

                    // Each line needs to have at least 2 values (key, value)
                    if (splitted.Length == 1)
                        throw new FileFormatException($"Each line needs to have at least 2 values (line: {line})");

                    switch (splitted.Length)
                    {
                        default:
                            entries.Add(splitted[0], new LocalizationEntry(splitted[1]));
                            break;
                        case 3:
                            entries.Add(splitted[0], new LocalizationEntry(splitted[1], splitted[2]));
                            break;
                        case 4:
                            entries.Add(splitted[0], new LocalizationEntry(splitted[1], splitted[2], splitted[3]));
                            break;
                    }
                }
            }

            return entries;
        }

        #endregion

    }
}