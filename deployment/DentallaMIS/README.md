# DentallaMIS desktop deployment

`DentallaMIS.exe` is the ordinary Windows desktop entry point for Dentalla MIS.

Architecture is unchanged:

`DentallaMIS.exe (Desktop) -> DentallaAPI Windows service -> Application -> Infrastructure -> SQL Server`

The Desktop executable never connects directly to SQL Server.

## Build the executable

From the repository root:

```powershell
powershell -executionpolicy bypass -file .\deployment\DentallaMIS\publish-desktop.ps1
```

Output:

```text
deployment\DentallaMIS\publish\DentallaMIS.exe
```

The publish is Windows x64, self-contained and single-file, so the workstation does not need a separately installed .NET runtime for the Desktop client.

`DentallaAPI` must already be installed/running and reachable at the configured API endpoint. On the current development workstation the endpoint is `http://127.0.0.1:5080`.

The generated `publish` directory is deployment output and must not be committed to Git.
