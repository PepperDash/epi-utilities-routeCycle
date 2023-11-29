using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using System.Collections.Generic;
using RouteCycle.JoinMaps;
using System.Threading;
using System.Linq;
using System;

namespace RouteCycle.Factories
{
	/// <summary>
	/// Plugin device for logic devices that don't communicate outside the program
	/// </summary>
	public class RouteCycleDevice : EssentialsBridgeableDevice
    {
        private int _maxIO = 32;
        private int _targetSource;
        private bool _inUse { get; set; }
        private bool _reportNotifyMessageTrigger { get; set; }
        private string _reportNofityMessage {get; set; }
        private List<CustomDeviceCollectionWithFeedback> _destinationFeedbacks { get; set;}
        private List<CustomDeviceCollectionWithFeedback> _sourceFeedbacks { get; set; }
        private List<CustomDeviceCollection> _destinationDevice { get; set; }
        private List<CustomDeviceCollection> _sourceDevice { get; set; }
        private BoolFeedback _reportNotifyFeedback { get; set; }
        private StringFeedback _reportNotifyMessageFeedback { get; set; }

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
            _reportNotifyFeedback = new BoolFeedback(() => _reportNotifyMessageTrigger);
            _reportNotifyMessageFeedback = new StringFeedback(() => _reportNofityMessage);
            _destinationFeedbacks = new List<CustomDeviceCollectionWithFeedback>();
            _sourceFeedbacks = new List<CustomDeviceCollectionWithFeedback>();
            _sourceDevice = new List<CustomDeviceCollection>();
            _destinationDevice = new List<CustomDeviceCollection>();
            _targetSource = 0;

            // Initialize the _destinationFeedbacks collection
            for (ushort i = 0; i < _maxIO; i++)
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
            for (ushort i = 0; i < _maxIO; i++)
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
            trilist.SetSigTrueAction(joinMap.SourcesClear.JoinNumber, _clearAllSourceEnables);
            trilist.SetSigTrueAction(joinMap.DestinationsClear.JoinNumber, _clearAllDestinationEnables);
            _reportNotifyFeedback.LinkInputSig(trilist.BooleanInput[joinMap.ReportNotifyPulse.JoinNumber]);
            _reportNotifyMessageFeedback.LinkInputSig(trilist.StringInput[joinMap.ReportNotifyMessage.JoinNumber]);

            #region _destinationFeedbacks
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

                localKvp.OnIndexEnabledTrueChanged += handleDestinationIndexEnabledTrueChanged;
                localKvp.OnIndexEnabledFalseChanged += handleDestinationIndexEnabledFalseChanged;

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
            #endregion 

            #region _sourceFeedbacks
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
                
                localKvp.OnIndexEnabledTrueChanged += handleSourceIndexEnabledTrueChanged;
                localKvp.OnIndexEnabledFalseChanged += handleSourceIndexEnabledFalseChanged;
                localKvp.OnIndexValueChanged += handleSourceIndexValueChanged;

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
            #endregion 

            UpdateFeedbacks();

            trilist.OnlineStatusChange += (o, a) =>
            {
                if (!a.DeviceOnLine) return;
                UpdateFeedbacks();
            };
        }
        #endregion
        #region customDeviceMethods

        /// <summary>
        /// Handle destination index enabled true
        /// </summary>
        /// <param name="index">ushort value</param>
        /// <param name="indexValue">ushort value</param>
        private void handleDestinationIndexEnabledTrueChanged(ushort index, ushort indexValue)
        {
            // Search for an existing CustomDeviceCollection with the specified Index
            var existingItem = _destinationDevice.FirstOrDefault(item => item.Index == index);
            if (existingItem != null)
            {
                // An item with the desired index already exists, so update it rather than create it
                existingItem.Route = indexValue;
            }
            else
            {
                // No item with the desired index exists, so add a new one to the list
                _destinationDevice.Add(new CustomDeviceCollection
                {
                    Index = index,
                    Route = indexValue
                });

                Debug.Console(2, this, "_destinationDevice List Count = {0}", _destinationDevice.Count);
                foreach (var kvp in _destinationDevice)
                {
                    Debug.Console(2, this, @"
---
Item Index =  {0},
Item Route Value = {1},
--- 
", kvp.Index, kvp.Route);
                }
            }

        }

        /// <summary>
        /// Handle destination index enabled false
        /// </summary>
        /// <param name="index">ushort value</param>
        private void handleDestinationIndexEnabledFalseChanged(ushort index)
        {
            RemoveDeviceByIndex(_destinationDevice, index);
            Debug.Console(2, this, "_destinationDevice List Count = {0}", _destinationDevice.Count);
            foreach (var kvp in _destinationDevice)
            {
                Debug.Console(2, this, @"
---
Item Index =  {0},
Item Route Value = {1},
--- 
", kvp.Index, kvp.Route);
            }
        }

        /// <summary>
        /// Handle source index enabled true
        /// </summary>
        /// <param name="index">ushort value</param>
        /// <param name="indexValue">ushort value</param>
        private void handleSourceIndexEnabledTrueChanged(ushort index, ushort indexValue)
        {
            // Search for an existing CustomDeviceCollection with the specified Index
            var existingItem = _sourceDevice.FirstOrDefault(item => item.Index == index);

            if (existingItem != null)
            {
                // An item with the desired index already exists, so you might want to update it
                existingItem.Route = indexValue;
                Debug.Console(2, this, "Existing source w/ index: {0} found, updating w/ route value: {1}", index, indexValue);
            }
            else
            {
                Debug.Console(2, this, "creating source item w/ index: {0}, and route value: {1}", index, indexValue);
                // No item with the desired index exists, so add a new one to the list
                _sourceDevice.Add(new CustomDeviceCollection
                {
                    Index = index,
                    Route = indexValue
                });
            }
            Debug.Console(2, this, "_sourceDevice List Count = {0}", _sourceDevice.Count);
            foreach(var kvp in _sourceDevice)
            {
                Debug.Console(2, this, @"
---
Item Index =  {0},
Item Route Value = {1},
--- 
", kvp.Index, kvp.Route);
            }
        }

        /// <summary>
        /// Handle source index enabled false
        /// </summary>
        /// <param name="index">ushort value</param>
        private void handleSourceIndexEnabledFalseChanged(ushort index)
        {
            RemoveDeviceByIndex(_sourceDevice, index);
            Debug.Console(2, this, "_sourceDevice List Count = {0}", _sourceDevice.Count);
            foreach (var kvp in _sourceDevice)
            {
                Debug.Console(2, this, @"

---
Item Index =  {0},
Item Route Value = {1},
--- 
", kvp.Index, kvp.Route);
            }
        }

        /// <summary>
        /// Handle source index value changes
        /// </summary>
        /// <param name="index">ushort value</param>
        /// <param name="indexValue">ushort value</param>
        private void handleSourceIndexValueChanged(ushort index, ushort indexValue)
        {
            // Search for an existing CustomDeviceCollection with the specified Index
            var existingItem = _sourceDevice.FirstOrDefault(item => item.Index == index);
            if (existingItem != null)
            {
                // An item with the desired index already exists, so update it
                existingItem.Route = indexValue;
            }
            else
            {
                // No item with the desired index exists and a new one should not be created, so return
                return;
            }
        }

        /// <summary>
        /// Handle showing the Report Notify Message Pop-up
        /// </summary>
        private void handleShowMessage()
        {
            _reportNotifyFeedback.SetTestValue(true);
            CrestronEnvironment.Sleep(500);
            _reportNotifyFeedback.SetTestValue(false);
        }

        /// <summary>
        /// Handle updating the Report Notify Message string content
        /// </summary>
        /// <param name="text"></param>
        private void handleReportNotifyMessage(string text)
        {
            _reportNotifyMessageFeedback.SetTestValue(text);
            handleShowMessage();
        }

        /// <summary>
        /// Removes device in the Custom Device Collection list by index
        /// </summary>
        /// <param name="deviceList">Collection name</param>
        /// <param name="index">Ushort index value specific to the device</param>
        private void RemoveDeviceByIndex(List<CustomDeviceCollection> deviceList, ushort index)
        {
            // Find the item in the list with the matching Index
            var itemToRemove = deviceList.Find(item => item.Index == index);

            // If an item was found, remove it from the list
            if (itemToRemove != null)
            {
                deviceList.Remove(itemToRemove);
            }
        }

        /// <summary>
        /// Set all Souces.IndexEnabled to false
        /// </summary>
        private void _clearAllSourceEnables()
        {
            var message = "Clearing all Enabled Sources...";
            handleReportNotifyMessage(message);
            for (ushort i = 0; i < (_maxIO - 1); i++) {
                if(_sourceFeedbacks[i].IndexEnabled)
                    _sourceFeedbacks[i].IndexEnabled = true;
            }
        }

        /// <summary>
        /// Set all Destinations.IndexEnabled to false 
        /// </summary>
        private void _clearAllDestinationEnables()
        {
            var message = "Clearing all Enabled Destinations...";
            handleReportNotifyMessage(message);
            for (ushort i = 0; i < (_maxIO - 1); i++)
            {
                if(_destinationFeedbacks[i].IndexEnabled)
                    _destinationFeedbacks[i].IndexEnabled = true;
            }
        }

        /// <summary>
        /// Cycle enabled source routes to enabled destinations
        /// </summary>
        private void CycleRoute()
        {
            Debug.Console(2, this, "--RC Triggered--");
            if (!_inUse)
            {
                Debug.Console(2, this, "CycleRoute called while device InUse not set");
                handleReportNotifyMessage("CycleRoute called while device InUse not set");
                return;
            }

            if (_sourceDevice.Count == 0 || _destinationDevice.Count == 0) // Source and Destination count must be greater than 0
            {
                Debug.Console(2, this, "Source or destination count invalid while CycleRoute called. Source count = {0}, destination count = {1}.", _sourceDevice.Count, _destinationDevice.Count);
                handleReportNotifyMessage("Source or destination count invalid (both must be greater than zero) while CycleRoute called.");
                return;
            }

            for (int i = 0; i < _destinationDevice.Count; i++)
            {
                Debug.Console(2, this, "---");
                Debug.Console(2, this, "RC: i = {0}", i);
                // Ensure _targetSource is within the bounds of _sourceDevice before accessing it
                if (_targetSource >= 0 && _targetSource < _sourceDevice.Count)
                {
                    Debug.Console(2, this, "RC: _targetSource = {0}", _targetSource);
                    Debug.Console(2, this, "RC: _sourceDevice[_targetSource].Route = {0}", _sourceDevice[_targetSource].Route);
                    _destinationDevice[i].Route = _sourceDevice[_targetSource].Route;
                }
                else
                {
                    // Handle the case where _targetSource is out of bounds
                    Debug.Console(2, this, "Invalid target source index: {0}", _targetSource);
                    handleReportNotifyMessage("Invalid target source index while CycleRoute called.");
                    // You might want to break the loop or set _targetSource to a valid index
                    break;
                }

                // Increment _targetSource for the next iteration, resetting it to 0 if it exceeds the count
                _targetSource = (_targetSource + 1) % _sourceDevice.Count;
                Debug.Console(2, this, "RC: _targetSource incremented or reset, value = {0}", _targetSource);
            }
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
    /// Custom device collection to define and update input and output arrays on EPI bridge
    /// </summary>
    public class CustomDeviceCollectionWithFeedback
    {
        private bool _boolValue;
        private ushort _intValue;
        public readonly BoolFeedback FeedbackBoolean;
        public readonly IntFeedback FeedbackInteger;
        public event Action<ushort, ushort> OnIndexEnabledTrueChanged;
        public event Action<ushort, ushort> OnIndexValueChanged;
        public event Action<ushort> OnIndexEnabledFalseChanged;
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
                    if (_boolValue)
                    {
                        Debug.Console(2, "CDCWFB, FIEU, _boolValue TRUE");
                        if (OnIndexEnabledTrueChanged != null)
                            OnIndexEnabledTrueChanged(this.Index, this.IndexValue);
                        return;
                    }
                    else if (!_boolValue)
                    {
                        Debug.Console(2, "CDCWFB, FIEU, _boolValue FALSE");
                        if (OnIndexEnabledFalseChanged != null)
                            OnIndexEnabledFalseChanged(this.Index);
                    }
                }
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
                if (OnIndexValueChanged != null)
                    OnIndexValueChanged(this.Index, value); 
            }
        }
        public ushort ShiftedIndex { get; set; }
        public ushort ShiftedIndexValue { get; set; }

        /// <summary>
        /// Default consructor
        /// </summary>
        public CustomDeviceCollectionWithFeedback()
        {
            FeedbackBoolean = new BoolFeedback(() => _boolValue);
            FeedbackInteger = new IntFeedback(() => _intValue);
        }
        
        /// <summary>
        /// Sets the value of the IndexEnabled property
        /// </summary>
        /// <param name="enabled">bool value</param>
        public void SetIndexEnabled(bool enabled){
            IndexEnabled = enabled; 
        }

        /// <summary>
        /// Sets the value of the IndexValue property
        /// </summary>
        /// <param name="value">ushort value</param>
        public void SetIndexValue(ushort value){
            IndexValue = value;
        }

        /// <summary>
        /// Returns the IndexValue property ushort value
        /// </summary>
        /// <returns></returns>
        public ushort FireIndexValueUpdate(){
            return IndexValue;
        }

        /// <summary>
        /// Returns the IndexEbabled property bool value
        /// </summary>
        /// <returns></returns>
        public bool FireIndexEnabledUpdate()
        {
            return IndexEnabled;
        }
    }

    /// <summary>
    /// Custom device collection containing two ushort values (Index and Route) per collection
    /// </summary>
    public class CustomDeviceCollection
    {
        private ushort _indexValue;
        private ushort _routeValue;
        public ushort Index
        {
            get { return _indexValue; }
            set { _indexValue = value; }
        }
        public ushort Route
        {
            get { return _routeValue; }
            set { _routeValue = value; }
        }

        /// <summary>
        /// Sets the value of the Index value
        /// </summary>
        /// <param name="value">ushort value</param>
        public void SetIndex(ushort value)
        {
            _indexValue = value;
        }

        /// <summary>
        /// Sets the value of the Route property
        /// </summary>
        /// <param name="value">ushort value</param>
        public void SetRoute(ushort value)
        {
            _routeValue = value;
        }

        /// <summary>
        /// Returns the IndexValue ushort value
        /// </summary>
        /// <returns></returns>
        public ushort FireIndexUpdate()
        {
            return _indexValue;
        }

        /// <summary>
        /// Returns the Route ushort value
        /// </summary>
        /// <returns></returns>
        public ushort FireRouteUpdate()
        {
            return _routeValue;
        }
    }
}

