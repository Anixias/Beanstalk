using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace FixedPointMath;

public readonly partial struct Fixed(long rawValue) : IFixedPoint<Fixed>
{
	[StructLayout(LayoutKind.Explicit)]
	private readonly struct ToUnsigned(long sourceValue)
	{
		[FieldOffset(0)] public readonly long sourceValue = sourceValue;
		[FieldOffset(0)] public readonly ulong castedValue;
	}
	
	[StructLayout(LayoutKind.Explicit)]
	private readonly struct ToSigned(ulong sourceValue)
	{
		[FieldOffset(0)] public readonly ulong sourceValue = sourceValue;
		[FieldOffset(0)] public readonly long castedValue;
	}
	
	public static readonly Fixed Epsilon = new(1L);
	public static readonly Fixed MaxValue = new(long.MaxValue);
	public static readonly Fixed MinValue = new(long.MinValue);
	public static Fixed Zero => new(0L);
	public static Fixed One => new(RawOne);
	public static readonly Fixed NegativeOne = new(RawNegativeOne);
	public static readonly Fixed Half = new(RawHalf);
	public static readonly Fixed Pi = new(RawPi);
	public static readonly Fixed PiOver2 = new(RawPiOver2);
	public static readonly Fixed Tau = new(RawTau);
	public static readonly Fixed Ln2 = new(RawLn2);
	public static readonly Fixed E = new(RawE);

	private static readonly Fixed Log2Max = BitCount - DecimalPlaces - 1;
	private static readonly Fixed Log2Min = DecimalPlaces - BitCount;
	private static readonly Fixed DegToRadConstant = Pi / 180;
	private static readonly Fixed RadToDegConstant = 180 / Pi;

	private const int BitCount = 64;
	internal const int DecimalPlaces = 32;
	private const long RawOne = 1L << DecimalPlaces;
	private const long RawHalf = 0x0_80000000L;
	private const long RawNegativeOne = -(1L << DecimalPlaces);
	private const long RawPi = 0x3_243F6A88L;
	private const long RawPiOver2 = 0x1_921FB544L;
	private const long RawTau = 0x6_487ED511L;
	private const long RawLn2 = 0x0_B17217F7L;
	private const long RawE = 0x2_B7E15163L;

	public long RawValue { get; } = rawValue;
	public ulong Bits => new ToUnsigned(RawValue).castedValue;

	public Fixed(int value) : this(value * RawOne)
	{
	}
	
	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespaceRegexImpl();
	private static readonly Regex WhitespaceRegex = WhitespaceRegexImpl();

	public static Fixed Parse(string number)
	{
		number = WhitespaceRegex.Replace(number, "");
		var groups = number.Split('.');

		if (groups.Length > 2)
			throw new ArgumentException("Cannot have more than one decimal point");

		if (!int.TryParse(groups[0], out var intPart))
			throw new ArgumentException("Failed to parse integer part");

		if (groups.Length < 2)
			return new Fixed((long)intPart << DecimalPlaces);

		var decimalString = groups[1];
		return From(intPart, decimalString);
	}

	public static bool TryParse(string number, out Fixed result)
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

	private static Fixed From(int intPart, string decimalPart)
	{
		var result = new Fixed((long)intPart << DecimalPlaces);

		if (string.IsNullOrWhiteSpace(decimalPart))
			return result;

		var decimalValue = Zero;
		var place = One / 10;

		foreach (var c in decimalPart)
		{
			var digit = c - '0';
			decimalValue += digit * place;
			place /= 10;

			if (IsZero(place))
				break;
		}

		if (intPart < 0)
			decimalValue = -decimalValue;
		
		result += decimalValue;
		
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsZero(Fixed value)
	{
		return value.RawValue == 0L;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsZero()
	{
		return IsZero(this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsNegative(Fixed value)
	{
		return value.RawValue < 0L;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsNegative()
	{
		return IsNegative(this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsOddInteger(Fixed value)
	{
		return IsInteger(value) && ((value.RawValue >> DecimalPlaces) & 1) == 1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsEvenInteger(Fixed value)
	{
		return IsInteger(value) && ((value.RawValue >> DecimalPlaces) & 1) == 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsPositive(Fixed value)
	{
		return value.RawValue > 0L;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsPositive()
	{
		return IsPositive(this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsInteger(Fixed value)
	{
		return Fract(value) == Zero;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsInteger()
	{
		return IsInteger(this);
	}

	public static Fixed DegToRad(Fixed value)
	{
		return value * DegToRadConstant;
	}

	public static Fixed RadToDeg(Fixed value)
	{
		return value * RadToDegConstant;
	}

	public static Fixed Lerp(Fixed a, Fixed b, Fixed t)
	{
		return a + t * (b - a);
	}

	public static int Sign(Fixed value)
	{
		return
			value.RawValue < 0 ? -1 :
			value.RawValue > 0 ? 1 :
			0;
	}

	public static Fixed Abs(Fixed value)
	{
		if (value == MinValue)
			return MaxValue;

		var mask = value.RawValue >> (BitCount - 1);
		return new Fixed((value.RawValue + mask) ^ mask);
	}

	public static Fixed Floor(Fixed value)
	{
		var rawValue = new ToUnsigned(value.RawValue).castedValue;
		var flooredValue = new ToSigned(rawValue & 0xFFFF_FFFF_0000_0000uL).castedValue;
		return new Fixed(flooredValue);
	}

	public static Fixed Ceil(Fixed value)
	{
		var hasDecimalPart = (value.RawValue & 0x0000_0000_FFFF_FFFFL) != 0L;
		return hasDecimalPart ? Floor(value) + One : value;
	}

	public static Fixed Fract(Fixed value)
	{
		return new Fixed(value.RawValue & 0x0000_0000_FFFF_FFFFL);
	}

	public static Fixed Round(Fixed value)
	{
		var decimalPart = value.RawValue & 0x0000_0000_FFFF_FFFFL;
		var integerPart = Floor(value);

		if (decimalPart < 0x8000_0000L)
			return integerPart;
		
		if (decimalPart > 0x8000_0000L)
			return integerPart + One;

		return (integerPart.RawValue & RawOne) == 0L
			? integerPart
			: integerPart + One;
	}

	public static Fixed Clamp(Fixed value, Fixed min, Fixed max)
	{
		if (value < min)
			return min;

		if (value > max)
			return max;

		return value;
	}

	public static Fixed PosMod(Fixed a, Fixed b)
	{
		var result = a % b;
		
		if (a.RawValue < 0L && b.RawValue > 0L || result.RawValue > 0L && b.RawValue < 0L)
			result += b;

		return result;
	}

	public static (Fixed, Fixed) SinCos(Fixed angle)
	{
		return (Sin(angle), Cos(angle));
	}

	public static Fixed Snapped(Fixed value, Fixed step)
	{
		return step.RawValue != 0L ? Floor(value / step + Half) * step : value;
	}

	public static Fixed operator +(Fixed left, Fixed right)
	{
		var leftRaw = left.RawValue;
		var rightRaw = right.RawValue;
		var sum = leftRaw + rightRaw;

		if ((~(leftRaw ^ rightRaw) & (leftRaw ^ sum) & long.MinValue) != 0L)
			sum = leftRaw > 0L ? long.MaxValue : long.MinValue;

		return new Fixed(sum);
	}

	public static Fixed operator -(Fixed left, Fixed right)
	{
		var leftRaw = left.RawValue;
		var rightRaw = right.RawValue;
		var difference = leftRaw - rightRaw;

		if (((leftRaw ^ rightRaw) & (leftRaw ^ difference) & long.MinValue) != 0L)
			difference = leftRaw < 0L ? long.MinValue : long.MaxValue;

		return new Fixed(difference);
	}

	private static long AddOverflow(long left, long right, ref bool overflow)
	{
		var sum = left + right;
		overflow |= ((left ^ right ^ sum) & long.MinValue) != 0L;
		return sum;
	}

	public static Fixed operator *(Fixed left, Fixed right)
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

		if (leftRaw == 0L || rightRaw == 0L)
			return Zero;

		var leftLow = leftRaw & 0x0000_0000_FFFF_FFFFL;
		var leftHigh = leftRaw >> DecimalPlaces;
		var rightLow = rightRaw & 0x0000_0000_FFFF_FFFFL;
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

		var opSignsEqual = ((leftRaw ^ rightRaw) & long.MinValue) == 0L;

		if (opSignsEqual)
		{
			if (sum < 0L || (overflow && leftRaw > 0L))
				return MaxValue;
		}
		else
		{
			if (sum > 0L)
				return MinValue;
		}

		var topCarry = highHigh >> DecimalPlaces;
		if (topCarry != 0L && topCarry != -1)
			return opSignsEqual ? MaxValue : MinValue;

		if (opSignsEqual)
			return new Fixed(sum);
		
		long posOp, negOp;
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

		return new Fixed(sum);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int CountLeadingZeroes(ulong value)
	{
		var result = 0;
		
		while ((value & 0xF000_0000_0000_0000uL) == 0)
		{
			result += 4;
			value <<= 4;
		}
		
		while ((value & 0x8000_0000_0000_0000uL) == 0)
		{
			result += 1;
			value <<= 1;
		}

		return result;
	}

	public static Fixed operator /(Fixed left, Fixed right)
	{
		var leftRaw = left.RawValue;
		var rightRaw = right.RawValue;

		if (rightRaw == 0L)
			throw new DivideByZeroException();

		if (right == 2)
			return new Fixed(leftRaw >> 1);

		var remainder = new ToUnsigned(leftRaw >= 0L ? leftRaw : -leftRaw).castedValue;
		var divisor = new ToUnsigned(rightRaw >= 0L ? rightRaw : -rightRaw).castedValue;
		var quotient = 0uL;
		var bitPos = BitCount / 2 + 1;

		while ((divisor & 0xFuL) == 0uL && bitPos >= 4)
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

			if ((division & ~(ulong.MaxValue >> bitPos)) != 0uL)
				return ((leftRaw ^ rightRaw) & long.MinValue) == 0L ? MaxValue : MinValue;

			remainder <<= 1;
			bitPos--;
		}

		quotient++;
		var result = (long)(quotient >> 1);
		if (((leftRaw ^ rightRaw) & long.MinValue) != 0L)
			result = -result;

		return new Fixed(result);
	}

	public static Fixed operator %(Fixed left, Fixed right)
	{
		return new Fixed(left.RawValue == long.MinValue & right.RawValue == -1L ? 0L : left.RawValue % right.RawValue);
	}

	private static Fixed Pow2(Fixed exponent)
	{
		if (exponent.RawValue == 0L)
			return One;

		var negative = exponent.RawValue < 0L;
		if (negative)
			exponent = -exponent;

		if (exponent == One)
			return negative ? Half : 2;

		if (exponent >= Log2Max)
			return negative ? One / MaxValue : MaxValue;

		if (exponent <= Log2Min)
			return negative ? MaxValue : Zero;

		var integerPart = (int)Floor(exponent);
		exponent = Fract(exponent);

		var result = One;
		var term = One;
		var i = 1;

		while (term.RawValue != 0L)
		{
			term = exponent * term * Ln2 / i;
			result += term;
			i++;
		}

		result = new Fixed(result.RawValue << integerPart);
		if (negative)
			result = One / result;

		return result;
	}

	public static Fixed Log2(Fixed value)
	{
		if (!IsPositive(value))
			throw new ArgumentOutOfRangeException(nameof(value));

		var b = 1L << (DecimalPlaces - 1);
		var y = 0L;

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

		var z = new Fixed(rawValue);

		for (var i = 0; i < DecimalPlaces; i++)
		{
			z *= z;
			if (z.RawValue >= RawOne << 1)
			{
				z = new Fixed(z.RawValue >> 1);
				y += b;
			}

			b >>= 1;
		}

		return new Fixed(y);
	}

	public static Fixed Ln(Fixed value)
	{
		return Log2(value) * Ln2;
	}

	public static Fixed Pow(Fixed @base, Fixed exponent)
	{
		if (@base < Zero)
		{
			// Todo: Handle properly
			if (!exponent.IsInteger())
				return Zero;

			var pow = Pow(-@base, exponent);
			if (exponent % 2 == 0)
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

	public static Fixed Sqrt(Fixed value)
	{
		var rawValue = value.RawValue;
		if (rawValue < 0L)
			throw new ArgumentOutOfRangeException(nameof(value));

		var number = new ToUnsigned(rawValue).castedValue;
		var result = 0uL;
		var bit = 1uL << (BitCount - 2);

		while (bit > number)
		{
			bit >>= 2;
		}

		for (var i = 0; i < 2; i++)
		{
			while (bit != 0uL)
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
			
			if (number > (1uL << (BitCount / 2)) - 1)
			{
				number -= result;
				number = (number << (BitCount / 2)) - 0x8000_0000uL;
				result = (result << (BitCount / 2)) + 0x8000_0000uL;
			}
			else
			{
				number <<= BitCount / 2;
				result <<= BitCount / 2;
			}

			bit = 1uL << (BitCount / 2 - 2);
		}

		if (number > result)
			result++;

		return new Fixed(new ToSigned(result).castedValue);
	}

	public static Fixed Wrap(Fixed value, Fixed minimum, Fixed maximum)
	{
		while (value < minimum)
			value += maximum - minimum;

		while (value >= maximum)
			value -= maximum - minimum;

		return value;
	}

	public static Fixed Sin(Fixed value)
	{
		return Cos(value - PiOver2);
	}

	public static Fixed Cos(Fixed value)
	{
		// 6 terms of taylor series
		value = Wrap(value, -Pi, Pi);
		return 1
		       - Pow(value, 2) / 2
		       + Pow(value, 4) / 24
		       - Pow(value, 6) / 720
		       + Pow(value, 8) / 40_320
		       - Pow(value, 10) / 3_628_800
		       + Pow(value, 12) / 479_001_600;
	}

	public static Fixed Tan(Fixed value)
	{
		return Sin(value) / Cos(value);
	}

	public static Fixed Acos(Fixed value)
	{
		if (value < -One || value > One)
			throw new ArgumentOutOfRangeException(nameof(value));

		if (IsZero(value))
			return PiOver2;

		var result = Atan(Sqrt(One - value * value) / value);
		return value.RawValue < 0 ? result + Pi : result;
	}

	public static Fixed Atan(Fixed value)
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
		var squared2 = squared * 2;
		var squaredPlusOne = squared + One;
		var squaredPlusOne2 = squaredPlusOne * 2;
		var dividend = squared2;
		var divisor = squaredPlusOne * 3;

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

	public static Fixed Atan2(Fixed y, Fixed x)
	{
		var rawY = y.RawValue;
		var rawX = x.RawValue;

		if (rawX == 0L)
		{
			return rawY switch
			{
				> 0L => PiOver2,
				< 0L => -PiOver2,
				_ => Zero
			};
		}
		
		const long rawPointTwoEight = 1202590844L;
		var pointTwoEight = new Fixed(rawPointTwoEight);

		Fixed atan;
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

	public static Fixed operator -(Fixed operand)
	{
		return operand.RawValue == long.MinValue ? MaxValue : new Fixed(-operand.RawValue);
	}

	public static bool operator ==(Fixed left, Fixed right)
	{
		return left.RawValue == right.RawValue;
	}

	public static bool operator !=(Fixed left, Fixed right)
	{
		return left.RawValue != right.RawValue;
	}

	public static bool operator >(Fixed left, Fixed right)
	{
		return left.RawValue > right.RawValue;
	}

	public static bool operator <(Fixed left, Fixed right)
	{
		return left.RawValue < right.RawValue;
	}

	public static bool operator >=(Fixed left, Fixed right)
	{
		return left.RawValue >= right.RawValue;
	}

	public static bool operator <=(Fixed left, Fixed right)
	{
		return left.RawValue <= right.RawValue;
	}

	public static explicit operator Fixed(long value)
	{
		return new Fixed(value * RawOne);
	}

	public static explicit operator long(Fixed value)
	{
		return value.RawValue >> DecimalPlaces;
	}
	
	public static explicit operator Fixed(float value)
	{
		return new Fixed((long)(value * RawOne));
	}

	public static explicit operator float(Fixed value)
	{
		return (float)value.RawValue / RawOne;
	}
	
	public static explicit operator Fixed(double value)
	{
		return new Fixed((long)(value * RawOne));
	}

	public static explicit operator double(Fixed value)
	{
		return (double)value.RawValue / RawOne;
	}
	
	public static explicit operator Fixed(decimal value)
	{
		return new Fixed((long)(value * RawOne));
	}

	public static explicit operator decimal(Fixed value)
	{
		return (decimal)value.RawValue / RawOne;
	}

	public static implicit operator Fixed(int value)
	{
		return new Fixed(value);
	}

	public static explicit operator int(Fixed value)
	{
		return (int)(value.RawValue / RawOne);
	}
	
	public static implicit operator Fixed(Coarse value)
	{
		const int shiftSize = DecimalPlaces - Coarse.DecimalPlaces;
		var rawValue = (long)value.RawValue << shiftSize;
		return new Fixed(rawValue);
	}

	public static explicit operator Fixed(Precise value)
	{
		const int shiftSize = Precise.DecimalPlaces - DecimalPlaces;
		var rawValue = new ToSigned((ulong)(value.Bits >> shiftSize)).castedValue;
		return new Fixed(rawValue);
	}

	public override bool Equals(object? obj)
	{
		return obj is Fixed fixedValue && Equals(fixedValue);
	}

	public override int GetHashCode()
	{
		return RawValue.GetHashCode();
	}

	public bool Equals(Fixed other)
	{
		return RawValue.Equals(other.RawValue);
	}

	public int CompareTo(Fixed other)
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
			var intPart = (int)(RawValue >> DecimalPlaces);
			result.Append(intPart);

			var intermediate = Fract(this);
			if (intermediate.IsZero())
			{
				return result.ToString();
			}

			result.Append('.');
			var ten = (Fixed)10;
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
			var intPart = (int)(RawValue >> DecimalPlaces);

			var intermediate = Fract(this);
			if (intermediate.IsZero())
			{
				result.Append(intPart);
				return result.ToString();
			}

			intermediate = One - intermediate;

			result.Append(intPart + 1);
			result.Append('.');
			var ten = (Fixed)10;
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
