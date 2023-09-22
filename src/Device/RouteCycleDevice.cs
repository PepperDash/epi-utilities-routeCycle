using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Queues;
using PepperDash.Essentials.Core.DeviceInfo;
using RouteCycle.Config;
using RouteCycle.JoinMaps;


namespace RouteCycle.Devices
{
	/// <summary>
	/// Plugin device template for third party devices that use IBasicCommunication
	/// </summary>
	/// <remarks>
	/// Rename the class to match the device plugin being developed.
	/// </remarks>
	/// <example>
	/// "EssentialsPluginDeviceTemplate" renamed to "SamsungMdcDevice"
	/// </example>
	public class RouteCycleDevice : EssentialsBridgeableDevice
    {
        /// <summary>
        /// It is often desirable to store the config
        /// </summary>
        private EssentialsPluginTemplateConfigObject _config;

        #region IBasicCommunication Properties and Constructor.  Remove if not needed.

        // TODO [x] Add, modify, remove properties and fields as needed for the plugin being developed

		/// <summary>
		/// Plugin device constructor for devices that need IBasicCommunication
		/// </summary>
		/// <param name="key"></param>
		/// <param name="name"></param>
		/// <param name="config"></param>
		/// <param name="comms"></param>
        public RouteCycleDevice(string key, string name, EssentialsPluginTemplateConfigObject config, IBasicCommunication comms)
			: base(key, name)
		{
			Debug.Console(0, this, "Constructing new {0} instance", name);
			_config = config;
        }
        #endregion


        #region Overrides of EssentialsBridgeableDevice

        /// <summary>
        /// Links the plugin device to the EISC bridge
        /// </summary>
        /// <param name="trilist"></param>
        /// <param name="joinStart"></param>
        /// <param name="joinMapKey"></param>
        /// <param name="bridge"></param>
        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new EssentialsPluginTemplateBridgeJoinMap(joinStart);

            // This adds the join map to the collection on the bridge
            if (bridge != null)
            {
                bridge.AddJoinMap(Key, joinMap);
            }

            var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);

            if (customJoins != null)
            {
                joinMap.SetCustomJoinData(customJoins);
            }

            Debug.Console(1, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.Console(0, "Linking to Bridge Type {0}", GetType().Name);

            // TODO [x] Implement bridge links as needed

            // links to bridge
            trilist.SetString(joinMap.DeviceName.JoinNumber, Name);

            //trilist.SetBoolSigAction(joinMap.Connect.JoinNumber, sig => Connect = sig);
            //ConnectFeedback.LinkInputSig(trilist.BooleanInput[joinMap.Connect.JoinNumber]);

            //StatusFeedback.LinkInputSig(trilist.UShortInput[joinMap.Status.JoinNumber]);
            //OnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
        }
        #endregion
    }
}

