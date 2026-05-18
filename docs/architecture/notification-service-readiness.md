# Notification Service Readiness

This document records the current Notification service behavior and the architectural decisions applied before deeper Order saga integration.

The goal is to keep Notification usable as an independent delivery service while preserving clear ownership boundaries and operational safety.

## Service Responsibility

Notification owns:

```text
notification message persistence
recipient preference enforcement
template-based message rendering
delivery attempt tracking
scheduled and pending notification dispatch
inbound notification request consumption
email delivery provider abstraction
notification-specific observability and health signals
```

Notification does not own:

```text
order workflow orchestration
payment lifecycle
stock reservation or shipment state
user profile source of truth
cross-service business policy beyond message delivery rules
```

## Current Delivery Model

The service currently supports `Email` notifications.

Each notification message stores:

```text
recipient identity (`RecipientId`)
delivery address (`Recipient`)
notification type
subject/body content
source event id
correlation id
scheduled timestamp
delivery status
delivery attempts
concurrency version
```

`RecipientId` and `Recipient` are intentionally separate:

- `RecipientId` is the preference and idempotency identity
- `Recipient` is the delivery target such as an email address

Do not use the delivery address as the preference identity.

## Idempotency Rule

Notification creation is idempotent when a source event id exists.

Current uniqueness boundary:

```text
SourceEventId + NotificationType + RecipientId
```

Behavior:

1. if a matching notification already exists, creation returns the existing notification id
2. if no matching notification exists, a new record is created
3. if concurrent creation races at the database layer, the handler re-reads and returns the existing record when possible

This avoids duplicate notification rows during message redelivery or retry.

## Concurrency Rule

Notification delivery state changes use an explicit concurrency token:

```text
NotificationMessage.ConcurrencyVersion
```

Purpose:

- protect the send path from overlapping state transitions
- surface real update conflicts as application conflicts
- keep delivery behavior explicit and traceable

Current send guards:

- `Pending` can move to processing
- `Processing` rejects overlapping send attempts
- `Sent` is idempotent and returns a success result without sending again
- `Cancelled` rejects sending
- `Skipped` returns a skipped result without provider delivery

## Recipient Preference Rule

Recipient preferences are owned inside Notification.

Current lookup boundary:

```text
RecipientId + Channel + NotificationType
```

Behavior:

- if no preference exists, delivery is allowed
- if a matching preference exists and is enabled, delivery is allowed
- if a matching preference exists and is disabled:
  - create flow stores the notification as `Skipped`
  - send flow does not call the provider and returns a skipped result

Skip reason should come from the stored preference when available.

## Template Rule

Notification supports template-based message creation through active email templates.

Current resolution rule:

```text
TemplateKey + Channel=Email + IsActive=true
highest Version wins
```

Template placeholders use the format:

```text
{{VariableName}}
```

Behavior:

- subject and body are rendered before message creation
- missing variables fail the request with an application conflict
- template rendering delegates final persistence to the normal create-notification flow

## Inbound Messaging Rule

Notification currently consumes transport-agnostic request contracts defined in the Notification application boundary.

Current inbound contracts:

```text
NotificationRequestedIntegrationEvent
TemplateNotificationRequestedIntegrationEvent
```

Consumer behavior:

1. consume request event
2. map event to application command
3. rely on application handlers for idempotency, preference checks, and persistence

Do not duplicate business rules inside MassTransit consumers.

## Dispatch Rule

Pending notifications are processed by an internal background dispatcher.

Dispatcher behavior:

- polls pending records in batches
- only picks notifications whose `ScheduledAtUtc` is null or due
- sends each picked notification through the existing `SendNotificationCommand`
- logs and continues on conflict or send failures

Current configuration:

```text
NotificationDelivery.DispatcherIntervalSeconds
NotificationDelivery.BatchSize
```

The dispatcher should remain a thin orchestration layer and should not re-implement send logic.

## Provider Rule

The current email provider is a configurable logging-based sender used for local development and controlled simulation.

Current configuration:

```text
EmailDelivery.ProviderName
EmailDelivery.SimulatedLatencyMs
EmailDelivery.FailureRecipients
```

Behavior:

- can simulate latency
- can simulate deterministic recipient-level failures
- returns structured success/failure results without external provider dependency

This is a development-safe provider, not a production email integration.

## Observability Notes

Notification currently emits:

```text
notification created
notification sent
notification failed
notification skipped
notification cancelled
delivery attempt started
delivery attempt succeeded
delivery attempt failed
```

The service also exposes:

```text
/metrics
/health/live
/health/ready
```

## Test Coverage Baseline

Current Notification test coverage includes:

```text
domain state transition rules
create-notification behavior
send-notification behavior
preference-based skipping
template rendering behavior
consumer-to-command mapping
configurable email sender behavior
```

This baseline should be preserved as new delivery channels or new inbound events are added.
