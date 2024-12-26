# Multi Launcher

`multi-launcher` is a .NET application designed to launch and manage multiple SPA (Single-Page Applications) and processes on Windows and Linux systems. It provides an easy way to configure and run multiple applications or services from a single configuration file.

---

## Features
- Cross-platform support (Windows, Linux).
- Launch and manage Single-Page Applications (SPAs) with dynamic URLs.
- Execute and manage background processes with platform-specific configurations.
- Graceful cleanup when the application is terminated (e.g., Ctrl+C or close event).

---

#### Explanation of Configuration:

##### **MultiLauncher.SpaApps**
| Key                 | Description                                                                                       
|---------------------|---------------------------------------------------------------------------------------------------
| `Name`              | A unique name for the SPA application.
| `IndexHtml`         | The main `index.html` file of the SPA application.
| `BindUrls`          | A list of URLs to bind the SPA to.       
| `ResponseContentType` | The content type for SPA responses (e.g., `text/html`).
| `ResponseHeaders`   | Optional. A list of headers to include in SPA responses (e.g., for CORS rules).
| `WindowsPath`       | The file path to the application's folder on Windows.
| `LinuxPath`         | The file path to the application's folder on Linux.

##### **MultiLauncher.Processes**
| Key      | Description
|----------|---------------------------------------------------------------------------------------------------------------
| `Name`   | A unique name for the process.
| `Windows.Path`   | The location where the process will run.
| `Windows.Cmd`    | The command to execute (e.g., `cmd` on Windows).
| `Windows.Args`   | Arguments to pass into the command (e.g., `/c python main.py --mode api`).
| `Linux.Path`   | The location where the process will run.
| `Linux.Cmd`    | The command to execute (e.g., `bash` on Linux).
| `Linux.Args`   | Arguments to pass into the command (e.g., `python main.py --mode api`).
| `ProcessEnvironment`   | Optional. A list of environment variables to include when launching process. 
   
---

#### Optional Fields:
- **`SpaApps`**: This node is optional. If omitted or left empty, no SPAs will be launched.
- **`Processes`**: This node is optional. If omitted or left empty, no additional processes will be launched.

---
# Example Usage
### Windows running as a service: 
Put multilauncher.exe and appsettings.json in the same folder `C:\example\multi-launcher` with a couple batch files. Register startme.bat as a service using [nssm](https://nssm.cc/)

Create batch files:

`C:\example\multi-launcher\startme.bat`
```batch
cd C:\example\multi-launcher
call setpy.bat
multi-launcher > out.log 2>&1
```

`C:\example\multi-launcher\setpy.bat` (for some reason this doesn't behave correctly unless it's in a seperate batch file)
```batch
pyenv shell 3.11.9
```
Then:

* [Install Chocolatey](https://chocolatey.org/install) 
* `choco install pyenv-win`
* `choco install nssm` 
* `nssm install my-multi-launcher c:\example\multi-launcher\startme.bat` 

Tips:
* `psexec -i -s cmd.exe` to launch command prompt as Local System in order to debug problems launching service as local system.
* Default install of pyenv-win might only set path for local user, so that needs to be copied or moved to System path
  * C:\Users\username\\.pyenv\pyenv-win\bin
  * C:\Users\username\\.pyenv\pyenv-win\shims
* Todo: create instructions that aren't tied to specific username


