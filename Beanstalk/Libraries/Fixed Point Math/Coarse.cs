using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace FixedPointMath;

public readonly struct Coarse(int rawValue) : IFixedPoint<Coarse>
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
	
	public static readonly Coarse Epsilon = new(1);
	public static readonly Coarse MaxValue = new(int.MaxValue);
	public static readonly Coarse MinValue = new(int.MinValue);
	public static Coarse Zero => new(0);
	public static Coarse One => new(RawOne);
	public static readonly Coarse NegativeOne = new(RawNegativeOne);
	public static readonly Coarse Half = new(RawHalf);
	public static readonly Coarse Pi = new(RawPi);
	public static readonly Coarse PiOver2 = new(RawPiOver2);
	public static readonly Coarse PiTimes2 = new(RawPiTimes2);
	public static readonly Coarse Ln2 = new(RawLn2);

	private static readonly Coarse Log2Max = (Coarse)(BitCount - DecimalPlaces - 1);
	private static readonly Coarse Log2Min = (Coarse)(DecimalPlaces - BitCount);
	private static readonly Coarse DegToRadConstant = Pi / (Coarse)180;
	private static readonly Coarse RadToDegConstant = (Coarse)180 / Pi;

	private const int BitCount = 32;
	internal const int DecimalPlaces = 16;
	private const int RawOne = 1 << DecimalPlaces;
	private const int RawHalf = 0x8000;
	private const int RawNegativeOne = -(1 << DecimalPlaces);
	private const int RawPi = 0x3_243F;
	private const int RawPiOver2 = 0x1_921F;
	private const int RawPiTimes2 = 0x6_487E;
	private const int RawLn2 = 0x0_B172;

	public int RawValue { get; } = rawValue;
	public uint Bits => new ToUnsigned(RawValue).castedValue;

	public static Coarse Parse(string number)
	{
		var groups = number.Split('.', StringSplitOptions.TrimEntries);

		if (groups.Length > 2)
			throw new ArgumentException("Cannot have more than one decimal point");

		if (!short.TryParse(groups[0], out var intPart))
			throw new ArgumentException("Failed to parse integer part");

		if (groups.Length < 2)
			return new Coarse(intPart << DecimalPlaces);

		var decimalString = groups[1];
		return From(intPart, decimalString);
	}

	public static bool TryParse(string number, out Coarse result)
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

	private static Coarse From(short intPart, string decimalPart)
	{
		var result = new Coarse(intPart << DecimalPlaces);

		if (string.IsNullOrWhiteSpace(decimalPart))
			return result;

		var decimalValue = Zero;
		var place = One / (Coarse)10;

		foreach (var c in decimalPart)
		{
			var digit = c - '0';
			decimalValue += (Coarse)digit * place;
			place /= (Coarse)10;

			if (IsZero(place))
				break;
		}

		if (intPart < 0)
			decimalValue = -decimalValue;
		
		result += decimalValue;
		
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsZero(Coarse value)
	{
		return value.RawValue == 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsZero()
	{
		return IsZero(this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsNegative(Coarse value)
	{
		return value.RawValue < 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsNegative()
	{
		return IsNegative(this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsOddInteger(Coarse value)
	{
		return IsInteger(value) && ((value.RawValue >> DecimalPlaces) & 1) == 1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsEvenInteger(Coarse value)
	{
		return IsInteger(value) && ((value.RawValue >> DecimalPlaces) & 1) == 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsPositive(Coarse value)
	{
		return value.RawValue > 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsPositive()
	{
		return IsPositive(this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsInteger(Coarse value)
	{
		return Fract(value) == Zero;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsInteger()
	{
		return IsInteger(this);
	}

	public static Coarse DegToRad(Coarse value)
	{
		return value * DegToRadConstant;
	}

	public static Coarse RadToDeg(Coarse value)
	{
		return value * RadToDegConstant;
	}

	public static Coarse Lerp(Coarse a, Coarse b, Coarse t)
	{
		return a + t * (b - a);
	}

	public static int Sign(Coarse value)
	{
		return
			value.RawValue < 0 ? -1 :
			value.RawValue > 0 ? 1 :
			0;
	}

	public static Coarse Abs(Coarse value)
	{
		if (value == MinValue)
			return MaxValue;

		var mask = value.RawValue >> (BitCount - 1);
		return new Coarse((value.RawValue + mask) ^ mask);
	}

	public static Coarse Floor(Coarse value)
	{
		var rawValue = new ToUnsigned(value.RawValue).castedValue;
		var flooredValue = new ToSigned(rawValue & 0xFFFF_0000u).castedValue;
		return new Coarse(flooredValue);
	}

	public static Coarse Ceil(Coarse value)
	{
		var hasDecimalPart = (value.RawValue & 0x0000_FFFF) != 0L;
		return hasDecimalPart ? Floor(value) + One : value;
	}

	public static Coarse Fract(Coarse value)
	{
		return new Coarse(value.RawValue & 0x0000_FFFF);
	}

	public static Coarse Round(Coarse value)
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

	public static Coarse Clamp(Coarse value, Coarse min, Coarse max)
	{
		if (value < min)
			return min;

		if (value > max)
			return max;

		return value;
	}

	public static Coarse PosMod(Coarse a, Coarse b)
	{
		var result = a % b;
		
		if (a.RawValue < 0 && b.RawValue > 0 || result.RawValue > 0 && b.RawValue < 0)
			result += b;

		return result;
	}

	public static (Coarse, Coarse) SinCos(Coarse angle)
	{
		return (Sin(angle), Cos(angle));
	}

	public static Coarse Snapped(Coarse value, Coarse step)
	{
		return step.RawValue != 0 ? Floor(value / step + Half) * step : value;
	}

	public static Coarse operator +(Coarse left, Coarse right)
	{
		var leftRaw = left.RawValue;
		var rightRaw = right.RawValue;
		var sum = leftRaw + rightRaw;

		if ((~(leftRaw ^ rightRaw) & (leftRaw ^ sum) & int.MinValue) != 0)
			sum = leftRaw > 0 ? int.MaxValue : int.MinValue;

		return new Coarse(sum);
	}

	public static Coarse operator -(Coarse left, Coarse right)
	{
		var leftRaw = left.RawValue;
		var rightRaw = right.RawValue;
		var difference = leftRaw - rightRaw;

		if (((leftRaw ^ rightRaw) & (leftRaw ^ difference) & int.MinValue) != 0)
			difference = leftRaw < 0 ? int.MinValue : int.MaxValue;

		return new Coarse(difference);
	}

	private static int AddOverflow(int left, int right, ref bool overflow)
	{
		var sum = left + right;
		overflow |= ((left ^ right ^ sum) & int.MinValue) != 0;
		return sum;
	}

	public static Coarse operator *(Coarse left, Coarse right)
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
			return new Coarse(sum);
		
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

		return new Coarse(sum);
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

	public static Coarse operator /(Coarse left, Coarse right)
	{
		var leftRaw = left.RawValue;
		var rightRaw = right.RawValue;

		if (rightRaw == 0L)
			throw new DivideByZeroException();

		if (right == (Coarse)2)
			return new Coarse(leftRaw >> 1);

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

		return new Coarse(result);
	}

	public static Coarse operator %(Coarse left, Coarse right)
	{
		return new Coarse(left.RawValue == int.MinValue & right.RawValue == -1 ? 0 : left.RawValue % right.RawValue);
	}

	private static Coarse Pow2(Coarse exponent)
	{
		if (exponent.RawValue == 0)
			return One;

		var negative = exponent.RawValue < 0;
		if (negative)
			exponent = -exponent;

		if (exponent == One)
			return negative ? Half : (Coarse)2;

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
			term = exponent * term * Ln2 / (Coarse)i;
			result += term;
			i++;
		}

		result = new Coarse(result.RawValue << integerPart);
		if (negative)
			result = One / result;

		return result;
	}

	public static Coarse Log2(Coarse value)
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

		var z = new Coarse(rawValue);

		for (var i = 0; i < DecimalPlaces; i++)
		{
			z *= z;
			if (z.RawValue >= RawOne << 1)
			{
				z = new Coarse(z.RawValue >> 1);
				y += b;
			}

			b >>= 1;
		}

		return new Coarse(y);
	}

	public static Coarse Ln(Coarse value)
	{
		return Log2(value) * Ln2;
	}

	public static Coarse Pow(Coarse @base, Coarse exponent)
	{
		if (@base < Zero)
		{
			// Todo: Handle properly
			if (!exponent.IsInteger())
				return Zero;

			var pow = Pow(-@base, exponent);
			if (exponent % (Coarse)2 == Zero)
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

	public static Coarse Sqrt(Coarse value)
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

		return new Coarse(new ToSigned(result).castedValue);
	}

	public static Coarse Wrap(Coarse value, Coarse minimum, Coarse maximum)
	{
		while (value < minimum)
			value += maximum - minimum;

		while (value >= maximum)
			value -= maximum - minimum;

		return value;
	}

	public static Coarse Sin(Coarse value)
	{
		return Cos(value - PiOver2);
	}

	public static Coarse Cos(Coarse value)
	{
		// 3 terms of taylor series
		value = Wrap(value, -Pi, Pi);
		return One
		       - Pow(value, (Coarse)2) / (Coarse)2
		       + Pow(value, (Coarse)4) / (Coarse)24
		       - Pow(value, (Coarse)6) / (Coarse)720;
	}

	public static Coarse Tan(Coarse value)
	{
		return Sin(value) / Cos(value);
	}

	public static Coarse Acos(Coarse value)
	{
		if (value < -One || value > One)
			throw new ArgumentOutOfRangeException(nameof(value));

		if (IsZero(value))
			return PiOver2;

		var result = Atan(Sqrt(One - value * value) / value);
		return value.RawValue < 0 ? result + Pi : result;
	}

	public static Coarse Atan(Coarse value)
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
		var squared2 = squared * (Coarse)2;
		var squaredPlusOne = squared + One;
		var squaredPlusOne2 = squaredPlusOne * (Coarse)2;
		var dividend = squared2;
		var divisor = squaredPlusOne * (Coarse)3;

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

	public static Coarse Atan2(Coarse y, Coarse x)
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
		var pointTwoEight = new Coarse(rawPointTwoEight);

		Coarse atan;
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

	public static Coarse operator -(Coarse operand)
	{
		return operand.RawValue == int.MinValue ? MaxValue : new Coarse(-operand.RawValue);
	}

	public static bool operator ==(Coarse left, Coarse right)
	{
		return left.RawValue == right.RawValue;
	}

	public static bool operator !=(Coarse left, Coarse right)
	{
		return left.RawValue != right.RawValue;
	}

	public static bool operator >(Coarse left, Coarse right)
	{
		return left.RawValue > right.RawValue;
	}

	public static bool operator <(Coarse left, Coarse right)
	{
		return left.RawValue < right.RawValue;
	}

	public static bool operator >=(Coarse left, Coarse right)
	{
		return left.RawValue >= right.RawValue;
	}

	public static bool operator <=(Coarse left, Coarse right)
	{
		return left.RawValue <= right.RawValue;
	}

	public static explicit operator Coarse(int value)
	{
		return new Coarse(value * RawOne);
	}

	public static explicit operator int(Coarse value)
	{
		return value.RawValue >> DecimalPlaces;
	}
	
	public static explicit operator Coarse(float value)
	{
		return new Coarse((int)(value * RawOne));
	}

	public static explicit operator float(Coarse value)
	{
		return (float)value.RawValue / RawOne;
	}
	
	public static explicit operator Coarse(double value)
	{
		return new Coarse((int)(value * RawOne));
	}

	public static explicit operator double(Coarse value)
	{
		return (double)value.RawValue / RawOne;
	}
	
	public static explicit operator Coarse(decimal value)
	{
		return new Coarse((int)(value * RawOne));
	}

	public static explicit operator decimal(Coarse value)
	{
		return (decimal)value.RawValue / RawOne;
	}

	public static explicit operator Coarse(Fixed value)
	{
		const int shiftSize = 16;
		var rawValue = new ToSigned((uint)(value.Bits >> shiftSize)).castedValue;
		return new Coarse(rawValue);
	}

	public static explicit operator Coarse(Precise value)
	{
		const int shiftSize = 48;
		var rawValue = new ToSigned((uint)(value.Bits >> shiftSize)).castedValue;
		return new Coarse(rawValue);
	}

	public override bool Equals(object? obj)
	{
		return obj is Coarse fixedValue && Equals(fixedValue);
	}

	public override int GetHashCode()
	{
		return RawValue.GetHashCode();
	}

	public bool Equals(Coarse other)
	{
		return RawValue.Equals(other.RawValue);
	}

	public int CompareTo(Coarse other)
	{
		return RawValue.CompareTo(other.RawValue);
	}

	public override string ToString()
	{
		const int decimalsToRender = 5;
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
			var ten = (Coarse)10;
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
			var ten = (Coarse)10;
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
