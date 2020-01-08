using System;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Nuke.Docker;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Docker.DockerTasks;


namespace FplBot.Build
{
    [CheckBuildProjectConfigurations]
    [UnsetVisualStudioEnvironmentVariables]
    // [GitHubActions("CI", 
    //     GitHubActionsImage.Ubuntu1604, 
    //     GitHubActionsImage.WindowsLatest, 
    //     On = new[]
    //     {
    //         GitHubActionsTrigger.Push,
    //     },
    //     OnPushBranches = new []{ "master" },
    //     InvokedTargets = new []{nameof(Pack), nameof(BuildDockerImage)} ,
    //     ImportSecrets = new []
    //     {
    //         "fpl__login", // needed for integration tests
    //         "fpl__password", // needed for integration tests
    //     })]
    // [GitHubActions("Release", 
    //     GitHubActionsImage.Ubuntu1604, 
    //     On = new[]
    //     {
    //         GitHubActionsTrigger.Push,
    //     },
    //     OnPushBranches = new []{ "master" },
    //     InvokedTargets = new []{ nameof(PushDockerImage) },
    //     ImportSecrets = new []
    //     {
    //         "fpl__login", // needed for integration tests
    //         "fpl__password", // needed for integration tests
    //         "DockerHub_Username",
    //         "DockerHub_Password"
    //     })]
    class TheNukeBuild : NukeBuild
    {
        /// Support plugins are available for:
        ///   - JetBrains ReSharper        https://nuke.build/resharper
        ///   - JetBrains Rider            https://nuke.build/rider
        ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
        ///   - Microsoft VSCode           https://nuke.build/vscode

        public static int Main () => Execute<TheNukeBuild>(x => x.Pack);

        [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
        readonly Configuration Configuration = Configuration.Release;

        [Solution] readonly Solution Solution;
        [GitRepository] readonly GitRepository GitRepository;

        AbsolutePath SourceDirectory => RootDirectory / "src";
        AbsolutePath OutputDirectory => RootDirectory / "releases";

        Target Clean => _ => _
            .Executes(() =>
            {
                SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
                EnsureCleanDirectory(OutputDirectory);
            });

        Target Restore => _ => _
            .DependsOn(Clean)
            .Executes(() =>
            {
                DotNetRestore(_ => _
                    .SetProjectFile(Solution));
            });

        Target Build => _ => _
            .DependsOn(Restore)
            .Executes(() =>
            {
                DotNetBuild(_ => _
                    .SetProjectFile(Solution)
                    .SetConfiguration(Configuration)
                    .EnableNoRestore());
            });
        
        Target Test => _ => _
            .DependsOn(Build)
            .Executes(() =>
            {
                DotNetTest(_ => _
                    .SetProjectFile(Solution)
                    .SetConfiguration(Configuration)
                    .EnableNoRestore()
                    .EnableNoBuild());
            });

        string FplClient = "Fpl.Client";
        string FplClientVersion = "0.3.1";
        
        string FplBotSlackExtension = "Slackbot.Net.Extensions.FplBot";
        string FplBotSlackExtensionVersion = "0.3.3";
        
        Target Pack => _ => _
            .DependsOn(Test)
            .Executes(() =>
            {
                DotNetPack(_ => _
                    .SetProject(Solution.GetProject(FplClient))
                    .SetConfiguration(Configuration)
                    .SetVersion(FplClientVersion)
                    .SetOutputDirectory(OutputDirectory)
                    .EnableNoRestore()
                    .EnableNoBuild());

                DotNetPack(_ => _
                    .SetProject($"{SourceDirectory}/{FplBotSlackExtension}/{FplBotSlackExtension}.Release.csproj")
                    .SetConfiguration(Configuration)
                    .SetVersion(FplBotSlackExtensionVersion)
                    .SetOutputDirectory(OutputDirectory));

            });

        Target PublishFplClient => _ => _
            .DependsOn(Pack)
            .Executes(() =>
            {
                DotNetNuGetPush(_ => _
                    .SetTargetPath($"{OutputDirectory}/{FplClient}.{FplClientVersion}.nupkg")
                    .SetSource("https://api.nuget.org/v3/index.json")
                    .SetApiKey(Environment.GetEnvironmentVariable("NUGET_API_KEY")));
            });
        
        Target PublishSlackbotExtension => _ => _
            .DependsOn(Pack)
            .Executes(() =>
            {
                DotNetNuGetPush(_ => _
                    .SetTargetPath($"{OutputDirectory}/{FplBotSlackExtension}.{FplBotSlackExtensionVersion}.nupkg")
                    .SetSource("https://api.nuget.org/v3/index.json")
                    .SetApiKey(Environment.GetEnvironmentVariable("NUGET_API_KEY")));
            });
        
        Target BuildDockerImage => _ => _
            .DependsOn(Test)
            .Executes(() =>
            {
                DockerBuild(_ => _
                    .SetWorkingDirectory(Solution.Directory)
                    .SetTag("fplbot/fplbot:latest")
                    .SetPath(".")
                );
            });
        
        Target PushDockerImage => _ => _
            .DependsOn(BuildDockerImage)
            .Executes(() =>
            {
                
                var username = Environment.GetEnvironmentVariable("DockerHub_Username");
                var password = Environment.GetEnvironmentVariable("DockerHub_Password");
                
                DockerLogin(_ => _
                    .SetArgumentConfigurator(_ => {
                        _.Add("-u {value}", username);
                        _.Add("-p {value}", password, secret:true);
                        return _;
                    }));
                DockerPush(_ => _.SetName("fplbot/fplbot:latest"));
            });
    }
}
