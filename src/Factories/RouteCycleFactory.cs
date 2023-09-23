﻿using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using Crestron.SimplSharpPro.UI;
using RouteCycle.JoinMaps;
using RouteCycle.Config;

namespace RouteCycle.Factories
{
    /// <summary>
    /// Plugin device factory for logic devices that don't communicate
    /// </summary>
    /// <remarks>
    /// Rename the class to match the device plugin being developed
    /// </remarks>
    /// <example>
    /// "EssentialsPluginFactoryTemplate" renamed to "MyLogicDeviceFactory"
    /// </example>
    public class RouteCycleFactoryDevice : EssentialsPluginDeviceFactory<RouteCycleDevice>
    {
		/// <summary>
		/// Plugin device factory constructor
		/// </summary>
		/// <remarks>
		/// Update the MinimumEssentialsFrameworkVersion & TypeNames as needed when creating a plugin
		/// </remarks>
		/// <example>
 		/// Set the minimum Essentials Framework Version
		/// <code>
		/// MinimumEssentialsFrameworkVersion = "1.6.4";
        /// </code>
		/// In the constructor we initialize the list with the typenames that will build an instance of this device
        /// <code>
		/// TypeNames = new List<string>() { "SamsungMdc", "SamsungMdcDisplay" };
        /// </code>
		/// </example>
        public RouteCycleFactoryDevice()
        {
            // Set the minimum Essentials Framework Version
			// TODO [x] Update the Essentials minimum framework version which this plugin has been tested against
			MinimumEssentialsFrameworkVersion = "1.15.2";

            // In the constructor we initialize the list with the typenames that will build an instance of this device
			// TODO [x] Update the TypeNames for the plugin being developed
            TypeNames = new List<string>() { "routeCycle" };
        }
        
		/// <summary>
        /// Builds and returns an instance of EssentialsPluginTemplateLogicDevice
		/// </summary>
		/// <param name="dc">device configuration</param>
		/// <returns>plugin device or null</returns>
		/// <remarks>		
		/// The example provided below takes the device key, name, properties config and the comms device created.
		/// Modify the EssetnialsPlugingDeviceTemplate constructor as needed to meet the requirements of the plugin device.
		/// </remarks>
		/// <seealso cref="PepperDash.Core.eControlMethod"/>
        public override EssentialsDevice BuildDevice(PepperDash.Essentials.Core.Config.DeviceConfig dc)
        {

            Debug.Console(1, "[{0}] Factory Attempting to create new device from type: {1}", dc.Key, dc.Type);

            // get the plugin device properties configuration object & check for null 
            var propertiesConfig = dc.Properties.ToObject<EssentialsPluginTemplateConfigObject>();
            if (propertiesConfig == null)
            {
                Debug.Console(0, "[{0}] Factory: failed to read properties config for {1}", dc.Key, dc.Name);
                return null;
            }

            var controlConfig = CommFactory.GetControlPropertiesConfig(dc);

            if (controlConfig == null)
            {
                return new RouteCycleDevice(dc.Key, dc.Name, propertiesConfig);
            }
            else
            {
                Debug.Console(0, "[{0}] Factory: Unable to get control properties from device config for {1}", dc.Key, dc.Name);
                return null;
            }
        }
    }
}

          