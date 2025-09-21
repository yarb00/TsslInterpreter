// https://yarb00.dev

using System.Text.RegularExpressions;

namespace TsslInterpreter;

internal static partial class StringExtensions
{
	public static bool IsNullOrEmpty(this string @string) => string.IsNullOrEmpty(@string);
	public static bool IsNullOrWhiteSpace(this string @string) => string.IsNullOrWhiteSpace(@string);

	public static bool IsAlphaNumericWithUnderscores(this string @string) => AlphaNumericWithUnderscoresRegex().IsMatch(@string);
	public static bool IsAlphaNumericWithSpaces(this string @string) => AlphaNumericWithSpacesRegex().IsMatch(@string);

	[GeneratedRegex(@"^[a-zA-Z0-9_]+$")] // a-z and A-Z and 0-9 and underscores; "\w" can't be used because it's not ECMAScript complaint in C# and includes unicode characters
	private static partial Regex AlphaNumericWithUnderscoresRegex();

	[GeneratedRegex(@"^[a-zA-Z0-9 ]+$")] // a-z and A-Z and 0-9 and spaces
	private static partial Regex AlphaNumericWithSpacesRegex();
}
