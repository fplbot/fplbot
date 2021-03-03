using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Data;
using Fpl.Data.Repositories;
using FplBot.Core.Extensions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FplBot.Core.Handlers.InternalCommands
{
    public record ConnectEntryToPLPlayer(int EntryId, int PlayerId, int Gameweek) : INotification;
    
    public class ConnectEntryToPLPlayerCommandHandler : INotificationHandler<ConnectEntryToPLPlayer>
    {
        private readonly IVerifiedPLEntriesRepository _repo;
        private readonly IGlobalSettingsClient _settings;
        private readonly IMediator _mediator;
        private readonly ILogger<ConnectEntryToPLPlayerCommandHandler> _logger;

        public ConnectEntryToPLPlayerCommandHandler(
            IVerifiedPLEntriesRepository repo,
            IGlobalSettingsClient settings,
            IMediator mediator,
            ILogger<ConnectEntryToPLPlayerCommandHandler> logger)
        {
            _repo = repo;
            _settings = settings;
            _mediator = mediator;
            _logger = logger;
        }
        
        public async Task Handle(ConnectEntryToPLPlayer notification, CancellationToken cancellationToken)
        {
            var settings = await _settings.GetGlobalSettings();
            _logger.LogInformation($"Inserting hardcoded list");

            var allTeams = settings.Teams.ToList();
            var allPlayers = settings.Players.ToList();

            var plPlayer = allPlayers.Get(notification.PlayerId);

            if (plPlayer != null)
            {
                _logger.LogInformation($"FOUND PLAYER {plPlayer.WebName}, INSERTING PL ENTRY {notification.EntryId}");
                var teamForPLEntry = allTeams.Get(plPlayer.TeamId);

                await _repo.Insert(new VerifiedPLEntry(
                    EntryId: notification.EntryId,
                    TeamId: teamForPLEntry.Id,
                    TeamCode: teamForPLEntry.Code,
                    TeamName: teamForPLEntry.Name,
                    PlayerId: plPlayer.Id,
                    PlayerCode: plPlayer.Code,
                    PlayerWebName: plPlayer.WebName,
                    PlayerFullName: plPlayer.FullName
                ));

                _logger.LogInformation($"{notification.EntryId} inserted");
                
                await _mediator.Publish(new UpdateSelfishStatsForPLEntry(notification.Gameweek, notification.EntryId), cancellationToken);
            }
            else
            {
                _logger.LogError($"PL Player {plPlayer.Id} not found");
            }
        }
    }
}