# P3k.PlayerLadderController

A Unity ladder interaction package for player characters, targeting **.NET Standard 2.1**. Provides smooth mount, climb, and dismount behaviour driven by a `ScriptableObject` configuration.

---

## Features

- **Proximity detection** — probes for nearby `LadderVolume` colliders within a configurable range
- **Smooth mount / dismount transitions** — animated over a configurable duration with obstruction checking
- **Camera-relative climbing** — vertical input is adjusted based on camera orientation so the player always climbs in the expected direction
- **Auto-dismount** — optionally ejects the player when they reach the top or bottom climb-height limits
- **Layer mask control** — separate masks for detection and obstruction layers
- **Editor gizmos** — visualise ladder volumes, mount points, and probe ranges in the Scene view
- **Network-ready** — stable `LadderId` per ladder, snapshot capture/restore, and silent reconciliation helpers for server-authoritative workflows

---

## Setup

### 1 — Create a config asset

In the **Project** window, right-click and select:

```
Create → P3k → Player Ladder Config
```

Adjust the fields described in [Configuration](#configuration) to match your character dimensions and desired feel.

### 2 — Add `LadderVolume` to each ladder

Add the `LadderVolume` component to each ladder `GameObject`. A `BoxCollider` is required and will be added automatically. Resize the collider to cover the climbable area of the ladder mesh.

The `BoxCollider` is **automatically forced to a trigger** at runtime. It is used purely for spatial queries — detecting which face the player is approaching and computing valid mount points. It does not block physical movement.

For the player to be physically blocked from walking through the ladder, the ladder mesh should have its own **separate, non-trigger collider** (e.g., a `MeshCollider` or `BoxCollider` on the model object). This ensures mount-side detection works correctly while the solid geometry prevents the player from teleporting through the ladder during a dismount.

---

## Configuration

`PlayerLadderConfig` is a `ScriptableObject` with the following properties:

### Detection

| Property | Default | Description |
|---|---|---|
| `ProbeRange` | `3` | Radius (metres) within which ladders are detected |
| `CharacterHeight` | `2` | Character capsule height used for obstruction checks |
| `CharacterRadius` | `0.3` | Character capsule radius used for obstruction checks |

### Mounting

| Property | Default | Description |
|---|---|---|
| `MountDuration` | `0.2` | Seconds to animate the player onto the ladder |
| `DismountDuration` | `0.15` | Seconds to animate the player off the ladder |

### Climbing

| Property | Default | Description |
|---|---|---|
| `MoveSpeed` | `2` | Climb speed in metres per second |
| `SprintSpeedMultiplier` | `1.5` | Multiplier applied to `MoveSpeed` while sprinting |

### Layer Masks

| Property | Default | Description |
|---|---|---|
| `ObstructionsLayerMask` | Everything | Layers tested when checking for dismount obstructions |
| `ExclusionsLayerMask` | Nothing | Layers excluded from all detection and obstruction checks |

---

## LadderVolume Properties

| Property | Description |
|---|---|
| `LadderId` | Stable unique identifier for referencing this ladder across a network |
| `AutoDismountEnabled` | Eject the player automatically at the climb height limits |
| `GizmosEnabled` | Draw debug gizmos for this volume in the Scene view |
| `MountDistance` | How close the player must be to the ladder face to mount |
| `LocalMountHeightMin/Max` | Vertical range in local space from which the player can mount |
| `MinClimbHeight / MaxClimbHeight` | Full climbable range inset from the volume extents |
| `ClimbHeightInsetTop / ClimbHeightInsetBottom` | Inset distances from the top and bottom of the volume that define the climbable range |
| `LocalAutoDismountHeightMin/Max` | Heights at which auto-dismount triggers, clamped to the climbable range (requires `AutoDismountEnabled`) |

---

## PlayerLadderService

`PlayerLadderService` is the core API. Instantiate it in your character controller and call its methods each frame.

```csharp
// Initialise once (e.g. in Awake)
var ladderService = new PlayerLadderService(config, transform);
```

### Properties

| Property | Type | Description |
|---|---|---|
| `IsMounted` | `bool` | `true` while the player is mounted on a ladder |
| `IsSprinting` | `bool` | `true` while the player is sprinting on the ladder |
| `IsTransitioning` | `bool` | `true` while a mount or dismount transition is in progress |
| `CurrentLadder` | `ILadderVolume` | The active ladder volume, or `null` |
| `CurrentMountPoint` | `LadderMountPoint` | The current mount point data |
| `NormalizedProgress` | `float` | 0–1 climb progress along the ladder rail |
| `LadderPosition` | `Vector3` | World-space position on the current ladder |

### Methods

#### Gameplay

| Method | Description |
|---|---|
| `TryMount(headPosition, headRotation)` | Attempts to mount a nearby ladder from the given head position/rotation. Returns `true` on success |
| `Move(verticalInput, dt, isSprinting, cameraForwardY)` | Moves the player along the ladder. `isSprinting` defaults to `false`. `cameraForwardY` is optional — when supplied, camera-relative input adjustment is applied automatically |
| `TickMountingAnimators(dt, allowAutoDismount)` | Advances mount/dismount transition animations. Call every frame |
| `Dismount()` | Begins a normal dismount with obstruction-checked placement |
| `Dismount(position, rotation)` | Dismounts to a specific target position/rotation, bypassing landing detection. For server-authoritative dismounting |
| `ForceDismount()` | Immediately clears ladder state regardless of obstructions. Fires `FinishUsingLadder` |
| `CanTryAutoDismount()` | Returns `true` if the player is at an auto-dismount boundary |
| `SetLadderDetectionDistance(distance)` | Dynamically overrides the probe range at runtime |
| `AdjustInputForCamera(verticalInput, cameraForwardY, characterForwardY)` | Static helper — flips input when the camera is behind the player |

#### Network / Server-Authoritative

| Method | Description |
|---|---|
| `ForceMount(ladder, mountPoint)` | Mounts onto a known ladder and mount point without detection. Fires `MountStarted`. Returns `true` on success |
| `GetSnapshot()` | Captures the current ladder state into a `LadderSnapshot` (ladderId, mountPoint, progress, position, isMounted, isTransitioning) |
| `RestoreSnapshot(snapshot, ladder?)` | Silently restores ladder state from a snapshot. No events fired. Optional `ladder` parameter for when `CurrentLadder` is null |

### Events

| Event | Signature | Description |
|---|---|---|
| `MountStarted` | `(ILadderVolume, LadderMountPoint)` | Fired when a mount transition begins |
| `DismountStarted` | `(Vector3 startPos, Quaternion startRot, Vector3 targetPos, Quaternion targetRot)` | Fired when a dismount transition begins. Includes both the current and target landing positions |
| `FinishUsingLadder` | `()` | Fired when the dismount transition completes |

### Per-frame usage

```csharp
void Update()
{
    var verticalInput = /* read from your input system */;
    var isSprinting = /* read from your input system */;

    if (!ladderService.IsMounted)
    {
        var head = cameraTransform;
        ladderService.TryMount(head.position, head.rotation);
    }

    ladderService.Move(verticalInput, Time.deltaTime, isSprinting, cameraTransform.forward.y);
    ladderService.TickMountingAnimators(Time.deltaTime);
}
```

---

## Data Types

### LadderSnapshot

`LadderSnapshot` is an immutable struct that captures the full ladder state for network synchronisation.

| Field | Type | Description |
|---|---|---|
| `LadderId` | `int` | Stable ladder identifier (0 if not mounted) |
| `MountPoint` | `LadderMountPoint` | The mount point used when mounting |
| `Progress` | `float` | Normalised 0–1 climb progress along the rail |
| `Position` | `Vector3` | World-space position on the ladder |
| `IsMounted` | `bool` | Whether the player is mounted |
| `IsTransitioning` | `bool` | Whether a transition is active |
| `IsSprinting` | `bool` | Whether the player is sprinting on the ladder |

### LadderMountPoint

| Field | Type | Description |
|---|---|---|
| `Position` | `Vector3` | World-space position of the mount point |
| `Rotation` | `Quaternion` | Orientation the player faces when mounted |
| `FaceNormal` | `Vector3` | World-space normal of the mounted face |

---

## Network Usage

The service provides all primitives needed for server-authoritative ladder interactions. Below is the recommended flow.

### Mount

1. **Client** calls `TryMount()` for instant local feedback
2. **Client** subscribes to `MountStarted` → sends `LadderId` + `MountPoint` to server
3. **Server** validates and calls `ForceMount(ladder, mountPoint)`
4. **Server** sends result back to client
5. **If rejected**: client calls `RestoreSnapshot(preMountSnapshot)` to silently roll back

### Climb

1. **Client** calls `Move()` each frame for local prediction
2. **Client** periodically sends `GetSnapshot()` data to server
3. **Server** compares progress/position against its own state
4. **If mismatch**: server sends authoritative snapshot → client calls `RestoreSnapshot(serverSnapshot)`

### Dismount

1. **Client** calls `Dismount()` — `DismountStarted` fires with `(startPos, startRot, targetPos, targetRot)`
2. **Client** sends dismount request + target position to server
3. **Server** validates and calls `Dismount()` or `Dismount(pos, rot)`
4. **If rejected**: client calls `RestoreSnapshot(lastMountedSnapshot, ladderRef)` to silently remount at last known position
5. **If accepted with different landing**: standard netcode position correction (ladder no longer drives the transform after dismount)

### RestoreSnapshot Details

`RestoreSnapshot` is fully silent — no `MountStarted`, `DismountStarted`, or `FinishUsingLadder` events are fired. This makes it safe for reconciliation without triggering gameplay side effects.

When restoring to a **mounted** state from a **dismounted** state (e.g., server rejects a dismount), pass the `ILadderVolume` reference as the second parameter since `CurrentLadder` will be null:

```csharp
ladderService.RestoreSnapshot(lastMountedSnapshot, ladderReference);
```

When restoring to a **dismounted** state, the snapshot's `Position` is applied to the character transform:

```csharp
var dismountedSnapshot = new LadderSnapshot(0, default, 0f, landingPos, false, false, false);
ladderService.RestoreSnapshot(dismountedSnapshot);
```

---