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

		[JoinName("destinationRouteIn")]
		public JoinDataComplete DestinationRouteIn = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 1,
				JoinSpan = 32
			},
			new JoinMetadata
			{
				Description = "Routed Destination Source Value",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Analog
			});

        [JoinName("destinationRouteOut")]
        public JoinDataComplete DestinationRouteOut = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 32
            },
            new JoinMetadata
            {
                Description = "Routed Destination Source Value",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });

		#endregion


		#region Serial

		// TODO [x] Add serial joins below plugin being developed

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