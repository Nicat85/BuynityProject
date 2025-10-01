using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace OnlineShppingSystem.Application.Shared
{
    public class BaseResponse
    {
        public bool IsSuccess { get; set; }
        public List<string> Errors { get; set; } = new();
        public string? Message { get; set; }
        public HttpStatusCode StatusCode { get; set; }

        public BaseResponse(HttpStatusCode statusCode) => StatusCode = statusCode;

        public BaseResponse(string message, HttpStatusCode statusCode)
        {
            Message = message;
            IsSuccess = false;
            StatusCode = statusCode;
        }

        public BaseResponse(string message, bool isSuccess, HttpStatusCode statusCode)
        {
            Message = message;
            IsSuccess = isSuccess;
            StatusCode = statusCode;
        }

        public static BaseResponse Success(string message = "Success.")
            => new BaseResponse(message, true, HttpStatusCode.OK);

        public static BaseResponse Fail(string message)
            => new BaseResponse(message, false, HttpStatusCode.BadRequest);

        public static BaseResponse Fail(string message, HttpStatusCode statusCode)
            => new BaseResponse(message, false, statusCode);

        public static BaseResponse Fail(IEnumerable<string> messages)
        {
            var response = new BaseResponse("Validation failed.", false, HttpStatusCode.BadRequest);
            response.Errors = messages.ToList();
            return response;
        }
    }

    public class BaseResponse<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();
        public string? Message { get; set; }
        public HttpStatusCode StatusCode { get; set; }

        public BaseResponse() { }
        public BaseResponse(HttpStatusCode statusCode) => StatusCode = statusCode;

        public BaseResponse(string message, HttpStatusCode statusCode)
        {
            Message = message;
            IsSuccess = false;
            StatusCode = statusCode;
        }

        public BaseResponse(string message, bool isSuccess, HttpStatusCode statusCode)
        {
            Message = message;
            IsSuccess = isSuccess;
            StatusCode = statusCode;
        }

        public BaseResponse(string message, T? data, bool isSuccess, HttpStatusCode statusCode)
        {
            Message = message;
            Data = data;
            IsSuccess = isSuccess;
            StatusCode = statusCode;
        }

        public static BaseResponse<T> Success(string message = "Success.")
            => new BaseResponse<T>(message, default, true, HttpStatusCode.OK);

        public static BaseResponse<T> CreateSuccess(T data, string message = "Success.")
            => new BaseResponse<T>(message, data, true, HttpStatusCode.OK);

        public static BaseResponse<T> CreateSuccess(T data, string message, HttpStatusCode statusCode)
            => new BaseResponse<T>(message, data, true, statusCode);

        public static BaseResponse<T> Fail(string message)
            => new BaseResponse<T>(message, false, HttpStatusCode.BadRequest);

        public static BaseResponse<T> Fail(string message, HttpStatusCode statusCode)
            => new BaseResponse<T>(message, false, statusCode);

        public static BaseResponse<T> Fail(IEnumerable<string> messages)
        {
            var response = new BaseResponse<T>("Validation failed.", false, HttpStatusCode.BadRequest);
            response.Errors = messages.ToList();
            return response;
        }
    }
}