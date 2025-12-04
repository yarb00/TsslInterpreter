# [TsslInterpreter](https://tssl.yarb00.dev)

## About Too Simple Scripting Language

### Documentation

Documentation will be written later when the language becomes more stable.

### Editor support

The official [Notepad++](https://github.com/notepad-plus-plus/notepad-plus-plus) syntax file (User Defined Language) for the latest version of TSSL can be found at `editors/npp/TooSimpleScriptingLanguage.xml`.

## Get TsslInterpreter

### Pre-built

For Windows (amd64), Linux (glibc + amd64) and macOS (amd64 and arm64), [get the latest version from the Releases section](https://github.com/yarb00/TsslInterpreter/releases/latest).

Also, builds of each new commit are available from GitHub Actions (for the same platforms), until they expire in 400 days.

If you're using a different platform, you can build TsslInterpreter yourself.

### Build yourself

1. Install the latest [Git](https://git-scm.com/downloads) and the latest [.NET SDK](https://dot.net/download) (version 10 or higher), if you don't have them already.
2. Make sure that you have `git` and `dotnet` available in your PATH.
3. Clone TsslInterpreter source code with `git clone https://github.com/yarb00/TsslInterpreter.git <optional directory name>` (you can also use SSH or git:// protocol).
4. `cd TsslInterpreter`
5. (Optional): Run `git checkout v<A>.<B>.<C>` to build the specific release. Otherwise, the latest commit will be used (which can be ahead of the latest version).
6. Build the project with `dotnet publish -r <RID>`. See the [list of RIDs](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog#known-rids) and replace `<RID>` with the right one.
7. The executable will be placed in `<project directory>/src/bin/Release/net10.0/<RID>/publish`.
