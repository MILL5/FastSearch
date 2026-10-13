namespace PasswordSearch
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            return new Window(new MainPage())
            {
                Title = "FastSearch Password Search",
                Width = 1024,
                Height = 768
            };
        }
    }
}
