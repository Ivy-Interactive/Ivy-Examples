using Ivy.Cli.Commands.Config;
using Ivy.Cli.Commands.NuGet;
using Ivy.Cli.Commands.Sliplane;
using Ivy.Cli.Commands.Sliplane.Credentials;
using Ivy.Cli.Commands.Sliplane.OAuth;
using Ivy.Cli.Commands.Sliplane.Projects;
using Ivy.Cli.Commands.Sliplane.Servers;
using Ivy.Cli.Commands.Sliplane.Services;
using Ivy.Cli.Commands.Tendrils;
using Spectre.Console.Cli;

// ─────────────────────────────────────────────────────────────────────────────
// Command description style guide
//
// Top-level branches: "<Action> <project> <scope>" — e.g. "Manage Sliplane resources".
// Sub-branches:       "Manage <resource-plural>"   — context (Sliplane/...) comes from path.
// Leaf commands follow a verb-first imperative pattern in Title case:
//   list   → "List <resource-plural>"
//   get    → "Get <resource>"
//   create → "Create <resource>"
//   update → "Update <resource>"
//   delete → "Delete <resource>"
//   other  → "<Verb> <resource>"   (e.g. "Pause service", "Rescale server")
// Keep descriptions short (≤ 60 chars). Put usage examples in README, not in descriptions.
// ─────────────────────────────────────────────────────────────────────────────

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("ivy-examples");
    config.SetApplicationVersion("0.1.0");

    // ── Config (CLI-wide) ─────────────────────────────────────────────
    config.AddBranch("config", cfg =>
    {
        cfg.SetDescription("Manage saved CLI configuration (~/.ivy/config.json)");
        cfg.AddCommand<ConfigListCommand>("list")
            .WithDescription("List config values");
        cfg.AddCommand<ConfigGetCommand>("get")
            .WithDescription("Get config value");
        cfg.AddCommand<ConfigSetCommand>("set")
            .WithDescription("Set config value");
        cfg.AddCommand<ConfigUnsetCommand>("unset")
            .WithDescription("Unset config value");
        cfg.AddCommand<ConfigClearCommand>("clear")
            .WithDescription("Clear all config");
    });

    // ── Sliplane ──────────────────────────────────────────────────────
    config.AddBranch("sliplane", sliplane =>
    {
        sliplane.SetDescription("Manage Sliplane resources");

        sliplane.AddCommand<MeCommand>("me")
            .WithDescription("Get current identity");

        sliplane.AddBranch("projects", branch =>
        {
            branch.SetDescription("Manage projects");
            branch.AddCommand<ListProjectsCommand>("list")
                .WithDescription("List projects");
            branch.AddCommand<CreateProjectCommand>("create")
                .WithDescription("Create project");
            branch.AddCommand<UpdateProjectCommand>("update")
                .WithDescription("Update project");
            branch.AddCommand<DeleteProjectCommand>("delete")
                .WithDescription("Delete project");
        });

        sliplane.AddBranch("servers", branch =>
        {
            branch.SetDescription("Manage servers");
            branch.AddCommand<ListServersCommand>("list")
                .WithDescription("List servers");
            branch.AddCommand<GetServerCommand>("get")
                .WithDescription("Get server");
            branch.AddCommand<CreateServerCommand>("create")
                .WithDescription("Create server");
            branch.AddCommand<DeleteServerCommand>("delete")
                .WithDescription("Delete server");
            branch.AddCommand<RescaleServerCommand>("rescale")
                .WithDescription("Rescale server (scale up only)");
            branch.AddCommand<ServerMetricsCommand>("metrics")
                .WithDescription("Get server metrics");
            branch.AddCommand<ListServerVolumesCommand>("volumes")
                .WithDescription("List server volumes");
            branch.AddCommand<CreateServerVolumeCommand>("create-volume")
                .WithDescription("Create server volume");
        });

        sliplane.AddBranch("services", branch =>
        {
            branch.SetDescription("Manage services");
            branch.AddCommand<ListServicesCommand>("list")
                .WithDescription("List services");
            branch.AddCommand<GetServiceCommand>("get")
                .WithDescription("Get service");
            branch.AddCommand<CreateServiceCommand>("create")
                .WithDescription("Create service");
            branch.AddCommand<UpdateServiceCommand>("update")
                .WithDescription("Update service");
            branch.AddCommand<DeleteServiceCommand>("delete")
                .WithDescription("Delete service");
            branch.AddCommand<PauseServiceCommand>("pause")
                .WithDescription("Pause service");
            branch.AddCommand<UnpauseServiceCommand>("unpause")
                .WithDescription("Unpause service");
            branch.AddCommand<DeployServiceCommand>("deploy")
                .WithDescription("Deploy service");
            branch.AddCommand<ServiceLogsCommand>("logs")
                .WithDescription("Get service logs");
            branch.AddCommand<ServiceMetricsCommand>("metrics")
                .WithDescription("Get service metrics");
            branch.AddCommand<ServiceEventsCommand>("events")
                .WithDescription("Get service events");
            branch.AddCommand<AddDomainCommand>("add-domain")
                .WithDescription("Add service domain");
            branch.AddCommand<RemoveDomainCommand>("remove-domain")
                .WithDescription("Remove service domain");
        });

        sliplane.AddBranch("credentials", branch =>
        {
            branch.SetDescription("Manage registry credentials");
            branch.AddCommand<ListCredentialsCommand>("list")
                .WithDescription("List credentials");
            branch.AddCommand<GetCredentialsCommand>("get")
                .WithDescription("Get credentials");
            branch.AddCommand<CreateCredentialsCommand>("create")
                .WithDescription("Create credentials");
            branch.AddCommand<UpdateCredentialsCommand>("update")
                .WithDescription("Update credentials");
            branch.AddCommand<DeleteCredentialsCommand>("delete")
                .WithDescription("Delete credentials");
        });

        sliplane.AddBranch("oauth", branch =>
        {
            branch.SetDescription("Manage OAuth clients");
            branch.AddCommand<ListOAuthClientsCommand>("list")
                .WithDescription("List OAuth clients");
            branch.AddCommand<GetOAuthClientCommand>("get")
                .WithDescription("Get OAuth client");
            branch.AddCommand<UpdateOAuthClientCommand>("update")
                .WithDescription("Update OAuth client");
            branch.AddCommand<ListOAuthClientUsersCommand>("users")
                .WithDescription("List OAuth client users");
        });
    });

    // ── NuGet ─────────────────────────────────────────────────────────
    config.AddBranch("nuget", nuget =>
    {
        nuget.SetDescription("Manage NuGet package statistics");
        nuget.AddCommand<NuGetSummaryCommand>("summary")
            .WithDescription("Overall stats summary");
        nuget.AddCommand<NuGetStarsCommand>("stars")
            .WithDescription("Star counts per package");
        nuget.AddCommand<NuGetStarredCommand>("starred")
            .WithDescription("List starred packages");
        nuget.AddCommand<NuGetUnstarredCommand>("unstarred")
            .WithDescription("List unstarred packages");
        nuget.AddCommand<NuGetDownloadsCommand>("downloads")
            .WithDescription("Download counts per package");
        nuget.AddCommand<NuGetDownloadsHistoryCommand>("downloads-history")
            .WithDescription("Download history over time");
    });

    // ── Tendril ───────────────────────────────────────────────────────
    // Requires: TENDRIL_BASE_URL (+ TENDRIL_API_KEY if server has key configured)
    //           SLIPLANE_API_KEY (for status/servers/projects that forward to Sliplane)
    config.AddBranch("tendril", tendril =>
    {
        tendril.SetDescription("Manage Tendril deployments");
        tendril.AddCommand<DeployTendrilCommand>("deploy")
            .WithDescription("Deploy Tendril instance");
        tendril.AddCommand<GetTendrilStatusCommand>("status")
            .WithDescription("Get Tendril status");
        tendril.AddCommand<ListTendrilServersCommand>("servers")
            .WithDescription("List available servers");
        tendril.AddCommand<ListTendrilProjectsCommand>("projects")
            .WithDescription("List available projects");
    });
});

return app.Run(args);
