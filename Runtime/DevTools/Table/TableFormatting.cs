using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;

namespace DavidUtils.DevTools.Table
{
    public static class TableFormatting
    {
        private const string VALID_COLOR = "white";
        private const string INVALID_COLOR = "red";
        
        public static string ToTableLine(this IEnumerable<string> values, int[] colLengths) =>
            string.Join(" | ", values.Select((v, i) =>
                i < colLengths.Length ? v.TruncateFixedSize(colLengths[i]) : v));
        
        public static string ToTableLine_ColoredByValidation(this string[] values, int[] colLengths, bool[] badFlags) =>
            string.Join(" | ", values.Select((v, i) => 
                    badFlags[i]
                        ? (string.IsNullOrEmpty(v) ? "NULL" : v).TruncateFixedSize(colLengths[i]).Colored(INVALID_COLOR)
                        : v.TruncateFixedSize(colLengths[i])))
                .Colored(VALID_COLOR);
    }
}
