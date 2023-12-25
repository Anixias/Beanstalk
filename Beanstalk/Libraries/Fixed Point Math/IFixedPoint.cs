using System.Numerics;

namespace FixedPointMath;

public interface IFixedPoint<T> : IAdditionOperators<T, T, T>, IMultiplyOperators<T, T, T>,
	ISubtractionOperators<T, T, T>, IDivisionOperators<T, T, T>, IModulusOperators<T, T, T>,
	IComparable<T>, IEquatable<T>
	where T : IAdditionOperators<T, T, T>?, IMultiplyOperators<T, T, T>?, ISubtractionOperators<T, T, T>?,
	IDivisionOperators<T, T, T>?, IModulusOperators<T, T, T>?
{
	static abstract T One { get; }
	static abstract T Zero { get; }
	
	static abstract T Parse(string number);
}