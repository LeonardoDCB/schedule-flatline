# Experimental IL2CPP compatibility with Schedule I 0.4.7f12

This is a community compatibility proposal based on Flatline 1.0.2, which
targets Schedule I 0.4.6f12. It is **not a release**, and it is not a complete
port of every feature. The Mono build was not tested or retargeted.

## What changed

- The game's `TimeManager` no longer exposes sleep state, sleep-end callbacks
  or `StartSleep`. Use `SleepController` for those in IL2CPP, wait for it before
  registering callbacks, and unsubscribe on session exit. Use
  `TimeManager.SkipToTimeAndSync` for the former time-skip call.
- Adapt avatar appearance (gender/weight), first-person ragdoll camera and
  clothing evaluation to the 0.4.7 IL2CPP interfaces. Clothing warmth uses
  a **provisional name match** against equipped clothing item IDs; this needs
  more testing with specific outfits.
- Defer Flatline's consumption update and mod-specific save hook until setup
  is complete. Catch Flatline save-data exceptions without aborting the game's
  own save call.
- Default `PermanentDeath` to `false` for a **new IL2CPP configuration** while
  its death UI is broken. Existing saved preferences are not overwritten;
  the Mono default remains unchanged.
- Remove the project's hard-coded post-build copy into a Steam installation.
  The build is labeled `1.0.2.47` to distinguish the experimental DLL from
  the official 1.0.2; the maintainer can choose the eventual release version.

## Reproduction and validation

- Schedule I **0.4.7f12**, default **IL2CPP** backend; MelonLoader **0.7.3**.
- Original Flatline 1.0.2 failed during `Setup()` with
  `MissingMethodException: TimeManager.get_onSleepEnd()`; its save hook also
  produced `NullReferenceException` before setup completed.
- Recompiled with .NET SDK 6.0.428 against the generated 0.4.7f12 proxy
  assemblies and the existing NuGet dependencies: build succeeds (zero
  compile errors). The original game-specific proxy DLLs and compiled binaries
  are **not** part of this proposal.
- In a user's game test of the experimental IL2CPP DLL, the hunger, thirst
  and cold/temperature indicators and survival mechanics worked. This is
  user-reported gameplay validation, **not** a full regression test.

## Known blocker: permadeath is not fixed

After player death, the game remains on the death message: the player does
not respawn, and the save **is not deleted**. The observed log reports:

```text
[ERROR] [Flatline] Unhandled exception in coroutine. It will not continue executing.
System.NullReferenceException: Object reference not set to an instance of an object.
   at Flatline.FlatlineUIModule.GenerateDeathScreenScore()+MoveNext()
```

The 0.4.7 death UI and save-deletion path need a separate investigation.
As a starting point, `InitiateDeathScreen()` expects a `Title` child with a
`TextMeshProUGUI` component and creates the scoreboard. Confirm those objects
exist in 0.4.7 before the score coroutine dereferences them; the log does not
identify which particular reference is null.
Do not advertise this as a fully compatible release. Back up saves and check
that `PermanentDeath = false` in `UserData/MelonPreferences.cfg` before trying
the experimental build (especially if an existing profile had it enabled). Test
sleep, saving/reloading, the death UI, multiplayer and clothing items separately.

## Rebuild

Populate `IL2Cpp/libs-il2cpp/` with the generated 0.4.7f12 proxy DLLs listed
in `IL2Cpp/Flatline-IL2Cpp.csproj`. Its `Il2Cpp.dll` reference is the
MelonLoader IL2CPP support module; `Il2CppInterop.Runtime.dll` and
`Newtonsoft.Json.dll` can be taken from MelonLoader's `net6` directory. Then
run `dotnet build IL2Cpp/Flatline-IL2Cpp.csproj -c Release`. Keep these local
dependencies and the compiled DLL out of source control.
