# BATTLE CLUBS — Network team roster

## Implemented in this layer

- Server allocates the first free seat across five teams with four seats each.
- Team assignment replicates through a server-write/everyone-read NetworkVariable.
- Existing Combatant receives the matching team ID (team-1 through team-5).
- Existing TeamSpawnManager places the player at the generated team spawn.
- Disconnecting a client releases that seat for a later connection.
- The roster refuses to allocate more than 20 seats.

## Unity scene and prefab wiring

1. Add NetworkTeamRosterAuthority to the same scene GameObject as NetworkManager and NetworkSessionBootstrap.
2. Assign the existing TeamSpawnManager to its Spawn Manager field.
3. In NetworkManager, set the actual player prefab as Player Prefab.
4. Add NetworkTeamAssignment to that player prefab. It requires NetworkObject and Combatant.
5. Ensure the player prefab is registered in NetworkManager's Network Prefabs list.
6. Ensure spawn anchors are named TeamSpawn_1 through TeamSpawn_5 under the existing TeamSpawnManager hierarchy.
7. Run a host plus multiple clients and confirm server logs show team/seat allocations.

## Important scope

This layer handles server-owned team-seat allocation and spawn placement only. It does not yet provide lobby/matchmaking, Relay or NAT traversal, synchronized player movement, server-authoritative bat combat/revive/scoring, reconnect persistence, or a complete online match. Those systems must be wired and tested separately before online multiplayer can be called complete.
