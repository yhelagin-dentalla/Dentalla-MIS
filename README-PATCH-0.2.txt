MIS Dentalla Server Sprint 0.2 PATCH

Purpose:
- connect the new MIS to local SQL Server 2022 via Windows Authentication;
- create a separate Dentalla database through EF Core migration;
- seed permission reference data;
- add database status endpoint.

IMPORTANT:
- PZ_TEST is NOT modified and is NOT used as the live Dentalla database.
- extract this archive into C:\Projects\MIS Dentalla with file replacement.
- the patch contains server-side files only and does not overwrite Desktop XAML.

After replacement:
  dotnet build .\Dentalla.Server.slnf
  dotnet run --project .\src\Dentalla.Api\Dentalla.Api.csproj

Then open:
  http://127.0.0.1:5080/api/server/database
