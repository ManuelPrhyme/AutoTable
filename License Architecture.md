# Licensing System Architecture (Minute-Level Granularity)

## Overview
A hybrid licensing system that combines:
- **Calendar time (Unix timestamp)** for expiry tracking.
- **TPM monotonic counter** for tamper-resistant progression.
- **ECDSA signatures** for license file integrity.

This ensures licenses expire correctly by minutes while preventing rollback or manipulation.

---

## Components

### 1. License File
- Stores:
  - `registrationTime` → Unix timestamp at activation.
  - `registrationCounter` → TPM monotonic counter value at activation.
  - `lastValidatedTime` → Unix timestamp of last successful validation.
  - `lastValidatedCounter` → TPM counter value at last validation.
  - `licensePeriodMinutes` → total allowed minutes before expiry.
- Encrypted with AES for confidentiality.
- Signed with ECDSA private key for integrity.
- Verified in-app using ECDSA public key.

### 2. TPM Monotonic Counter
- Hardware-backed, forward-only counter.
- Incremented manually by the application.
- Used to detect rollback or tampering.
- Parameters tracked:
  - `counterId` → unique identifier for the TPM counter.
  - `currentCounter` → latest TPM counter value.
  - `expectedCounter` → calculated based on elapsed minutes.

### 3. Calendar Time Anchor
- Derived from `DateTimeOffset.UtcNow.ToUnixTimeSeconds()`.
- Provides actual elapsed minutes since registration.
- Parameters tracked:
  - `registrationTime` → baseline timestamp.
  - `currentTime` → current system UTC time.
  - `elapsedMinutes` → `(currentTime - registrationTime) / 60`.

---

## Workflow

### Registration
1. Generate license file:
   - `registrationTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()`
   - `registrationCounter = TPM.ReadCounter()`
   - `licensePeriodMinutes = e.g., 43200 (30 days)`
2. Encrypt license file with AES.
3. Sign license file with ECDSA private key.
4. Distribute license file to client.

### Validation (on each run)
1. Decrypt license file.
2. Verify ECDSA signature.
3. Read current state:
   - `currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()`
   - `currentCounter = TPM.ReadCounter()`
4. Compute elapsed minutes:
   - `elapsedMinutes = (currentTime - registrationTime) / 60`
   - `expectedCounter = registrationCounter + elapsedMinutes`
5. Reconcile:
   - If `currentCounter < expectedCounter` → increment TPM until match.
   - If `currentCounter > expectedCounter` → tampering detected.
   - If `currentTime < registrationTime` → system clock rollback detected.
6. Expiry check:
   - If `elapsedMinutes > licensePeriodMinutes` → license expired.
7. Update license file:
   - `lastValidatedTime = currentTime`
   - `lastValidatedCounter = currentCounter`

---

## Security Layers
- **ECDSA signature** → prevents forged license files.
- **AES encryption** → prevents casual tampering.
- **TPM counter** → ensures monotonic progression.
- **Calendar time** → enforces real-world expiry.
- **Hybrid reconciliation** → detects rollback and manipulation.

---

## Tracking Parameters Summary

| Parameter              | Purpose                                    |
|-------------------------|--------------------------------------------|
| `registrationTime`      | Baseline calendar time at activation       |
| `registrationCounter`   | TPM counter value at activation            |
| `currentTime`           | Current system UTC time                    |
| `currentCounter`        | Current TPM counter value                  |
| `elapsedMinutes`        | Minutes passed since registration          |
| `expectedCounter`       | Counter value expected based on elapsed    |
| `lastValidatedTime`     | Timestamp of last successful validation    |
| `lastValidatedCounter`  | TPM counter value at last validation       |
| `licensePeriodMinutes`  | Total allowed minutes before expiry        |

---

## Benefits
- **Tamper-resistant**: TPM prevents rollback.
- **Precise expiry**: Minute-level granularity.
- **Unified cryptography**: Reuses blockchain ECDSA keypair.
- **Local enforcement**: Works offline, no server dependency.
