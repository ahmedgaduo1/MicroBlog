using System;

namespace MicroBlog.Application.Common.Results;

public class Result<T> : Result
{
    internal Result(T value, bool isSuccess, string message)
        : base(isSuccess, message)
    {
        Value = value;
    }

    public T Value { get; }
}
