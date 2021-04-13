using DevExpress.Mvvm;

namespace RocketBuild.Build
{
    public class DisplayBuild : BindableBase
    {
        public bool IsChecked
        {
            get => GetProperty(() => IsChecked);
            set => SetProperty(() => IsChecked, value);
        }

        public string Path { get; set; }

        public int DefinitionId { get; set; }

        public string Name { get; set; }

        public string LastBuild { get; set; }

        public DisplayBuildStatus? LastBuildStatus { get; set; }

        public BuildResult? LastBuildResult { get; set; }

        public string LastBuildLink { get; set; }

        public string LastCheckin { get; set; }
    }
}
