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
  its death flow is being validated. Existing saved preferences are not
  overwritten; the Mono default remains unchanged.
- Experimental solo death flow in 1.0.2.48: tolerate missing death-screen
  title/score data, use the actual respawn button as the clone source, and
  return to the menu automatically after a confirmed death even if the custom
  button cannot be found. Only after the menu finishes loading, delete the
  *active* save if its absolute path and slot match
  `Saves/<profile>/SaveGame_<slot>`. Refuse unknown paths and symlinks; do not
  automatically delete multiplayer saves.
- Remove the project's hard-coded post-build copy into a Steam installation.
  The new candidate is labeled `1.0.2.48` (earlier survival-only test: 1.0.2.47)
  to distinguish experimental DLLs from
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
  user-reported gameplay validation **of 1.0.2.47**, not a full regression test.
- The 1.0.2.48 candidate builds, and `SaveFolderGuard` tests pass with fake
  saves in a separate temporary sandbox. It has **not** been tested in-game.

## Known blocker: permadeath candidate needs an in-game test

In the 1.0.2.47 test, the player did not respawn during the session, but the
save **was not deleted**: after leaving, the user could load it again. The
observed log reports:

```text
[ERROR] [Flatline] Unhandled exception in coroutine. It will not continue executing.
System.NullReferenceException: Object reference not set to an instance of an object.
   at Flatline.FlatlineUIModule.GenerateDeathScreenScore()+MoveNext()
```

The 1.0.2.48 changes address the likely null references and the old deletion
coroutine's unreliable wait for `IsLoading == true`, but **compile tests cannot
prove that the death screen, menu transition or Steam Cloud save deletion work**.
Do not advertise it as fully compatible. Back up saves and keep
`PermanentDeath = false` in `UserData/MelonPreferences.cfg` on existing profiles.
For an actual deletion test, create a disposable *solo* save, confirm its
backup, enable `PermanentDeath = true`, die, and verify that only that slot
disappears both in-game and after restarting Steam. Test sleep, save/reload,
death UI, multiplayer and clothing items separately.

## Rebuild

Populate `IL2Cpp/libs-il2cpp/` with the generated 0.4.7f12 proxy DLLs listed
in `IL2Cpp/Flatline-IL2Cpp.csproj`. Its `Il2Cpp.dll` reference is the
MelonLoader IL2CPP support module; `Il2CppInterop.Runtime.dll` and
`Newtonsoft.Json.dll` can be taken from MelonLoader's `net6` directory. Then
run `dotnet build IL2Cpp/Flatline-IL2Cpp.csproj -c Release`. Keep these local
dependencies and the compiled DLL out of source control.

To run non-destructive path-validation tests, set `FLATLINE_TEST_TEMP_ROOT`
to a dedicated writable temporary directory and run
`dotnet run --project Tests/SaveFolderGuard.Tests.csproj -c Release`. The tests
create and remove only their own uniquely named sandbox there.
