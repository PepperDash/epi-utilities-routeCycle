﻿using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using Crestron.SimplSharpPro.UI;
using RouteCycle.JoinMaps;

namespace RouteCycle.Factories
{
    /// <summary>
    /// Plugin device factory for logic devices that don't communicate
    /// </summary>
    public class RouteCycleFactoryDevice : EssentialsPluginDeviceFactory<RouteCycleDevice>
    {
		/// <summary>
		/// Plugin device factory constructor
		/// </summary>
        public RouteCycleFactoryDevice()
        {
            // Set the minimum Essentials Framework Version
			// TODO [x] Update the Essentials minimum framework version which this plugin has been tested against
			MinimumEssentialsFrameworkVersion = "2.4.4";

            // In the constructor we initialize the list with the typenames that will build an instance of this device
			// TODO [x] Update the TypeNames for the plugin being developed
            TypeNames = new List<string>() { "routeCycle", "utilitiesRouteCycle" };
        }
        
		/// <inheritdoc/>
        public override EssentialsDevice BuildDevice(PepperDash.Essentials.Core.Config.DeviceConfig dc)
        {

            Debug.LogDebug("[{0}] Factory Attempting to create new device from type: {1}", dc.Key, dc.Type);

            var controlConfig = CommFactory.GetControlPropertiesConfig(dc);

            return new RouteCycleDevice(dc.Key, dc.Name);
        }
    }
}

          