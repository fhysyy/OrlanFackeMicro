using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeMicro.Interfaces
{
    using Orleans;
    using System;
    using System.Collections.Generic;

    namespace FakeMicro.Interfaces
    {
        [GenerateSerializer]
        public class BaseResultModel
        {
            [Id(0)]
            public bool Success { get; set; } = true;

            [Id(1)]
            public int Code { get; set; } = 200;

            [Id(2)]
            public string? ErrorMessage { get; set; }

            //[Id(3)]
            [Id(3)]
            public  Object Data { get; set; }

            [Id(4)]
            public int TotalCount { get; set; } = 0;

            [Id(5)]
            public string? TraceId { get; set; }= Guid.NewGuid().ToString();

            public static BaseResultModel SuccessResult(object? data = null, string message = "")
            {
                return new BaseResultModel
                {
                    Success = true,
                    Code = 200,
                    ErrorMessage = message,
                    TraceId = Guid.NewGuid().ToString(),
                    Data = data
                };
            }

            public static BaseResultModel FailedResult(object? data = null, string message = "", int code = 500)
            {
                return new BaseResultModel
                {
                    Success = false,
                    Code = code,
                    TraceId = Guid.NewGuid().ToString(),
                    ErrorMessage = message,
                    Data = data
                };
            }
        }

        [GenerateSerializer]
        public class BaseResultModel<T>
        {
            [Id(0)]
            public bool Success { get; set; } = true;

            [Id(1)]
            public int Code { get; set; } = 200;

            [Id(2)]
            public string? ErrorMessage { get; set; }

            [Id(3)]
            public T? Data { get; set; }

            [Id(4)]
            public int TotalCount { get; set; } = 0;

            [Id(5)]
            public string? TraceId { get; set; }

            public static BaseResultModel<T> SuccessResult(T? data = default, string message = "")
            {
                return new BaseResultModel<T>
                {
                    Success = true,
                    Code = 200,
                    TraceId = Guid.NewGuid().ToString(),
                    ErrorMessage = message,
                    Data = data
                };
            }

            public static BaseResultModel<T> FailedResult(T? data = default, string message = "", int code = 500)
            {
                return new BaseResultModel<T>
                {
                    Success = false,
                    Code = code,
                    TraceId = Guid.NewGuid().ToString(),
                    ErrorMessage = message,
                    Data = data
                };
            }
        }
    }
}
