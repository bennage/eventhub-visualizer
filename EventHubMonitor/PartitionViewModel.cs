namespace EventHubMonitor
{
    using System;
    using System.ComponentModel;
    using System.Reactive.Subjects;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Annotations;
    using Microsoft.ServiceBus.Messaging;

    public class PartitionViewModel : INotifyPropertyChanged
    {
        private readonly EventHubClient _client;
        private readonly ISubject<long> _whenEventReceived = new Subject<long>();
        private long _eventCount;
        private long _lastSequence;
        private DateTime _lastTime;
        private double _ratePerSecond;

        public PartitionViewModel(string partitionId, EventHubClient client)
        {
            _client = client;
            PartitionId = partitionId;
        }

        public IObservable<long> WhenEventReceived => _whenEventReceived;
        public string PartitionId { get; }

        public long EventCount
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
            var partition = await _client.GetPartitionRuntimeInformationAsync(PartitionId)
                    .ConfigureAwait(false);

            _lastSequence = partition.LastEnqueuedSequenceNumber;
            _lastTime = partition.LastEnqueuedTimeUtc;

            await Task.Delay(TimeSpan.FromMilliseconds(new Random().NextDouble() * 1000), token)
                .ConfigureAwait(false); ;

            while (!token.IsCancellationRequested)
            {
                partition = await _client.GetPartitionRuntimeInformationAsync(PartitionId)
                    .ConfigureAwait(false);

                var deltaSequence = partition.LastEnqueuedSequenceNumber - _lastSequence;
                var deltaTime = partition.LastEnqueuedTimeUtc - _lastTime;

                _lastSequence = partition.LastEnqueuedSequenceNumber;
                _lastTime = partition.LastEnqueuedTimeUtc;

                if (deltaSequence > 0 && deltaTime.Ticks > 0)
                {
                    EventCount += deltaSequence;
                    RatePerSecond = deltaSequence / deltaTime.TotalSeconds;

                    _whenEventReceived.OnNext(deltaSequence);
                }

                await Task.Delay(TimeSpan.FromSeconds(3), token)
                    .ConfigureAwait(false);
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}