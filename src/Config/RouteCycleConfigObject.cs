using System.Collections.Generic;
using Newtonsoft.Json;
using PepperDash.Essentials.Core;

namespace RouteCycle.Config
{
	/// <summary>
	/// Plugin device configuration object
	/// </summary>
	/// <remarks>
	/// Rename the class to match the device plugin being created
	/// </remarks>
	/// <example>
	/// "EssentialsPluginConfigObjectTemplate" renamed to "SamsungMdcConfig"
	/// </example>
	[ConfigSnippet("\"properties\":{\"control\":{}")]
	public class EssentialsPluginTemplateConfigObject
	{
		/// <summary>
		/// Example dictionary of objects
		/// </summary>
		/// <remarks>
		/// This is an example collection configuration object.  This should be modified or deleted as needed for the plugin being built.
		/// </remarks>
		/// <example>
		/// <code>
		/// "properties": {
		///		"presets": {
		///			"preset1": {
		///				"enabled": true,
		///				"name": "Preset 1"
		///			}
		///		}
		/// }
		/// </code>
		/// </example>
		/// <example>
		/// <code>
		/// "properties": {
		///		"inputNames": {
		///			"input1": "Input 1",
		///			"input2": "Input 2"		
		///		}
		/// }
		/// </code>
		/// </example>
		[JsonProperty("DeviceDictionary")]
		public Dictionary<string, EssentialsPluginTemplateConfigObjectDictionary> DeviceDictionary { get; set; }

		/// <summary>
		/// Constuctor
		/// </summary>
		/// <remarks>
		/// If using a collection you must instantiate the collection in the constructor
		/// to avoid exceptions when reading the configuration file 
		/// </remarks>
        public EssentialsPluginTemplateConfigObject()
		{
			DeviceDictionary = new Dictionary<string, EssentialsPluginTemplateConfigObjectDictionary>();
		}
	}

	/// <summary>
	/// Example plugin configuration dictionary object
	/// </summary>
	/// <remarks>
	/// This is an example collection of configuration objects.  This can be modified or deleted as needed for the plugin being built.
	/// </remarks>
	/// <example>
	/// <code>
	/// "properties": {
	///		"dictionary": {
	///			"item1": {
	///				"name": "Item 1 Name",
	///				"value": "Item 1 Value"
	///			}
	///		}
	/// }
	/// </code>
	/// </example>
	public class EssentialsPluginTemplateConfigObjectDictionary
	{
		/// <summary>
		/// Serializes collection name property
		/// </summary>
		/// <remarks>
		/// This is an example collection of configuration objects.  This can be modified or deleted as needed for the plugin being built.
		/// </remarks>
		[JsonProperty("name")]
		public string Name { get; set; }

		/// <summary>
		/// Serializes collection value property
		/// </summary>
		/// <remarks>
		/// This is an example collection of configuration objects.  This can be modified or deleted as needed for the plugin being built.
		/// </remarks>
		[JsonProperty("value")]
		public uint Value { get; set; }
	}
}