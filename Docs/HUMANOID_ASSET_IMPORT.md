# CC0 humanoid asset bootstrap

The Unity Editor menu **BATTLE CLUBS > Characters > Download CC0 Humanoid + Animations** downloads the official KinemIQ FBX release directly into `Assets/ThirdParty/KinemIQ`.

Source: https://github.com/Kinemiq/free-game-animations/releases/tag/v1.0

- Licence: CC0 1.0; commercial use permitted.
- Archive: `KinemIQ_Free_Animations_CC0_FBX.zip`.
- SHA-256 is pinned in the downloader and checked before extraction.
- The package contains a rigged mannequin FBX and 35 humanoid animation clips on one skeleton.
- The importer refreshes Unity assets and reports the discovered mannequin model.
- ZIP extraction rejects paths that escape the destination directory.

## Integration status and limitations

This automation prepares and imports the source assets when run inside the Unity Editor. It does not claim that a Unity scene or Player prefab has already been assembled. The existing `HumanoidCharacterIntegrationWizard` remains the integration path; it must be run against the actual scene's `Player_Root` after confirming the imported FBX Avatar is Humanoid.

The mannequin is a real skinned 3D humanoid base, not a capsule, but it is not a photorealistic final player character. The pack's CC0 animation clips still need to be mapped to the existing `BC_Humanoid.controller` states and verified in Unity.
