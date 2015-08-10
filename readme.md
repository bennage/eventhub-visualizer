# Partition Visualizer for Azure Event Hubs

This is a hacked together WPF application that makes it a little easier to see how events are following through an [Event Hub][].

You'll need to copy the config template (`EventHubMonitor.Template.config`) and rename it to `EventHubMonitor.config`, and
supply the appropriate values.

:memo: 
_This will consume egress on your targeted event hub and potentially cause contention._
_Also, if there is enough activity on your hub, you'll probably overwhelm this app._


[Event Hub]: http://azure.microsoft.com/en-us/services/event-hubs/