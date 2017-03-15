# TickHistoryBroker
RabbitMQ Broker which writes tick by tick history (quotes).

Broker connects to "quotes" queue and writes quote to the storage.

## Broker settings

The broker uses following sections from global settings:

```
{
  "SlackNotifications": 
  {
    ...
  },
  "RabbitMq": 
  {
    ...
  },
  "FeedQuotesHistoryWriterBroker": 
  {
    ...
  }
}
```

## Docker configuration

The broker requires following sections to be defined in `docker-compose.yml`:

```
version: '2'
services:
  feedquoteshistorywriterbroker:
    image: lykkex/feedquoteshistorywriterbroker:1.0.0
    container_name: feedquoteshistorywriterbroker
    environment:
      - BROKER_SETTINGS_URL=${SETTINGURL}
```

