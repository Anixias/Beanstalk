using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace FixedPointMath;

public readonly partial struct Fixed128(Int128 rawValue) : IFixedPoint<Fixed128>
{
	[StructLayout(LayoutKind.Explicit)]
	private readonly struct ToUnsigned(Int128 sourceValue)
	{
		[FieldOffset(0)] public readonly Int128 sourceValue = sourceValue;
		[FieldOffset(0)] public readonly UInt128 castedValue;
	}
	
	[StructLayout(LayoutKind.Explicit)]
	private readonly struct ToSigned(UInt128 sourceValue)
	{
		[FieldOffset(0)] public readonly UInt128 sourceValue = sourceValue;
		[FieldOffset(0)] public readonly Int128 castedValue;
	}

	private const int BitCount = 128;
	internal const int DecimalPlaces = 64;
	private static readonly Int128 RawOne = Int128.One << DecimalPlaces;
	private static readonly Int128 RawHalf = RawOne >> 1;
	private static readonly Int128 RawNegativeOne = -RawOne;
	private static readonly Int128 RawPi = new(3uL, 2611923443488327892uL);
	private static readonly Int128 RawLn2 = new(0uL, 12786308645202655667uL);
	private static readonly Int128 RawE = new(2uL, 13249961062380153451uL);

	public static readonly Fixed128 Epsilon = new(Int128.One);
	public static readonly Fixed128 MaxValue = new(Int128.MaxValue);
	public static readonly Fixed128 MinValue = new(Int128.MinValue);
	public static Fixed128 Zero => new(Int128.Zero);
	public static Fixed128 One => new(RawOne);
	public static readonly Fixed128 NegativeOne = new(RawNegativeOne);
	public static readonly Fixed128 Half = new(RawHalf);
	public static readonly Fixed128 Pi = new(RawPi);
	public static readonly Fixed128 PiOver2 = Pi / 2;
	public static readonly Fixed128 Tau = Pi * 2;
	public static readonly Fixed128 Ln2 = new(RawLn2);
	public static readonly Fixed128 E = new(RawE);

	private static readonly Fixed128 Log2Max = BitCount - DecimalPlaces - 1;
	private static readonly Fixed128 Log2Min = DecimalPlaces - BitCount;
	private static readonly Fixed128 DegToRadConstant = Pi / 180;
	private static readonly Fixed128 RadToDegConstant = 180 / Pi;

	public Int128 RawValue { get; } = rawValue;
	public UInt128 Bits => new ToUnsigned(RawValue).castedValue;

	public Fixed128(int value) : this(value * RawOne)
	{
	}

	public Fixed128(long value) : this(value * RawOne)
	{
	}
	
	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespaceRegexImpl();
	private static readonly Regex WhitespaceRegex = WhitespaceRegexImpl();

	public static Fixed128 Parse(string number)
	{
		number = WhitespaceRegex.Replace(number, "");
		var groups = number.Split('.');

		if (groups.Length > 2)
			throw new ArgumentException("Cannot have more than one decimal point");

		if (!long.TryParse(groups[0], out var intPart))
			throw new ArgumentException("Failed to parse integer part");

		if (groups.Length < 2)
			return new Fixed128((Int128)intPart << DecimalPlaces);

		var decimalString = groups[1];
		return From(intPart, decimalString);
	}

	public static bool TryParse(string number, out Fixed128 result)
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

	private static Fixed128 From(long intPart, string decimalPart)
	{
		var result = new Fixed128((Int128)intPart << DecimalPlaces);

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
	public static bool IsZero(Fixed128 value)
	{
		return value.RawValue == 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsZero()
	{
		return IsZero(this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsNegative(Fixed128 value)
	{
		return value.RawValue < 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsNegative()
	{
		return IsNegative(this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsOddInteger(Fixed128 value)
	{
		return IsInteger(value) && ((value.RawValue >> DecimalPlaces) & 1) == 1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsEvenInteger(Fixed128 value)
	{
		return IsInteger(value) && ((value.RawValue >> DecimalPlaces) & 1) == 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsPositive(Fixed128 value)
	{
		return value.RawValue > 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsPositive()
	{
		return IsPositive(this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsInteger(Fixed128 value)
	{
		return Fract(value) == Zero;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsInteger()
	{
		return IsInteger(this);
	}

	public static Fixed128 DegToRad(Fixed128 value)
	{
		return value * DegToRadConstant;
	}

	public static Fixed128 RadToDeg(Fixed128 value)
	{
		return value * RadToDegConstant;
	}

	public static Fixed128 Lerp(Fixed128 a, Fixed128 b, Fixed128 t)
	{
		return a + t * (b - a);
	}

	public static int Sign(Fixed128 value)
	{
		return
			value.RawValue < 0 ? -1 :
			value.RawValue > 0 ? 1 :
			0;
	}

	public static Fixed128 Abs(Fixed128 value)
	{
		if (value == MinValue)
			return MaxValue;

		var mask = value.RawValue >> (BitCount - 1);
		return new Fixed128((value.RawValue + mask) ^ mask);
	}

	public static Fixed128 Floor(Fixed128 value)
	{
		var rawValueCast = new ToUnsigned(value.RawValue);
		var flooredValue = new ToSigned(rawValueCast.castedValue & ((UInt128)0xFFFF_FFFF_FFFF_FFFFuL << DecimalPlaces));
		return new Fixed128(flooredValue.castedValue);
	}

	public static Fixed128 Ceil(Fixed128 value)
	{
		var hasDecimalPart = (value.RawValue & 0xFFFF_FFFF_FFFF_FFFFuL) != Int128.Zero;
		return hasDecimalPart ? Floor(value) + One : value;
	}

	public static Fixed128 Fract(Fixed128 value)
	{
		return new Fixed128(value.RawValue & 0xFFFF_FFFF_FFFF_FFFFuL);
	}

	public static Fixed128 Round(Fixed128 value)
	{
		var decimalPart = value.RawValue & 0xFFFF_FFFF_FFFF_FFFFuL;
		var integerPart = Floor(value);

		if (decimalPart < 0x8000_0000_0000_0000L)
			return integerPart;
		
		if (decimalPart > 0x8000_0000_0000_0000L)
			return integerPart + One;

		return (integerPart.RawValue & RawOne) == Int128.Zero
			? integerPart
			: integerPart + One;
	}

	public static Fixed128 Clamp(Fixed128 value, Fixed128 min, Fixed128 max)
	{
		if (value < min)
			return min;

		if (value > max)
			return max;

		return value;
	}

	public static Fixed128 PosMod(Fixed128 a, Fixed128 b)
	{
		var result = a % b;

		if (a.RawValue < Int128.Zero && b.RawValue > Int128.Zero ||
		    result.RawValue > Int128.Zero && b.RawValue < Int128.Zero)
			result += b;

		return result;
	}

	public static (Fixed128, Fixed128) SinCos(Fixed128 angle)
	{
		return (Sin(angle), Cos(angle));
	}

	public static Fixed128 Snapped(Fixed128 value, Fixed128 step)
	{
		return step.RawValue != Int128.Zero ? Floor(value / step + Half) * step : value;
	}

	public static Fixed128 operator +(Fixed128 left, Fixed128 right)
	{
		var leftRaw = left.RawValue;
		var rightRaw = right.RawValue;
		var sum = leftRaw + rightRaw;

		if ((~(leftRaw ^ rightRaw) & (leftRaw ^ sum) & Int128.MinValue) != Int128.Zero)
			sum = leftRaw > Int128.Zero ? Int128.MaxValue : Int128.MinValue;

		return new Fixed128(sum);
	}

	public static Fixed128 operator -(Fixed128 left, Fixed128 right)
	{
		var leftRaw = left.RawValue;
		var rightRaw = right.RawValue;
		var difference = leftRaw - rightRaw;

		if (((leftRaw ^ rightRaw) & (leftRaw ^ difference) & Int128.MinValue) != Int128.Zero)
			difference = leftRaw < Int128.Zero ? Int128.MinValue : Int128.MaxValue;

		return new Fixed128(difference);
	}

	private static Int128 AddOverflow(Int128 left, Int128 right, ref bool overflow)
	{
		var sum = left + right;
		overflow |= ((left ^ right ^ sum) & Int128.MinValue) != Int128.Zero;
		return sum;
	}

	public static Fixed128 operator *(Fixed128 left, Fixed128 right)
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
		
		if (leftRaw == Int128.Zero || rightRaw == Int128.Zero)
			return Zero;

		var leftLow = leftRaw & 0xFFFF_FFFF_FFFF_FFFFL;
		var leftHigh = leftRaw >> DecimalPlaces;
		var rightLow = rightRaw & 0xFFFF_FFFF_FFFF_FFFFL;
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

		var opSignsEqual = ((leftRaw ^ rightRaw) & Int128.MinValue) == Int128.Zero;

		if (opSignsEqual)
		{
			if (sum < Int128.Zero || (overflow && leftRaw > Int128.Zero))
				return MaxValue;
		}
		else
		{
			if (sum > Int128.Zero)
				return MinValue;
		}

		var topCarry = highHigh >> DecimalPlaces;
		if (topCarry != Int128.Zero && topCarry != -1)
			return opSignsEqual ? MaxValue : MinValue;

		if (opSignsEqual)
			return new Fixed128(sum);
		
		Int128 posOp, negOp;
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

		return new Fixed128(sum);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int CountLeadingZeroes(UInt128 value)
	{
		var result = 0;
		
		while ((value & ((UInt128)0xF000_0000_0000_0000uL << DecimalPlaces)) == 0)
		{
			result += 4;
			value <<= 4;
		}
		
		while ((value & ((UInt128)0x8000_0000_0000_0000uL << DecimalPlaces)) == 0)
		{
			result += 1;
			value <<= 1;
		}

		return result;
	}

	public static Fixed128 operator /(Fixed128 left, Fixed128 right)
	{
		var leftRaw = left.RawValue;
		var rightRaw = right.RawValue;

		if (rightRaw == Int128.Zero)
			throw new DivideByZeroException();

		if (right == 2)
			return new Fixed128(leftRaw >> 1);

		var remainder = new ToUnsigned(leftRaw >= Int128.Zero ? leftRaw : -leftRaw).castedValue;
		var divisor = new ToUnsigned(rightRaw >= Int128.Zero ? rightRaw : -rightRaw).castedValue;
		var quotient = UInt128.Zero;
		var bitPos = BitCount / 2 + 1;

		while ((divisor & 0xFuL) == UInt128.Zero && bitPos >= 4)
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

			if ((division & ~(UInt128.MaxValue >> bitPos)) != UInt128.Zero)
				return ((leftRaw ^ rightRaw) & Int128.MinValue) == Int128.Zero ? MaxValue : MinValue;

			remainder <<= 1;
			bitPos--;
		}

		quotient++;
		var result = (Int128)(quotient >> 1);
		if (((leftRaw ^ rightRaw) & Int128.MinValue) != Int128.Zero)
			result = -result;

		return new Fixed128(result);
	}

	public static Fixed128 operator %(Fixed128 left, Fixed128 right)
	{
		return new Fixed128(left.RawValue == Int128.MinValue & right.RawValue == -1L ? Int128.Zero : left.RawValue % right.RawValue);
	}

	private static Fixed128 Pow2(Fixed128 exponent)
	{
		if (exponent.RawValue == Int128.Zero)
			return One;

		var negative = exponent.RawValue < Int128.Zero;
		if (negative)
			exponent = -exponent;

		if (exponent == One)
			return negative ? Half : 2;

		if (exponent >= Log2Max)
			return negative ? One / MaxValue : MaxValue;

		if (exponent <= Log2Min)
			return negative ? MaxValue : Zero;

		var integerPart = (long)Floor(exponent);
		exponent = Fract(exponent);

		var result = One;
		var term = One;
		var i = 1;

		while (term.RawValue != Int128.Zero)
		{
			term = exponent * term * Ln2 / i;
			result += term;
			i++;
		}

		var resultShift = result.RawValue << (int)integerPart;
		resultShift <<= (int)(integerPart >> 32);
		result = new Fixed128(resultShift);
		if (negative)
			result = One / result;

		return result;
	}

	public static Fixed128 Log2(Fixed128 value)
	{
		if (!IsPositive(value))
			throw new ArgumentOutOfRangeException(nameof(value));

		var b = Int128.One << (DecimalPlaces - 1);
		var y = Int128.Zero;

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

		var z = new Fixed128(rawValue);

		for (var i = 0; i < DecimalPlaces; i++)
		{
			z *= z;
			if (z.RawValue >= RawOne << 1)
			{
				z = new Fixed128(z.RawValue >> 1);
				y += b;
			}

			b >>= 1;
		}

		return new Fixed128(y);
	}

	public static Fixed128 Ln(Fixed128 value)
	{
		return Log2(value) * Ln2;
	}

	public static Fixed128 Pow(Fixed128 @base, Fixed128 exponent)
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

	public static Fixed128 Sqrt(Fixed128 value)
	{
		var rawValue = value.RawValue;
		if (rawValue < Int128.Zero)
			throw new ArgumentOutOfRangeException(nameof(value));

		var number = new ToUnsigned(rawValue).castedValue;
		var result = UInt128.Zero;
		var bit = UInt128.One << (BitCount - 2);

		while (bit > number)
		{
			bit >>= 2;
		}

		for (var i = 0; i < 2; i++)
		{
			while (bit != UInt128.Zero)
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
			
			if (number > (UInt128.One << (BitCount / 2)) - 1)
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

		return new Fixed128(new ToSigned(result).castedValue);
	}

	public static Fixed128 Wrap(Fixed128 value, Fixed128 minimum, Fixed128 maximum)
	{
		while (value < minimum)
			value += maximum - minimum;

		while (value >= maximum)
			value -= maximum - minimum;

		return value;
	}

	public static Fixed128 Sin(Fixed128 value)
	{
		return Cos(value - PiOver2);
	}

	public static Fixed128 Cos(Fixed128 value)
	{
		// 10 terms of taylor series
		value = Wrap(value, -Pi, Pi);
		return 1
		       - Pow(value, 2) / 2L
		       + Pow(value, 4) / 24L
		       - Pow(value, 6) / 720L
		       + Pow(value, 8) / 40_320L
		       - Pow(value, 10) / 3_628_800L
		       + Pow(value, 12) / 479_001_600L
		       - Pow(value, 14) / 87_178_291_200L
		       + Pow(value, 16) / 20_922_789_888_000L
		       - Pow(value, 18) / 6_402_373_705_728_000L
		       + Pow(value, 20) / 2_432_902_008_176_640_000L;
	}

	public static Fixed128 Tan(Fixed128 value)
	{
		return Sin(value) / Cos(value);
	}

	public static Fixed128 Acos(Fixed128 value)
	{
		if (value < -One || value > One)
			throw new ArgumentOutOfRangeException(nameof(value));

		if (IsZero(value))
			return PiOver2;

		var result = Atan(Sqrt(One - value * value) / value);
		return value.RawValue < 0 ? result + Pi : result;
	}

	public static Fixed128 Atan(Fixed128 value)
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

	public static Fixed128 Atan2(Fixed128 y, Fixed128 x)
	{
		var rawY = y.RawValue;
		var rawX = x.RawValue;

		if (rawX == Int128.Zero)
		{
			if (rawY > Int128.Zero)
				return PiOver2;
			if (rawY < Int128.Zero)
				return -PiOver2;
			
			return Zero;
		}
		
		var rawPointTwoEight = new Int128(0uL, 5165088340638674452uL);
		var pointTwoEight = new Fixed128(rawPointTwoEight);

		Fixed128 atan;
		var z = y / x;
		var zSquared = z * z;

		if (One + pointTwoEight * zSquared == MaxValue)
			return y < Zero ? -PiOver2 : PiOver2;

		if (Abs(z) < One)
		{
			atan = z / (One + pointTwoEight * zSquared);

			if (rawX >= Int128.Zero)
				return atan;
			
			if (rawY < Int128.Zero)
				return atan - Pi;

			return atan + Pi;
		}

		atan = PiOver2 - z / (zSquared + pointTwoEight);

		if (rawY < Int128.Zero)
			return atan - Pi;

		return atan;
	}

	public static Fixed128 operator -(Fixed128 operand)
	{
		return operand.RawValue == long.MinValue ? MaxValue : new Fixed128(-operand.RawValue);
	}

	public static bool operator ==(Fixed128 left, Fixed128 right)
	{
		return left.RawValue == right.RawValue;
	}

	public static bool operator !=(Fixed128 left, Fixed128 right)
	{
		return left.RawValue != right.RawValue;
	}

	public static bool operator >(Fixed128 left, Fixed128 right)
	{
		return left.RawValue > right.RawValue;
	}

	public static bool operator <(Fixed128 left, Fixed128 right)
	{
		return left.RawValue < right.RawValue;
	}

	public static bool operator >=(Fixed128 left, Fixed128 right)
	{
		return left.RawValue >= right.RawValue;
	}

	public static bool operator <=(Fixed128 left, Fixed128 right)
	{
		return left.RawValue <= right.RawValue;
	}

	public static explicit operator Int128(Fixed128 value)
	{
		return value.RawValue >> DecimalPlaces;
	}
	
	public static explicit operator Fixed128(float value)
	{
		return new Fixed128((Int128)(value * 2.0f * (long)(RawOne >> 1)));
	}

	public static explicit operator float(Fixed128 value)
	{
		throw new NotImplementedException();
		//return (float)value.RawValue / RawOne;
	}
	
	public static explicit operator Fixed128(double value)
	{
		return new Fixed128((Int128)(value * 2.0 * (long)(RawOne >> 1)));
	}

	public static explicit operator double(Fixed128 value)
	{
		throw new NotImplementedException();
	}
	
	public static explicit operator Fixed128(decimal value)
	{
		return new Fixed128((Int128)(value * 2.0m * (long)(RawOne >> 1)));
	}

	public static explicit operator decimal(Fixed128 value)
	{
		throw new NotImplementedException();
	}

	public static implicit operator Fixed128(int value)
	{
		return new Fixed128(value);
	}

	public static explicit operator int(Fixed128 value)
	{
		return (int)(value.RawValue / RawOne);
	}

	public static explicit operator long(Fixed128 value)
	{
		return (long)(value.RawValue / RawOne);
	}

	public static implicit operator Fixed128(long value)
	{
		return new Fixed128(value);
	}

	public static implicit operator Fixed128(Fixed32 value)
	{
		var lower = (ulong)((long)Fixed32.Fract(value).RawValue << (DecimalPlaces - Fixed32.DecimalPlaces));
		var upper = (ulong)((long)Fixed32.Floor(value).RawValue >> Fixed32.DecimalPlaces);
		return new Fixed128(new Int128(upper, lower));
	}

	public static implicit operator Fixed128(Fixed64 value)
	{
		var lower = (ulong)(Fixed64.Fract(value).RawValue << (DecimalPlaces - Fixed64.DecimalPlaces));
		var upper = (ulong)(Fixed64.Floor(value).RawValue >> Fixed64.DecimalPlaces);
		return new Fixed128(new Int128(upper, lower));
	}

	public override bool Equals(object? obj)
	{
		return obj is Fixed128 fixedValue && Equals(fixedValue);
	}

	public override int GetHashCode()
	{
		return RawValue.GetHashCode();
	}

	public bool Equals(Fixed128 other)
	{
		return RawValue.Equals(other.RawValue);
	}

	public int CompareTo(Fixed128 other)
	{
		return RawValue.CompareTo(other.RawValue);
	}

	public override string ToString()
	{
		const int decimalsToRender = 20;
		var result = new StringBuilder();

		if (IsZero())
			return "0";

		if (IsPositive())
		{
			var intPart = (long)(RawValue >> DecimalPlaces);
			result.Append(intPart);

			var intermediate = Fract(this);
			if (intermediate.IsZero())
			{
				return result.ToString();
			}

			result.Append('.');
			var ten = (Fixed128)10;
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
			var intPart = (long)(RawValue >> DecimalPlaces);

			var intermediate = Fract(this);
			if (intermediate.IsZero())
			{
				result.Append(intPart);
				return result.ToString();
			}

			intermediate = One - intermediate;

			result.Append(intPart + 1);
			result.Append('.');
			var ten = (Fixed128)10;
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
