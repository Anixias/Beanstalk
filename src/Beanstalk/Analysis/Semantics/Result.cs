namespace Beanstalk.Analysis.Semantics;

public sealed class Result<TResult, TError>
{
	public readonly bool hasError;
	public readonly TResult? result;
	public readonly TError? error;

	private Result(bool hasError, TResult? result, TError? error)
	{
		this.hasError = hasError;
		this.result = result;
		this.error = error;
	}

	public static Result<TResult, TError> FromSuccess(TResult result)
	{
		return new Result<TResult, TError>(false, result, default);
	}

	public static Result<TResult, TError> FromError(TError error)
	{
		return new Result<TResult, TError>(true, default, error);
	}
}