using System.Collections;
using System.Globalization;
using FixedPointMath;

namespace Beanstalk.Analysis.Text;

public sealed class Lexer(IBuffer source) : ILexer
{
	private IBuffer Source { get; } = source;

	public ScanResult? ScanToken(int position)
	{
		if (position >= Source.Length)
			return null;
		
		var character = Source[position];

		if (char.IsWhiteSpace(character))
			return ScanWhiteSpace(position);

		if (char.IsDigit(character))
			return ScanNumber(position);

		if (char.IsLetter(character) || character == '_')
			return ScanIdentifier(position);

		return ScanOperator(position);
	}

	private List<Token> ScanAllTokens()
	{
		var tokens = new List<Token>();
		var position = 0;
		
		while (true)
		{
			if (ScanToken(position) is not { } lexerResult)
			{
				return tokens;
			}

			tokens.Add(lexerResult.Token);
			position = lexerResult.NextPosition;
		}
	}
	
	private int? ScanStorageSize(int position)
	{
		var end = position;
		while (end < Source.Length && char.IsDigit(Source[end]))
			end++;

		if (end <= position)
			return null;

		return int.Parse(Source.GetText(new TextRange(position, end)));
	}

	private ScanResult ScanOperator(int position)
	{
		var end = position;
		while (end < Source.Length)
		{
			var character = Source[end];
			if (char.IsLetterOrDigit(character) || char.IsWhiteSpace(character) || character == '_')
				break;
			
			end++;
		}

		while (end > position)
		{
			var range = new TextRange(position, end);
			var operatorString = Source.GetText(range);
			if (TokenType.GetOperator(operatorString) is { } @operator)
			{
				var token = new Token(@operator, range, Source);
				return new ScanResult(token, end);
			}
			
			end--;
		}

		end = position + 1;
		return new ScanResult(new Token(TokenType.Invalid, new TextRange(position, end), Source), end);
	}

	private ScanResult ScanWhiteSpace(int position)
	{
		var end = position;
		while (end < Source.Length && char.IsWhiteSpace(Source[end]))
			end++;

		var range = new TextRange(position, end);
		var token = new Token(TokenType.Whitespace, range, Source);
		return new ScanResult(token, end);
	}
	
	private ScanResult ScanIdentifier(int position)
	{
		var end = position;
		while (end < Source.Length && (char.IsLetterOrDigit(Source[end]) || Source[end] == '_'))
			end++;

		var range = new TextRange(position, end);
		var text = Source.GetText(range);
		var tokenType = TokenType.GetKeyword(text) ?? TokenType.Identifier;
		var token = new Token(tokenType, range, Source);
		return new ScanResult(token, end);
	}

	private ScanResult ScanNumber(int position)
	{
		// 123, 123.456, 123.456f, 123.456x, 123u, 8i64, 0xAF80, 0b10110011, 2e3, 2e-3, 3.14e+3f
		var end = position;

		if (position <= Source.Length - 2)
		{
			if (Source[position] == '0')
			{
				switch (Source[position + 1])
				{
					case 'x':
					case 'X':
						return ScanHexadecimal(position);
					case 'b':
					case 'B':
						return ScanBinary(position);
				}
			}
		}

		object? value = null;
		
		while (end < Source.Length && char.IsDigit(Source[end]))
		{
			end++;
		}

		if (end < Source.Length)
		{
			if (Source[end] == '.' && end + 1 < Source.Length && char.IsDigit(Source[end + 1]))
			{
				end++;
				while (end < Source.Length && char.IsDigit(Source[end]))
				{
					end++;
				}

				var valueString = Source.GetText(new TextRange(position, end));

				if (end < Source.Length)
				{
					switch (Source[end])
					{
						case 'e':
						case 'E':
							end++;
							if (end < Source.Length && Source[end] is '+' or '-')
								end++;

							while (end < Source.Length && char.IsDigit(Source[end]))
								end++;

							valueString = Source.GetText(new TextRange(position, end));
							if (end < Source.Length)
							{
								switch (Source[end])
								{
									case 'f':
									case 'F':
										end++;
										value = float.TryParse(valueString, NumberStyles.Float, null,
											out var floatEValue)
											? floatEValue
											: null;
										break;
									case 'd':
									case 'D':
										end++;
										value = double.TryParse(valueString, NumberStyles.Float, null,
											out var doubleEValue)
											? doubleEValue
											: null;
										break;
									case 'm':
									case 'M':
										end++;
										value = decimal.TryParse(valueString, NumberStyles.Float, null,
											out var decimalEValue)
											? decimalEValue
											: null;
										break;
									default:
										value = double.TryParse(valueString, NumberStyles.Float, null,
											out var defaultEValue)
											? defaultEValue
											: null;
										break;
								}
							}
							else
							{
								value = double.TryParse(valueString, NumberStyles.Float, null,
									out var doubleEValue)
									? doubleEValue
									: null;
							}
							
							break;
						case 'f':
						case 'F':
							end++;
							value = float.TryParse(valueString, out var floatValue) ? floatValue : null;
							break;
						case 'd':
						case 'D':
							end++;
							value = double.TryParse(valueString, out var doubleValue) ? doubleValue : null;
							break;
						case 'm':
						case 'M':
							end++;
							value = decimal.TryParse(valueString, out var decimalValue) ? decimalValue : null;
							break;
						case 'x':
						case 'X':
							end++;
							value = Fixed.TryParse(valueString, out var fixedValue) ? fixedValue : null;
							break;
						default:
							value = double.TryParse(valueString, out var defaultValue) ? defaultValue : null;
							break;
					}
				}
				else
				{
					value = double.TryParse(valueString, out var doubleValue) ? doubleValue : null;
				}
			}
			else
			{
				var valueString = Source.GetText(new TextRange(position, end));

				if (end < Source.Length)
				{
					switch (Source[end])
					{
						case 'u':
						case 'U':
							end++;
							switch (ScanStorageSize(end))
							{
								case 8:
									end++;
									value = byte.TryParse(valueString, out var byteValue) ? byteValue : null;
									break;
								case 16:
									end += 2;
									value = ushort.TryParse(valueString, out var ushortValue) ? ushortValue : null;
									break;
								case 32:
									end += 2;
									value = uint.TryParse(valueString, out var uintValue) ? uintValue : null;
									break;
								case 64:
									end += 2;
									value = ulong.TryParse(valueString, out var ulongValue) ? ulongValue : null;
									break;
								case 128:
									end += 3;
									value = UInt128.TryParse(valueString, out var uint128Value) ? uint128Value : null;
									break;
								case null:
									value = uint.TryParse(valueString, out var uDefaultValue) ? uDefaultValue : null;
									break;
							}
							break;
						case 'i':
						case 'I':
							end++;
							switch (ScanStorageSize(end))
							{
								case 8:
									end++;
									value = sbyte.TryParse(valueString, out var sbyteValue) ? sbyteValue : null;
									break;
								case 16:
									end += 2;
									value = short.TryParse(valueString, out var shortValue) ? shortValue : null;
									break;
								case 32:
									end += 2;
									value = int.TryParse(valueString, out var sIntValue) ? sIntValue : null;
									break;
								case 64:
									end += 2;
									value = long.TryParse(valueString, out var longValue) ? longValue : null;
									break;
								case 128:
									end += 3;
									value = Int128.TryParse(valueString, out var int128Value) ? int128Value : null;
									break;
								case null:
									value = int.TryParse(valueString, out var iDefaultValue) ? iDefaultValue : null;
									break;
							}
							break;
						case 'e':
						case 'E':
							end++;
							if (end < Source.Length && Source[end] is '+' or '-')
								end++;

							while (end < Source.Length && char.IsDigit(Source[end]))
								end++;

							valueString = Source.GetText(new TextRange(position, end));
							if (end < Source.Length)
							{
								switch (Source[end])
								{
									case 'f':
									case 'F':
										end++;
										value = float.TryParse(valueString, NumberStyles.Float, null,
											out var floatEValue)
											? floatEValue
											: null;
										break;
									case 'd':
									case 'D':
										end++;
										value = double.TryParse(valueString, NumberStyles.Float, null,
											out var doubleEValue)
											? doubleEValue
											: null;
										break;
									case 'm':
									case 'M':
										end++;
										value = decimal.TryParse(valueString, NumberStyles.Float, null,
											out var decimalEValue)
											? decimalEValue
											: null;
										break;
									default:
										value = double.TryParse(valueString, NumberStyles.Float, null,
											out var defaultEValue)
											? defaultEValue
											: null;
										break;
								}
							}
							else
							{
								value = double.TryParse(valueString, NumberStyles.Float, null,
									out var defaultEValue)
									? defaultEValue
									: null;
							}
							break;
						case 'f':
						case 'F':
							end++;
							value = float.TryParse(valueString, out var floatValue) ? floatValue : null;
							break;
						case 'd':
						case 'D':
							end++;
							value = double.TryParse(valueString, out var doubleValue) ? doubleValue : null;
							break;
						case 'm':
						case 'M':
							end++;
							value = decimal.TryParse(valueString, out var decimalValue) ? decimalValue : null;
							break;
						case 'x':
						case 'X':
							end++;
							value = Fixed.TryParse(valueString, out var fixedValue) ? fixedValue : null;
							break;
						default:
							if (int.TryParse(valueString, out var intValue))
							{
								value = intValue;
							}
							else if (long.TryParse(valueString, out var longValue))
							{
								value = longValue;
							}
							else if (Int128.TryParse(valueString, out var int128Value))
							{
								value = int128Value;
							}
							else
							{
								var invalidToken = new Token(TokenType.InvalidNumberLiteral,
									new TextRange(position, end), Source, value);
								return new ScanResult(invalidToken, end);
							}
							break;
					}
				}
				else
				{
					if (int.TryParse(valueString, out var intValue))
					{
						value = intValue;
					}
					else if (long.TryParse(valueString, out var longValue))
					{
						value = longValue;
					}
					else if (Int128.TryParse(valueString, out var int128Value))
					{
						value = int128Value;
					}
					else
					{
						var invalidToken = new Token(TokenType.InvalidNumberLiteral, new TextRange(position, end),
							Source, value);
						return new ScanResult(invalidToken, end);
					}
				}
			}
		}
		else
		{
			var valueString = Source.GetText(new TextRange(position, end));
			if (int.TryParse(valueString, out var intValue))
			{
				value = intValue;
			}
			else if (uint.TryParse(valueString, out var uintValue))
			{
				value = uintValue;
			}
			else if (long.TryParse(valueString, out var longValue))
			{
				value = longValue;
			}
			else if (ulong.TryParse(valueString, out var ulongValue))
			{
				value = ulongValue;
			}
			else if (Int128.TryParse(valueString, out var int128Value))
			{
				value = int128Value;
			}
			else if (UInt128.TryParse(valueString, out var uint128Value))
			{
				value = uint128Value;
			}
			else
			{
				var invalidToken = new Token(TokenType.InvalidNumberLiteral, new TextRange(position, end), Source,
					value);
				return new ScanResult(invalidToken, end);
			}
		}

		var token = new Token(TokenType.NumberLiteral, new TextRange(position, end), Source, value);
		return new ScanResult(token, end);
	}

	private ScanResult ScanHexadecimal(int position)
	{
		var end = position + 2;
		while (end < Source.Length && char.IsAsciiHexDigit(Source[end]))
		{
			end++;
		}

		var valueString = Source.GetText(new TextRange(position + 2, end));
		object? value = null;
		
		var scannedSuffix = false;
		if (end < Source.Length)
		{
			switch (Source[end])
			{
				case 'u':
				case 'U':
					end++;
					scannedSuffix = true;
					switch (ScanStorageSize(end))
					{
						case 8:
							end++;
							if (byte.TryParse(valueString, NumberStyles.AllowHexSpecifier, null, out var byteValue))
								value = byteValue;
							break;
						case 16:
							end += 2;
							if (ushort.TryParse(valueString, NumberStyles.AllowHexSpecifier, null, out var ushortValue))
								value = ushortValue;
							break;
						case 32:
							end += 2;
							if (uint.TryParse(valueString, NumberStyles.AllowHexSpecifier, null, out var uintValue))
								value = uintValue;
							break;
						case 64:
							end += 2;
							if (ulong.TryParse(valueString, NumberStyles.AllowHexSpecifier, null, out var ulongValue))
								value = ulongValue;
							break;
						case 128:
							end += 3;
							if (UInt128.TryParse(valueString, NumberStyles.AllowHexSpecifier, null,
								    out var uint128Value))
								value = uint128Value;
							break;
						case null:
							if (uint.TryParse(valueString, NumberStyles.AllowHexSpecifier, null, out var uDefaultValue))
								value = uDefaultValue;
							break;
					}

					break;
				case 'i':
				case 'I':
					end++;
					scannedSuffix = true;
					switch (ScanStorageSize(end))
					{
						case 8:
							end++;
							if (sbyte.TryParse(valueString, NumberStyles.AllowHexSpecifier, null, out var sbyteValue))
								value = sbyteValue;
							break;
						case 16:
							end += 2;
							if (short.TryParse(valueString, NumberStyles.AllowHexSpecifier, null, out var shortValue))
								value = shortValue;
							break;
						case 32:
							end += 2;
							if (int.TryParse(valueString, NumberStyles.AllowHexSpecifier, null, out var sIntValue))
								value = sIntValue;
							break;
						case 64:
							end += 2;
							if (long.TryParse(valueString, NumberStyles.AllowHexSpecifier, null, out var longValue))
								value = longValue;
							break;
						case 128:
							end += 3;
							if (Int128.TryParse(valueString, NumberStyles.AllowHexSpecifier, null,
								    out var int128Value))
								value = int128Value;
							break;
						case null:
							if (int.TryParse(valueString, NumberStyles.AllowHexSpecifier, null, out var iDefaultValue))
								value = iDefaultValue;
							break;
					}
					break;
			}
		}

		if (!scannedSuffix)
		{
			if (int.TryParse(valueString, NumberStyles.AllowHexSpecifier, null, out var intValue))
			{
				value = intValue;
			}
			else if (uint.TryParse(valueString, NumberStyles.AllowHexSpecifier, null, out var uintValue))
			{
				value = uintValue;
			}
			else if (long.TryParse(valueString, NumberStyles.AllowHexSpecifier, null, out var longValue))
			{
				value = longValue;
			}
			else if (ulong.TryParse(valueString, NumberStyles.AllowHexSpecifier, null, out var ulongValue))
			{
				value = ulongValue;
			}
			else if (Int128.TryParse(valueString, NumberStyles.AllowHexSpecifier, null, out var int128Value))
			{
				value = int128Value;
			}
			else if (UInt128.TryParse(valueString, NumberStyles.AllowHexSpecifier, null, out var uint128Value))
			{
				value = uint128Value;
			}
			else
			{
				var invalidToken = new Token(TokenType.InvalidNumberLiteral, new TextRange(position, end), Source,
					value);
				return new ScanResult(invalidToken, end);
			}
		}

		var token = new Token(TokenType.NumberLiteral, new TextRange(position, end), Source, value);
		return new ScanResult(token, end);
	}

	private ScanResult ScanBinary(int position)
	{
		var end = position + 2;
		while (end < Source.Length && Source[end] is '0' or '1')
		{
			end++;
		}

		var valueString = Source.GetText(new TextRange(position + 2, end));
		object? value = null;

		var scannedSuffix = false;
		if (end < Source.Length)
		{
			switch (Source[end])
			{
				case 'u':
				case 'U':
					end++;
					scannedSuffix = true;
					switch (ScanStorageSize(end))
					{
						case 8:
							end++;
							try
							{
								value = Convert.ToByte(valueString, 2);
							}
							catch (OverflowException)
							{
								var invalidToken = new Token(TokenType.InvalidNumberLiteral,
									new TextRange(position, end), Source, value);
								return new ScanResult(invalidToken, end);
							}
							break;
						case 16:
							end += 2;
							try
							{
								value = Convert.ToUInt16(valueString, 2);
							}
							catch (OverflowException)
							{
								var invalidToken = new Token(TokenType.InvalidNumberLiteral,
									new TextRange(position, end), Source, value);
								return new ScanResult(invalidToken, end);
							}
							break;
						case 32:
							end += 2;
							try
							{
								value = Convert.ToUInt32(valueString, 2);
							}
							catch (OverflowException)
							{
								var invalidToken = new Token(TokenType.InvalidNumberLiteral,
									new TextRange(position, end), Source, value);
								return new ScanResult(invalidToken, end);
							}
							break;
						case 64:
							end += 2;
							try
							{
								value = Convert.ToUInt64(valueString, 2);
							}
							catch (OverflowException)
							{
								var invalidToken = new Token(TokenType.InvalidNumberLiteral,
									new TextRange(position, end), Source, value);
								return new ScanResult(invalidToken, end);
							}
							break;
						case 128:
							end += 3;
							if (UInt128.TryParse(valueString, NumberStyles.AllowBinarySpecifier, null,
								    out var uint128Value))
							{
								value = uint128Value;
							}
							else
							{
								var invalidToken = new Token(TokenType.InvalidNumberLiteral,
									new TextRange(position, end), Source, value);
								return new ScanResult(invalidToken, end);
							}
							break;
						case null:
							try
							{
								value = Convert.ToUInt32(valueString, 2);
							}
							catch (OverflowException)
							{
								var invalidToken = new Token(TokenType.InvalidNumberLiteral,
									new TextRange(position, end), Source, value);
								return new ScanResult(invalidToken, end);
							}
							break;
					}

					break;
				case 'i':
				case 'I':
					end++;
					scannedSuffix = true;
					switch (ScanStorageSize(end))
					{
						case 8:
							end++;
							try
							{
								value = Convert.ToSByte(valueString, 2);
							}
							catch (OverflowException)
							{
								var invalidToken = new Token(TokenType.InvalidNumberLiteral,
									new TextRange(position, end), Source, value);
								return new ScanResult(invalidToken, end);
							}
							break;
						case 16:
							end += 2;
							try
							{
								value = Convert.ToInt16(valueString, 2);
							}
							catch (OverflowException)
							{
								var invalidToken = new Token(TokenType.InvalidNumberLiteral,
									new TextRange(position, end), Source, value);
								return new ScanResult(invalidToken, end);
							}
							break;
						case 32:
							end += 2;
							try
							{
								value = Convert.ToInt32(valueString, 2);
							}
							catch (OverflowException)
							{
								var invalidToken = new Token(TokenType.InvalidNumberLiteral,
									new TextRange(position, end), Source, value);
								return new ScanResult(invalidToken, end);
							}
							break;
						case 64:
							end += 2;
							try
							{
								value = Convert.ToInt64(valueString, 2);
							}
							catch (OverflowException)
							{
								var invalidToken = new Token(TokenType.InvalidNumberLiteral,
									new TextRange(position, end), Source, value);
								return new ScanResult(invalidToken, end);
							}
							break;
						case 128:
							end += 3;
							if (Int128.TryParse(valueString, NumberStyles.AllowBinarySpecifier, null,
								    out var int128Value))
							{
								value = int128Value;
							}
							else
							{
								var invalidToken = new Token(TokenType.InvalidNumberLiteral,
									new TextRange(position, end), Source, value);
								return new ScanResult(invalidToken, end);
							}
							break;
						case null:
							try
							{
								value = Convert.ToInt32(valueString, 2);
							}
							catch (OverflowException)
							{
								var invalidToken = new Token(TokenType.InvalidNumberLiteral,
									new TextRange(position, end), Source, value);
								return new ScanResult(invalidToken, end);
							}
							break;
					}
					break;
			}
		}

		if (!scannedSuffix)
		{
			try
			{
				value = Convert.ToInt32(valueString, 2);
			}
			catch (OverflowException)
			{
				try
				{
					value = Convert.ToUInt32(valueString, 2);
				}
				catch (OverflowException)
				{
					try
					{
						value = Convert.ToInt64(valueString, 2);
					}
					catch (OverflowException)
					{
						try
						{
							value = Convert.ToUInt64(valueString, 2);
						}
						catch (OverflowException)
						{
							if (Int128.TryParse(valueString, NumberStyles.AllowBinarySpecifier, null,
								    out var int128Value))
							{
								value = int128Value;
							}
							else
							{
								if (UInt128.TryParse(valueString, NumberStyles.AllowBinarySpecifier, null,
									    out var uint128Value))
								{
									value = uint128Value;
								}
								else
								{
									var invalidToken = new Token(TokenType.InvalidNumberLiteral,
										new TextRange(position, end), Source, value);
									return new ScanResult(invalidToken, end);
								}
							}
						}
					}
				}
			}
		}

		var token = new Token(TokenType.NumberLiteral, new TextRange(position, end), Source, value);
		return new ScanResult(token, end);
	}

	public IEnumerator<Token> GetEnumerator()
	{
		return ScanAllTokens().GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}