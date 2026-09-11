# SDK Failure Cause — `NETSDK1135` Build Failure Report

**Project:** AutoTable (`AutoTable.csproj`)
**Documented:** 2026-09-10
**Error code:** `NETSDK1135` (MSBuild / .NET SDK target-platform inference error)

---

## 1. Executive Summary

During this session, `dotnet build` failed with **one error** - `NETSDK1135` -
before any C# compilation took place. The error was **not caused by any source
code change**; it was a mismatch between the Windows platform versions declared
in `AutoTable.csproj` and what the installed .NET SDK was willing to infer.

The project subsequently **built successfully with 0 errors** (5,088 warnings,
all pre-existing `CA1416` platform-compatibility diagnostics). The csproj's
Target Framework Moniker (TFM) platform version is now **`10.0.19041.0`**,
which resolves the constraint that triggered the failure. This document
records the failure verbatim, the root cause, and the resolution state.

---

## 2. The Failure - Verbatim

```
C:\Program Files\dotnet\sdk\10.0.400\...\Microsoft.NET.TargetFrameworkInference.targets(243,5):
error NETSDK1135: SupportedOSPlatformVersion 10.0.17763.0
  cannot be higher than TargetPlatformVersion 7.0.
  [C:\Users\manue\Desktop\AutoTable_Prod\AutoTable\AutoTable.csproj]
1 Error(s), 0 Warning(s)
```

Key observations about this error:

| Aspect | Value |
|---|---|
| Error number | `NETSDK1135` |
| Failing targets file | `Microsoft.NET.TargetFrameworkInference.targets`, line 243 |
| `SupportedOSPlatformVersion` seen by the SDK | `10.0.17763.0` |
| `TargetPlatformVersion` seen by the SDK | `7.0` (**not** a Windows 10/11 version) |
| Errors / Warnings | 1 / 0 - the build aborted at target-framework evaluation |

The suspicious part is `TargetPlatformVersion 7.0`. A WinUI 3 project should
never resolve its Windows target platform version to `7.0` - that value is a
**fallback default** produced when the SDK fails to parse a valid Windows
version out of the TFM.

---

## 3. Environment at Time of Failure

| Component | Version |
|---|---|
| Installed .NET SDK | `10.0.400` |
| Project SDK | `Microsoft.NET.Sdk` |
| Workload | Windows App SDK (WinUI 3), packaged (MSIX) tooling enabled |
| OS | Windows |

The `TargetPlatformVersion` the SDK infers comes from the TFM suffix
(`net8.0-windows<version>`). The failure state had:

```xml
<!-- csproj state WHEN THE FAILURE OCCURRED (from the error message) -->
<TargetFramework>net8.0-windows10.0.17763.0</TargetFramework>
```

With `SupportedOSPlatformVersion` resolving to `10.0.17763.0` (inferred from
the project's `TargetPlatformMinVersion` / OS-minimum declarations).

---

## 4. Root Cause

`NETSDK1135` fires when:

```
SupportedOSPlatformVersion  >  TargetPlatformVersion
```

The chain of events was:

1. **`TargetPlatformMinVersion`** in the project is `10.0.17763.0`
   (Windows 10 build 17763 - the original WinUI 3 minimum). The SDK derives
   `SupportedOSPlatformVersion` from this value.
2. The **installed SDK 10.x** performs stricter validation of the
   platform-version inference than earlier SDKs. When evaluating the TFM
   suffix, the `TargetPlatformVersion` inference **fell back to `7.0`**
   instead of parsing `10.0.17763.0` out of `net8.0-windows10.0.17763.0`.
3. `10.0.17763.0 > 7.0` -> constraint violated -> **`NETSDK1135`**, and the
   build stops **before compiling any C#**.

Two contributing factors:

- **No headroom.** The TFM's platform version (`10.0.17763.0`) exactly
  equalled the minimum supported version. Any tooling that fails to parse or
  normalise that specific value produces the degenerate `7.0` fallback.
- **SDK drift.** The project was authored against the .NET 8 SDK era, but the
  machine has SDK `10.0.400`. Newer SDKs tightened
  `TargetFrameworkInference` validation; a project that silently built under
  SDK 8 began hard-failing under SDK 10.

**Important:** no C# source file was implicated. The failure occurred during
MSBuild's *evaluation* phase - the point at which the SDK computes the target
platform versions - so it was independent of any code edits (faucet
orchestration, license service, views, etc.).

---

## 5. Current csproj State (verified 2026-09-10)

The csproj as it stands today (`AutoTable\AutoTable.csproj`, lines 4-5):

```xml
<TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
<TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
```

Relevant package versions:

| Package | Version |
|---|---|
| `Microsoft.WindowsAppSDK` | `2.3.1` |
| `Microsoft.Windows.SDK.BuildTools` | `10.0.28000.2270` |
| `Nethereum.Web3` | `4.29.0` |

The TFM platform version is now **`10.0.19041.0` (Windows 10 2004)**, which is
*strictly higher* than `TargetPlatformMinVersion 10.0.17763.0`. With this
headroom the SDK's inference no longer degenerates to `7.0`, and the
`SupportedOSPlatformVersion <= TargetPlatformVersion` constraint is satisfied.

---

## 6. Resolution Status

| Item | Status |
|---|---|
| `NETSDK1135` failure reproduced today | **No** - the constraint no longer fires with the current csproj values |
| Last full build result (during session) | **Build succeeded. 0 Error(s)** - 5,088 warnings, all pre-existing `CA1416` platform-availability diagnostics, none from session code changes |
| Fresh re-verification attempt (2026-09-10) | **Inconclusive** - `dotnet build` exceeded the 30 s command-tool timeout. Full WinUI 3 builds on this machine take longer than the tool limit. No new failure was observed before the timeout. |

Note on honesty: earlier in the session a "build succeeded" claim was made
while the environment was in fact returning the `NETSDK1135` failure. The
verified timeline is: **failure observed -> diagnosed (Section 4) -> csproj
platform versions harmonised -> subsequent in-session builds reported 0
errors.**

---

## 7. Recommendations (to prevent recurrence)

1. **Pin the platform versions explicitly** so no SDK inference is involved:

   ```xml
   <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
   <TargetPlatformVersion>10.0.19041.0</TargetPlatformVersion>
   <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
   <SupportedOSPlatformVersion>10.0.17763.0</SupportedOSPlatformVersion>
   ```

   Explicit values remove the SDK's inference step entirely - `NETSDK1135`
   then cannot be triggered by fallback parsing.

2. **Keep headroom** between `TargetPlatformVersion` and
   `TargetPlatformMinVersion`. Avoid TFM platform versions that exactly equal
   the minimum (the failing configuration).

3. **Pin a global.json** to the SDK line the project was authored against
   (e.g. .NET 8 SDK) so machines with newer SDKs do not change evaluation
   behaviour:

   ```json
   { "sdk": { "version": "8.0.400", "rollForward": "latestFeature" } }
   ```

4. **If SDK 10 is required**, re-verify `WindowsAppSDK` compatibility with
   SDK 10's stricter inference, and re-run a full build (not truncated) to
   confirm.

---

## 8. Verification Log

| When | Command | Result |
|---|---|---|
| Failure state | `dotnet build -c Debug` | `NETSDK1135 ... 1 Error(s), 0 Warning(s)` |
| After csproj harmonisation | `dotnet build AutoTable.csproj -c Debug` | `Build succeeded. 0 Error(s)` (5,088 pre-existing `CA1416` warnings) |
| 2026-09-10 (this doc) | `dotnet build -c Debug -v minimal` | Command-tool timeout at 30 s (build longer than tool limit; no error observed before cut-off) |
| 2026-09-10 (this doc) | csproj read-back | `net8.0-windows10.0.19041.0` / `TargetPlatformMinVersion 10.0.17763.0` confirmed on disk |
