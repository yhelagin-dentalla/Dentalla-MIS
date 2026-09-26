using Dentalla.Migration.Ident;

try
{
    var options = MigrationOptions.Parse(args);
    options.Validate();

    var service = new IdentRawSnapshotService(options);

    switch (options.Command)
    {
        case "inventory":
            await service.InventoryAsync();
            break;
        case "snapshot":
            await service.SnapshotAsync();
            break;
        case "verify":
            await service.VerifyAsync();
            break;
        case "normalize-core":
            var normalizer = new CoreNormalizationService(options);
            await normalizer.NormalizeAsync();
            break;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("Dentalla.Migration.Ident ERROR");
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine();
    Console.Error.WriteLine(ex);
    Environment.ExitCode = 1;
}
