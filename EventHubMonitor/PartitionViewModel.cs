namespace EventHubMonitor
{
    using System;
    using System.Collections.Generic;
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

    public class PartitionViewModel : INotifyPropertyChanged
    {
        private readonly EventHubConsumerGroup _consumerGroup;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private int _eventCount;
        private double _ratePerSecond;
        private object _task;

        public PartitionViewModel(string partitionId, EventHubConsumerGroup consumerGroup)
        {
            _consumerGroup = consumerGroup;
            PartitionId = partitionId;
        }

        private readonly ISubject<int> _whenEventReceived = new Subject<int>();
        public IObservable<int> WhenEventReceived => _whenEventReceived;

        public string PartitionId { get; }

        public int EventCount
        {
            get { return _eventCount; }
            private set
            {
                if (value == _eventCount) return;
                _eventCount = value;
                OnPropertyChanged();
            }
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

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task StartAsync(CancellationToken token)
        {
            var receiver = await _consumerGroup.CreateReceiverAsync(PartitionId, DateTime.UtcNow).ConfigureAwait(false);
            _task = ListenAsync(receiver, CancellationToken.None).ConfigureAwait(false);

            _subscriptions.Add(_whenEventReceived
                .Buffer(TimeSpan.FromSeconds(5))
                .TimeInterval()
                .Select(x => x.Value.Sum()/x.Interval.TotalSeconds)
                .Subscribe(rate => Dispatcher.CurrentDispatcher.Invoke(() => { RatePerSecond = rate; })));
        }

        private async Task ListenAsync(EventHubReceiver receiver, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var events = await receiver
                    .ReceiveAsync(5000, TimeSpan.FromSeconds(3))
                    .ConfigureAwait(false);

                var count = events.Count();
                _whenEventReceived.OnNext(count);
                EventCount += count;

                await Task.Yield();
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}