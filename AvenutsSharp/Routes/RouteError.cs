﻿using AventusSharp.Tools;
using System;
using System.Runtime.CompilerServices;

namespace AventusSharp.Routes
{
    public enum RouteErrorCode
    {
        UnknowError,
        FormContentTypeUnknown,
        CantGetValueFromBody
    }
    public class RouteError : GenericError<RouteErrorCode>
    {
        public RouteError(RouteErrorCode code, string message, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0) : base(code, message, callerPath, callerNo)
        {
        }

        public RouteError(RouteErrorCode code, Exception exception, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0) : base(code, "", callerPath, callerNo)
        {
            Message = exception.Message;
        }
    }
}
