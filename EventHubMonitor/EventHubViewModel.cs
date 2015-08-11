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
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    public class EventHubViewModel : INotifyPropertyChanged
    {
        private readonly ISubject<long> _aggregatedEventCount = new Subject<long>();
        private readonly List<Task> _listeners = new List<Task>();
        private double _ratePerSecond;
        private readonly NamespaceManager _nsm;

        public EventHubViewModel(string eventHubName, NamespaceManager nsm)
        {
            _nsm = nsm;
            EventHubName = eventHubName;
            Partitions = new ObservableCollection<PartitionViewModel>();
            ConsumerGroups = new ObservableCollection<string>();
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
        public ObservableCollection<string> ConsumerGroups { get; }

        public async Task StartAsync(EventHubClient client, CancellationToken token)
        {
            var groups = await _nsm.GetConsumerGroupsAsync(EventHubName);
            groups.ToList()
                .ForEach(g => ConsumerGroups.Add(g.Name));

            var defaultConsumerGroup = ConsumerGroups.First();

            var runtime = await client.GetRuntimeInformationAsync();
            runtime.PartitionIds
                .Select(partitionId =>
                {
                    Func<Task<PartitionDescription>> f = () => _nsm.GetEventHubPartitionAsync(EventHubName, defaultConsumerGroup, partitionId);
                    return new PartitionViewModel(partitionId, client, f);
                })
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