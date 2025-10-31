# [TsslInterpreter](https://tssl.yarb00.dev)

## About the "Too Simple Scripting Language"

### Documentation

Documentation will be written later when the language becomes more stable.

### Editor support

The official [Notepad++](https://github.com/notepad-plus-plus/notepad-plus-plus) syntax file (UDL) for the latest version of TSSL can be found at `editors/npp/TooSimpleScriptingLanguage.xml`.

> [!NOTE]
> Notepad++ is only available for Windows. There is a crossplatform reimplementation called [NotepadNext](https://github.com/dail8859/NotepadNext), but it doesn't support UDLs currently.

## How to use

### Pre-built

Compiled builds of each new commit are available from GitHub Actions.

Starting with version 0.5, the Releases section also uses them.

### Build yourself

1. Install the latest [Git](https://git-scm.com/downloads) and the latest [.NET SDK](https://dot.net/download), if you don't have them already.  
> [!IMPORTANT]  
> While the TsslInterpreter itself targets .NET 8, you'll need to have at least .NET 9 because of usage of the XML solution file (.slnx).

2. Make sure that you have `git` and `dotnet` available in your PATH.
3. Run `git clone https://github.com/yarb00/TsslInterpreter.git`. It will create a new `TsslInterpreter` directory in your current directory.
4. `cd TsslInterpreter`
5. Build the project with `dotnet publish -r <RID>`. The full list of RIDs can be found [here](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog#known-rids).
6. The executable will be placed in `<project directory>/src/bin/Release/net8.0/<RID>/publish`, along with the debug file (.PDB/.DBG).
