

using System.Linq;
using System.Text.RegularExpressions;

namespace _1313.Omnisharp.Extensions.Helpers
{
    static class Indentifiers
    {
        internal static string SafeIdentifier(this string unsafeIdentifier)
        {
            return string.Join('.', unsafeIdentifier.Split('.').Select(part =>
                        {
                            if (Regex.IsMatch(part, "^[0-9].*"))
                            {
                                return "_" + part;
                            }
                            return part;
                        }));
        }
    }
}