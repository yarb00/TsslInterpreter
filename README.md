# [TsslInterpreter](https://tssl.yarb00.dev)

## About the "Too Simple Scripting Language"

### Documentation

Documentation will be written later when the language becomes more stable.

### Editor support

The official [Notepad++](https://github.com/notepad-plus-plus/notepad-plus-plus) syntax file (User Defined Language) for the latest version of TSSL can be found at `editors/npp/TooSimpleScriptingLanguage.xml`.

> [!NOTE]
> Notepad++ is only available for Windows. There is a cross platform reimplementation called [NotepadNext](https://github.com/dail8859/NotepadNext), but it doesn't support UDLs currently.

## Get TsslInterpreter

### Pre-built

For Windows (amd64), pre-built release versions are available on the Releases section.

For Windows (amd64), Linux (amd64 + glibc) and macOS (amd64/arm64), builds of each new commit are available from GitHub Actions, until they expire in 300 days.

If you're using a different platform, you can build TsslInterpreter yourself.

### Build yourself

1. Install the latest [Git](https://git-scm.com/downloads) and the latest [.NET SDK](https://dot.net/download) (version 10 or higher), if you don't have them already.
2. Make sure that you have `git` and `dotnet` available in your PATH.
3. Clone TsslInterpreter source code with `git clone https://github.com/yarb00/TsslInterpreter.git` (you can also use SSH or git:// protocol).
4. `cd TsslInterpreter`
5. (Optional): Run `git checkout v<A>.<B>.<C>` to build the specific release. Otherwise, the latest commit will be used (which can be ahead of the latest version).
6. Build the project with `dotnet publish -r <RID>`. See the [list of RIDs](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog#known-rids) and replace `<RID>` with the right one.
7. The executable will be placed in `<project directory>/src/bin/Release/net10.0/<RID>/publish`.
