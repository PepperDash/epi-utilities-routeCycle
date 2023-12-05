![PepperDash Logo](/images/essentials-plugin-blue.png)

## License

Provided under MIT license

# PepperDash Essentials Utilities Route Cycle Plugin (c) 2023

This repo contains a plugin for use with [PepperDash Essentials](https://github.com/PepperDash/Essentials). This plugin enables Essentials to cycle source values to various destinations based on selected sources and destinations. Typical use case includes a scenrio where the count of sources far exceed the count of available destinations. The desired amount of sources would cycle through the desired destinations.

### Plugin Configuration Object

Update the configuration object as needed for the plugin being developed.

```json
{
	"devices": [
		{
        "key": "ar-bridge-1",
        "uid": 0,
        "name": "Auto Route Cycler",
        "type": "routeCycle",
        "group": "switcher",
        "parentDeviceKey": "processor",
        "properties": {
          "parentDeviceKey": "processor",
          "control": {
            "method": "tcpIp",
            "tcpSshProperties": {
              "address": "",
              "port": 23,
              "autoReconnect": true,
              "autoReconnectIntervalMs": 10000,
              "username": "",
              "password": ""
            }
          }
        }
     },		
	]
}
```

### Plugin Bridge Configuration Object

Update the bridge configuration object as needed for the plugin being developed.

```json
{
	"devices": [
		{
        "key": "autoRouteBridge",
        "uid": 0,
        "name": "Auto Route Bridge 1",
        "group": "api",
        "type": "eiscApiAdvanced",
        "properties": {
          "control": {
            "tcpSshProperties": {
              "address": "127.0.0.2",
              "port": 0
            },
            "ipid": "11",
            "method": "ipidTcp"
          },
          "devices": [
            {
              "deviceKey": "ar-bridge-1",
              "joinStart": 1
            }
          ]
        }
      }
	]
}
```

### SiMPL EISC Bridge Map

The selection below documents the digital, analog, and serial joins used by the SiMPL EISC. Update the bridge join maps as needed for the plugin being developed.

#### Digitals
| dig-o (Input/Triggers)     | I/O   | dig-i (Feedback)     |
|----------------------------|-------|----------------------|
| InUse_Fb                   | 1     | Report Notify Pulse  |
| CycleRoute                 | 2     |                      |
| SourcesClear               | 3     |                      |
| DestinationsClear          | 4     |                      |
| SourceSelect               | 11-32 | SourceSelectFb       |
| DestinationSelect          | 51-82 | DestinationSelectFb  |

#### Analogs
| an_o (Input/Triggers) | I/O  | an_i (Feedback) |
|-----------------------|------|-----------------|
| Destination Route-In  | 1-32 | Destination Route-Out |
|                       | 2    |                       |
|                       | 3    |                       |
|                       | 4    |                       |
|                       | 5    |                       |

#### Serials
| serial-o (Input/Triggers) | I/O | serial-i (Feedback)  |
|---------------------------|-----|----------------------|
|                           | 1   | Notification Message |
|                           | 2   |                      |
|                           | 3   |                      |
|                           | 4   |                      |
|                           | 5   |                      |

## Github Actions

This repo contains two Github Action workflows that will build this project automatically. Modify the SOLUTION_PATH and SOLUTION_FILE environment variables as needed. Any branches named `feature/*`, `release/*`, `hotfix/*` or `development` will automatically be built with the action and create a release in the repository with a version number based on the latest release on the master branch. If there are no releases yet, the version number will be 0.0.1. The version number will be modified based on what branch triggered the build:

- `feature` branch builds will be tagged with an `alpha` descriptor, with the Action run appended: `0.0.1-alpha-1`
- `development` branch builds will be tagged with a `beta` descriptor, with the Action run appended: `0.0.1-beta-2`
- `release` branches will be tagged with an `rc` descriptor, with the Action run appended: `0.0.1-rc-3`
- `hotfix` branch builds will be tagged with a `hotfix` descriptor, with the Action run appended: `0.0.1-hotfix-4`

Builds on the `Main` branch will ONLY be triggered by manually creating a release using the web interface in the repository. They will be versioned with the tag that is created when the release is created. The tags MUST take the form `major.minor.revision` to be compatible with the build process. A tag like `v0.1.0-alpha` is NOT compatabile and may result in the build process failing.

If you have any questions about the action, contact [Andrew Welker](mailto:awelker@pepperdash.com) or [Neil Dorin](mailto:ndorin@pepperdash.com).

## Dependencies

The [Essentials](https://github.com/PepperDash/Essentials) libraries are required. They referenced via nuget. You must have nuget.exe installed and in the `PATH` environment variable to use the following command. Nuget.exe is available at [nuget.org](https://dist.nuget.org/win-x86-commandline/latest/nuget.exe).

### Installing Dependencies

To install dependencies once nuget.exe is installed, run the following command from the root directory of your repository:
`nuget install .\packages.config -OutputDirectory .\packages -excludeVersion`.
Alternatively, you can simply run the `GetPackages.bat` file.
To verify that the packages installed correctly, open the plugin solution in your repo and make sure that all references are found, then try and build it.

### Installing Different versions of PepperDash Core

If you need a different version of PepperDash Core, use the command `nuget install .\packages.config -OutputDirectory .\packages -excludeVersion -Version {versionToGet}`. Omitting the `-Version` option will pull the version indicated in the packages.config file.