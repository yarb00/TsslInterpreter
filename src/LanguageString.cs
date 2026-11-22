// SPDX-FileCopyrightText: yarb00 <https://www.yarb00.dev>
// SPDX-License-Identifier: MPL-2.0

// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace TsslInterpreter;

internal readonly record struct LanguageStringInterpolation(params LanguageString VariableName);

internal readonly union LanguageStringAtom(string, LanguageStringInterpolation)
{
	public readonly bool TryCalculate(IReadOnlyDictionary<string, string> variables, [NotNullWhen(true)] out string? result)
	{
		switch (this)
		{
			// default(LanguageStringAtom) should be an empty string
			case null:
				result = string.Empty;
				return true;

			case string @string:
				result = @string;
				return true;

			case LanguageStringInterpolation interpolation:
				// default(LanguageStringInterpolation) should be invalid
				if (interpolation.VariableName.TryCalculate(variables, out string? variableName) && variableName.IsValidLanguageIdentifier)
					return variables.TryGetValue(variableName.LanguageTrim(), out result);
				result = null;
				return false;
		}

		throw new UnreachableException("Union value is invalid.");
	}
}

[CollectionBuilder(typeof(LanguageString), nameof(Create))]
internal readonly struct LanguageString(params ReadOnlySpan<LanguageStringAtom> atoms) : IEnumerable<LanguageStringAtom>
{
	private readonly ImmutableArray<LanguageStringAtom> atoms = atoms.ToImmutableArray();

	public static implicit operator LanguageString(LanguageStringAtom atom) => new(atom);

	public static implicit operator LanguageString(LanguageStringInterpolation interpolation) => new(interpolation);

	public static implicit operator LanguageString(string @string) => new(@string);

	public static LanguageString Create(params ReadOnlySpan<LanguageStringAtom> atoms) => new(atoms);

	public readonly bool TryCalculate(IReadOnlyDictionary<string, string> variables, [NotNullWhen(true)] out string? result)
	{
		result = string.Empty;

		// default(LanguageString) and [] should be an empty string
		if (atoms.IsDefaultOrEmpty) return true;

		foreach (LanguageStringAtom atom in atoms)
		{
			if (!atom.TryCalculate(variables, out string? @string))
			{
				result = null;
				return false;
			}

			result += @string;
		}

		return true;
	}

	IEnumerator<LanguageStringAtom> IEnumerable<LanguageStringAtom>.GetEnumerator() => ((IEnumerable<LanguageStringAtom>)atoms).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)atoms).GetEnumerator();
}
