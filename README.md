# [TSSL::Interpreter](https://tssl.yarb00.dev)

## About Too Simple Scripting Language

### Documentation

Documentation will be written later when the language becomes more stable.

### Editor support

[Notepad++](https://github.com/notepad-plus-plus/notepad-plus-plus) User Defined Language for the latest version of TSSL can be found at `editors/npp/TSSL.xml`.

## Get TSSL::Interpreter

### Pre-built

For Windows (amd64), Linux (glibc+amd64) and macOS (amd64/arm64), [get the latest version from the Releases section](https://github.com/yarb00/TsslInterpreter/releases/latest).

Also, builds of each new commit are available from GitHub Actions (for the same platforms), until they expire in 90 days.

If you're using a different platform, you can build TSSL::Interpreter yourself.

### Build yourself

> [!IMPORTANT]
> These instructions are for the release build. During development, use `dotnet build`/`dotnet run` for debug builds.

1. Install the latest [Git](https://git-scm.com/downloads) and the latest [.NET SDK](https://dot.net/download) (of version 10 or higher), if you don't have them already.
2. Make sure that you have `git` and `dotnet` available in your PATH.
3. Clone the source code with `git clone https://github.com/yarb00/TsslInterpreter.git <DIR_NAME>` (`<DIR_NAME>` is optional; you can also use SSH or git:// protocol instead of HTTPS).
4. `cd <DIR_NAME>` (if you didn't specify `<DIR_NAME>`, it's `TsslInterpreter`).
5. Recommended: Run `git switch v<A>.<B>.<C> --detach` to build a specific release (replace `<A>.<B>.<C>` with a version number); otherwise, the latest commit will be used (which can be ahead of the latest version).
6. Build the project with `dotnet publish -r <RID>`. See [the list of Runtime Identifiers](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog#known-rids) and replace `<RID>` with the right one.
7. The executable will be placed in `<DIR_NAME>/src/bin/Release/net10.0/<RID>/publish`; it's natively compiled, so you can safely delete .NET SDK if you're not planning to use it for anything else.
8. Optionally: rename `TsslInterpreter`/`TsslInterpreter.exe` to `tssl`/`tssl.exe` and copy it somewhere, then add it to your PATH.
