-- Internal API exception diagnostics (not exposed to clients).
IF OBJECT_ID('ApiExceptionLogs', 'U') IS NULL
CREATE TABLE ApiExceptionLogs (
    LogId uniqueidentifier NOT NULL PRIMARY KEY,
    CreatedAt datetime2 NOT NULL,
    HttpMethod nvarchar(16) NOT NULL,
    RequestPath nvarchar(500) NOT NULL,
    StatusCode int NOT NULL,
    UserId uniqueidentifier NULL,
    IpAddress nvarchar(64) NULL,
    OperationKey nvarchar(80) NULL,
    UserMessage nvarchar(500) NOT NULL,
    ExceptionType nvarchar(200) NOT NULL,
    ExceptionMessage nvarchar(max) NOT NULL,
    InnerExceptionMessage nvarchar(max) NULL,
    StackTrace nvarchar(max) NOT NULL
);
