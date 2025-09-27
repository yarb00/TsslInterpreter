# [TsslInterpreter](https://tssl.yarb00.dev)

## About the "Too Simple Scripting Language"

### Documentation

Documentation will be written later when the language becomes more stable.

### Editor support

The official [Notepad++](https://github.com/notepad-plus-plus/notepad-plus-plus) syntax file can be found at `editors/npp/TooSimpleScriptingLanguage.xml`.

> [!NOTE]
> Notepad++ is only available for Windows. If you're using a different OS, you can try the crossplatform reimplementation called [NotepadNext](https://github.com/dail8859/NotepadNext), though I haven't tested it.

## Build

> [!IMPORTANT]
> For each new version, building and usability are tested on the latest Windows 11 and on the latest Arch Linux. TsslIntepreter PROBABLY works on macOS, but I can't test it.

1. Install the latest [Git](https://git-scm.com/downloads) and the latest [.NET SDK](https://dot.net/download), if you don't have them already.
2. Make sure that you have `git` and `dotnet` available in your PATH.
3. Run `git clone https://github.com/yarb00/TsslInterpreter.git`. It will create a new `TsslInterpreter` directory in your current directory.
4. `cd TsslInterpreter`
5. Build the project with `dotnet publish -r <RID>`. The full list of RIDs can be found [here](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog). If you are building for yourself, you can omit the `-r <RID>` part.
6. The executable will be placed in `<project directory>/src/bin/Release/net8.0/<RID>/publish`, along with the debug data (.PDB/.DBG file).
