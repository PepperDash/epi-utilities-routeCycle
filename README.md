![PepperDash Logo](/images/essentials-plugin-blue.png)

## License

Provided under MIT license

# PepperDash Essentials Utilities Route Cycle Plugin (c) 2023

This repo contains a plugin for use with [PepperDash Essentials](https://github.com/PepperDash/Essentials). 

## Overview
The RouteCycle Device Plugin provides a solution for managing source to destination routing within Crestron control systems using the SIMPL# platform. This plugin allows for logic devices that do not communicate outside the program to interact with an EISC (Ethernet to Serial Control) bridge.

## Features
- Management of routing for a maximum of 32 Input/Output (IO) points.
- Handles route cycling which maps enabled sources to enabled destinations.
- Supports feedback mechanism to update state through EISC bridge.
- Implements custom events for handling route changes.

## Use Case
Typical use case involves a large count of sources with a small count of destinations. The desired amount of sources would cycle through the desired destinations.

### Plugin Configuration Object

Update the configuration object as needed for the plugin being developed.

```json
{
	"devices": [
		{
        "key": "routeCycle",
        "uid": 0,
        "name": "Route Cycle",
        "type": "routeCycle",
        "group": "switcher",        
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
        "key": "routeCycle-bridge",
        "uid": 0,
        "name": "Route Cycle Bridge",
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
              "deviceKey": "routeCycle",
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
| an_o (Input/Triggers) | I/O  | an_i (Feedback)            |
|-----------------------|------|----------------------------|
| Source Input          | 1-32 | Destination Source Request |
|                       | 33   |                            |
|                       | 34   |                            |
|                       | 35   |                            |
|                       | 36   |                            |

#### Serials
| serial-o (Input/Triggers) | I/O | serial-i (Feedback)  |
|---------------------------|-----|----------------------|
|                           | 1   | Notification Message |
|                           | 2   |                      |
|                           | 3   |                      |
|                           | 4   |                      |
|                           | 5   |                      |

# Plugin Classes

### RouteCycleDevice : EssentialsBridgeableDevice
This class is the core of the plugin and extends from the `EssentialsBridgeableDevice` class provided by the PepperDash Essentials framework.

#### Properties
- `Key`: Unique identifier for the device.
- `Name`: Friendly name for device identification and logging.
- `BoolValue`, `IntValue`, `StringValue`: Various types of feedback values that the device may report.

#### Methods
- `LinkToApi()`: Integrates the device with a SIMPL EISC to allow for seamless interaction and control.
- `CycleRoute()`: Cycles through the enabled routes, updating the destinations with the selected source routes.
- `UpdateFeedbacks()`: Updates all linked SIMPL bridge signals to reflect the current state.

### CustomDeviceCollectionWithFeedback
Represents a collection of custom devices with feedback capabilities. This class is meant to define and update input and output arrays on the EISC bridge.

#### Events
- `IndexEnabledChange`: Triggered when the index enabled state changes.
- `OnIndexEnabledTrueChanged`, `OnIndexEnabledFalseChanged`, `OnIndexValueChanged`: Triggered to notify subscribers of specific state changes.

### CustomDeviceCollection
This class contains a simple collection consisting of an index and a route value, allowing you to manage a list of devices and their corresponding routes.

### ROSBool : IDisposable
A utility class that encapsulates a boolean value with a built-in timer to automatically reset after a specified delay.

#### Events
- `ValueChanged`: Notifies subscribers when the boolean value changes.

## Usage
Create an instance of `RouteCycleDevice` by providing a unique key and name for the device. Link the device to the API using the `LinkToApi()` method, and use the `CycleRoute()` method to initiate route cycling within the system.

## Events Subscription
The plugin supports custom event subscriptions that allow other parts of the application to respond to routing changes. For instance, a subscriber can listen for when a route is enabled (`OnIndexEnabledTrueChanged`) and perform necessary actions in response.

For more information about the system architecture and for a detailed API reference, please refer to the official PepperDash Essentials documentation.

---

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