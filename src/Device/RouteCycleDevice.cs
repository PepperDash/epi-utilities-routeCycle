using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using System.Collections.Generic;
using System.Threading;
using RouteCycle.Config;
using RouteCycle.JoinMaps;

namespace RouteCycle.Factories
{
	/// <summary>
	/// Plugin device template for logic devices that don't communicate outside the program
	/// </summary>
	public class RouteCycleDevice : EssentialsBridgeableDevice
    {
        /// <summary>
        /// It is often desirable to store the config
        /// </summary>
        private RouteCycleConfigObject _config;

        /// <summary>
        /// Plugin device constructor
        /// </summary>
        /// <param name="key">Device unique key</param>
        /// <param name="name">Device friendly name</param>
        /// <param name="config">Device configuration</param>
        public RouteCycleDevice(string key, string name, RouteCycleConfigObject config)
            : base(key, name)
        {
            Debug.Console(0, this, "Constructing new {0} instance", name);
            _config = config;
        }

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
            var joinMap = new RouteCycleBridgeJoinMap(joinStart);

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

            // links to bridge
            trilist.SetString(joinMap.DeviceName.JoinNumber, Name);

            trilist.OnlineStatusChange += (o, a) =>
            {
                if (!a.DeviceOnLine) return;
                trilist.SetString(joinMap.DeviceName.JoinNumber, Name);
            };
        }
        #endregion
    }

    public class OutputFeedback
    {
        public ushort Index { get; set; }
        public bool IndexEnabled { get; set; }
        public ushort IndexValue { get; set; }
        public string IndexLabel { get; set; }
        public ushort ShiftedIndex { get; set; }
        public ushort ShiftedIndexValue { get; set; }
    }

    public class MyPluginBridgeEpi
    {
        private CTimer shiftTimer;
        private List<OutputFeedback> outputFeedbacks = new List<OutputFeedback>();

        //Initialize your timer here and set interval
        //shiftTimer = new Timer(1000); // 1000 ms = 1 second
        shiftTimer = new CTimer(ShiftTimer_Elapsed, null, 1000, 1000);  // 1000 ms = 1 second
        shiftTimer.Elapsed += ShiftTimer_Elapsed;

        // Initialize the OutputFeedbacks collection here
        for (ushort i = 0; i < 32; i++)
        {
            outputFeedbacks.Add(new OutputFeedback
            {
                Index = i,
                IndexEnabled = false,
                IndexValue = 0,
                IndexLabel = $"Index {i}",
                ShiftedIndex = 0,
                ShiftedIndexValue = 0
            });
        }
        
        private void ShiftTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // First loop
            for (int i = 0; i < outputFeedbacks.Count - 1; i++)
            {
                var current = outputFeedbacks[i];
                var next = outputFeedbacks[i + 1];
                if (current.IndexEnabled)
                {
                    current.ShiftedIndexValue = next.IndexValue;
                }
            }

            // Second loop
            for (int i = 0; i < outputFeedbacks.Count; i++)
            {
                var current = outputFeedbacks[i];
                if (current.IndexEnabled)
                {
                    var shiftedItem = outputFeedbacks[current.ShiftedIndex];
                    shiftedItem.IndexValue = current.IndexValue;
                    shiftedItem.IndexLabel = current.IndexLabel;
                }
            }
        }

        public void SetTimerEnabled(bool enabled)
        {
            if (enabled)
                shiftTimer.Start();
            else
                shiftTimer.Stop();
        }

        public OutputFeedback GetOutputFeedback(int index)
        {
            return outputFeedbacks[index];
        }

        public void SetOutputFeedback(OutputFeedback feedback)
        {
            var item = outputFeedbacks[feedback.Index];
            item.IndexEnabled = feedback.IndexEnabled;
            item.IndexValue = feedback.IndexValue;
            item.IndexLabel = feedback.IndexLabel;
        }

    }
}

