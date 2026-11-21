# Community Map and Revive Prototype

## Community blockout

1. Open the Unity scene used for the local prototype.
2. Create an empty GameObject named `CommunityMap`.
3. Add `UrbanCommunityBlockout` from `Assets/Scripts/World`.
4. Press Play, or use the component context menu **Generate Community Blockout**.
5. Adjust block counts, block size, road width, alley width, and random seed in the Inspector.

The generator creates a greybox layout from Unity primitives: ground, compact houses, rooftop slabs, cover walls, a central court, and five team spawn markers. It is deliberately an editable prototype, not final art. Replace primitive materials and geometry with optimized modular assets before production.

## Revive interaction

1. Add `ReviveInteraction` to the player GameObject that performs the revive.
2. Ensure the player and downed teammate have colliders and a `Combatant` component.
3. Connect the Android revive button's Pointer Down event to `BeginRevive()`.
4. Connect Pointer Up / Pointer Exit to `CancelRevive()`.
5. For PC, call `BeginRevive()` on the interaction key down and `CancelRevive()` on key up.

The revive takes four seconds by default and is cancelled if the target leaves range or is no longer downed. The current script is local-only. In online matches, the server must validate teammate identity, distance, state, revive duration, and cancellation, then replicate the result to all clients.

## Important production notes

- The canonical match is five teams of four (20 players); spawn markers are named `TeamSpawn_1` through `TeamSpawn_5`. They are empty transforms, not a matchmaking/spawn system.
- The generated houses use primitive colliders and simple Standard materials. Check the render pipeline and replace materials if the project uses URP.
- No licensed club crest, sponsor, official kit texture, player likeness, or third-party map asset is included.
- Run the project in the Unity Editor and inspect the Console before treating this prototype as build-ready.