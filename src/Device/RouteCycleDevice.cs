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
        private bool _routeCycleBusy { get; set; }
        private bool _reportNotifyMessageTrigger { get; set; }
        private string _reportNofityMessage {get; set; }
        private List<CustomDeviceCollectionWithFeedback> _destinationFeedbacks { get; set;}
        private List<CustomDeviceCollectionWithFeedback> _sourceFeedbacks { get; set; }
        private List<CustomDeviceCollection> _destinationDevice { get; set; }
        private List<CustomDeviceCollection> _sourceDevice { get; set; }
        private BoolFeedback _reportNotifyFeedback { get; set; }
        private StringFeedback _reportNotifyMessageFeedback { get; set; }
        private ROSBool _customCollectionWithFeedbackBusy { get; set; }

        /// <summary>
        /// Plugin device constructor
        /// </summary>
        /// <param name="key">Device unique key</param>
        /// <param name="name">Device friendly name</param>
        public RouteCycleDevice(string key, string name)
            : base(key, name)
        {
            CrestronConsole.AddNewConsoleCommand(_reportDestinationFeedbacksIndexValues, "reportrcinfo", "Reports the Route Cycle Destination Feedback Index Values", ConsoleAccessLevelEnum.AccessOperator);
            Debug.LogInformation(this, "Constructing new {0} instance", name);
            _customCollectionWithFeedbackBusy = new ROSBool(2000);
            _reportNotifyFeedback = new BoolFeedback(() => _reportNotifyMessageTrigger);
            _reportNotifyMessageFeedback = new StringFeedback(() => _reportNofityMessage);
            _destinationFeedbacks = new List<CustomDeviceCollectionWithFeedback>();
            _sourceFeedbacks = new List<CustomDeviceCollectionWithFeedback>();
            _sourceDevice = new List<CustomDeviceCollection>();
            _destinationDevice = new List<CustomDeviceCollection>();
            _routeCycleBusy = new bool();
            _targetSource = 0;

            // Initialize the _destinationFeedbacks collection
            for (ushort i = 0; i < _maxIO; i++)
            {
                _destinationFeedbacks.Add(new CustomDeviceCollectionWithFeedback
                {
                    Index = i,
                    IndexEnabled = false,
                    IndexValue = 0
                });
            }

            // Initialize the _sourceFeedbacks collection
            for (ushort i = 0; i < _maxIO; i++)
            {
                _sourceFeedbacks.Add(new CustomDeviceCollectionWithFeedback
                {
                    Index = i,
                    IndexEnabled = false,
                    IndexValue = 0
                });
            }

            foreach (var item in _destinationFeedbacks)
            { item.IndexEnabledChange += TriggerROSBool; }
            foreach (var item in _sourceFeedbacks)
            { item.IndexEnabledChange += TriggerROSBool; }
        }
        #region Overrides of EssentialsBridgeableDevice

        /// <inheritdoc/>
        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new RouteCycleBridgeJoinMap(joinStart);

            // This adds the join map to the collection on the bridge
            if (bridge != null)
            { bridge.AddJoinMap(Key, joinMap); }

            var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);

            if (customJoins != null)
            { joinMap.SetCustomJoinData(customJoins); }

            Debug.LogDebug(this, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.LogDebug(this, "Linking to Bridge Type {0}", GetType().Name);

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

                Debug.LogVerbose(this, "_destinationDevice List Count = {0}", _destinationDevice.Count);
                foreach (var kvp in _destinationDevice)
                {
                    Debug.LogVerbose(this, @"
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
            Debug.LogVerbose(this, "_destinationDevice List Count = {0}", _destinationDevice.Count);
            foreach (var kvp in _destinationDevice)
            {
                Debug.LogVerbose(this, @"
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
                Debug.LogVerbose(this, "Existing source w/ index: {0} found, updating w/ route value: {1}", index, indexValue);
            }
            else
            {
                Debug.LogVerbose(this, "creating source item w/ index: {0}, and route value: {1}", index, indexValue);
                // No item with the desired index exists, so add a new one to the list
                _sourceDevice.Add(new CustomDeviceCollection
                {
                    Index = index,
                    Route = indexValue
                });
            }
            Debug.LogVerbose(this, "_sourceDevice List Count = {0}", _sourceDevice.Count);
            foreach(var kvp in _sourceDevice)
            {
                Debug.LogVerbose(this, @"
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
            Debug.LogVerbose(this, "_sourceDevice List Count = {0}", _sourceDevice.Count);
            foreach (var kvp in _sourceDevice)
            {
                Debug.LogVerbose(this, @"

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
            if (_routeCycleBusy)
            {
                Debug.LogVerbose(this, "CycleRoute busy, wait for it to complete before duplicate calls");
                handleReportNotifyMessage("CycleRoute busy, wait for it to complete before duplicate calls");
                return;
            }
            if (_customCollectionWithFeedbackBusy == null)
            {
                Debug.LogError(this, "_customCollectionWithFeedbackBusy is null");
                return;
            }

            if (_sourceDevice == null)
            {
                Debug.LogError(this, "_sourceDevice is null");
                return;
            }

            if (_destinationDevice == null)
            {
                Debug.LogError(this, "_destinationDevice is null");
                return;
            }

            if (_destinationFeedbacks == null)
            {
                Debug.LogError(this, "_destinationFeedbacks is null");
                return;
            }

            if (_customCollectionWithFeedbackBusy.Value)
            {
                Debug.LogVerbose(this, "CycleRoute not available while sources and destination changing");
                handleReportNotifyMessage("CycleRoute not available while sources and destinations change");
                return;  
            }

            // Set block for toggling sources and destinations
            _routeCycleBusy = true;

            Debug.LogVerbose(this, "--RC Triggered--");
            if (!_inUse)
            {
                Debug.LogVerbose(this, "CycleRoute called while device InUse not set");
                handleReportNotifyMessage("CycleRoute called while device InUse not set");
                _routeCycleBusy = false;
                return;
            }

            if (_sourceDevice.Count == 0 || _destinationDevice.Count == 0) // Source and Destination count must be greater than 0
            {
                Debug.LogError(this, "Source or destination count invalid while CycleRoute called. Source count = {0}, destination count = {1}.", _sourceDevice.Count, _destinationDevice.Count);
                handleReportNotifyMessage("Source or destination count invalid (both must be greater than zero) while CycleRoute called.");
                _routeCycleBusy = false;
                return;
            }

            for (int i = 0; i < _destinationDevice.Count; i++)
            {
                Debug.LogVerbose(this, "---");
                Debug.LogVerbose(this, "RC: i = {0}", i);
                // Ensure _targetSource is within the bounds of _sourceDevice before accessing it
                if (_targetSource >= 0 && _targetSource < _sourceDevice.Count)
                {
                    Debug.LogVerbose(this, "RC: _targetSource = {0}", _targetSource);
                    Debug.LogVerbose(this, "RC: _sourceDevice[_targetSource].Route = {0}", _sourceDevice[_targetSource].Route);
                    _destinationDevice[i].Route = _sourceDevice[_targetSource].Route;
                    var tempIndex = _destinationDevice[i].Index;

                    Debug.LogVerbose(this, "RC: tempIndex = {0}", tempIndex);
                    _destinationFeedbacks[tempIndex].IndexValue = _sourceDevice[_targetSource].Route;
                    _destinationFeedbacks[tempIndex].FeedbackInteger.InvokeFireUpdate();
                }
                else
                {
                    // Handle the case where _targetSource is out of bounds
                    Debug.LogError(this, "Invalid target source index: {0}", _targetSource);
                    handleReportNotifyMessage("Invalid target source index while CycleRoute called.");
                    // You might want to break the loop or set _targetSource to a valid index
                    break;
                }

                // Increment _targetSource for the next iteration, resetting it to 0 if it exceeds the count
                _targetSource = (_targetSource + 1) % _sourceDevice.Count;
                Debug.LogVerbose(this, "RC: _targetSource incremented or reset, value = {0}", _targetSource);
            }
            _routeCycleBusy = false;
        }

        /// <summary>
        /// Update Feedbacks and SIMPL Bridge
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

        /// <summary>
        /// Trigger custom ROSBool class bool
        /// </summary>
        private void TriggerROSBool()
        {
            Debug.LogVerbose(this, "TriggerROSBool Triggered");
            _customCollectionWithFeedbackBusy.Value = true;
        }

        private void _reportDestinationFeedbacksIndexValues(string command)
        {
            foreach (var item in _destinationFeedbacks)
            {
                if (item != null)
                    Debug.LogVerbose(this, "All _destFdbk Index Value = {0}", item.IndexValue);
            }
        }
        #endregion
    }

    /// <summary>
    /// Custom device collection to define and update input and output arrays on EPI bridge
    /// </summary>
    public class CustomDeviceCollectionWithFeedback : IDisposable
    {
        private bool _boolValue;
        private ushort _intValue;
        
        /// <summary>
        /// Boolean feedback for the enabled state
        /// </summary>
        public readonly BoolFeedback FeedbackBoolean;
        
        /// <summary>
        /// Integer feedback for the index value
        /// </summary>
        public readonly IntFeedback FeedbackInteger;
        
        /// <summary>
        /// Custom event handler delegate
        /// </summary>
        public delegate void CustomEventHandler();
        
        /// <summary>
        /// Event raised when index enabled state changes
        /// </summary>
        public event CustomEventHandler IndexEnabledChange;
        
        /// <summary>
        /// Event raised when index is enabled and set to true
        /// </summary>
        public event Action<ushort, ushort> OnIndexEnabledTrueChanged;
        
        /// <summary>
        /// Event raised when index value changes
        /// </summary>
        public event Action<ushort, ushort> OnIndexValueChanged;
        
        /// <summary>
        /// Event raised when index is enabled and set to false
        /// </summary>
        public event Action<ushort> OnIndexEnabledFalseChanged;
        
        /// <summary>
        /// Gets or sets the index value
        /// </summary>
        public ushort Index { get; set; }
        
        /// <summary>
        /// Gets or sets the enabled state of the index
        /// </summary>
        public bool IndexEnabled 
        { 
            get 
            {
                return _boolValue;
            } 
            set 
            {
                onIndexEnabled();
                // Only toggle the value if the incoming value is true
                if (value == true)
                {
                    _boolValue = !_boolValue;
                    FeedbackBoolean.FireUpdate();
                    if (_boolValue)
                    {
                        if (OnIndexEnabledTrueChanged != null)
                            OnIndexEnabledTrueChanged(this.Index, this.IndexValue);
                        return;
                    }
                    else if (!_boolValue)
                    {
                        if (OnIndexEnabledFalseChanged != null)
                            OnIndexEnabledFalseChanged(this.Index);
                    }
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the index value for routing
        /// </summary>
        public ushort IndexValue
        {
            get
            {
                return _intValue;
            }
            set
            {
                _intValue = value;
                FeedbackInteger.SetValueFunc(() => delegateIntValueFunction());
                //FeedbackInteger.FireUpdate();
                if (OnIndexValueChanged != null)
                    OnIndexValueChanged(this.Index, value); 
            }
        }

        /// <summary>
        /// Default consructor
        /// </summary>
        public CustomDeviceCollectionWithFeedback()
        {
            FeedbackBoolean = new BoolFeedback(() => _boolValue);
            FeedbackInteger = new IntFeedback(() => _intValue);
        }
        
        /// <summary>
        /// Method to raise the IndexEnabledChange event
        /// </summary>
        protected virtual void onIndexEnabled()
        {
            if (IndexEnabledChange != null)
            {
                IndexEnabledChange(); // Raise the event
            }
        }

        private int delegateIntValueFunction()
        {
            return _intValue;
        }

        #region IDisposable Members
        /// <summary>
        /// Dispose members
        /// </summary>
        public void Dispose()
        {
            if (OnIndexEnabledTrueChanged != null)
                { OnIndexEnabledTrueChanged = null; }
            if (OnIndexValueChanged != null)
                { OnIndexValueChanged = null; }
            if (OnIndexEnabledFalseChanged != null)
                { OnIndexEnabledFalseChanged = null; }
            GC.SuppressFinalize(this); // To prevent finalizer from running
        }

        /// <summary>
        /// Destructor for CustomDeviceCollectionWithFeedback
        /// </summary>
        ~CustomDeviceCollectionWithFeedback()
        {
            Dispose();
        }
        #endregion
    }

    /// <summary>
    /// Custom device collection containing two ushort values (Index and Route) per collection
    /// </summary>
    public class CustomDeviceCollection
    {
        private ushort _indexValue;
        private ushort _routeValue;
        
        /// <summary>
        /// Gets or sets the index value
        /// </summary>
        public ushort Index
        {
            get { return _indexValue; }
            set { _indexValue = value; }
        }
        
        /// <summary>
        /// Gets or sets the route value
        /// </summary>
        public ushort Route
        {
            get { return _routeValue; }
            set { _routeValue = value; }
        }
    }

    /// <summary>
    /// Class triggers a bool with built-in timer to auto-reset
    /// </summary>
    public class ROSBool : IDisposable
    {
        private bool _value;
        private CTimer _timer;
        private object _lock = new object();  // Lock for thread synchronization
        private int _timerDelay;  // Instance field to store the timer delay
        
        /// <summary>
        /// Event raised when the value changes
        /// </summary>
        public event EventHandler<BoolEventArgs> ValueChanged;
        
        /// <summary>
        /// Gets or sets the boolean value with auto-reset timer functionality
        /// </summary>
        public bool Value
        {
            get
            {
                lock (_lock)
                {
                    return _value;
                }
            }
            set
            {
                lock (_lock)
                {
                    if (value != _value) // Check if the value is actually changing
                    {
                        _value = value;
                        OnValueChanged(value); // Raise the event with the new value

                        if (value) // If being set to true
                        {
                            // If the timer is already running, stop it first
                            if (_timer != null)
                            {
                                _timer.Stop();
                                _timer.Dispose();
                            }
                            // Start or restart the timer for the delay set at instantiation
                            _timer = new CTimer(TimerCallback, null, _timerDelay);
                        }
                        else // If being set to false
                        {
                            // Stop and dispose of the timer if it's not needed
                            if (_timer != null)
                            {
                                _timer.Stop();
                                _timer.Dispose();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Constructor that accepts a delay parameter to set the timer delay
        /// </summary>
        /// <param name="delayMilliseconds"></param>
        public ROSBool(int delayMilliseconds)
        {
            _timerDelay = delayMilliseconds;  // Set the timer delay to the provided value
        }

        /// <summary>
        /// Safely update the Value property from the timer thread
        /// </summary>
        /// <param name="o"></param>
        private void TimerCallback(object o) { Value = false; }

        /// <summary>
        /// Method to raise the ValueChanged event
        /// </summary>
        /// <param name="newValue"></param>
        protected virtual void OnValueChanged(bool newValue)
        {
            // Create a new BoolEventArgs with the new value
            BoolEventArgs args = new BoolEventArgs(newValue);

            // Make a temporary copy of the event to avoid possibility of 
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<BoolEventArgs> handler = ValueChanged;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        #region IDisposable Members
        /// <summary>
        /// Make sure to dispose the timer when the object is being disposed or finalized
        /// </summary>
        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }
            GC.SuppressFinalize(this); // To prevent finalizer from running
        }

        /// <summary>
        /// Destructor for ROSBool
        /// </summary>
        ~ROSBool()
        {
            Dispose();
        }
        #endregion
    }

    /// <summary>
    /// Custom subclass of EventArgs to hold boolean value to pass with event 
    /// </summary>
    public class BoolEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the boolean value associated with this event
        /// </summary>
        public bool Value { get; private set; }

        /// <summary>
        /// Subclass method of EventArgs for the boolean value
        /// </summary>
        /// <param name="value"></param>
        public BoolEventArgs(bool value)
        {
            Value = value;
        }
    }
}

