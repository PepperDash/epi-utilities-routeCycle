using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using System.Collections.Generic;
using System.Threading;
using System;
using RouteCycle.JoinMaps;

namespace RouteCycle.Factories
{
	/// <summary>
	/// Plugin device for logic devices that don't communicate outside the program
	/// </summary>
	public class RouteCycleDevice : EssentialsBridgeableDevice
    {
        private int maxIO = 32;
        private bool _inUse { get; set; }
        private List<CustomDeviceCollectionWithFeedback> _destinationFeedbacks { get; set;}
        private List<CustomDeviceCollectionWithFeedback> _sourceFeedbacks { get; set; }
        private List<CustomDeviceCollection> _destinationDevice { get; set; }
        private List<CustomDeviceCollection> _sourceDevice { get; set; }
        public Action<ushort, ushort> AddSourceDevice;
        public Action<ushort> RemoveSourceDevice;
        public Action<ushort, ushort> AddDestinationDevice;
        public Action<ushort> RemoveDestinationDevice;
       
        
        //public Action<ushort, ushort> AddSourceDevice = (ushortIndexValue, ushortRouteValue) =>
        //{
        //    _sourceDevice.Add(new CustomDeviceCollection
        //    {
        //        Index = ushortIndexValue,
        //        Route = ushortRouteValue
        //    });
        //};
        //public Action<ushort> RemoveSourceDevice = (ushortIndexValue) =>
        //{
        //    _sourceDevice.RemoveAt(ushortIndexValue);
        //};
        //public Action<ushort, ushort> AddDestinationDevice = (ushortIndexValue, ushortRouteValue) =>
        //{
        //    _sourceDevice.Add(new CustomDeviceCollection
        //    {
        //        Index = ushortIndexValue,
        //        Route = ushortRouteValue
        //    });
        //};
        //public Action<ushort> RemoveDestinationDevice = (ushortIndexValue) =>
        //{
        //    _destinationDevice.RemoveAt(ushortIndexValue);
        //};

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
            _sourceDevice = new List<CustomDeviceCollection>();
            _destinationDevice = new List<CustomDeviceCollection>();

            // Initialize the _destinationFeedbacks collection
            for (ushort i = 1; i < maxIO; i++)
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
            for (ushort i = 1; i < maxIO; i++)
            {
                _sourceFeedbacks.Add(new CustomDeviceCollectionWithFeedback
                {
                    Index = i,
                    IndexEnabled = false,
                    IndexValue = 0,
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
            trilist.SetBoolSigAction(joinMap.InUse.JoinNumber, (input) => { _inUse = input; });
            trilist.SetSigTrueAction(joinMap.CycleRoute.JoinNumber, CycleRoute);
            trilist.SetSigTrueAction(joinMap.SourcesClear.JoinNumber, _setSourceEnablesClear);
            trilist.SetSigTrueAction(joinMap.DestinationsClear.JoinNumber, _setDestinationEnablesClear);

            // Now, you can assign the lambda expression to the AddSourceDevice delegate
            AddSourceDevice = (ushortIndexValue, ushortRouteValue) =>
            {
                _sourceDevice.Add(new CustomDeviceCollection
                {
                    Index = ushortIndexValue,
                    Route = ushortRouteValue
                });
            };

            foreach (var kvp in _destinationFeedbacks)
            {
                // Create a local copy of the loop variable
                // Note: If you don't assign a local variable within the foreach loop the lambda will use the last value
                // assigned to the variable, which will be the last item of the foreach loop.
                var localKvp = kvp;

                // Get the actual join number of the signal
                var destinationSelectJoin = localKvp.Index + joinMap.DestinationSelect.JoinNumber;
                // Link incoming from SIMPL EISC bridge (AKA destination select) to internal method
                trilist.SetBoolSigAction(destinationSelectJoin, (input) => { localKvp.IndexEnabled = input; });

                // Link outbound SIMPL EISC bridge signal from internal method
                var feedbackEnabled = localKvp.FeedbackBoolean;
                if (feedbackEnabled == null) continue;
                feedbackEnabled.LinkInputSig(trilist.BooleanInput[destinationSelectJoin]);

                // Get the actual join number of the signal
                var DestinationRouteJoin = localKvp.Index + joinMap.DestinationRouteOut.JoinNumber;
                trilist.SetUShortSigAction(DestinationRouteJoin, (input) => { localKvp.IndexValue = input; });

                // Link outbound SIMPL EISC bridge signal from internal method
                var feedbackIndex = localKvp.FeedbackInteger;
                if (feedbackIndex == null) continue;
                feedbackIndex.LinkInputSig(trilist.UShortInput[DestinationRouteJoin]);
            }

            foreach (var kvp in _sourceFeedbacks)
            {
                // Create a local copy of the loop variable
                // Note: If you don't assign a local variable within the foreach loop the lambda will use the last value
                // assigned to the variable, which will be the last item of the foreach loop.
                var localKvp = kvp;

                // Get the actual join number of the signal
                var sourceSelectJoin = localKvp.Index + joinMap.SourceSelect.JoinNumber;
                // Link incoming from SIMPL EISC bridge to internal method
                trilist.SetBoolSigAction(sourceSelectJoin, (input) => { localKvp.IndexEnabled = input; });

                //trilist.SetUShortSigAction(sourceSelectJoin, (input) => { _addSourceDevice(input); });

                // Link inbound SIMPL EISC bridge signal to internal method
                var feedbackEnabled = localKvp.FeedbackBoolean;
                if (feedbackEnabled == null) continue;
                feedbackEnabled.LinkInputSig(trilist.BooleanInput[sourceSelectJoin]);

                // Get the actual join number of the signal
                var SourceInputValueJoin = localKvp.Index + joinMap.SourceInputValue.JoinNumber;
                trilist.SetUShortSigAction(SourceInputValueJoin, (input) => { localKvp.IndexValue = input; });

                // Link inbound SIMPL EISC bridge signal to internal method
                var feedbackIndex = localKvp.FeedbackInteger;
                if (feedbackIndex == null) continue;
                feedbackIndex.LinkInputSig(trilist.UShortInput[SourceInputValueJoin]);
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
            if (!_inUse)
            {
                Debug.Console(2, this, "CycleRoute called while device InUse not set");
                return;
            }

            var DestinationEnabledCount = GetDestinationCountEnabled();
            var SourceEnabledCount = GetSourceCountEnabled();

            if (SourceEnabledCount < DestinationEnabledCount) //Source count must be >= Destination count
            {
                Debug.Console(2, this, "Source count invalid while CycleRoute called. Source count must be greater than or equal to Destination count.");
                return;
            }

            /// First loop
            for (int i = 0; i < _destinationFeedbacks.Count - 1; i++)
            {
                var current = _destinationFeedbacks[i];
                var next = _destinationFeedbacks[i + 1];
                Debug.Console(2, this, "--------------------");
                Debug.Console(2, this, "FL: Count: {0}", i);
                if (current.IndexEnabled)
                {
                    Debug.Console(2, this, "FL: current.IndexValue = {0}", current.IndexValue);
                    current.ShiftedIndexValue = next.IndexValue;
                    Debug.Console(2, this, "FL: current.ShiftedIndexValue = {0}", current.ShiftedIndexValue);
                }
            }

            /// Second loop
            for (int i = 0; i < _destinationFeedbacks.Count - 1; i++)
            {
                var current = _destinationFeedbacks[i];

                Debug.Console(2, this, "--------------------");
                if (current.IndexEnabled)
                {
                    var shiftedItem = _destinationFeedbacks[current.ShiftedIndex];
                    Debug.Console(2, this, "SL: current.IndexValue = {0}", current.IndexValue);
                    shiftedItem.IndexValue = current.IndexValue;
                    Debug.Console(2, this, "SL: current.ShiftedIndexValue = {0}", current.ShiftedIndexValue);
                }
            }  
        }

        /// <summary>
        /// Call to return count of DestinationFeedbacks enabled
        /// </summary>
        /// <returns>INT count of destinations enabled</returns>
        private int GetDestinationCountEnabled()
        {
            var count = 0;
            foreach(var kvp in _destinationFeedbacks)
            {
                if (kvp.IndexEnabled)
                    count = count + 1;
            }
            return count;
        }

        /// <summary>
        /// Call to return count of SourceFeedbacks enabled
        /// </summary>
        /// <returns>INT count of sources enabled</returns>
        private int GetSourceCountEnabled()
        {
            var count = 0;
            foreach (var kvp in _sourceFeedbacks)
            {
                if (kvp.IndexEnabled)
                    count = count + 1;
            }
            return count;
        }

        /// <summary>
        /// Retuns object at specific index containing three params
        /// within single object called OutputFeedback
        /// </summary>
        /// <param name="index">Index of OutputFeedback</param>
        /// <returns></returns>
        private CustomDeviceCollectionWithFeedback GetCustomDeviceCollectionInstance(int index)
        {
            return _destinationFeedbacks[index];
        }

        /// <summary>
        /// Manually set OutputFeedback, requires full OutputFeedback object w/ three params
        /// </summary>
        /// <param name="feedback">Complex object w/ bool IndexEnabled, ushort IndexValue, string IndexLabel</param>
        private void SetCustomDeviceCollectionInstance(CustomDeviceCollectionWithFeedback feedback)
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
            foreach (var item in _sourceFeedbacks)
                item.FeedbackInteger.FireUpdate();
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
                // Only toggle the value if the incoming value is true
                if (value == true)
                {
                    _boolValue = !_boolValue;
                    FeedbackBoolean.FireUpdate();
                    return;
                }
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

    /// <summary>
    /// Custom device collection to define array of outputs on bridge
    /// </summary>
    public class CustomDeviceCollection
    {
        private ushort _indexValue;
        private ushort _routeValue;

        public ushort Index
        {
            get
            {
                return _indexValue;
            }
            set
            {
                _indexValue = value;
            }
        }

        public ushort Route
        {
            get
            {
                return _routeValue;
            }
            set
            {
                _routeValue = value;
            }
        }

        // Method sets the value of the IndexValue property
        public void SetIndex(ushort value)
        {
            _indexValue = value;
        }

        // Method sets the value of the IndexValue property
        public void SetRoute(ushort value)
        {
            _routeValue = value;
        }

        // Method returns the IndexValue property ushort value
        public ushort FireIndexUpdate()
        {
            return _indexValue;
        }

        // Method returns the IndexValue property ushort value
        public ushort FireRouteUpdate()
        {
            return _routeValue;
        }
    }
}

