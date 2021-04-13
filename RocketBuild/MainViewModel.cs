using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using RocketBuild.Build;
using RocketBuild.Deploy;
using RocketBuild.Extensions;
using LinqExtensions = DevExpress.Mvvm.Native.LinqExtensions;
using MessageBox = System.Windows.Forms.MessageBox;

namespace RocketBuild
{
    public class MainViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly DeployHelper _deployHelper = new DeployHelper();
        private readonly BuildHelper _buildHelper = new BuildHelper();

        public MainViewModel()
        {
            RefreshBuilds()
                .ContinueWith(t => RefreshEnvironments()
                    .ContinueWith(t2 => RestoreSelections()));
        }

        public bool IsBusy
        {
            get => GetProperty(() => IsBusy);
            set => SetProperty(() => IsBusy, value);
        }

        public ObservableCollection<DisplayBuild> Builds
        {
            get => GetProperty(() => Builds);
            set => SetProperty(() => Builds, value);
        }

        public ListCollectionView BuildsView
        {
            get => GetProperty(() => BuildsView);
            set => SetProperty(() => BuildsView, value);
        }
        
        public List<DisplayEnvironment> Environments
        {
            get => GetProperty(() => Environments);
            set => SetProperty(() => Environments, value);
        }

        public DisplayEnvironment SelectedEnvironment
        {
            get => GetProperty(() => SelectedEnvironment);
            set
            {
                SetProperty(() => SelectedEnvironment, value);
                CurrentReleases = value?.Releases
                    .OrderBy(r => r.Name)
                    .ToList();
            }
        }

        public List<DisplayRelease> CurrentReleases
        {
            get => GetProperty(() => CurrentReleases);
            set => SetProperty(() => CurrentReleases, value);
        }

        public void OnClosing()
        {
            Settings.GlobalSettings.Current.LastSelectedEnvironment = SelectedEnvironment?.Name;

            if (Builds != null)
            {
                Settings.GlobalSettings.Current.LastSelectedBuildIds.Clear();
                Settings.GlobalSettings.Current.LastSelectedBuildIds.AddRange(Builds
                    .Where(b => b.IsChecked)
                    .Select(b => b.DefinitionId));
            }

            if (Environments != null)
            {
                Settings.GlobalSettings.Current.LastSelectedReleaseIds.Clear();
                Settings.GlobalSettings.Current.LastSelectedReleaseIds.AddRange(Environments
                    .ToDictionary(e => e.Name, e => e.Releases
                        .Where(r => r.IsChecked)
                        .Select(r => r.Id)));
            }

            Settings.GlobalSettings.Save();
        }

        [Command]
        public async Task RefreshBuilds()
        {
            try
            {
                IsBusy = true;

                int[] backupSelectedBuildIds = Builds?
                    .Where(b => b.IsChecked)
                    .Select(b => b.DefinitionId)
                    .ToArray();

                Builds = LinqExtensions.ToObservableCollection(
                    (await _buildHelper.GetBuildsAsync())
                    .OrderBy(b => b.Name));

                var collectionView = new ListCollectionView(Builds);
                collectionView.GroupDescriptions?.Add(new PropertyGroupDescription(nameof(DisplayBuild.Path)));
                BuildsView = collectionView;

                if (backupSelectedBuildIds != null && Builds != null)
                {
                    foreach (DisplayBuild build in Builds)
                    {
                        build.IsChecked = backupSelectedBuildIds.Contains(build.DefinitionId);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [Command]
        public async Task RefreshEnvironments()
        {
            try
            {
                IsBusy = true;

                string backupSelectedEnvironmentName = SelectedEnvironment?.Name;

                int[] backupSelectedReleases = CurrentReleases
                    ?.Where(r => r.IsChecked)
                    .Select(r => r.Id)
                    .ToArray();

                Environments = (await _deployHelper.GetEnvironmentsAsync())
                    .OrderBy(e => e.Name)
                    .ToList();

                if (backupSelectedEnvironmentName != null)
                {
                    SelectedEnvironment = Environments
                        .FirstOrDefault(e => String.Equals(e.Name, backupSelectedEnvironmentName,
                            StringComparison.OrdinalIgnoreCase));
                }

                if (backupSelectedReleases != null && CurrentReleases != null)
                {
                    foreach (DisplayRelease release in CurrentReleases)
                    {
                        release.IsChecked = backupSelectedReleases.Contains(release.Id);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public bool CanQueueBuilds() => Builds?.Any(b => b.IsChecked) == true;

        [Command]
        public async void QueueBuilds()
        {
            try
            {
                DisplayBuild[] buildsToQueue = Builds
                    .Where(b => b.IsChecked)
                    .ToArray();

                if (MessageBox.Show(
                        $@"Do you want to queue builds:{String.Join("\n", buildsToQueue.Select(b => b.Name))}",
                        @"Confirm build", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2)
                    != DialogResult.Yes)
                    return;

                IsBusy = true;

                foreach (DisplayBuild build in buildsToQueue)
                {
                    await _buildHelper.StartBuildAsync(build);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public bool CanQueueDeploy() => CurrentReleases?.Any(b => b.IsChecked) == true;

        [Command]
        public async void QueueDeploy()
        {
            try
            {
                DisplayRelease[] releasesToQueue = CurrentReleases
                    .Where(r => r.IsChecked)
                    .ToArray();

                if (MessageBox.Show(
                        $@"Do you want to deploy releases:{String.Join("\n", releasesToQueue.Select(b => b.Name))}",
                        @"Confirm deploy", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2)
                    != DialogResult.Yes)
                    return;

                IsBusy = true;

                foreach (DisplayRelease release in releasesToQueue)
                {
                    await _deployHelper.StartDeploymentAsync(release);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #region Title commands

        [Command]
        public void MinimizeWindow(Window window) => SystemCommands.MinimizeWindow(window);

        [Command]
        public void MaximizeWindow(Window window)
        {
            if (window.WindowState == WindowState.Maximized)
                SystemCommands.RestoreWindow(window);
            else
                SystemCommands.MaximizeWindow(window);
        }

        [Command]
        public void CloseWindow(Window window) => SystemCommands.CloseWindow(window);

        #endregion

        private void RestoreSelections()
        {
            if (Builds != null)
            {
                foreach (DisplayBuild build in Builds)
                {
                    build.IsChecked = Settings.GlobalSettings.Current.LastSelectedBuildIds.Contains(build.DefinitionId);
                }
            }

            if (Environments != null)
            {
                foreach (DisplayEnvironment environment in Environments)
                {
                    int[] lastSelectedReleases = Settings.GlobalSettings.Current.LastSelectedReleaseIds?
                        .GetValueOrDefault(environment.Name)?
                        .ToArray();

                    foreach (DisplayRelease release in environment.Releases)
                    {
                        release.IsChecked = lastSelectedReleases?.Contains(release.Id) ?? false;
                    }
                }
            }

            SelectedEnvironment = Environments
                ?.FirstOrDefault(e => String.Equals(e.Name, Settings.GlobalSettings.Current.LastSelectedEnvironment,
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}
