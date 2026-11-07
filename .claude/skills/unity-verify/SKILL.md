---
name: unity-verify
description: Check that Grid World 2 compiles and its tests pass, whether or not the Unity Editor is open. Use after changing C# code, or when the user asks whether something compiles or the tests pass.
---

# Unity verify workflow

Unity allows only one Editor per project, so first check whether it's open:

```powershell
Test-Path "Unity Project/Temp/UnityLockfile"
Get-Process Unity -ErrorAction SilentlyContinue | Select-Object Id, MainWindowTitle
```

Never close my Editor. It may have unsaved work.

## If the Editor is open

1. Ask me to focus the Unity window so it recompiles, and to tell you when it finishes.
2. Read the log at `%LOCALAPPDATA%\Unity\Editor\Editor.log` and search for `error CS` and `warning CS`. The log accumulates during a session, so only count entries from the latest compile, and confirm the log was written after your last edit. Otherwise the result is stale.
3. Ask me to run tests in **Window → General → Test Runner**, and to give you the results.

## If the Editor is closed

Run in batch mode. Write the output to the session scratchpad, and run it in the background because an import can take minutes.

```powershell
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe'
$project = (Resolve-Path 'Unity Project').Path
$out = '<scratchpad>'
$p = Start-Process $unity -Wait -PassThru -ArgumentList '-batchmode','-projectPath',"`"$project`"",'-runTests','-testPlatform','EditMode','-testResults',"`"$out\results.xml`"",'-logFile',"`"$out\unity.log`""
$p.ExitCode
```

- Don't pass `-quit` with `-runTests`, as Unity exits on its own when the tests finish.
- Exit code 0 means all tests passed. Otherwise, check `unity.log` for `error CS`, then `results.xml` for failures.
- To check compilation only, drop the `-runTests`, `-testPlatform`, and `-testResults` arguments and add `-quit`.

## Report

State which of these were actually confirmed, and how:

- **Compiles:** yes or no, with any errors and new warnings.
- **EditMode tests:** pass and fail counts.
- **Play mode and feel:** Claude can't confirm these. Give me a short checklist to try.

Never report something as working if it wasn't checked. Say what wasn't.
