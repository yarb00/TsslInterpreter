# [TSSL::Interpreter](https://tssl.yarb00.dev)

## About Too Simple Scripting Language

### Documentation

Documentation will be written later when the language becomes more stable.

### Editor support

A [Notepad++](https://github.com/notepad-plus-plus/notepad-plus-plus) User Defined Language for the latest version of TSSL can be found at `editors/npp/TSSL.xml`.

## Get TSSL::Interpreter

### Pre-built

For Windows (x64), Linux (x64/arm64, glibc), and macOS (universal binary, x64/arm64), [get the latest version from the Releases section](https://github.com/yarb00/TsslInterpreter/releases/latest).

Also, builds of each new commit are available from GitHub Actions (for same platforms), until they expire in 90 days.

If you're using a different platform, you can build TSSL::Interpreter yourself.

### Build it yourself

> [!IMPORTANT]
> These instructions are for the release build. During development, use `dotnet build`/`dotnet run` for debug builds.

#### 1. If Git is not installed

- Install the latest version of [Git](https://git-scm.com/install) to be able to clone the repository.
- If you're using the Git for Windows graphical installer, you may leave all options as they are set by default.
- After installation, run `git --version` to verify that Git installed successfully and is available in your PATH.

#### 2. Clone the repository

- Run `git clone --filter=blob:none https://github.com/yarb00/TsslInterpreter.git TsslInterpreter` to clone the repository.
- Change your working directory with `cd TsslInterpreter`.

#### 3. If .NET SDK (v11) is not installed

##### Install via `dotnetup`

###### If `dotnetup` is not installed

- Install the latest version of [`dotnetup`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnetup/getting-started#download-dotnetup). You don't need to run the first-time setup yet, just install it and add it to your PATH.
- After changing your working directory with `cd TsslInterpreter`, run `dotnetup install`.
- `dotnetup` will ask you to select the SDK channel and the access mode. Leave the SDK channel as it's set by default, and unless you plan to use the .NET SDK after building TSSL::Interpreter, select `None` as your access mode.
- After installation, verify that `dotnetup dotnet --version` prints `11.0.XYZ` (for example, `11.0.100`).

###### If `dotnetup` is already installed

- After changing your working directory with `cd TsslInterpreter`, run `dotnetup install`.
- Verify that `dotnetup dotnet --version` prints `11.0.XYZ` (for example, `11.0.100`).

##### Install manually

- Install the latest version of [.NET 11 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/11.0).
- After installation, run `dotnet --list-sdks` and verify that version `11.0.XYZ` (for example, `11.0.100`) is present.
- When your working directory is the TSSL::Interpreter repository (`cd TsslInterpreter`), `dotnet --version` should also print `11.0.XYZ` (for example, `11.0.100`).

#### 4. Build TSSL::Interpreter

- Verify that your current working directory is the TSSL::Interpreter repository (`cd TsslInterpreter`).
- **Recommended**: Run `git switch v<A>.<B>.<C> --detach` to build a specific release (replace `v<A>.<B>.<C>` with the version of TSSL::Interpreter you want to build, for example, `v0.6.1`); otherwise, the latest commit will be used (which can be ahead of the latest version).
- Build the project with `dotnet publish -r <RID>`. See [the list of Runtime Identifiers](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog#known-rids) and replace `<RID>` with the one matching your system.
- The executable will be placed in `artifacts/publish/TsslInterpreter/release_<RID>`.
- **Recommended**: rename `TsslInterpreter`/`TsslInterpreter.exe` to `tssl`/`tssl.exe` and copy it somewhere, then add it to your PATH.
