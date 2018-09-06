//////////////////////////////////////////////////////////////////////
// Dependencies
//////////////////////////////////////////////////////////////////////
#tool "nuget:?package=OctopusTools"
#tool "nuget:?package=GitVersion.CommandLine&version=4.0.0-beta0014"

using System.Text.RegularExpressions;

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
bool.TryParse(EnvironmentVariable("TF_BUILD"), out bool tfBuild);

var artifactsDirectory = Argument("buildartifacts", tfBuild ? EnvironmentVariable("BUILD_ARTIFACTSTAGINGDIRECTORY") : "./dist");
var buildDirectory = Argument("buildartifacts", tfBuild ? EnvironmentVariable("BUILD_BINARIESDIRECTORY") : "./build");

var sourceVersion = Argument("sourceversion", tfBuild ? EnvironmentVariable("BUILD_SOURCEVERSION") : "localdev");
GitVersion versionInfo = null;

//////////////////////////////////////////////////////////////////////
// Helpers
//////////////////////////////////////////////////////////////////////

Action<string> SubHeader = (str) =>
{
    Information("");
    Information("----------------------------------------");
    Information(str);
    Information("----------------------------------------");
};

Action<string, string> SetVariable = (varName, value) =>
{
    Information($"Setting: {varName} = {value}");
    Information($"##vso[task.setvariable variable={varName};]{value}");
};

//////////////////////////////////////////////////////////////////////
// BUILD STEPS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
    {
        CleanDirectory(buildDirectory);
        CleanDirectory(artifactsDirectory);
    });

Task("UpdateVersionNum")
    .Does(() =>
    {
        GitVersion(new GitVersionSettings { OutputType = GitVersionOutput.BuildServer });
        versionInfo = GitVersion(new GitVersionSettings { OutputType = GitVersionOutput.Json });
    });

Task("Pack")
    .IsDependentOn("UpdateVersionNum")
    .Does(() =>
    {
        var settings = new DotNetCorePackSettings
        {
            Configuration = "Release",
            OutputDirectory = artifactsDirectory,
            IncludeSymbols = true,
            IncludeSource = true,
            ArgumentCustomization = (args) => {
                return args
                    .Append("/p:Version={0}", versionInfo.AssemblySemVer)
                    .Append("/p:AssemblyVersion={0}", versionInfo.AssemblySemVer)
                    .Append("/p:FileVersion={0}", versionInfo.AssemblySemFileVer)
                    .Append("/p:AssemblyInformationalVersion={0}", versionInfo.FullSemVer);
            }
        };

        DotNetCorePack(".", settings);
    });

Task("Default")
    .IsDependentOn("Clean")
    .IsDependentOn("Pack");

//////////////////////////////////////////////////////////////////////
// BUILD
//////////////////////////////////////////////////////////////////////
RunTarget(Argument("target", "Default"));