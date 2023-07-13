using AventusSharp.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data
{
    public enum DataErrorCode
    {
        DefaultDMGenericType,
        DMOnlyForceInherit,
        TypeNotStorable,
        TypeTooMuchStorable,
        GenericNotAbstract,
        ParentNotAbstract,
        InfiniteLoop,
        InterfaceNotUnique,
        SelfReferecingDependance,
        DMNotExist,
        DMAlreadyExist,
        MethodNotFound,
        StorageDisconnected,
        StorageNotFound,
        NoConnectionInsideStorage,
        TypeNotExistInsideStorage,
        UnknowError,
        NoItemProvided,
        NoTransactionInProgress,
        WrongType,
        NoTypeIdentifierFoundInsideQuery,
        ItemNoExistInsideStorage,
        ValidationError
    }
    public class DataError : GenericError<DataErrorCode>
    {
        public DataError(DataErrorCode code, string message, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0) : base(code, message, callerPath, callerNo)
        {
        }

        public DataError(DataErrorCode code, Exception exception, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0) : base(code, "", callerPath, callerNo)
        {
            Message = exception.Message;
        }
    }
}
