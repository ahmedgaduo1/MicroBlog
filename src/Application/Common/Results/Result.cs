using System;

namespace MicroBlog.Application.Common.Results;

public class Result
{
    protected Result(bool isSuccess, string message)
    {
        IsSuccess = isSuccess;
        Message = message;
    }

    public bool IsSuccess { get; }
    public string Message { get; }

    public static Result Success(string message = "Operation successful")
    {
        return new Result(true, message);
    }

    public static Result Failure(string message)
    {
        return new Result(false, message);
    }

    public static Result<T> Success<T>(T value, string message = "Operation successful")
    {
        return new Result<T>(value, true, message);
    }

    public static Result<T> Failure<T>(string message)
    {
        return new Result<T>(default!, false, message);
    }
}
