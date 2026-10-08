using Orbit.LocalDemo;

var count = 0;
void Check(bool condition, string scenario)
{
    if (!condition)
        throw new InvalidOperationException($"FAILED: {scenario}");
    count++;
}

foreach (var role in new[] { FleetRole.Viewer, FleetRole.Editor, FleetRole.Admin, (FleetRole)99 })
{
    Check(Simulation.CanRead(role) == (role != (FleetRole)99), $"Read role {role}");
    foreach (var owns in new[] { false, true })
        Check(Simulation.CanModify(role, owns) == (role == FleetRole.Admin || (role == FleetRole.Editor && owns)),
            $"Modify role {role}, owner {owns}");
}

var simulation = new Simulation();
Check(simulation.GetFleets("alex").Count == 2, "Two fleets");
Check(simulation.GetFleets("alex").Single(fleet => fleet.Id == "nebula").Role == "Viewer", "Local role differs");
Check(simulation.GetFleets("visitor").Count == 0, "Visitor has no fleets");
Check(simulation.GetFleets("unknown").Count == 0, "Unknown persona denied");
Check(simulation.GetMissions("blair", "nebula") is null, "Cross-fleet access denied");
Check(simulation.GetMissions("visitor", "aurora") is null, "Nonmember denied");
Check(simulation.GetMissions("alex", "unknown") is null, "Unknown fleet denied");
Check(simulation.GetMission("alex", "aurora", "deep-space") is null, "Cross-fleet mission denied");
Check(simulation.GetMission("alex", "aurora", "unknown") is null, "Unknown mission denied");
Check(simulation.GetMissions("alex", "aurora")?.Count == 2, "Fleet-scoped list");
Check(simulation.GetMission("alex", "nebula", "deep-space")?.WouldAllowUpdateOrDelete == false, "Admin cannot carry role across fleets");
Check(simulation.GetMission("blair", "aurora", "solar-scout")?.WouldAllowUpdateOrDelete == true, "Editor owns mission");
Check(simulation.GetMission("blair", "aurora", "lunar-relay")?.WouldAllowUpdateOrDelete == false, "Editor cannot modify another owner's mission");
Check(simulation.GetMission("alex", "aurora", "solar-scout")?.WouldAllowUpdateOrDelete == true, "Fleet admin can modify another owner's mission");
Console.WriteLine($"Passed {count} deterministic simulation checks.");
if (args.Contains("--http", StringComparer.Ordinal))
    await Orbit.LocalDemo.Checks.HttpChecks.RunAsync();
