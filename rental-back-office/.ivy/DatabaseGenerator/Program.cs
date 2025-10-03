using RentalBackOffice;
using System.ComponentModel;
using Spectre.Console.Cli;

var app = new CommandApp<GenerateCommand>();

app.Configure(config =>
{
    config.PropagateExceptions();
    config.UseStrictParsing();
});

return await app.RunAsync(args);

public class GenerateCommand : AsyncCommand<GenerateCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("--verbose")]
        [Description("Enable verbose output")]
        public bool Verbose { get; set; } = false;

        [CommandOption("--output")]
        [Description("Output directory for generated files.")]
        public string? OutputDirectory { get; set; } = null;

        [CommandOption("--data-provider")]
        [Description("The database provider")]
        public DatabaseProvider? DataProvider { get; set; }

        [CommandOption("--connection-string")]
        [Description("Connection string for the database.")]
        public string? ConnectionString { get; set; } = null;

        [CommandOption("--yes-to-all")]
        [Description("Automatically answer yes to all questions.")]
        public bool YesToAll { get; set; } = false;
        
        [CommandOption("--delete-database")]
        [Description("Delete the existing database before generating a new one. Dangerous operation!")]
        public bool DeleteDatabase { get; set; } = false;
        
        [CommandOption("--seed-database")]
        [Description("Seed the database with initial data. ")]
        public bool SeedDatabase { get; set; } = false;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var generator = new DatabaseGenerator();
        try {
            return await generator.GenerateAsync(
                        typeof(DataContext),
                        typeof(DataSeeder),
                        settings.Verbose,
                        settings.YesToAll,
                        settings.DeleteDatabase,
                        settings.SeedDatabase,
                        settings.OutputDirectory,
                        settings.ConnectionString,
                        settings.DataProvider
                    );
        } 
        catch (Exception ex) 
        {
            ex = ex.InnerException ?? ex;
            await Console.Error.WriteLineAsync($"Error: {ex.Message}");
            await Console.Error.WriteLineAsync($"Stack Trace:n{ex.StackTrace}");
            return 1;
        }
    }
}