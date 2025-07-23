using PepperDash.Essentials.Core;

namespace RouteCycle.JoinMaps
{
	/// <summary>
	/// Plugin device Bridge Join Map
	/// </summary>
	/// <remarks>
	/// Rename the class to match the device plugin being developed.  Reference Essentials JoinMaps, if one exists for the device plugin being developed
	/// </remarks>
	/// <see cref="PepperDash.Essentials.Core.Bridges"/>
	/// <example>
	/// "EssentialsPluginBridgeJoinMapTemplate" renamed to "SamsungMdcBridgeJoinMap"
	/// </example>
	public class RouteCycleBridgeJoinMap : JoinMapBaseAdvanced
	{
		#region Digital

		// TODO [x] Add digital joins below plugin being developed

		/// <summary>
		/// Allow routes to be cycled
		/// </summary>
		[JoinName("inUse")]
		public JoinDataComplete InUse = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 1,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Allow routes to be cycled",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

        /// <summary>
        /// Manually cycle routes
        /// </summary>
        [JoinName("cycleRoute")]
        public JoinDataComplete CycleRoute = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 2,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Manually cycle routes",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Clear all source objects from routeCycle
        /// </summary>
        [JoinName("sourcesClear")]
        public JoinDataComplete SourcesClear = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Clear all source objects from routeCycle",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Clear all destination objects from routeCycle
        /// </summary>
        [JoinName("destinationsClear")]
        public JoinDataComplete DestinationsClear = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 4,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Clear all destination objects from routeCycle",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Toggle to include source item in routeCycle
        /// </summary>
        [JoinName("sourceSelect")]
        public JoinDataComplete SourceSelect = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 11,
                JoinSpan = 32
            },
            new JoinMetadata
            {
                Description = "Toggle to include item in routeCycle",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Toggle to include destination item in routeCycle
        /// </summary>
        [JoinName("destinationSelect")]
        public JoinDataComplete DestinationSelect = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 51,
                JoinSpan = 32
            },
            new JoinMetadata
            {
                Description = "Toggle to include item in routeCycle",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });	

        // Feedback side of wrapper below

        /// <summary>
        /// Item pulses when new message is sent
        /// </summary>
        [JoinName("reportNotifyPulse")]
        public JoinDataComplete ReportNotifyPulse = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Item pulses when new message is sent",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Feedback state of Source, when active will be included in routeCycle
        /// </summary>
        [JoinName("sourceFeedback")]
        public JoinDataComplete SourceFeedback = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 11,
                JoinSpan = 32
            },
            new JoinMetadata
            {
                Description = "Feedback state of Source, when active will be included in routeCycle",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Feedback state of Destination, when active will be included in routeCycle
        /// </summary>
        [JoinName("destinationFeedback")]
        public JoinDataComplete DestinationFeedback = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 51,
                JoinSpan = 32
            },
            new JoinMetadata
            {
                Description = "Feedback state of Destination, when active will be included in routeCycle",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });	

		#endregion


		#region Analog

		// TODO [x] Add analog joins below plugin being developed

        /// <summary>
        /// Source Input Value
        /// </summary>
        [JoinName("sourceInputValue")]
        public JoinDataComplete SourceInputValue = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 32
            },
            new JoinMetadata
            {
                Description = "Source Input Value",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Analog
            });

        /// <summary>
        /// Destination Source Request Value
        /// </summary>
        [JoinName("destinationRouteOut")]
        public JoinDataComplete DestinationRouteOut = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 32
            },
            new JoinMetadata
            {
                Description = "Destination Source Request Value",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });

		#endregion


		#region Serial

		// TODO [x] Add serial joins below plugin being developed

        /// <summary>
        /// Notification Message
        /// </summary>
        [JoinName("reportNotifyMessage")]
		public JoinDataComplete ReportNotifyMessage = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 1,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Notification Message",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Serial
			});

		#endregion

		/// <summary>
		/// Plugin device BridgeJoinMap constructor
		/// </summary>
		/// <param name="joinStart">This will be the join it starts on the EISC bridge</param>
        public RouteCycleBridgeJoinMap(uint joinStart)
            : base(joinStart, typeof(RouteCycleBridgeJoinMap))
		{
		}
	}
}