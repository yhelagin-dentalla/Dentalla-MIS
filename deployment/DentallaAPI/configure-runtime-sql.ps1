#Requires -RunAsAdministrator
param(
    [string]$server = 'localhost',
    [string]$database = 'Dentalla',
    [string]$serviceName = 'DentallaAPI'
)

$ErrorActionPreference = 'Stop'
$login = "NT SERVICE\$serviceName"

Write-Host "Configuring SQL runtime identity: $login"

$connectionString = "Server=$server;Database=master;Integrated Security=True;TrustServerCertificate=True;Application Name=DentallaAPI Installer"
$connection = New-Object System.Data.SqlClient.SqlConnection $connectionString
$connection.Open()

try {
    $escapedLogin = $login.Replace(']', ']]')
    $escapedDatabase = $database.Replace(']', ']]')
    $loginLiteral = $login.Replace("'", "''")

    $sql = @"
IF DB_ID(N'$($database.Replace("'", "''"))') IS NULL
    THROW 51000, 'Dentalla database does not exist. Apply EF migrations before provisioning the runtime identity.', 1;

IF SUSER_ID(N'$loginLiteral') IS NULL
    CREATE LOGIN [$escapedLogin] FROM WINDOWS;

USE [$escapedDatabase];

IF USER_ID(N'$loginLiteral') IS NULL
    CREATE USER [$escapedLogin] FOR LOGIN [$escapedLogin];

IF IS_ROLEMEMBER(N'db_datareader', N'$loginLiteral') <> 1
    ALTER ROLE [db_datareader] ADD MEMBER [$escapedLogin];

IF IS_ROLEMEMBER(N'db_datawriter', N'$loginLiteral') <> 1
    ALTER ROLE [db_datawriter] ADD MEMBER [$escapedLogin];
"@

    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $command.CommandTimeout = 60
    [void]$command.ExecuteNonQuery()
}
finally {
    $connection.Dispose()
}

Write-Host "SQL runtime access configured for $login on database $database."
Write-Host 'Granted database roles: db_datareader, db_datawriter. No CREATE DATABASE, db_owner or sysadmin rights were granted.'
