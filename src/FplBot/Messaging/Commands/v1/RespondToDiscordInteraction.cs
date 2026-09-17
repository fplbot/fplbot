namespace FplBot.Messaging.Contracts.Commands.v1;

public record RespondToDiscordInteraction(string InteractionToken, string Title, string Description);
