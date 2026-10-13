namespace PasswordSearch
{
    public partial class MainPage : ContentPage
    {
        private bool? _compactLayout;

        public MainPage()
        {
            InitializeComponent();
        }

        private void OnWorkspaceSizeChanged(object sender, EventArgs e)
        {
            var compact = Workspace.Width < 820;
            if (_compactLayout == compact)
                return;

            _compactLayout = compact;
            Workspace.ColumnDefinitions.Clear();
            Workspace.RowDefinitions.Clear();
            Workspace.ColumnDefinitions.Add(new ColumnDefinition { Width = compact ? GridLength.Star : new GridLength(280) });
            if (!compact)
                Workspace.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            Workspace.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            if (compact)
                Workspace.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

            Workspace.RowSpacing = compact ? 24 : 0;
            Grid.SetColumn(ResultsPanel, compact ? 0 : 1);
            Grid.SetRow(ResultsPanel, compact ? 1 : 0);
        }
    }
}
