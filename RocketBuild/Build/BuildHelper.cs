using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using RocketBuild.Helpers;

namespace RocketBuild.Build
{
    public class BuildHelper
    {
        public async Task<List<DisplayBuild>> GetBuildsAsync()
        {
            var client = VssClientHelper.GetClient<BuildHttpClient>(
                Settings.GlobalSettings.Current.AccountUrl,
                Settings.GlobalSettings.Current.ApiKey,
                Settings.GlobalSettings.Current.UseSsl);

            var tfsClient = VssClientHelper.GetClient<TfvcHttpClient>(
                Settings.GlobalSettings.Current.AccountUrl,
                Settings.GlobalSettings.Current.ApiKey,
                Settings.GlobalSettings.Current.UseSsl);

            List<BuildDefinitionReference> definitions = await client.GetDefinitionsAsync(Settings.GlobalSettings.Current.Project, name: null);
            var tfsBuilds = await client.GetBuildsAsync(Settings.GlobalSettings.Current.Project, definitions: definitions.Select(d => d.Id), maxBuildsPerDefinition: 1, queryOrder: BuildQueryOrder.FinishTimeDescending);

            var builds = new List<DisplayBuild>();

            TfvcChangesetRef changeset = (await tfsClient.GetChangesetsAsync(Settings.GlobalSettings.Current.Project, top: 1)).FirstOrDefault();

            foreach (BuildDefinitionReference definition in definitions)
            {
                var lastTfsBuild = tfsBuilds.FirstOrDefault(b => b.Definition.Id == definition.Id);
                string lastTfsBuildLink = (lastTfsBuild?.Links.Links["web"] as ReferenceLink)?.Href;

                builds.Add(new DisplayBuild
                {
                    DefinitionId = definition.Id,
                    Path = definition.Path.TrimStart('\\'),
                    Name = definition.Name,
                    LastBuild = lastTfsBuild != null ? Regex.Match(lastTfsBuild.BuildNumber, Settings.GlobalSettings.Current.BuildNameExtractVersionRegex).Value : String.Empty,
                    LastBuildStatus = (DisplayBuildStatus?)lastTfsBuild?.Status,
                    LastBuildResult = (BuildResult?)lastTfsBuild?.Result,
                    LastBuildLink = lastTfsBuildLink,
                    LastCheckin = changeset?.ChangesetId.ToString()
                });
            }

            return builds;
        }

        public async Task StartBuildAsync(DisplayBuild build)
        {
            var client = VssClientHelper.GetClient<BuildHttpClient>(
                Settings.GlobalSettings.Current.AccountUrl,
                Settings.GlobalSettings.Current.ApiKey,
                Settings.GlobalSettings.Current.UseSsl);

            await client.QueueBuildAsync(new Microsoft.TeamFoundation.Build.WebApi.Build
            {
                Definition = new DefinitionReference { Id = build.DefinitionId }
            }, Settings.GlobalSettings.Current.Project);
        }
    }
}
