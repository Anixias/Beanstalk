using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace FixedPointMath;

public readonly partial struct Fixed64(long rawValue) : IFixedPoint<Fixed64>
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
	
	public static readonly Fixed64 Epsilon = new(1L);
	public static readonly Fixed64 MaxValue = new(long.MaxValue);
	public static readonly Fixed64 MinValue = new(long.MinValue);
	public static Fixed64 Zero => new(0L);
	public static Fixed64 One => new(RawOne);
	public static readonly Fixed64 NegativeOne = new(RawNegativeOne);
	public static readonly Fixed64 Half = new(RawHalf);
	public static readonly Fixed64 Pi = new(RawPi);
	public static readonly Fixed64 PiOver2 = new(RawPiOver2);
	public static readonly Fixed64 Tau = new(RawTau);
	public static readonly Fixed64 Ln2 = new(RawLn2);
	public static readonly Fixed64 E = new(RawE);

	private static readonly Fixed64 Log2Max = BitCount - DecimalPlaces - 1;
	private static readonly Fixed64 Log2Min = DecimalPlaces - BitCount;
	private static readonly Fixed64 DegToRadConstant = Pi / 180;
	private static readonly Fixed64 RadToDegConstant = 180 / Pi;

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

	public Fixed64(int value) : this(value * RawOne)
	{
	}
	
	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespaceRegexImpl();
	private static readonly Regex WhitespaceRegex = WhitespaceRegexImpl();

	public static Fixed64 Parse(string number)
	{
		number = WhitespaceRegex.Replace(number, "");
		var groups = number.Split('.');

		if (groups.Length > 2)
			throw new ArgumentException("Cannot have more than one decimal point");

		if (!int.TryParse(groups[0], out var intPart))
			throw new ArgumentException("Failed to parse integer part");

		if (groups.Length < 2)
			return new Fixed64((long)intPart << DecimalPlaces);

		var decimalString = groups[1];
		return From(intPart, decimalString);
	}

	public static bool TryParse(string number, out Fixed64 result)
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

	private static Fixed64 From(int intPart, string decimalPart)
	{
		var result = new Fixed64((long)intPart << DecimalPlaces);

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
	public static bool IsZero(Fixed64 value)
	{
		return value.RawValue == 0L;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsZero()
	{
		return IsZero(this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsNegative(Fixed64 value)
	{
		return value.RawValue < 0L;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsNegative()
	{
		return IsNegative(this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsOddInteger(Fixed64 value)
	{
		return IsInteger(value) && ((value.RawValue >> DecimalPlaces) & 1) == 1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsEvenInteger(Fixed64 value)
	{
		return IsInteger(value) && ((value.RawValue >> DecimalPlaces) & 1) == 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsPositive(Fixed64 value)
	{
		return value.RawValue > 0L;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsPositive()
	{
		return IsPositive(this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsInteger(Fixed64 value)
	{
		return Fract(value) == Zero;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsInteger()
	{
		return IsInteger(this);
	}

	public static Fixed64 DegToRad(Fixed64 value)
	{
		return value * DegToRadConstant;
	}

	public static Fixed64 RadToDeg(Fixed64 value)
	{
		return value * RadToDegConstant;
	}

	public static Fixed64 Lerp(Fixed64 a, Fixed64 b, Fixed64 t)
	{
		return a + t * (b - a);
	}

	public static int Sign(Fixed64 value)
	{
		return
			value.RawValue < 0 ? -1 :
			value.RawValue > 0 ? 1 :
			0;
	}

	public static Fixed64 Abs(Fixed64 value)
	{
		if (value == MinValue)
			return MaxValue;

		var mask = value.RawValue >> (BitCount - 1);
		return new Fixed64((value.RawValue + mask) ^ mask);
	}

	public static Fixed64 Floor(Fixed64 value)
	{
		var rawValue = new ToUnsigned(value.RawValue).castedValue;
		var flooredValue = new ToSigned(rawValue & 0xFFFF_FFFF_0000_0000uL).castedValue;
		return new Fixed64(flooredValue);
	}

	public static Fixed64 Ceil(Fixed64 value)
	{
		var hasDecimalPart = (value.RawValue & 0x0000_0000_FFFF_FFFFL) != 0L;
		return hasDecimalPart ? Floor(value) + One : value;
	}

	public static Fixed64 Fract(Fixed64 value)
	{
		return new Fixed64(value.RawValue & 0x0000_0000_FFFF_FFFFL);
	}

	public static Fixed64 Round(Fixed64 value)
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

	public static Fixed64 Clamp(Fixed64 value, Fixed64 min, Fixed64 max)
	{
		if (value < min)
			return min;

		if (value > max)
			return max;

		return value;
	}

	public static Fixed64 PosMod(Fixed64 a, Fixed64 b)
	{
		var result = a % b;
		
		if (a.RawValue < 0L && b.RawValue > 0L || result.RawValue > 0L && b.RawValue < 0L)
			result += b;

		return result;
	}

	public static (Fixed64, Fixed64) SinCos(Fixed64 angle)
	{
		return (Sin(angle), Cos(angle));
	}

	public static Fixed64 Snapped(Fixed64 value, Fixed64 step)
	{
		return step.RawValue != 0L ? Floor(value / step + Half) * step : value;
	}

	public static Fixed64 operator +(Fixed64 left, Fixed64 right)
	{
		var leftRaw = left.RawValue;
		var rightRaw = right.RawValue;
		var sum = leftRaw + rightRaw;

		if ((~(leftRaw ^ rightRaw) & (leftRaw ^ sum) & long.MinValue) != 0L)
			sum = leftRaw > 0L ? long.MaxValue : long.MinValue;

		return new Fixed64(sum);
	}

	public static Fixed64 operator -(Fixed64 left, Fixed64 right)
	{
		var leftRaw = left.RawValue;
		var rightRaw = right.RawValue;
		var difference = leftRaw - rightRaw;

		if (((leftRaw ^ rightRaw) & (leftRaw ^ difference) & long.MinValue) != 0L)
			difference = leftRaw < 0L ? long.MinValue : long.MaxValue;

		return new Fixed64(difference);
	}

	private static long AddOverflow(long left, long right, ref bool overflow)
	{
		var sum = left + right;
		overflow |= ((left ^ right ^ sum) & long.MinValue) != 0L;
		return sum;
	}

	public static Fixed64 operator *(Fixed64 left, Fixed64 right)
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
			return new Fixed64(sum);
		
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

		return new Fixed64(sum);
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

	public static Fixed64 operator /(Fixed64 left, Fixed64 right)
	{
		var leftRaw = left.RawValue;
		var rightRaw = right.RawValue;

		if (rightRaw == 0L)
			throw new DivideByZeroException();

		if (right == 2)
			return new Fixed64(leftRaw >> 1);

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

		return new Fixed64(result);
	}

	public static Fixed64 operator %(Fixed64 left, Fixed64 right)
	{
		return new Fixed64(left.RawValue == long.MinValue & right.RawValue == -1L ? 0L : left.RawValue % right.RawValue);
	}

	private static Fixed64 Pow2(Fixed64 exponent)
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

		result = new Fixed64(result.RawValue << integerPart);
		if (negative)
			result = One / result;

		return result;
	}

	public static Fixed64 Log2(Fixed64 value)
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

		var z = new Fixed64(rawValue);

		for (var i = 0; i < DecimalPlaces; i++)
		{
			z *= z;
			if (z.RawValue >= RawOne << 1)
			{
				z = new Fixed64(z.RawValue >> 1);
				y += b;
			}

			b >>= 1;
		}

		return new Fixed64(y);
	}

	public static Fixed64 Ln(Fixed64 value)
	{
		return Log2(value) * Ln2;
	}

	public static Fixed64 Pow(Fixed64 @base, Fixed64 exponent)
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

	public static Fixed64 Sqrt(Fixed64 value)
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

		return new Fixed64(new ToSigned(result).castedValue);
	}

	public static Fixed64 Wrap(Fixed64 value, Fixed64 minimum, Fixed64 maximum)
	{
		while (value < minimum)
			value += maximum - minimum;

		while (value >= maximum)
			value -= maximum - minimum;

		return value;
	}

	public static Fixed64 Sin(Fixed64 value)
	{
		return Cos(value - PiOver2);
	}

	public static Fixed64 Cos(Fixed64 value)
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

	public static Fixed64 Tan(Fixed64 value)
	{
		return Sin(value) / Cos(value);
	}

	public static Fixed64 Acos(Fixed64 value)
	{
		if (value < -One || value > One)
			throw new ArgumentOutOfRangeException(nameof(value));

		if (IsZero(value))
			return PiOver2;

		var result = Atan(Sqrt(One - value * value) / value);
		return value.RawValue < 0 ? result + Pi : result;
	}

	public static Fixed64 Atan(Fixed64 value)
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

	public static Fixed64 Atan2(Fixed64 y, Fixed64 x)
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
		var pointTwoEight = new Fixed64(rawPointTwoEight);

		Fixed64 atan;
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

	public static Fixed64 operator -(Fixed64 operand)
	{
		return operand.RawValue == long.MinValue ? MaxValue : new Fixed64(-operand.RawValue);
	}

	public static bool operator ==(Fixed64 left, Fixed64 right)
	{
		return left.RawValue == right.RawValue;
	}

	public static bool operator !=(Fixed64 left, Fixed64 right)
	{
		return left.RawValue != right.RawValue;
	}

	public static bool operator >(Fixed64 left, Fixed64 right)
	{
		return left.RawValue > right.RawValue;
	}

	public static bool operator <(Fixed64 left, Fixed64 right)
	{
		return left.RawValue < right.RawValue;
	}

	public static bool operator >=(Fixed64 left, Fixed64 right)
	{
		return left.RawValue >= right.RawValue;
	}

	public static bool operator <=(Fixed64 left, Fixed64 right)
	{
		return left.RawValue <= right.RawValue;
	}

	public static explicit operator Fixed64(long value)
	{
		return new Fixed64(value * RawOne);
	}

	public static explicit operator long(Fixed64 value)
	{
		return value.RawValue >> DecimalPlaces;
	}
	
	public static explicit operator Fixed64(float value)
	{
		return new Fixed64((long)(value * RawOne));
	}

	public static explicit operator float(Fixed64 value)
	{
		return (float)value.RawValue / RawOne;
	}
	
	public static explicit operator Fixed64(double value)
	{
		return new Fixed64((long)(value * RawOne));
	}

	public static explicit operator double(Fixed64 value)
	{
		return (double)value.RawValue / RawOne;
	}
	
	public static explicit operator Fixed64(decimal value)
	{
		return new Fixed64((long)(value * RawOne));
	}

	public static explicit operator decimal(Fixed64 value)
	{
		return (decimal)value.RawValue / RawOne;
	}

	public static implicit operator Fixed64(int value)
	{
		return new Fixed64(value);
	}

	public static explicit operator int(Fixed64 value)
	{
		return (int)(value.RawValue / RawOne);
	}
	
	public static implicit operator Fixed64(Fixed32 value)
	{
		const int shiftSize = DecimalPlaces - Fixed32.DecimalPlaces;
		var rawValue = (long)value.RawValue << shiftSize;
		return new Fixed64(rawValue);
	}

	public static explicit operator Fixed64(Fixed128 value)
	{
		const int shiftSize = Fixed128.DecimalPlaces - DecimalPlaces;
		var rawValue = new ToSigned((ulong)(value.Bits >> shiftSize)).castedValue;
		return new Fixed64(rawValue);
	}

	public override bool Equals(object? obj)
	{
		return obj is Fixed64 fixedValue && Equals(fixedValue);
	}

	public override int GetHashCode()
	{
		return RawValue.GetHashCode();
	}

	public bool Equals(Fixed64 other)
	{
		return RawValue.Equals(other.RawValue);
	}

	public int CompareTo(Fixed64 other)
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
			var ten = (Fixed64)10;
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
			var ten = (Fixed64)10;
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
