using System.Threading.Tasks;

namespace FplBot.WebApi.Data
{
    public interface ISlackTeamRepository
    {
        Task Insert(SlackTeam slackTeam);
        Task Delete(string teamId);
    }
}