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
        private List<CustomDeviceCollectionWithFeedback> _destinationFeedbacks { get; set;}
        private List<CustomDeviceCollectionWithFeedback> _sourceFeedbacks { get; set; }
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
            _destinationFeedbacks = new List<CustomDeviceCollectionWithFeedback>();
            _sourceFeedbacks = new List<CustomDeviceCollectionWithFeedback>();

            // Initialize the _destinationFeedbacks collection
            for (ushort i = 0; i < maxIO; i++)
            {
                _destinationFeedbacks.Add(new CustomDeviceCollectionWithFeedback
                {
                    Index = i,
                    IndexEnabled = false,
                    IndexValue = 0,
                    ShiftedIndex = 0,
                    ShiftedIndexValue = 0
                });
            }

            // Initialize the _sourceFeedbacks collection
            for (ushort i = 0; i < maxIO; i++)
            {
                _sourceFeedbacks.Add(new CustomDeviceCollectionWithFeedback
                {
                    Index = i,
                    IndexEnabled = false
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
            trilist.SetSigTrueAction(joinMap.InUse.JoinNumber, _setInUseStateTrue);
            trilist.SetSigFalseAction(joinMap.InUse.JoinNumber, _setInUseStateFalse);

            trilist.SetSigTrueAction(joinMap.CycleRoute.JoinNumber, CycleRoute);
            trilist.SetSigTrueAction(joinMap.SourcesClear.JoinNumber, _setSourceEnablesClear);
            trilist.SetSigTrueAction(joinMap.DestinationsClear.JoinNumber, _setDestinationEnablesClear);

            foreach (var kvp in _destinationFeedbacks)
            {
                // Get the actual join number of the signal
                var destinationSelectJoin = kvp.Index + joinMap.DestinationSelect.JoinNumber - 1;
                // Link incoming from SIMPL EISC bridge (aka route request) to internal method
                trilist.SetBoolSigAction(destinationSelectJoin, (input) => { kvp.IndexEnabled = input; });
                trilist.SetUShortSigAction(destinationSelectJoin, (input) => { kvp.IndexValue = input; });

                var feedbackEnabled = kvp.FeedbackBoolean;
                if (feedbackEnabled == null) continue;
                feedbackEnabled.LinkInputSig(trilist.BooleanInput[destinationSelectJoin]);
                var feedbackIndex = kvp.FeedbackInteger;
                if (feedbackIndex == null) continue;
                feedbackIndex.LinkInputSig(trilist.UShortInput[destinationSelectJoin]);
            }

            foreach (var kvp in _destinationFeedbacks)
            {
                // Get the actual join number of the signal
                var sourceSelectJoin = kvp.Index + joinMap.SourceSelect.JoinNumber - 1;
                // Link incoming from SIMPL EISC bridge to internal method
                trilist.SetBoolSigAction(sourceSelectJoin, (input) => { kvp.IndexEnabled = input; });

                var feedbackEnabled = kvp.FeedbackBoolean;
                if (feedbackEnabled == null) continue;
                feedbackEnabled.LinkInputSig(trilist.BooleanInput[sourceSelectJoin]);
            }

            UpdateFeedbacks();

            trilist.OnlineStatusChange += (o, a) =>
            {
                if (!a.DeviceOnLine) return;
                UpdateFeedbacks();
            };
        }
        #endregion
        #region customDeviceLogic

        // Set _inUse state
        private void _setInUseStateTrue(){ _inUse = true; }
        private void _setInUseStateFalse() { _inUse = false; }

        // Set all Souces Enable booleans to false
        private void _setSourceEnablesClear()
        {
            for (ushort i = 0; i < (maxIO - 1); i++) {
                _sourceFeedbacks[i].IndexEnabled = false;
            }
        }

        // Set all Destinations.IndexEnabled to false 
        private void _setDestinationEnablesClear()
        {
            for (ushort i = 0; i < (maxIO - 1); i++)
            {
                _destinationFeedbacks[i].IndexEnabled = false;
            }
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
            for (int i = 0; i < _destinationFeedbacks.Count - 1; i++)
            {
                var current = _destinationFeedbacks[i];
                var next = _destinationFeedbacks[i + 1];
                if (current.IndexEnabled)
                {
                    current.ShiftedIndexValue = next.IndexValue;
                }
            }

            /// Second loop
            for (int i = 0; i < _destinationFeedbacks.Count; i++)
            {
                var current = _destinationFeedbacks[i];
                if (current.IndexEnabled)
                {
                    var shiftedItem = _destinationFeedbacks[current.ShiftedIndex];
                    shiftedItem.IndexValue = current.IndexValue;
                }
            }
        }

        /// <summary>
        /// Retuns object at specific index containing three params
        /// within single object called OutputFeedback
        /// </summary>
        /// <param name="index">Index of OutputFeedback</param>
        /// <returns></returns>
        public CustomDeviceCollectionWithFeedback GetCustomDeviceCollectionInstance(int index)
        {
            return _destinationFeedbacks[index];
        }

        /// <summary>
        /// Manually set OutputFeedback, requires full OutputFeedback object w/ three params
        /// </summary>
        /// <param name="feedback">Complex object w/ bool IndexEnabled, ushort IndexValue, string IndexLabel</param>
        public void SetCustomDeviceCollectionInstance(CustomDeviceCollectionWithFeedback feedback)
        {
            var item = _destinationFeedbacks[feedback.Index];
            item.IndexEnabled = feedback.IndexEnabled;
            item.IndexValue = feedback.IndexValue;
        }

        /// <summary>
        /// Void method that updates Feedbacks which updates Bridge
        /// </summary>
        private void UpdateFeedbacks()
        {
            foreach (var item in _destinationFeedbacks)
                item.FeedbackBoolean.FireUpdate();
            foreach (var item in _destinationFeedbacks)
                item.FeedbackInteger.FireUpdate();
            foreach (var item in _sourceFeedbacks)
                item.FeedbackBoolean.FireUpdate();
        }
        #endregion
    }

    /// <summary>
    /// Custom device collection to define array of outputs on bridge
    /// </summary>
    public class CustomDeviceCollectionWithFeedback
    {
        private bool _boolValue;
        private ushort _intValue;
        public readonly BoolFeedback FeedbackBoolean;
        public readonly IntFeedback FeedbackInteger;
        public ushort Index { get; set; }
        public bool IndexEnabled 
        { 
            get 
            {
                return _boolValue;
            } 
            set 
            {
                _boolValue = value;
                FeedbackBoolean.FireUpdate();
            }
        }
        public ushort IndexValue
        {
            get
            {
                return _intValue;
            }
            set
            {
                _intValue = value;
                FeedbackInteger.FireUpdate();
            }
        }
        public ushort ShiftedIndex { get; set; }
        public ushort ShiftedIndexValue { get; set; }

        public CustomDeviceCollectionWithFeedback()
        {
            FeedbackBoolean = new BoolFeedback(() => _boolValue);
            FeedbackInteger = new IntFeedback(() => _intValue);
        }

        // Method sets the value of the IndexEnabled property
        public void SetIndexEnabled(bool enabled){
            IndexEnabled = enabled; 
        }

        // Method sets the value of the IndexValue property
        public void SetIndexValue(ushort value){
            IndexValue = value;
        }

        // Method returns the IndexValue property ushort value
        public ushort FireIndexValueUpdate(){
            return IndexValue;
        }

        // Method returns the IndexEbabled property bool value
        public bool FireIndexEnabledUpdate()
        {
            return IndexEnabled;
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

