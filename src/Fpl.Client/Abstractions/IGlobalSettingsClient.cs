using Fpl.Client.Models;

namespace Fpl.Client.Abstractions;

public interface IGlobalSettingsClient
{
    Task<GlobalSettings> GetGlobalSettings();
}