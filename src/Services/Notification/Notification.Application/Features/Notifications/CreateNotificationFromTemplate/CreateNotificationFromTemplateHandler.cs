using System.Text.RegularExpressions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Notification.Application.Abstractions.Persistence;
using Notification.Application.Common.Exceptions;
using Notification.Domain.Enums;

namespace Notification.Application.Features.Notifications.CreateNotificationFromTemplate;

public partial class CreateNotificationFromTemplateHandler(
    INotificationDbContext context,
    ISender sender) : IRequestHandler<CreateNotificationFromTemplateCommand, Guid>
{
    public async Task<Guid> Handle(CreateNotificationFromTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await context.NotificationTemplates
            .AsNoTracking()
            .Where(x => x.Key == request.TemplateKey &&
                        x.Channel == NotificationChannel.Email &&
                        x.IsActive)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (template is null)
        {
            throw new NotFoundException($"Active notification template '{request.TemplateKey}' was not found.");
        }

        var subject = RenderTemplate(template.SubjectTemplate, request.Variables, request.TemplateKey, "subject");
        var body = RenderTemplate(template.BodyTemplate, request.Variables, request.TemplateKey, "body");

        return await sender.Send(
            new CreateNotification.CreateNotificationCommand(
                request.NotificationType,
                request.RecipientId,
                request.Recipient,
                subject,
                body,
                request.SourceEventId,
                request.CorrelationId,
                request.ScheduledAtUtc),
            cancellationToken);
    }

    private static string RenderTemplate(
        string template,
        IReadOnlyDictionary<string, string> variables,
        string templateKey,
        string contentName)
    {
        var missingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var rendered = TemplateVariableRegex().Replace(template, match =>
        {
            var key = match.Groups[1].Value;

            if (variables.TryGetValue(key, out var value))
            {
                return value;
            }

            missingKeys.Add(key);
            return match.Value;
        });

        if (missingKeys.Count > 0)
        {
            throw new ConflictException(
                $"Notification template '{templateKey}' {contentName} is missing variables: {string.Join(", ", missingKeys.OrderBy(x => x))}.");
        }

        return rendered;
    }

    [GeneratedRegex("\\{\\{\\s*([A-Za-z0-9_]+)\\s*\\}\\}")]
    private static partial Regex TemplateVariableRegex();
}
