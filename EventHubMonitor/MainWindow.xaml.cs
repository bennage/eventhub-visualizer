namespace EventHubMonitor
{
    using System.Configuration;
    using System.Threading;
    using System.Windows;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    public partial class MainWindow : Window
    {
        private readonly string _connectionString;

        public MainWindow()
        {
            InitializeComponent();

            EventHubNamespace = ConfigurationManager.AppSettings["EventHubNamespace"];
            EventHubPath = ConfigurationManager.AppSettings["EventHubPath"];
            SasKeyName = ConfigurationManager.AppSettings["EventHubSasKeyName"];
            SasKey = ConfigurationManager.AppSettings["EventHubSasKey"];

            var endpoint = ServiceBusEnvironment.CreateServiceUri("sb", EventHubNamespace, string.Empty);
            _connectionString = ServiceBusConnectionStringBuilder.CreateUsingSharedAccessKey(endpoint, SasKeyName,
                SasKey);

            var nsm = NamespaceManager.CreateFromConnectionString(_connectionString);

            EventHub = new EventHubViewModel(EventHubPath, nsm);

            DataContext = EventHub;

            Loaded += MainWindow_Loaded;
        }

        public static string EventHubNamespace { get; private set; }
        public static string EventHubPath { get; private set; }
        public static string SasKeyName { get; private set; }
        public static string SasKey { get; private set; }
        public EventHubViewModel EventHub { get; }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var client = EventHubClient.CreateFromConnectionString(_connectionString, EventHubPath);
            EventHub.StartAsync(client, CancellationToken.None).ConfigureAwait(false);
        }
    }
}