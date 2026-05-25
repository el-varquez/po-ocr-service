  namespace PoOcr.Domain.Audit;

  public sealed class AuditEvent
  {
      private AuditEvent(
          Guid id,
          string action,
          string actor,
          string message,
          DateTimeOffset occurredAt)
      {
          Id = id;
          Action = action;
          Actor = actor;
          Message = message;
          OccurredAt = occurredAt;
      }

      public Guid Id { get; private set; }
      public string Action { get; private set; }
      public string Actor { get; private set; }
      public string Message { get; private set; }
      public DateTimeOffset OccurredAt { get; private set; }

      public static AuditEvent Create(string action, string actor, string message)
      {
          return new AuditEvent(
              Guid.NewGuid(),
              action.Trim(),
              actor.Trim(),
              message.Trim(),
              DateTimeOffset.UtcNow);
      }
  }