namespace EventHubMonitor
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Threading;
    using Annotations;
    using Microsoft.ServiceBus.Messaging;

    public class EventHubViewModel : INotifyPropertyChanged
    {
        private readonly ISubject<int> _aggregatedEventCount = new Subject<int>();
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private double _ratePerSecond;

        public EventHubViewModel(string eventHubName)
        {
            EventHubName = eventHubName;
            Partitions = new ObservableCollection<PartitionViewModel>();
        }

        public double RatePerSecond
        {
            get { return _ratePerSecond; }
            set
            {
                if (value.Equals(_ratePerSecond)) return;
                _ratePerSecond = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<PartitionViewModel> Partitions { get; }
        public string EventHubName { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task StartAsync(EventHubClient hubClient, CancellationToken token)
        {
            var consumerGroup = hubClient.GetDefaultConsumerGroup();

            var runtime = await hubClient.GetRuntimeInformationAsync();

            runtime.PartitionIds
                .Select(partitionId => new PartitionViewModel(partitionId, consumerGroup))
                .ToList()
                .ForEach(p => Partitions.Add(p));

            foreach (var partition in Partitions)
            {
                _subscriptions.Add(partition.ObservableEventCount.Subscribe(x => { _aggregatedEventCount.OnNext(x); }));

                partition.StartAsync(CancellationToken.None).ConfigureAwait(false);
            }

            _subscriptions.Add(_aggregatedEventCount
                .Buffer(TimeSpan.FromSeconds(5))
                .TimeInterval()
                .Select(x => x.Value.Sum()/x.Interval.TotalSeconds)
                .Subscribe(rate => Dispatcher.CurrentDispatcher.Invoke(() => { RatePerSecond = rate; })));
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}