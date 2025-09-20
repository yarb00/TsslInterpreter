// https://yarb00.dev

namespace TsslInterpreter;

internal static class StringExtensions
{
	public static bool IsNullOrEmpty(this string @string) => string.IsNullOrEmpty(@string);
	public static bool IsNullOrWhiteSpace(this string @string) => string.IsNullOrWhiteSpace(@string);
}
