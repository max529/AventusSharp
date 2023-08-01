
var AventusSharp;
(AventusSharp||(AventusSharp = {}));
(function (AventusSharp) {
const moduleName = `AventusSharp`;


var DataErrorCode;
(function (DataErrorCode) {
    DataErrorCode[DataErrorCode["DefaultDMGenericType"] = 0] = "DefaultDMGenericType";
    DataErrorCode[DataErrorCode["DMOnlyForceInherit"] = 1] = "DMOnlyForceInherit";
    DataErrorCode[DataErrorCode["TypeNotStorable"] = 2] = "TypeNotStorable";
    DataErrorCode[DataErrorCode["TypeTooMuchStorable"] = 3] = "TypeTooMuchStorable";
    DataErrorCode[DataErrorCode["GenericNotAbstract"] = 4] = "GenericNotAbstract";
    DataErrorCode[DataErrorCode["ParentNotAbstract"] = 5] = "ParentNotAbstract";
    DataErrorCode[DataErrorCode["InfiniteLoop"] = 6] = "InfiniteLoop";
    DataErrorCode[DataErrorCode["InterfaceNotUnique"] = 7] = "InterfaceNotUnique";
    DataErrorCode[DataErrorCode["SelfReferecingDependance"] = 8] = "SelfReferecingDependance";
    DataErrorCode[DataErrorCode["DMNotExist"] = 9] = "DMNotExist";
    DataErrorCode[DataErrorCode["DMAlreadyExist"] = 10] = "DMAlreadyExist";
    DataErrorCode[DataErrorCode["MethodNotFound"] = 11] = "MethodNotFound";
    DataErrorCode[DataErrorCode["StorageDisconnected"] = 12] = "StorageDisconnected";
    DataErrorCode[DataErrorCode["StorageNotFound"] = 13] = "StorageNotFound";
    DataErrorCode[DataErrorCode["NoConnectionInsideStorage"] = 14] = "NoConnectionInsideStorage";
    DataErrorCode[DataErrorCode["TypeNotExistInsideStorage"] = 15] = "TypeNotExistInsideStorage";
    DataErrorCode[DataErrorCode["UnknowError"] = 16] = "UnknowError";
    DataErrorCode[DataErrorCode["NoItemProvided"] = 17] = "NoItemProvided";
    DataErrorCode[DataErrorCode["NoTransactionInProgress"] = 18] = "NoTransactionInProgress";
    DataErrorCode[DataErrorCode["WrongType"] = 19] = "WrongType";
    DataErrorCode[DataErrorCode["NoTypeIdentifierFoundInsideQuery"] = 20] = "NoTypeIdentifierFoundInsideQuery";
    DataErrorCode[DataErrorCode["ItemNoExistInsideStorage"] = 21] = "ItemNoExistInsideStorage";
    DataErrorCode[DataErrorCode["ItemAlreadyExist"] = 22] = "ItemAlreadyExist";
    DataErrorCode[DataErrorCode["ValidationError"] = 23] = "ValidationError";
    DataErrorCode[DataErrorCode["GetAllNotAllowed"] = 24] = "GetAllNotAllowed";
    DataErrorCode[DataErrorCode["GetByIdNotAllowed"] = 25] = "GetByIdNotAllowed";
    DataErrorCode[DataErrorCode["GetByIdsNotAllowed"] = 26] = "GetByIdsNotAllowed";
    DataErrorCode[DataErrorCode["WhereNotAllowed"] = 27] = "WhereNotAllowed";
    DataErrorCode[DataErrorCode["CreateNotAllowed"] = 28] = "CreateNotAllowed";
    DataErrorCode[DataErrorCode["UpdateNotAllowed"] = 29] = "UpdateNotAllowed";
    DataErrorCode[DataErrorCode["DeleteNotAllowed"] = 30] = "DeleteNotAllowed";
})(DataErrorCode || (DataErrorCode = {}));
(AventusSharp.Data||(AventusSharp.Data = {}));
AventusSharp.Data.DataErrorCode=DataErrorCode;
})(AventusSharp);
