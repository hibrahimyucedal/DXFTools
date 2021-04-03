using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DXFParser
{
    class Program
    {
        static void Main(string[] args)
        {
            var file = ReadFile("bos.dxf");
            var sections = GetSections(file);
        }

        private static Dictionary<string, List<BaseDXFProp>> ProcessSection(string sectionName, List<string> rawValues)
        {
            var dict = new Dictionary<string, List<BaseDXFProp>>();
            if (sectionName.Equals(nameof(SectionType.HEADER)))
            {
                var varIndexes = new List<int>();
                for (int i = 0; i < rawValues.Count; i++)
                {
                    if (rawValues[i].Equals("9"))
                    {
                        varIndexes.Add(i);
                    }
                }

                for (int i = 0; i < varIndexes.Count; i++)
                {
                    var currentVarIndex = varIndexes[i];
                    var nextVarIndex = varIndexes.Count == i + 1 ? -1 : varIndexes[i + 1];
                    var varNameIndex = currentVarIndex + 1;

                    var varName = rawValues[varNameIndex];
                    var varRawValues = new List<string>();
                    if (!nextVarIndex.Equals(-1))
                    {
                        varRawValues = rawValues.GetRange(varNameIndex + 1, nextVarIndex - varNameIndex - 1);
                    }
                    else
                    {
                        varRawValues = rawValues.GetRange(varNameIndex + 1, rawValues.Count - varNameIndex - 1);
                    }

                    var varValues = new List<BaseDXFProp>();

                    for (int page = 0; page < Math.Floor((decimal)varRawValues.Count / 2) + 1; page++)
                    {
                        var rel = varRawValues.Skip(page * 2).Take(2).ToList();
                        if (rel.Any())
                        {
                            varValues.Add(new BaseDXFProp
                            {
                                Type = GetPropType(rel[0]),
                                Value = rel[1]
                            });
                        }
                    }

                    dict[varName] = varValues;
                }
            }

            return dict;
        }

        private static PropType GetPropType(string rawGroupCode)
        {
            var groupCode = Convert.ToInt32(rawGroupCode);

            if (TypeGroupCodes.STRING.Any(x => x.Item1 <= groupCode && groupCode <= x.Item2))
            {
                return PropType.STRING;
            }

            return PropType.UNDEFINED;
        }

        private static Dictionary<string, Dictionary<string, List<BaseDXFProp>>> GetSections(List<string> file)
        {
            var result = new Dictionary<string, Dictionary<string, List<BaseDXFProp>>>();

            var sectionNameStartIndexDict = new Dictionary<string, int>();
            var sectionEndIndexes = new List<int>();

            for (int i = 0; i < file.Count; i++)
            {
                var previousLine = i == 0 ? null : file[i - 1];
                var currentLine = file[i];
                var nextLine = i+1 == file.Count ? null : file[i + 1];

                if (previousLine != null && nextLine != null && previousLine.Equals("0") && currentLine.Equals("SECTION") && nextLine.Equals("2"))
                {
                    sectionNameStartIndexDict[file[i+2]] = i+3;
                }

                if (nextLine != null && currentLine.Equals("ENDSEC") && nextLine.Equals("0"))
                {
                    sectionEndIndexes.Add(i-1);
                }
            }

            foreach (var sectionStart in sectionNameStartIndexDict)
            {
                var sectionName = sectionStart.Key;
                var sectionValue = file.GetRange(sectionStart.Value, sectionEndIndexes.First() - sectionStart.Value);

                result[sectionStart.Key] = ProcessSection(sectionName, sectionValue);
                sectionEndIndexes.RemoveAt(0);
            }

            return result;
        }

        private static List<string> ReadFile(string directory)
        {
            var result = new List<string>();

            using (StreamReader sr = File.OpenText(directory))
            {
                string s = null;
                while ((s = sr.ReadLine()) != null)
                {
                    result.Add(s.Trim());
                }
            }

            return result;
        }
    }

    public static class TypeGroupCodes
    {
        public static List<Tuple<int, int>> STRING = new List<Tuple<int, int>>
        {
            /*
             String (with the introduction of extended symbol names in AutoCAD 2000, 
             the 255-character limit has been increased to 2049 single-byte characters 
             not including the newline at the end of the line)
             */
            new Tuple<int, int>(0,9),            /*             String (255-character maximum; less for Unicode strings)            */            new Tuple<int, int>(100,100),

            /*             String (255-character maximum; less for Unicode strings)            */
            new Tuple<int, int>(102,102),

            /*             String representing hexadecimal (hex) handle value            */
            new Tuple<int, int>(105,105),

            /*             Arbitrary text string            */
            new Tuple<int, int>(300,309),

            /*             String representing hex value of binary chun            */
            new Tuple<int, int>(310,319),

            /*             String representing hex handle value            */
            new Tuple<int, int>(320,329),

            /*             String representing hex object IDs            */
            new Tuple<int, int>(330,369),

            /*             String representing hex handle value            */
            new Tuple<int, int>(410,419),

            /*             String            */
            new Tuple<int, int>(430,439),

            /*             String            */
            new Tuple<int, int>(470,479),

            /*             String representing hex handle value            */
            new Tuple<int, int>(480,481),

            /*             String (same limits as indicated with 0-9 code range)            */
            new Tuple<int, int>(1000,1009)
        };
    }

    public class BaseDXFProp
    {
        public PropType Type { get; set; }

        public string Value { get; set; }
    }

    public enum PropType
    {
        UNDEFINED,
        INT,
        STRING,
        DECIMAL
    }

    public enum SectionType
    {
        UNDEFINED,
        HEADER,
        CLASSES,
        TABLES,
        BLOCKS,
        ENTITIES,
        ACDSDATA,
        OBJECTS
    }
}