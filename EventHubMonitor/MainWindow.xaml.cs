namespace EventHubMonitor
{
    using System.Configuration;
    using System.Threading;
    using System.Windows;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            EventHubNamespace = ConfigurationManager.AppSettings["EventHubNamespace"];
            EventHubPath = ConfigurationManager.AppSettings["EventHubPath"];
            SasKeyName = ConfigurationManager.AppSettings["EventHubSasKeyName"];
            SasKey = ConfigurationManager.AppSettings["EventHubSasKey"];

            EventHub = new EventHubViewModel(EventHubPath);
            DataContext = EventHub;

            Loaded += MainWindow_Loaded;
        }

        public string EventHubNamespace { get; set; }
        public string EventHubPath { get; set; }
        public string SasKeyName { get; set; }
        public string SasKey { get; set; }
        public EventHubViewModel EventHub { get; }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var endpoint = ServiceBusEnvironment.CreateServiceUri("sb", EventHubNamespace, string.Empty);
            var connection = ServiceBusConnectionStringBuilder.CreateUsingSharedAccessKey(endpoint, SasKeyName, SasKey);

            var hubClient = EventHubClient.CreateFromConnectionString(connection, EventHubPath);
            EventHub.StartAsync(hubClient, CancellationToken.None).ConfigureAwait(false);
        }
    }
}