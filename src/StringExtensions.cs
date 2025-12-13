// https://tssl.yarb00.dev

using System.Text.RegularExpressions;

namespace TsslInterpreter;

internal static partial class StringExtensions
{
	extension(string @string)
	{
		public bool IsEmpty => string.IsNullOrEmpty(@string);
		public bool IsEmptyOrWhitespace => string.IsNullOrWhiteSpace(@string);

		public bool IsAlphanumericWithUnderscores => Regex_AlphanumericWithUnderscores().IsMatch(@string);
		public bool IsAlphanumericWithSpaces => Regex_AlphanumericWithSpaces().IsMatch(@string);
	}

	[GeneratedRegex(@"^[a-zA-Z0-9_]+$")] // a-z and A-Z and 0-9 and underscores; "\w" can't be used because it includes Unicode characters in C#
	private static partial Regex Regex_AlphanumericWithUnderscores();

	[GeneratedRegex(@"^[a-zA-Z0-9 ]+$")] // a-z and A-Z and 0-9 and spaces
	private static partial Regex Regex_AlphanumericWithSpaces();
}
