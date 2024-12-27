using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace FixedPointMath;

public readonly partial struct Fixed32(int rawValue) : IFixedPoint<Fixed32>
{
	[StructLayout(LayoutKind.Explicit)]
	private readonly struct ToUnsigned(int sourceValue)
	{
		[FieldOffset(0)] public readonly int sourceValue = sourceValue;
		[FieldOffset(0)] public readonly uint castedValue;
	}
	
	[StructLayout(LayoutKind.Explicit)]
	private readonly struct ToSigned(uint sourceValue)
	{
		[FieldOffset(0)] public readonly uint sourceValue = sourceValue;
		[FieldOffset(0)] public readonly int castedValue;
	}
	
	public static readonly Fixed32 Epsilon = new(1);
	public static readonly Fixed32 MaxValue = new(int.MaxValue);
	public static readonly Fixed32 MinValue = new(int.MinValue);
	public static Fixed32 Zero => new(0);
	public static Fixed32 One => new(RawOne);
	public static readonly Fixed32 NegativeOne = new(RawNegativeOne);
	public static readonly Fixed32 Half = new(RawHalf);
	public static readonly Fixed32 E = new(RawE);
	public static readonly Fixed32 Pi = new(RawPi);
	public static readonly Fixed32 PiOver2 = new(RawPiOver2);
	public static readonly Fixed32 Tau = new(RawTau);
	public static readonly Fixed32 Ln2 = new(RawLn2);

	private static readonly Fixed32 Log2Max = (Fixed32)(BitCount - DecimalPlaces - 1);
	private static readonly Fixed32 Log2Min = (Fixed32)(DecimalPlaces - BitCount);
	private static readonly Fixed32 DegToRadConstant = Pi / (Fixed32)180;
	private static readonly Fixed32 RadToDegConstant = (Fixed32)180 / Pi;

	private const int BitCount = 32;
	internal const int DecimalPlaces = 16;
	private const int RawOne = 1 << DecimalPlaces;
	private const int RawHalf = 0x8000;
	private const int RawNegativeOne = -(1 << DecimalPlaces);
	private const int RawPi = 0x3_243F;
	private const int RawE = 0x2_B7E1;
	private const int RawPiOver2 = 0x1_921F;
	private const int RawTau = 0x6_487E;
	private const int RawLn2 = 0x0_B172;

	public int RawValue { get; } = rawValue;
	public uint Bits => new ToUnsigned(RawValue).castedValue;
	
	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespaceRegexImpl();
	private static readonly Regex WhitespaceRegex = WhitespaceRegexImpl();

	public static Fixed32 Parse(string number)
	{
		number = WhitespaceRegex.Replace(number, "");
		var groups = number.Split('.');

		if (groups.Length > 2)
			throw new ArgumentException("Cannot have more than one decimal point");

		if (!short.TryParse(groups[0], out var intPart))
			throw new ArgumentException("Failed to parse integer part");

		if (groups.Length < 2)
			return new Fixed32(intPart << DecimalPlaces);

		var decimalString = groups[1];
		return From(intPart, decimalString);
	}

	public static bool TryParse(string number, out Fixed32 result)
	{
		try
		{
			result = Parse(number);
			return true;
		}
		catch
		{
			result = Zero;
			return false;
		}
	}

	private static Fixed32 From(short intPart, string decimalPart)
	{
		var result = new Fixed32(intPart << DecimalPlaces);

		if (string.IsNullOrWhiteSpace(decimalPart))
			return result;

		var decimalValue = Zero;
		var place = One / (Fixed32)10;

		foreach (var c in decimalPart)
		{
			var digit = c - '0';
			decimalValue += (Fixed32)digit * place;
			place /= (Fixed32)10;

			if (IsZero(place))
				break;
		}

		if (intPart < 0)
			decimalValue = -decimalValue;
		
		result += decimalValue;
		
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsZero(Fixed32 value)
	{
		return value.RawValue == 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsZero()
	{
		return IsZero(this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsNegative(Fixed32 value)
	{
		return value.RawValue < 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsNegative()
	{
		return IsNegative(this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsOddInteger(Fixed32 value)
	{
		return IsInteger(value) && ((value.RawValue >> DecimalPlaces) & 1) == 1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsEvenInteger(Fixed32 value)
	{
		return IsInteger(value) && ((value.RawValue >> DecimalPlaces) & 1) == 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsPositive(Fixed32 value)
	{
		return value.RawValue > 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsPositive()
	{
		return IsPositive(this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsInteger(Fixed32 value)
	{
		return Fract(value) == Zero;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsInteger()
	{
		return IsInteger(this);
	}

	public static Fixed32 DegToRad(Fixed32 value)
	{
		return value * DegToRadConstant;
	}

	public static Fixed32 RadToDeg(Fixed32 value)
	{
		return value * RadToDegConstant;
	}

	public static Fixed32 Lerp(Fixed32 a, Fixed32 b, Fixed32 t)
	{
		return a + t * (b - a);
	}

	public static int Sign(Fixed32 value)
	{
		return
			value.RawValue < 0 ? -1 :
			value.RawValue > 0 ? 1 :
			0;
	}

	public static Fixed32 Abs(Fixed32 value)
	{
		if (value == MinValue)
			return MaxValue;

		var mask = value.RawValue >> (BitCount - 1);
		return new Fixed32((value.RawValue + mask) ^ mask);
	}

	public static Fixed32 Floor(Fixed32 value)
	{
		var rawValue = new ToUnsigned(value.RawValue).castedValue;
		var flooredValue = new ToSigned(rawValue & 0xFFFF_0000u).castedValue;
		return new Fixed32(flooredValue);
	}

	public static Fixed32 Ceil(Fixed32 value)
	{
		var hasDecimalPart = (value.RawValue & 0x0000_FFFF) != 0L;
		return hasDecimalPart ? Floor(value) + One : value;
	}

	public static Fixed32 Fract(Fixed32 value)
	{
		return new Fixed32(value.RawValue & 0x0000_FFFF);
	}

	public static Fixed32 Round(Fixed32 value)
	{
		var decimalPart = value.RawValue & 0x0000_FFFF;
		var integerPart = Floor(value);

		if (decimalPart < 0x8000)
			return integerPart;
		
		if (decimalPart > 0x8000)
			return integerPart + One;

		return (integerPart.RawValue & RawOne) == 0
			? integerPart
			: integerPart + One;
	}

	public static Fixed32 Clamp(Fixed32 value, Fixed32 min, Fixed32 max)
	{
		if (value < min)
			return min;

		if (value > max)
			return max;

		return value;
	}

	public static Fixed32 PosMod(Fixed32 a, Fixed32 b)
	{
		var result = a % b;
		
		if (a.RawValue < 0 && b.RawValue > 0 || result.RawValue > 0 && b.RawValue < 0)
			result += b;

		return result;
	}

	public static (Fixed32, Fixed32) SinCos(Fixed32 angle)
	{
		return (Sin(angle), Cos(angle));
	}

	public static Fixed32 Snapped(Fixed32 value, Fixed32 step)
	{
		return step.RawValue != 0 ? Floor(value / step + Half) * step : value;
	}

	public static Fixed32 operator +(Fixed32 left, Fixed32 right)
	{
		var leftRaw = left.RawValue;
		var rightRaw = right.RawValue;
		var sum = leftRaw + rightRaw;

		if ((~(leftRaw ^ rightRaw) & (leftRaw ^ sum) & int.MinValue) != 0)
			sum = leftRaw > 0 ? int.MaxValue : int.MinValue;

		return new Fixed32(sum);
	}

	public static Fixed32 operator -(Fixed32 left, Fixed32 right)
	{
		var leftRaw = left.RawValue;
		var rightRaw = right.RawValue;
		var difference = leftRaw - rightRaw;

		if (((leftRaw ^ rightRaw) & (leftRaw ^ difference) & int.MinValue) != 0)
			difference = leftRaw < 0 ? int.MinValue : int.MaxValue;

		return new Fixed32(difference);
	}

	private static int AddOverflow(int left, int right, ref bool overflow)
	{
		var sum = left + right;
		overflow |= ((left ^ right ^ sum) & int.MinValue) != 0;
		return sum;
	}

	public static Fixed32 operator *(Fixed32 left, Fixed32 right)
	{
		if (left == One)
			return right;

		if (right == One)
			return left;

		if (left == NegativeOne)
			return -right;

		if (right == NegativeOne)
			return -left;
		
		var leftRaw = left.RawValue;
		var rightRaw = right.RawValue;

		if (leftRaw == 0 || rightRaw == 0)
			return Zero;

		var leftLow = leftRaw & 0x0000_FFFF;
		var leftHigh = leftRaw >> DecimalPlaces;
		var rightLow = rightRaw & 0x0000_FFFF;
		var rightHigh = rightRaw >> DecimalPlaces;

		var leftLowCast = new ToUnsigned(leftLow);
		var rightLowCast = new ToUnsigned(rightLow);
		
		var lowLow = leftLowCast.castedValue * rightLowCast.castedValue;
		var lowHigh = leftLow * rightHigh;
		var highLow = leftHigh * rightLow;
		var highHigh = leftHigh * rightHigh;

		var lowResult = new ToSigned(lowLow >> DecimalPlaces).castedValue;
		var highResult = highHigh << DecimalPlaces;

		var overflow = false;
		var sum = AddOverflow(lowResult, lowHigh, ref overflow);
		sum = AddOverflow(sum, highLow, ref overflow);
		sum = AddOverflow(sum, highResult, ref overflow);

		var opSignsEqual = ((leftRaw ^ rightRaw) & int.MinValue) == 0;

		if (opSignsEqual)
		{
			if (sum < 0 || (overflow && leftRaw > 0))
				return MaxValue;
		}
		else
		{
			if (sum > 0)
				return MinValue;
		}

		var topCarry = highHigh >> DecimalPlaces;
		if (topCarry != 0 && topCarry != -1)
			return opSignsEqual ? MaxValue : MinValue;

		if (opSignsEqual)
			return new Fixed32(sum);
		
		int posOp, negOp;
		if (leftRaw > rightRaw)
		{
			posOp = leftRaw;
			negOp = rightRaw;
		}
		else
		{
			posOp = rightRaw;
			negOp = leftRaw;
		}

		if (sum > negOp && negOp < -RawOne && posOp > RawOne)
			return MinValue;

		return new Fixed32(sum);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int CountLeadingZeroes(uint value)
	{
		var result = 0;
		
		while ((value & 0xF000_0000u) == 0)
		{
			result += 4;
			value <<= 4;
		}
		
		while ((value & 0x8000_0000u) == 0)
		{
			result += 1;
			value <<= 1;
		}

		return result;
	}

	public static Fixed32 operator /(Fixed32 left, Fixed32 right)
	{
		var leftRaw = left.RawValue;
		var rightRaw = right.RawValue;

		if (rightRaw == 0L)
			throw new DivideByZeroException();

		if (right == (Fixed32)2)
			return new Fixed32(leftRaw >> 1);

		var remainder = new ToUnsigned(leftRaw >= 0 ? leftRaw : -leftRaw).castedValue;
		var divisor = new ToUnsigned(rightRaw >= 0 ? rightRaw : -rightRaw).castedValue;
		var quotient = 0u;
		var bitPos = BitCount / 2 + 1;

		while ((divisor & 0xFu) == 0u && bitPos >= 4)
		{
			divisor >>= 4;
			bitPos -= 4;
		}

		while (remainder != 0 && bitPos >= 0)
		{
			var shift = CountLeadingZeroes(remainder);
			if (shift > bitPos)
				shift = bitPos;

			remainder <<= shift;
			bitPos -= shift;

			var division = remainder / divisor;
			remainder %= divisor;
			quotient += division << bitPos;

			if ((division & ~(uint.MaxValue >> bitPos)) != 0u)
				return ((leftRaw ^ rightRaw) & int.MinValue) == 0 ? MaxValue : MinValue;

			remainder <<= 1;
			bitPos--;
		}

		quotient++;
		var result = (int)(quotient >> 1);
		if (((leftRaw ^ rightRaw) & int.MinValue) != 0)
			result = -result;

		return new Fixed32(result);
	}

	public static Fixed32 operator %(Fixed32 left, Fixed32 right)
	{
		return new Fixed32(left.RawValue == int.MinValue & right.RawValue == -1 ? 0 : left.RawValue % right.RawValue);
	}

	private static Fixed32 Pow2(Fixed32 exponent)
	{
		if (exponent.RawValue == 0)
			return One;

		var negative = exponent.RawValue < 0;
		if (negative)
			exponent = -exponent;

		if (exponent == One)
			return negative ? Half : (Fixed32)2;

		if (exponent >= Log2Max)
			return negative ? One / MaxValue : MaxValue;

		if (exponent <= Log2Min)
			return negative ? MaxValue : Zero;

		var integerPart = (int)Floor(exponent);
		exponent = Fract(exponent);

		var result = One;
		var term = One;
		var i = 1;

		while (term.RawValue != 0)
		{
			term = exponent * term * Ln2 / (Fixed32)i;
			result += term;
			i++;
		}

		result = new Fixed32(result.RawValue << integerPart);
		if (negative)
			result = One / result;

		return result;
	}

	public static Fixed32 Log2(Fixed32 value)
	{
		if (!IsPositive(value))
			throw new ArgumentOutOfRangeException(nameof(value));

        var b = 1 << (DecimalPlaces - 1);
		var y = 0;

		var rawValue = value.RawValue;
		while (rawValue < RawOne)
		{
			rawValue <<= 1;
			y -= RawOne;
		}

		while (rawValue >= RawOne << 1)
		{
			rawValue >>= 1;
			y += RawOne;
		}

		var z = new Fixed32(rawValue);

		for (var i = 0; i < DecimalPlaces; i++)
		{
			z *= z;
			if (z.RawValue >= RawOne << 1)
			{
				z = new Fixed32(z.RawValue >> 1);
				y += b;
			}

			b >>= 1;
		}

		return new Fixed32(y);
	}

	public static Fixed32 Ln(Fixed32 value)
	{
		return Log2(value) * Ln2;
	}

	public static Fixed32 Pow(Fixed32 @base, Fixed32 exponent)
	{
		if (@base < Zero)
		{
			// Todo: Handle properly
			if (!exponent.IsInteger())
				return Zero;

			var pow = Pow(-@base, exponent);
			if (exponent % (Fixed32)2 == Zero)
				return pow;

			return -pow;
		}
		
		if (@base == One)
			return One;

		if (exponent == Zero)
			return One;

		if (@base == Zero)
		{
			if (exponent < Zero)
				throw new DivideByZeroException();

			return Zero;
		}

		var log2 = Log2(@base);
		return Pow2(exponent * log2);
	}

	public static Fixed32 Sqrt(Fixed32 value)
	{
		var rawValue = value.RawValue;
		if (rawValue < 0)
			throw new ArgumentOutOfRangeException(nameof(value));

		var number = new ToUnsigned(rawValue).castedValue;
		var result = 0u;
		var bit = 1u << (BitCount - 2);

		while (bit > number)
		{
			bit >>= 2;
		}

		for (var i = 0; i < 2; i++)
		{
			while (bit != 0u)
			{
				if (number >= result + bit)
				{
					number -= result + bit;
					result = (result >> 1) + bit;
				}
				else
				{
					result >>= 1;
				}

				bit >>= 2;
			}

			if (i != 0)
				continue;
			
			if (number > (1u << (BitCount / 2)) - 1)
			{
				number -= result;
				number = (number << (BitCount / 2)) - 0x8000u;
				result = (result << (BitCount / 2)) + 0x8000u;
			}
			else
			{
				number <<= BitCount / 2;
				result <<= BitCount / 2;
			}

			bit = 1u << (BitCount / 2 - 2);
		}

		if (number > result)
			result++;

		return new Fixed32(new ToSigned(result).castedValue);
	}

	public static Fixed32 Wrap(Fixed32 value, Fixed32 minimum, Fixed32 maximum)
	{
		while (value < minimum)
			value += maximum - minimum;

		while (value >= maximum)
			value -= maximum - minimum;

		return value;
	}

	public static Fixed32 Sin(Fixed32 value)
	{
		return Cos(value - PiOver2);
	}

	public static Fixed32 Cos(Fixed32 value)
	{
		// 3 terms of taylor series
		value = Wrap(value, -Pi, Pi);
		return One
		       - Pow(value, (Fixed32)2) / (Fixed32)2
		       + Pow(value, (Fixed32)4) / (Fixed32)24
		       - Pow(value, (Fixed32)6) / (Fixed32)720;
	}

	public static Fixed32 Tan(Fixed32 value)
	{
		return Sin(value) / Cos(value);
	}

	public static Fixed32 Acos(Fixed32 value)
	{
		if (value < -One || value > One)
			throw new ArgumentOutOfRangeException(nameof(value));

		if (IsZero(value))
			return PiOver2;

		var result = Atan(Sqrt(One - value * value) / value);
		return value.RawValue < 0 ? result + Pi : result;
	}

	public static Fixed32 Atan(Fixed32 value)
	{
		if (IsZero(value))
			return Zero;

		var negative = value.RawValue < 0;
		if (negative)
			value = -value;

		var invert = value > One;
		if (invert)
			value = One / value;

		var result = One;
		var term = One;

		var squared = value * value;
		var squared2 = squared * (Fixed32)2;
		var squaredPlusOne = squared + One;
		var squaredPlusOne2 = squaredPlusOne * (Fixed32)2;
		var dividend = squared2;
		var divisor = squaredPlusOne * (Fixed32)3;

		for (var i = 2; i < 30; i++)
		{
			term *= dividend / divisor;
			result += term;

			dividend += squared2;
			divisor += squaredPlusOne2;

			if (IsZero(term))
				break;
		}

		result = result * value / squaredPlusOne;

		if (invert)
			result = PiOver2 - result;

		if (negative)
			result = -result;

		return result;
	}

	public static Fixed32 Atan2(Fixed32 y, Fixed32 x)
	{
		var rawY = y.RawValue;
		var rawX = x.RawValue;

		if (rawX == 0L)
		{
			return rawY switch
			{
				> 0 => PiOver2,
				< 0 => -PiOver2,
				_ => Zero
			};
		}
		
		const int rawPointTwoEight = 0x0_47AE;
		var pointTwoEight = new Fixed32(rawPointTwoEight);

		Fixed32 atan;
		var z = y / x;
		var zSquared = z * z;

		if (One + pointTwoEight * zSquared == MaxValue)
			return y < Zero ? -PiOver2 : PiOver2;

		if (Abs(z) < One)
		{
			atan = z / (One + pointTwoEight * zSquared);

			if (rawX >= 0L)
				return atan;
			
			if (rawY < 0L)
				return atan - Pi;

			return atan + Pi;
		}

		atan = PiOver2 - z / (zSquared + pointTwoEight);

		if (rawY < 0L)
			return atan - Pi;

		return atan;
	}

	public static Fixed32 operator -(Fixed32 operand)
	{
		return operand.RawValue == int.MinValue ? MaxValue : new Fixed32(-operand.RawValue);
	}

	public static bool operator ==(Fixed32 left, Fixed32 right)
	{
		return left.RawValue == right.RawValue;
	}

	public static bool operator !=(Fixed32 left, Fixed32 right)
	{
		return left.RawValue != right.RawValue;
	}

	public static bool operator >(Fixed32 left, Fixed32 right)
	{
		return left.RawValue > right.RawValue;
	}

	public static bool operator <(Fixed32 left, Fixed32 right)
	{
		return left.RawValue < right.RawValue;
	}

	public static bool operator >=(Fixed32 left, Fixed32 right)
	{
		return left.RawValue >= right.RawValue;
	}

	public static bool operator <=(Fixed32 left, Fixed32 right)
	{
		return left.RawValue <= right.RawValue;
	}

	public static explicit operator Fixed32(int value)
	{
		return new Fixed32(value * RawOne);
	}

	public static explicit operator int(Fixed32 value)
	{
		return value.RawValue >> DecimalPlaces;
	}
	
	public static explicit operator Fixed32(float value)
	{
		return new Fixed32((int)(value * RawOne));
	}

	public static explicit operator float(Fixed32 value)
	{
		return (float)value.RawValue / RawOne;
	}
	
	public static explicit operator Fixed32(double value)
	{
		return new Fixed32((int)(value * RawOne));
	}

	public static explicit operator double(Fixed32 value)
	{
		return (double)value.RawValue / RawOne;
	}
	
	public static explicit operator Fixed32(decimal value)
	{
		return new Fixed32((int)(value * RawOne));
	}

	public static explicit operator decimal(Fixed32 value)
	{
		return (decimal)value.RawValue / RawOne;
	}

	public static explicit operator Fixed32(Fixed64 value)
	{
		const int shiftSize = 16;
		var rawValue = new ToSigned((uint)(value.Bits >> shiftSize)).castedValue;
		return new Fixed32(rawValue);
	}

	public static explicit operator Fixed32(Fixed128 value)
	{
		const int shiftSize = 48;
		var rawValue = new ToSigned((uint)(value.Bits >> shiftSize)).castedValue;
		return new Fixed32(rawValue);
	}

	public override bool Equals(object? obj)
	{
		return obj is Fixed32 fixedValue && Equals(fixedValue);
	}

	public override int GetHashCode()
	{
		return RawValue.GetHashCode();
	}

	public bool Equals(Fixed32 other)
	{
		return RawValue.Equals(other.RawValue);
	}

	public int CompareTo(Fixed32 other)
	{
		return RawValue.CompareTo(other.RawValue);
	}

	public override string ToString()
	{
		const int decimalsToRender = 10;
		var result = new StringBuilder();

		if (IsZero())
			return "0";

		if (IsPositive())
		{
			var intPart = (short)(RawValue >> DecimalPlaces);
			result.Append(intPart);

			var intermediate = Fract(this);
			if (intermediate.IsZero())
			{
				return result.ToString();
			}

			result.Append('.');
			var ten = (Fixed32)10;
			for (var i = 0; i < decimalsToRender; i++)
			{
				intermediate *= ten;
				var digit = intermediate.RawValue >> DecimalPlaces;

				intermediate = Fract(intermediate);
				result.Append(digit);
			}

			return result.ToString().TrimEnd('0');
		}
		else
		{
			var intPart = (short)(RawValue >> DecimalPlaces);

			var intermediate = Fract(this);
			if (intermediate.IsZero())
			{
				result.Append(intPart);
				return result.ToString();
			}

			intermediate = One - intermediate;

			result.Append(intPart + 1);
			result.Append('.');
			var ten = (Fixed32)10;
			for (var i = 0; i < decimalsToRender; i++)
			{
				intermediate *= ten;
				var digit = intermediate.RawValue >> DecimalPlaces;

				intermediate = Fract(intermediate);
				result.Append(digit);
			}

			return result.ToString().TrimEnd('0');
		}
	}
}
