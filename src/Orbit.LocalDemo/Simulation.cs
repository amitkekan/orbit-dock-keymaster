namespace Orbit.LocalDemo;

public enum FleetRole { Viewer, Editor, Admin }

public sealed record Persona(string Id, string Name);
public sealed record Fleet(string Id, string Name);
public sealed record Mission(string Id, string FleetId, string OwnerId, string Name);
public sealed record FleetAccess(string Id, string Name, string Role);
public sealed record MissionDecision(string Id, string Name, string OwnerId, bool WouldAllowUpdateOrDelete);

public sealed class Simulation
{
    public IReadOnlyList<Persona> Personas { get; } = Array.AsReadOnly(new[]
    {
        new Persona("alex", "Alex"), new Persona("blair", "Blair"),
        new Persona("casey", "Casey"), new Persona("visitor", "Visitor (no membership)")
    });

    private readonly Fleet[] fleets =
    [
        new("aurora", "Aurora Fleet"), new("nebula", "Nebula Fleet")
    ];

    private readonly Mission[] missions =
    [
        new("lunar-relay", "aurora", "alex", "Lunar Relay"),
        new("solar-scout", "aurora", "blair", "Solar Scout"),
        new("deep-space", "nebula", "casey", "Deep Space Beacon")
    ];

    private readonly Dictionary<(string Persona, string Fleet), FleetRole> memberships = new()
    {
        [("alex", "aurora")] = FleetRole.Admin,
        [("alex", "nebula")] = FleetRole.Viewer,
        [("blair", "aurora")] = FleetRole.Editor,
        [("casey", "nebula")] = FleetRole.Admin
    };

    public IReadOnlyList<FleetAccess> GetFleets(string personaId) =>
        fleets.Where(fleet => memberships.ContainsKey((personaId, fleet.Id)))
            .Select(fleet => new FleetAccess(fleet.Id, fleet.Name, memberships[(personaId, fleet.Id)].ToString()))
            .ToArray();

    public IReadOnlyList<MissionDecision>? GetMissions(string personaId, string fleetId)
    {
        if (!memberships.TryGetValue((personaId, fleetId), out var role) || !CanRead(role))
            return null;

        return missions.Where(mission => mission.FleetId == fleetId)
            .Select(mission => new MissionDecision(mission.Id, mission.Name, mission.OwnerId,
                CanModify(role, mission.OwnerId == personaId)))
            .ToArray();
    }

    public MissionDecision? GetMission(string personaId, string fleetId, string missionId) =>
        GetMissions(personaId, fleetId)?.SingleOrDefault(mission => mission.Id == missionId);

    public static bool CanRead(FleetRole role) => role is FleetRole.Viewer or FleetRole.Editor or FleetRole.Admin;

    public static bool CanModify(FleetRole role, bool ownsMission) =>
        role == FleetRole.Admin || (role == FleetRole.Editor && ownsMission);
}
