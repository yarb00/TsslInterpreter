// https://tssl.yarb00.dev

using System.Text.RegularExpressions;

namespace TsslInterpreter;

internal static partial class StringExtensions
{
	extension(string @string)
	{
		public bool IsEmpty => string.IsNullOrEmpty(@string);

		public bool IsEmptyOrWhitespace => string.IsNullOrWhiteSpace(@string);

		public bool IsValidLanguageIdentifier => GetValidLanguageIdentifiersRegex().IsMatch(@string);

		public string LanguageTrim() => @string.Trim(' ', '\t', '\r', '\n');
	}

	[GeneratedRegex("^[A-Za-z0-9_ ]+$")]
	private static partial Regex GetValidLanguageIdentifiersRegex();
}
