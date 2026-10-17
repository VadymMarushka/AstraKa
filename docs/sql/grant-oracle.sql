-- Grants the Oracle role to a real Auth0 account, directly against the Users table.
--
-- Oracle is never created through the API - there is no "onboard as Oracle" endpoint, on
-- purpose (an admin-style role should not be self-service). This script is the only way to get
-- an Oracle account, and it is meant for the dev `AstraKa` database (not `AstraKa_Tests`, which is
-- dropped and reseeded by every test run).
--
-- Fill in the two parameters below, then run the whole script. Idempotent: running it again for
-- the same account just confirms/leaves the row as Oracle; it never creates a duplicate.

DECLARE @ExternalId NVARCHAR(200) = N'auth0|REPLACE_WITH_THE_REAL_SUB';
DECLARE @Email NVARCHAR(256) = N'replace-with-the-real-email@example.com';
DECLARE @Name NVARCHAR(100) = N'Postman Oracle';

DECLARE @OracleRoleId TINYINT = 3; -- UserRole.Oracle - see AstraKa.Domain.Enums.UserRole

IF EXISTS (SELECT 1 FROM dbo.Users WHERE ExternalId = @ExternalId)
BEGIN
    UPDATE dbo.Users
    SET RoleId = @OracleRoleId,
        Email = @Email,
        Name = @Name
    WHERE ExternalId = @ExternalId;
END
ELSE
BEGIN
    INSERT INTO dbo.Users (Id, ExternalId, Email, Name, RoleId)
    VALUES (NEWID(), @ExternalId, @Email, @Name, @OracleRoleId);
END
GO
