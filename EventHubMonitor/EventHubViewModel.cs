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
    using Annotations;
    using Microsoft.ServiceBus.Messaging;

    public class EventHubViewModel : INotifyPropertyChanged
    {
        private readonly ISubject<long> _aggregatedEventCount = new Subject<long>();
        private readonly List<Task> _listeners = new List<Task>();
        private double _ratePerSecond;

        public EventHubViewModel(string eventHubName)
        {
            EventHubName = eventHubName;
            Partitions = new ObservableCollection<PartitionViewModel>();
        }

        public string EventHubName { get; private set; }

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

        public async Task StartAsync(EventHubClient hubClient, CancellationToken token)
        {
            var runtime = await hubClient.GetRuntimeInformationAsync();
            runtime.PartitionIds
                .Select(partitionId => new PartitionViewModel(partitionId, hubClient))
                .ToList()
                .ForEach(p => Partitions.Add(p));

            foreach (var partition in Partitions)
            {
                partition.WhenEventReceived.Subscribe(x => { _aggregatedEventCount.OnNext(x); });

                _listeners.Add(partition.StartAsync(CancellationToken.None));
            }

            _aggregatedEventCount
                .Buffer(TimeSpan.FromSeconds(5))
                .TimeInterval()
                .Select(x => x.Value.Sum() / x.Interval.TotalSeconds)
                .Subscribe(rate => RatePerSecond = rate);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}