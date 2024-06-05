using pm4h.data;
using pm4h.tpa;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mineguide.perspectives.interactiveannotation.modeltransformations
{
    public static class MetadataExtensions
    {
        #region General metadata
        /// <summary>
        /// Prefix for all the metadata properties used by Mineguide
        /// </summary>
        public static readonly string METADATA_PREFIX = ExtensibleObject.CreateHiddenPropertyId("#MINEGUIDE-DATA#");
        /// <summary>
        /// Separator used to separate the prefix from the data specific key
        /// </summary>
        public static readonly string METADATA_SEPARATOR = "|";

        /// <summary>
        /// Create a Mineguide metadata property id from the provided data specifiv key
        /// </summary>
        public static string CreateMinguidePropertyId(string key) => METADATA_PREFIX + METADATA_SEPARATOR + key;

        public static string GetKeyFromMineguidePropertyId(string key) => key.Split(METADATA_SEPARATOR).Last();

        public static bool IsMineguideKeyProperty(string key) => key.StartsWith(METADATA_PREFIX);

        public static void SaveToMetadata(this PMEvent _event, string key, object value)
        {
            key = CreateMinguidePropertyId(key);
            _event[key] = value;
        }

        public static object? ReadFromMetadata(this PMEvent _event, string key)
        {
            key = CreateMinguidePropertyId(key);
            if (_event.Properties.ContainsKey(key))
            {
                return _event.Properties[key];
            }
            return null;
        }

        public static T? ReadFromMetadata<T>(this PMEvent _event, string key)
        {
            key = CreateMinguidePropertyId(key);
            if (_event.Properties.ContainsKey(key) && _event.Properties[key] is T typedValue)
            {
                return typedValue;
            }
            return default;
        }

        public static void SaveToMetadata(this IEnumerable<IPMLog> _log, string key, object value)
        {
            foreach (var l in _log)
            {
                l.SaveToMetadata(key, value);
            }
        }

        public static void SaveToMetadata(this IPMLog _log, string key, object value)
        {
            key = CreateMinguidePropertyId(key);
            var md = _log.getMetaData();
            md[key] = value;
            _log.setMetaData(md);
        }

        public static IEnumerable<T>? ReadFromMetadata<T>(this IEnumerable<IPMLog> _log, string key) where T : class
        {
            key = CreateMinguidePropertyId(key);
            foreach (var l in _log)
            {
                var md = l.getMetaData();
                if (md != null && md.ContainsKey(key) && md[key] is T typedValue)
                {
                    yield return typedValue;
                }

            }
        }

        public static T? ReadFromMetadata<T>(this IPMLog _log, string key) where T : class
        {
            key = CreateMinguidePropertyId(key);
            var md = _log.getMetaData();
            if (md != null && md.ContainsKey(key) && md[key] is T typedValue)
            {
                return typedValue;
            }
            return default;
        }

        public static void SaveToMetadata(this TPATemplate.Node _node, string key, object value)
        {
            key = CreateMinguidePropertyId(key);
            _node.Metadata.Set(key, value);
        }

        public static T? ReadFromMetadata<T>(this TPATemplate.Node _node, string key)
        {
            key = CreateMinguidePropertyId(key);
            if (_node.Metadata.ContainsKey(key) && _node.Metadata[key] is T typedValue)
            {
                return typedValue;
            }
            return default;
        }

        #endregion

        #region Traceability metadata
        /// <summary>
        /// Key for the Traceability ID property
        /// </summary>
        public static readonly string Traceability_ID = "Traceability";

        /// <summary>
        /// Add Traceability information to the provided event
        /// </summary>
        public static void AddMetadataTraceability(this PMEvent _event, Guid TraceabilityId) => _event.SaveToMetadata(Traceability_ID, TraceabilityId.ToString());


        /// <summary>
        /// Add Traceability information to the provided event
        /// </summary>
        public static void AddMetadataTraceability(this PMEvent _event, Guid[] TraceabilityIds) => _event.SaveToMetadata(Traceability_ID, string.Join(";", TraceabilityIds.Select(t => t.ToString()).ToArray()));

        public static Guid[] GetMetadataTraceability(this PMEvent _event)
        {
            if (_event.ReadFromMetadata<string>(Traceability_ID) is string TraceabilityIds)
            {
                return TraceabilityIds.Split(';').Select(t => new Guid(t)).ToArray();
            }
            return new Guid[0];
        }

        public static bool TryGetMetadataTraceability(this PMEvent _event, out Guid[] TraceabilityIds)
        {
            TraceabilityIds = _event.GetMetadataTraceability();
            return TraceabilityIds.Length > 0;
        }

        public static Guid[] GetMetadataTraceability(this TPATemplate.Node node)
        {
            if (node.ReadFromMetadata<string>(Traceability_ID) is string TraceabilityIds)
            {
                return TraceabilityIds.Split(';').Select(t => new Guid(t)).ToArray();
            }
            return new Guid[0];
        }

        public static bool TryGetMetadataTraceability(this TPATemplate.Node node, out Guid[] TraceabilityIds)
        {
            TraceabilityIds = node.GetMetadataTraceability();
            return TraceabilityIds.Length > 0;
        }


        #endregion

        #region Conversion metadata

        /// <summary>
        /// Key for the conversion ID property
        /// </summary>
        public static readonly string CONVERSION_ID = "Conversion";

        public enum NodeConversion { None, Subprocess, Composition, DecisionNew, DecisionSame, CycleIntensional, CycleExtensional, Parallel, ForzeFusion, Rename };

        /// <summary>
        /// Add conversion information to the provided event
        /// </summary>
        public static void AddMetadataConversion(this PMEvent _event, NodeConversion conversionType) => _event.SaveToMetadata(CONVERSION_ID, conversionType);

        public static NodeConversion? GetMetadataConversion(this PMEvent _event) => _event.ReadFromMetadata<NodeConversion?>(CONVERSION_ID);

        public static bool TryGetMetadataConversion(this PMEvent _event, out NodeConversion conversionType)
        {
            if(_event.GetMetadataConversion() is NodeConversion conversion)
            {
                conversionType = conversion;
                return true;
            }          
            conversionType = NodeConversion.None;
            return false;
        }
        

        public static NodeConversion? GetMetadataConversion(this TPATemplate.Node node) => node.ReadFromMetadata<NodeConversion?>(CONVERSION_ID);

        public static bool TryGetMetadataConversion(this TPATemplate.Node node, out NodeConversion? conversionType)
        {
            if (node.GetMetadataConversion() is NodeConversion conversion)
            {
                conversionType = conversion;
                return true;
            }
            conversionType = null;// NodeConversion.None;
            return false;
        }

        #endregion
    }

}
