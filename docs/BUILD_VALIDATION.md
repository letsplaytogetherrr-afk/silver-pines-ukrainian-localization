# Local rebuild validation

Validated on **2026-11-07** with the published build script, Roslyn **4.8.0**, the matching loader dependencies, and locally generated Silver Pines interop references.

- The exported `src/LocalizationPlugin.cs` is byte-for-byte identical to the release source snapshot.
- `build.ps1` completed successfully without installing the result into the game.
- The rebuilt assembly's **88 managed method instruction bodies** were compared with the released plugin using Mono.Cecil and matched.
- The rebuilt binary hash differs, as expected for an original build that did not enable deterministic output. This is not a claim of byte-for-byte reproducibility.

Released DLL SHA256:

```text
174cc20ac36634708d3b6f218058131854ac0d7c8109ecab8b678e904d45e831
```

Rebuilt DLL SHA256 from this validation:

```text
3e565e45fa14067ebb768cbd6edacce88eca508f53b598e7fb622b95faaaa477
```

This verifies the documented plugin build and its managed method bodies. It is not an antivirus verdict, a verification of all third-party loader binaries, or a new runtime test of a different game version. No game assets or build dependency binaries are included in the source repository.
