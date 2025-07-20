using Fusion;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Corris.Loggers.Logger;
using static Corris.Loggers.LogUtils;

public class HostManager : NetworkBehaviour
{
    NetworkRunner NetRunner => BasicSpawner.Instance.NetRunner;
    // List of commands came to Host
    private Queue<Command> _commandQueue = new Queue<Command>();
    // for testing Host freezes
    private bool _isFreezeSimulated = false; // Pause flag

    public void Update()
    {
        // Toggle pause on Space key
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _isFreezeSimulated = !_isFreezeSimulated;

            Log($"{GetLogCallPrefix(GetType())} {(_isFreezeSimulated ? "Host paused." : "Host resumed.")}");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        HostProcessCommandsFromNetwork();
    }

    public void HostProcessCommandsFromNetwork()
    {

        // Get data from all active players
        foreach (var player in NetRunner.ActivePlayers)
        {
            if (NetRunner.TryGetInputForPlayer<NetworkInputData>(player, out var input))
            {
                HostReceiveCommand(player, input); // Add command to queue
            }
        }

        // Simulate network freeze
        if (_isFreezeSimulated)
        {
            LogWarning($"{GetLogCallPrefix(GetType())} Host is Freezed. Skipping HostProcessCommands.");

            return; // If the host is "frozen", do not process commands
        }

        // Process command queue
        HostProcessCommands();
    }


    public void HostProcessCommands()
    {
        // Sort commands by time
        var sortedCommands = _commandQueue.OrderBy(c => c.Input.timestamp).ToList();

        foreach (var command in sortedCommands)
        {
            // Get a list of all units for which there were changes in the current input (to find the central bearing point)
            var changedUnits = Unit.GetUnitsInInput(command.Input);
            // Find the center of the selected units - as the bearing point
            var center = changedUnits.GetCenter();

            // for (int i = 0; i < command.Input.unitIds.Length; i++)
            for (int i = 0; i < command.Input.unitCount; i++)
            {
                var unitId = command.Input.unitIds[i];
                var unit = FindUnitById(unitId);
                if (unit != null)
                {
                    // Ignore outdated commands
                    if (command.Input.timestamp > unit.LastCommandTimestamp)
                    {
                        // Find a personal point for each unit (keeping the center as the base)
                        var unitTargetPosition = unit.GetUnitTargetPosition(center, command.Input.targetPosition);

                        // unit.HostSetTarget(command.Input.targetPosition, command.Input.timestamp);
                        unit.HostSetTarget(unitTargetPosition, command.Input.timestamp);
                    }
                    else
                    {
                        LogWarning($"{GetLogCallPrefix(GetType())} Ignored outdated command for unit {unit.name} at {command.Input.timestamp}");
                    }
                }
            }
        }

        _commandQueue.Clear(); // Clear the queue after processing
    }

    public void HostReceiveCommand(PlayerRef player, NetworkInputData input)
    {
        var command = new Command
        {
            Player = player,
            Input = input
        };

        _commandQueue.Enqueue(command); // Add command to queue
    }

    private Unit FindUnitById(uint unitId)
    {
        // Assume all units have NetworkObject
        foreach (var unit in FindObjectsByType<Unit>(FindObjectsSortMode.None))
        {
            var networkObject = unit.GetComponent<NetworkObject>();
            if (networkObject != null && networkObject.Id.Raw == unitId)
            {
                return unit;
            }
        }
        return null;
    }

}
