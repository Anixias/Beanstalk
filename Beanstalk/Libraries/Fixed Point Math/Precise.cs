using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace FixedPointMath;

public readonly partial struct Precise(Int128 rawValue) : IEquatable<Precise>, IComparable<Precise>
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
	private const int DecimalPlaces = 64;
	private static readonly Int128 RawOne = Int128.One << DecimalPlaces;
	private static readonly Int128 RawHalf = RawOne >> 1;
	private static readonly Int128 RawNegativeOne = -(Int128.One << DecimalPlaces);
	private static readonly Int128 RawPi = new(3uL, 2611923443488327896uL);
	private static readonly Int128 RawPiOver2 = 0x1921FB544L;
	private static readonly Int128 RawPiTimes2 = 0x6487ED511L;
	private static readonly Int128 RawLn2 = 0xB17217F7L;
	private static readonly Int128 RawLog2Max = 0x1F00000000L;
	private static readonly Int128 RawLog2Min = -0x2000000000L;
	private static readonly int LutSize = (int)(RawPiOver2 >> 15);
	
	public static readonly Precise Epsilon = new(1L);
	public static readonly Precise MaxValue = new(Int128.MaxValue);
	public static readonly Precise MinValue = new(Int128.MinValue);
	public static readonly Precise Zero = new(Int128.Zero);
	public static readonly Precise One = new(RawOne);
	public static readonly Precise NegativeOne = new(RawNegativeOne);
	public static readonly Precise Half = new(RawHalf);
	public static readonly Precise Pi = new(RawPi);
	public static readonly Precise PiOver2 = new(RawPiOver2);
	public static readonly Precise Ln2 = new(RawLn2);

	private static readonly Precise Log2Max = new(RawLog2Max);
	private static readonly Precise Log2Min = new(RawLog2Min);
	private static readonly Precise DegToRadConstant = Pi / 180;
	private static readonly Precise RadToDegConstant = 180 / Pi;
	private static readonly Precise LutInterval = (LutSize - 1) / PiOver2;

	public Int128 RawValue { get; } = rawValue;

	public Precise(int value) : this(value * RawOne)
	{
	}

	public static Precise Parse(string number)
	{
		var groups = number.Split('.', StringSplitOptions.TrimEntries);

		if (groups.Length > 2)
			throw new ArgumentException("Cannot have more than one decimal point");

		if (!long.TryParse(groups[0], out var intPart))
			throw new ArgumentException("Failed to parse integer part");

		if (groups.Length < 2)
			return new Precise((Int128)intPart << DecimalPlaces);

		var decimalString = groups[1];
		return From(intPart, decimalString);
	}

	public static bool TryParse(string number, out Precise result)
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

	private static Precise From(long intPart, string decimalPart)
	{
		var result = new Precise((Int128)intPart << DecimalPlaces);

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
	public static bool IsZero(Precise value)
	{
		return value.RawValue == Int128.Zero;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsZero()
	{
		return RawValue == Int128.Zero;
	}

	public static Precise DegToRad(Precise value)
	{
		return value * DegToRadConstant;
	}

	public static Precise RadToDeg(Precise value)
	{
		return value * RadToDegConstant;
	}

	public static Precise Lerp(Precise a, Precise b, Precise t)
	{
		return a + t * (b - a);
	}

	public static int Sign(Precise value)
	{
		return
			value.RawValue < 0 ? -1 :
			value.RawValue > 0 ? 1 :
			0;
	}

	public static Precise Abs(Precise value)
	{
		if (value == MinValue)
			return MaxValue;

		var mask = value.RawValue >> 127;
		return new Precise((value.RawValue + mask) ^ mask);
	}

	public static Precise Floor(Precise value)
	{
		var rawValueCast = new ToUnsigned(value.RawValue);
		var flooredValue = new ToSigned(rawValueCast.castedValue & ((UInt128)0xFFFF_FFFF_FFFF_FFFFuL << DecimalPlaces));
		return new Precise(flooredValue.castedValue);
	}

	public static Precise Ceil(Precise value)
	{
		var hasDecimalPart = (value.RawValue & 0xFFFF_FFFF_FFFF_FFFFuL) != Int128.Zero;
		return hasDecimalPart ? Floor(value) + One : value;
	}

	public static Precise Fract(Precise value)
	{
		return new Precise(value.RawValue & 0xFFFF_FFFF_FFFF_FFFFuL);
	}

	public static Precise Round(Precise value)
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

	public static Precise Clamp(Precise value, Precise min, Precise max)
	{
		if (value < min)
			return min;

		if (value > max)
			return max;

		return value;
	}

	public static Precise PosMod(Precise a, Precise b)
	{
		var result = a % b;
		
		if (a.RawValue < Int128.Zero && b.RawValue > Int128.Zero || result.RawValue > Int128.Zero && b.RawValue < Int128.Zero)
			result += b;

		return result;
	}

	public static (Precise, Precise) SinCos(Precise angle)
	{
		return (Sin(angle), Cos(angle));
	}

	public static Precise Snapped(Precise value, Precise step)
	{
		return step.RawValue != Int128.Zero ? Floor(value / step + Half) * step : value;
	}

	public static Precise operator +(Precise left, Precise right)
	{
		var leftRaw = left.RawValue;
		var rightRaw = right.RawValue;
		var sum = leftRaw + rightRaw;

		if ((~(leftRaw ^ rightRaw) & (leftRaw ^ sum) & Int128.MinValue) != Int128.Zero)
			sum = leftRaw > Int128.Zero ? Int128.MaxValue : Int128.MinValue;

		return new Precise(sum);
	}

	public static Precise operator -(Precise left, Precise right)
	{
		var leftRaw = left.RawValue;
		var rightRaw = right.RawValue;
		var difference = leftRaw - rightRaw;

		if (((leftRaw ^ rightRaw) & (leftRaw ^ difference) & Int128.MinValue) != Int128.Zero)
			difference = leftRaw < Int128.Zero ? Int128.MinValue : Int128.MaxValue;

		return new Precise(difference);
	}

	private static Int128 AddOverflow(Int128 left, Int128 right, ref bool overflow)
	{
		var sum = left + right;
		overflow |= ((left ^ right ^ sum) & Int128.MinValue) != Int128.Zero;
		return sum;
	}

	public static Precise operator *(Precise left, Precise right)
	{
		var leftRaw = left.RawValue;
		var rightRaw = right.RawValue;

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
			return new Precise(sum);
		
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

		return new Precise(sum);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int CountLeadingZeroes(UInt128 value)
	{
		var result = 0;
		
		while ((value & ((UInt128)0xF000_0000_0000_0000 << DecimalPlaces)) == 0)
		{
			result += 4;
			value <<= 4;
		}
		
		while ((value & ((UInt128)0x8000_0000_0000_0000 << DecimalPlaces)) == 0)
		{
			result += 1;
			value <<= 1;
		}

		return result;
	}

	public static Precise operator /(Precise left, Precise right)
	{
		var leftRaw = left.RawValue;
		var rightRaw = right.RawValue;

		if (rightRaw == Int128.Zero)
			throw new DivideByZeroException();

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

			if ((division & ~(0xFFFF_FFFF_FFFF_FFFFuL >> bitPos)) != 0ul)
				return ((leftRaw ^ rightRaw) & Int128.MinValue) == Int128.Zero ? MaxValue : MinValue;

			remainder <<= 1;
			bitPos--;
		}

		quotient++;
		var result = (Int128)(quotient >> 1);
		if (((leftRaw ^ rightRaw) & Int128.MinValue) != Int128.Zero)
			result = -result;

		return new Precise(result);
	}

	public static Precise operator %(Precise left, Precise right)
	{
		return new Precise(left.RawValue == Int128.MinValue & right.RawValue == -1L ? Int128.Zero : left.RawValue % right.RawValue);
	}

	private static Precise Pow2(Precise exponent)
	{
		if (exponent.RawValue == Int128.Zero)
			return One;

		var negative = exponent.RawValue < Int128.Zero;
		if (negative)
			exponent = -exponent;

		if (exponent == One)
			return negative ? One / 2 : 2;

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
		result = new Precise(resultShift);
		if (negative)
			result = One / result;

		return result;
	}

	public static Precise Log2(Precise value)
	{
		if (value.RawValue <= Int128.Zero)
			throw new ArgumentOutOfRangeException(nameof(value));

		var b = 1L << (DecimalPlaces - 1);
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

		var z = new Precise(rawValue);

		for (var i = 0; i < DecimalPlaces; i++)
		{
			z *= z;
			if (z.RawValue >= RawOne << 1)
			{
				z = new Precise(z.RawValue >> 1);
				y += b;
			}

			b >>= 1;
		}

		return new Precise(y);
	}

	public static Precise Ln(Precise value)
	{
		return Log2(value) * Ln2;
	}

	public static Precise Pow(Precise @base, Precise exponent)
	{
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

	public static Precise Sqrt(Precise value)
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

		return new Precise(new ToSigned(result).castedValue);
	}

	private static Int128 ClampSinValue(Int128 angle, out bool flipHorizontal, out bool flipVertical)
	{
		throw new NotImplementedException();
		// (2 ^ 29) * Pi, where 29 is the largest N such that (2 ^ N) * Pi < MaxValue
		/*var largePi = 7244019458077122842L;

		var clamped2Pi = angle;
		for (var i = 0; i < 29; i++)
		{
			clamped2Pi %= largePi >> i;
		}

		if (angle < 0)
			clamped2Pi += RawPiTimes2;

		flipVertical = clamped2Pi >= RawPi;
		
		var clampedPi = clamped2Pi;
		while (clampedPi >= RawPi)
		{
			clampedPi -= RawPi;
		}
		
		flipHorizontal = clampedPi >= RawPiOver2;

		var clampedPiOver2 = clampedPi;
		if (clampedPiOver2 >= RawPiOver2)
			clampedPiOver2 -= RawPiOver2;

		return clampedPiOver2;*/
	}

	public static Precise Sin(Precise value)
	{
		throw new NotImplementedException();
		/*
		var clampedL = ClampSinValue(value.RawValue, out var flipHorizontal, out var flipVertical);
		var clamped = new Precise(clampedL);

		var rawIndex = clamped * LutInterval;
		var roundedIndex = (int)Round(rawIndex);
		var indexError = rawIndex - roundedIndex;
		
		var nearestValue = new Precise(SinLut[flipHorizontal
			? SinLut.Length - 1 - roundedIndex
			: roundedIndex]);
		
		var secondNearestValue = new Precise(SinLut[flipHorizontal
			? SinLut.Length - 1 - roundedIndex - Sign(indexError)
			: roundedIndex + Sign(indexError)]);

		var delta = (indexError * Abs(nearestValue - secondNearestValue)).RawValue;
		var interpolatedValue = nearestValue.RawValue + (flipHorizontal ? -delta : delta);
		var finalValue = flipVertical ? -interpolatedValue : interpolatedValue;
		return new Precise(finalValue);*/
	}

	public static Precise FastSin(Precise value)
	{
		throw new NotImplementedException();
		/*
		var clampedL = ClampSinValue(value.RawValue, out var flipHorizontal, out var flipVertical);

		var rawIndex = (uint)(clampedL >> 15);
		if (rawIndex >= LutSize)
			rawIndex = LutSize - 1;

		var nearestValue = SinLut[flipHorizontal
			? SinLut.Length - 1 - (int)rawIndex
			: (int)rawIndex];

		return new Precise(flipVertical ? -nearestValue : nearestValue);*/
	}

	public static Precise Cos(Precise value)
	{
		var rawValue = value.RawValue;
		var angle = rawValue + (rawValue > 0 ? -RawPi - RawPiOver2 : RawPiOver2);
		return Sin(new Precise(angle));
	}

	public static Precise FastCos(Precise value)
	{
		var rawValue = value.RawValue;
		var angle = rawValue + (rawValue > 0 ? -RawPi - RawPiOver2 : RawPiOver2);
		return FastSin(new Precise(angle));
	}

	public static Precise Tan(Precise value)
	{
		return Sin(value) / Cos(value);
	}

	public static Precise FastTan(Precise value)
	{
		return FastSin(value) / FastCos(value);
	}

	public static Precise Acos(Precise value)
	{
		if (value < -One || value > One)
			throw new ArgumentOutOfRangeException(nameof(value));

		if (IsZero(value))
			return PiOver2;

		var result = Atan(Sqrt(One - value * value) / value);
		return value.RawValue < 0 ? result + Pi : result;
	}

	public static Precise Atan(Precise value)
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

	public static Precise Atan2(Precise y, Precise x)
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
		
		const long rawPointTwoEight = 1202590844L;
		var pointTwoEight = new Precise(rawPointTwoEight);

		Precise atan;
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

	public static Precise operator -(Precise operand)
	{
		return operand.RawValue == long.MinValue ? MaxValue : new Precise(-operand.RawValue);
	}

	public static bool operator ==(Precise left, Precise right)
	{
		return left.RawValue == right.RawValue;
	}

	public static bool operator !=(Precise left, Precise right)
	{
		return left.RawValue != right.RawValue;
	}

	public static bool operator >(Precise left, Precise right)
	{
		return left.RawValue > right.RawValue;
	}

	public static bool operator <(Precise left, Precise right)
	{
		return left.RawValue < right.RawValue;
	}

	public static bool operator >=(Precise left, Precise right)
	{
		return left.RawValue >= right.RawValue;
	}

	public static bool operator <=(Precise left, Precise right)
	{
		return left.RawValue <= right.RawValue;
	}

	public static explicit operator Precise(long value)
	{
		return new Precise(value * RawOne);
	}

	public static explicit operator long(Precise value)
	{
		return (long)(value.RawValue >> DecimalPlaces);
	}
	
	public static explicit operator Precise(float value)
	{
		return new Precise((Int128)(value * 2.0f * (long)(RawOne >> 1)));
	}

	public static explicit operator float(Precise value)
	{
		throw new NotImplementedException();
		//return (float)value.RawValue / RawOne;
	}
	
	public static explicit operator Precise(double value)
	{
		return new Precise((Int128)(value * 2.0 * (long)(RawOne >> 1)));
	}

	public static explicit operator double(Precise value)
	{
		throw new NotImplementedException();
	}
	
	public static explicit operator Precise(decimal value)
	{
		return new Precise((Int128)(value * 2.0m * (long)(RawOne >> 1)));
	}

	public static explicit operator decimal(Precise value)
	{
		throw new NotImplementedException();
	}

	public static implicit operator Precise(int value)
	{
		return new Precise(value);
	}

	public static explicit operator int(Precise value)
	{
		return (int)(value.RawValue / RawOne);
	}

	public override bool Equals(object? obj)
	{
		return obj is Precise fixedValue && Equals(fixedValue);
	}

	public override int GetHashCode()
	{
		return RawValue.GetHashCode();
	}

	public bool Equals(Precise other)
	{
		return RawValue.Equals(other.RawValue);
	}

	public int CompareTo(Precise other)
	{
		return RawValue.CompareTo(other.RawValue);
	}

	public override string ToString()
	{
		var result = new StringBuilder();

		var intPart = (long)(RawValue >> DecimalPlaces);
		result.Append(intPart);
		
		var intermediate = Fract(this);
		if (intermediate.IsZero())
			return result.ToString();
		
		result.Append('.');
		for (var i = 0; i < 20; i++)
		{
			intermediate *= 10;
			var digit = intermediate.RawValue >> DecimalPlaces;

			intermediate = Fract(intermediate);
			result.Append(digit);
		}

		return result.ToString();
	}
}
