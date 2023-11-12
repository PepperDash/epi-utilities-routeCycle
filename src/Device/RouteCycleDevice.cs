using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using System.Collections.Generic;
using System.Threading;
using RouteCycle.JoinMaps;

namespace RouteCycle.Factories
{
	/// <summary>
	/// Plugin device template for logic devices that don't communicate outside the program
	/// </summary>
	public class RouteCycleDevice : EssentialsBridgeableDevice
    {
        private int maxIO = 32;
        private CTimer shiftTimer;
        private List<OutputFeedback> outputFeedbacks { get; set;}
        TrackableArray<bool> _sourceEnable { get; set;}
        private bool _inUse { get; set; }

        /// <summary>
        /// Plugin device constructor
        /// </summary>
        /// <param name="key">Device unique key</param>
        /// <param name="name">Device friendly name</param>
        /// <param name="config">Device configuration</param>
        public RouteCycleDevice(string key, string name)
            : base(key, name)
        {
            Debug.Console(0, this, "Constructing new {0} instance", name);
            
            //Initialize your timer here and set interval
            shiftTimer = new CTimer(shiftTimer_Elapsed, 5000);  // 5000 ms = 5 seconds
            //CTimer initilizes right way, trigger method to stop timer if running
            SetTimerEnabled(false);
            outputFeedbacks = new List<OutputFeedback>();
            _sourceEnable = new TrackableArray<bool>(32);

            // Initialize the OutputFeedbacks collection
            for (ushort i = 0; i < maxIO; i++)
            {
                outputFeedbacks.Add(new OutputFeedback
                {
                    Index = i,
                    IndexEnabled = false,
                    IndexValue = 0,
                    IndexLabel = string.Format("Index {0}", i),
                    ShiftedIndex = 0,
                    ShiftedIndexValue = 0
                });
            }
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
            { bridge.AddJoinMap(Key, joinMap); }

            var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);

            if (customJoins != null)
            { joinMap.SetCustomJoinData(customJoins); }

            Debug.Console(1, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.Console(0, "Linking to Bridge Type {0}", GetType().Name);

            // Device joinMap triggers and feedback
            trilist.SetSigTrueAction(joinMap.InUse.JoinNumber, SetInUseStateTrue);
            trilist.SetSigFalseAction(joinMap.InUse.JoinNumber, SetInUseStateFalse);

            trilist.SetSigTrueAction(joinMap.CycleRoute.JoinNumber, CycleRoute);

            trilist.SetSigTrueAction(joinMap.SourcesClear.JoinNumber, SetSourcesClear);
            trilist.SetSigTrueAction(joinMap.DestinationsClear.JoinNumber, SetDestinationsClear);

            //trilist.SetSigTrueAction(joinMap.SourceSelect.JoinNumber, doNothing);
            //trilist.SetSigTrueAction(joinMap.SourceSelect.JoinNumber + 1, object);
            //trilist.SetSigTrueAction(joinMap.SourceSelect.JoinNumber + 2, object);
            //trilist.SetSigTrueAction(joinMap.DestinationSelect.JoinNumber, object);
            //trilist.SetSigTrueAction(joinMap.DestinationSelect.JoinNumber + 1, object);
            //trilist.SetSigTrueAction(joinMap.DestinationSelect.JoinNumber + 2, object);

            foreach (var kvp in outputFeedbacks)
            {
                // Get the actual join number of the signal
                var sourceSelectJoin = kvp.Index + joinMap.SourceSelect.JoinNumber - 1;
                var destinationSelectJoin = kvp.Index + joinMap.DestinationSelect.JoinNumber - 1;
                // Get the actual output number which is the item.Index as read in from the configuraiton file
                var output = kvp.Index;
                // Link incoming from SIMPL EISC bridge (aka route request) to internal method
                trilist.SetBoolSigAction(destinationSelectJoin, (input) => { kvp.IndexEnabled = input; });
            }
        }
        #endregion
        #region customDeviceLogic

        // Set InUse state
        private void SetInUseStateTrue(){ _inUse = true; }
        private void SetInUseStateFalse() { _inUse = false; }

        // Set all SoucesEnable values to false
        private void SetSourcesClear()
        {
            for (ushort i = 0; i < (maxIO - 1); i++) {
                _sourceEnable[i] = false;
            }
        }

        // Set all Destinations.IndexEnabled to false 
        private void SetDestinationsClear()
        {
            for (ushort i = 0; i < (maxIO - 1); i++)
            {
                outputFeedbacks[i].IndexEnabled = false;
            }
        }

        /// <summary>
        /// Method called when shifTimer expires.
        /// </summary>
        /// <param name="sender"></param>
        private void shiftTimer_Elapsed(object sender){
            CycleRoute();
        }
        
        /// <summary>
        /// The first loop prepares for the shift by setting up a
        /// "next value" (ShiftedIndexValue) for each enabled element.
        /// The second loop executes the shift by updating the actual 
        /// values (IndexValue and IndexLabel) of elements identified
        /// by their ShiftedIndex with the values and labels from the 
        /// current iteration.
        /// </summary>
        private void CycleRoute()
        {
            /// First loop
            for (int i = 0; i < outputFeedbacks.Count - 1; i++)
            {
                var current = outputFeedbacks[i];
                var next = outputFeedbacks[i + 1];
                if (current.IndexEnabled)
                {
                    current.ShiftedIndexValue = next.IndexValue;
                }
            }

            /// Second loop
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

        /// <summary>
        /// Method provides a simple interface to control the timer,
        /// allowing other parts of the code to enable or disable the timer
        /// functionality by starting or stopping the periodic execution of a
        /// callback method
        /// </summary>
        /// <param name="enabled"></param>
        public void SetTimerEnabled(bool enabled)
        {
            if (enabled)
                shiftTimer.Reset(1000);  // Reset and restart the timer
            else
                shiftTimer.Stop();  // Stop the timer
        }

        /// <summary>
        /// Retuns object at specific index containing three params
        /// within single object called OutputFeedback
        /// </summary>
        /// <param name="index">Index of OutputFeedback</param>
        /// <returns></returns>
        public OutputFeedback GetOutputFeedback(int index)
        {
            return outputFeedbacks[index];
        }

        /// <summary>
        /// Manually set OutputFeedback, requires full OutputFeedback object w/ three params
        /// </summary>
        /// <param name="feedback">Complex object w/ bool IndexEnabled, ushort IndexValue, string IndexLabel</param>
        public void SetOutputFeedback(OutputFeedback feedback)
        {
            var item = outputFeedbacks[feedback.Index];
            item.IndexEnabled = feedback.IndexEnabled;
            item.IndexValue = feedback.IndexValue;
            item.IndexLabel = feedback.IndexLabel;
        }

        // This will set the IndexEnabled property to true
        //outputFeedbacks.SetIndexEnabled(true);
        //outputFeedbacks.SetIndexValue(ushort);
        #endregion
    }

    /// <summary>
    /// OutputFeedback custom object to define array of outputs on bridge
    /// </summary>
    public class OutputFeedback
    {
        public ushort Index { get; set; }
        public bool IndexEnabled { get; set; }
        public ushort IndexValue { get; set; }
        public string IndexLabel { get; set; }
        public ushort ShiftedIndex { get; set; }
        public ushort ShiftedIndexValue { get; set; }

        // This method sets the value of the IndexEnabled property
        public void SetIndexEnabled(bool enabled){
            IndexEnabled = enabled; 
        }

        // This method sets the value of the IndexValue property
        public void SetIndexValue(ushort value){
            IndexValue = value;
        }

        // This method sets the value of the IndexLabel property
        public void SetIndexLabel(string label){
            IndexLabel = label;
        }
    }

    public class TrackableArray<T>
    {
        private T[] array;
        public int LastChangedIndex { get; private set; }

        public TrackableArray(int size)
        {
            array = new T[size];
            LastChangedIndex = -1;  // -1 indicates no changes made yet
        }

        public T this[int index]
        {
            get { return array[index]; }
            set
            {
                if (!array[index].Equals(value)) // Check if the value is actually changing
                {
                    array[index] = value;
                    LastChangedIndex = index; // Update the last changed index
                }
            }
        }

        // This method exists only if T is bool
        public void SetBoolean(int index, bool value)
        {
            if (typeof(T) == typeof(bool))
            {
                if (!Equals(array[index], value)) // Compare current value with new value
                {
                    array[index] = (T)(object)value; // Cast bool to T (since T is bool)
                    LastChangedIndex = index; // Update the last changed index
                }
            }
            else
            {
                throw new System.Exception("SetBoolean method can only be used with TrackableArray of type bool.");
            }
        }
    }
}

