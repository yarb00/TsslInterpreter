// SPDX-FileCopyrightText: yarb00 <https://www.yarb00.dev>
// SPDX-License-Identifier: MPL-2.0

// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.RegularExpressions;

namespace TsslInterpreter;

internal static partial class StringExtensions
{
	extension(string @string)
	{
		public bool IsEmpty => string.IsNullOrEmpty(@string);

		public bool IsEmptyOrWhitespace => string.IsNullOrWhiteSpace(@string);

		public bool IsValidLanguageIdentifier => GetValidLanguageIdentifierRegex().IsMatch(@string);

		public string LanguageTrim() => @string.Trim(' ', '\t', '\r', '\n');
	}

	[GeneratedRegex("^[A-Za-z0-9_ ]+$")]
	private static partial Regex GetValidLanguageIdentifierRegex();
}
