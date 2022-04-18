using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Image;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MLNetSourceGenerator
{
    internal static class Utils
    {
        private static Dictionary<NumberDataViewType, string> numberTypeLookupTable = new Dictionary<NumberDataViewType, string>()
            {
                { NumberDataViewType.Single, "float" },
                { NumberDataViewType.Double, "double" },
                { NumberDataViewType.SByte, "sbyte" },
                { NumberDataViewType.Byte, "byte" },
                { NumberDataViewType.Int16, "short" },
                { NumberDataViewType.Int32, "int" },
                { NumberDataViewType.Int64, "long" },
                { NumberDataViewType.UInt16, "ushort" },
                { NumberDataViewType.UInt32, "uint" },
                { NumberDataViewType.UInt64, "ulong" },
            };

        public static string GenerateClassCodeFromSchema(string sanitizedClassName, DataViewSchema schema)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"public class {sanitizedClassName}");
            sb.AppendLine("{");
            foreach (var column in schema)
            {
                if (column.IsHidden)
                {
                    continue;
                }

                var parseColumnName = GenerateColumnName(column.Name);
                var typeString = column.Type switch
                {
                    PrimitiveDataViewType t => MappingPrimitiveTypeToString(t),
                    VectorDataViewType t => $"{MappingPrimitiveTypeToString(t.ItemType)}[]",
                    ImageDataViewType t => "System.Drawing.Bitmap",
                    _ => string.Empty,
                };

                if (typeString != string.Empty)
                {
                    sb.AppendLine($"[ColumnName(@\"{parseColumnName}\")]");
                    if (typeString == "System.Drawing.Bitmap")
                    {
                        var height = (column.Type as ImageDataViewType).Height;
                        var width = (column.Type as ImageDataViewType).Width;
                        sb.AppendLine($"[Microsoft.ML.Transforms.Image.ImageType({height},{width})]");
                    }
                    sb.AppendLine($"public {typeString} {Utils.Sanitize(column.Name)} {{get; set;}}");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        public static string GenerateColumnName(string column)
        {
            return column.Replace("\"", "\"\"");
        }

        private static string MappingPrimitiveTypeToString(PrimitiveDataViewType type)
        {
            return type switch
            {
                BooleanDataViewType => "bool",
                DateTimeDataViewType => "Datetime",
                NumberDataViewType numberType => numberTypeLookupTable[numberType],
                TextDataViewType => "string",
                DateTimeOffsetDataViewType => "DateTimeOffset",
                KeyDataViewType => "uint",
                TimeSpanDataViewType => "TimeSpan",
                _ => string.Empty,
            };
        }

        /// <summary>
        /// sanitize name to code, if <paramref name="name"/> contains chars other than letter or number, replace them with underscore. If the first letter is low-case, make it upper-case. Otherwise, if the first letter is number, put a underscore before it.
        /// </summary>
        /// <param name="name">name.</param>
        /// <returns>sanitized name.</returns>
        public static string Sanitize(string name)
        {
            // "ABC" => "ABC"
            // "aBC @#.123" => "ABC____123"
            // "1aBC" => "_1aBC"

            var sanitizedName = string.Join(string.Empty, name.Select(x => char.IsLetterOrDigit(x) ? x : '_'));
            if (char.IsLetter(sanitizedName[0]))
            {
                return char.ToUpperInvariant(sanitizedName[0]) + sanitizedName.Substring(1);
            }
            else
            {
                return $"_{sanitizedName}";
            }
        }

        public static string FormatCode(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);

            var syntaxNode = tree.GetRoot();
            return Formatter.Format(syntaxNode, new AdhocWorkspace()).ToFullString();
        }
    }
}
