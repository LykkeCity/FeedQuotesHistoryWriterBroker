# Quotes history service
The service stores a tick history (quotes) and provides API to read it.

The service connects to "quotes" queue and writes quotes to the storage.

## Settings

The service uses following sections from global settings:

```
{
  "SlackNotifications": 
  {
    ...
  },
  "QuotesHistory": 
  {
    ...
  }
}
```

## Logging

The service writes logs to the 
  * Slack thread, and 
  * Azure table ("QuotesHistoryLogs").

If the table does not exist the broker creates it.

## Queues

The broker creates queue with name `lykke.quotefeed.tickhistory`.

## Start

On successful start the broker creates several log records with `level="Info"` to inform that it is up & running.

## Data Storage

The broker stores quotes history in the table `QuotesHistory`.