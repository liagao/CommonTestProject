namespace CommonTestProjectNetCore
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml;

    /// <summary>
    /// Final Data of Latency Gatherer Job
    /// Will include latency of all nodes in all workflows
    /// alongwith Machine Outliers and QPS
    /// </summary>
    [DataContract]
    public class ExperimentLatencyData
    {
        [DataMember(IsRequired = true, Order = 1, Name = "ExperimentName")]
        public string Name { get; internal set; }

        [DataMember(IsRequired = true, Order = 2, Name = "StartTime")]
        public DateTime StartTime { get; internal set; }

        [DataMember(IsRequired = true, Order = 3, Name = "EndTime")]
        public DateTime EndTime { get; internal set; }

        public IList<WorkflowLatencyData> WorkflowNodeLatencies
        {
            get
            {
                return this.workflowNodeLatencies ?? (this.workflowNodeLatencies = new List<WorkflowLatencyData>());
            }
        }
        [DataMember(IsRequired = true, Order = 4, Name = "WorkflowNodeLatencies")]
        private List<WorkflowLatencyData> workflowNodeLatencies;
    }


    [DataContract]
    public class WorkflowLatencyData
    {
        [DataMember(IsRequired = true, Order = 1, Name = "WorkflowName")]
        public string WorkflowName { get; internal set; }

        public IList<NodeLatencyData> NodeLatencies
        {
            get { return this.nodeLatencies ?? (this.nodeLatencies = new List<NodeLatencyData>()); }
        }
        [DataMember(IsRequired = true, Order = 2, Name = "NodeLatencies")]
        private List<NodeLatencyData> nodeLatencies;

        [DataMember(IsRequired = true, Order = 3, Name = "MachinesQueried")]
        public uint MachinesQueried { get; set; }

        [DataMember(IsRequired = true, Order = 4, Name = "MachinesResponded")]
        public uint MachinesResponded { get; set; }

        [DataMember(IsRequired = true, Order = 5, Name = "QueryLatencySeconds")]
        public double QueryLatencySeconds { get; set; }
    }

    [DataContract]
    public class NodeLatencyData
    {
        /// <summary>
        /// Node name
        /// </summary>
        [DataMember(IsRequired = true, Order = 1, Name = "NodeName")]
        public string Name { get; internal set; }
        /// <summary>
        /// Node P999 latency
        /// </summary>
        [DataMember(IsRequired = true, Order = 2, Name = "LatMs999")]
        public UInt16 LatMs999 { get; internal set; }
        /// <summary>
        /// Node P99 latency
        /// </summary>
        [DataMember(IsRequired = true, Order = 3, Name = "LatMs99")]
        public UInt16 LatMs99 { get; internal set; }
        /// <summary>
        /// Node P95 latency
        /// </summary>
        [DataMember(IsRequired = true, Order = 4, Name = "LatMs95")]
        public UInt16 LatMs95 { get; internal set; }
        /// <summary>
        /// Number of node counts in an interval
        /// </summary>
        [DataMember(IsRequired = true, Order = 5, Name = "SampleCount")]
        public ulong SampleCount { get; internal set; }
        /// <summary>
        /// Boolean to indicate AH read P99 latency instead of the default P999
        /// </summary>
        [DataMember(IsRequired = true, Order = 6, Name = "ReadP99")]
        public bool ReadP99 { get; internal set; }
    }


    /// <summary>
    /// Aggregated Node timeout info for Experiment
    /// Final output of Timeout Calculator
    /// </summary>
    [DataContract]
    public class ExperimentNodeTimeoutInfo
    {
        [DataMember(IsRequired = true, Order = 1, Name = "ExperimentName")]
        public string ExperimentName { get; set; }

        public DateTime CreateTime { get; set; }

        [DataMember(IsRequired = true, Order = 2, Name = "StartTimeUtc")]
        public DateTime StartTimeUtc { get; set; }
        [DataMember(IsRequired = true, Order = 3, Name = "EndTimeUtc")]
        public DateTime EndTimeUtc { get; set; }

        public IList<WorkflowNodeTimeoutInfo> WorkflowNodeTimeouts
        {
            get
            {
                return this.workflowNodeTimeouts ?? (this.workflowNodeTimeouts = new List<WorkflowNodeTimeoutInfo>());
            }
        }
        [DataMember(IsRequired = true, Order = 3, Name = "WorkflowNodeTimeouts")]
        private List<WorkflowNodeTimeoutInfo> workflowNodeTimeouts;
    }

    [DataContract]
    public class WorkflowNodeTimeoutInfo
    {
        [DataMember(IsRequired = true, Order = 1, Name = "Name")]
        public string Name { get; internal set; }

        public IList<NodeTimeoutInfo> NodeTimeouts
        {
            get
            {
                return this.nodeTimeouts ?? (this.nodeTimeouts = new List<NodeTimeoutInfo>());
            }
        }
        [DataMember(IsRequired = true, Order = 2, Name = "NodeTimeouts")]
        private List<NodeTimeoutInfo> nodeTimeouts;
    }

    [DataContract]
    public class NodeTimeoutInfo
    {
        /// <summary>
        /// Node name
        /// </summary>
        [DataMember(IsRequired = true, Order = 1, Name = "Name")]
        public string Name { get; set; }
        /// <summary>
        /// Real node timeout. Should be around P999 for the node in most cases.
        /// Could be smaller than P99 during outage.
        /// </summary>
        [DataMember(IsRequired = true, Order = 2, Name = "TimeoutMs")]
        public UInt16 TimeoutMs { get; set; }
        /// <summary>
        /// Node P99 latency
        /// </summary>
        [DataMember(IsRequired = true, Order = 3, Name = "AggTimeoutMsNonCp")]
        public UInt16 AggTimeoutMsNonCp { get; set; }
        /// <summary>
        /// Node P95 latency
        /// </summary>
        [DataMember(IsRequired = true, Order = 4, Name = "AggTimeoutMsCp")]
        public UInt16 AggTimeoutMsCp { get; set; }
        /// <summary>
        /// Boolean to indicate AH read P99 latency instead of the default P999
        /// </summary>
        [DataMember(IsRequired = true, Order = 5, Name = "ReadP99")]
        public bool ReadP99 { get; set; }
    }

    public class CachedNodeTimeoutInfo
    {
        public string Name { get; set; }
        public ushort TimeoutMs { get; set; }
        public DateTime UpdatedOn { get; set; }
    }

    public class DataSerializer<T> where T : class
    {
        public void SerializeToXml(T objectT, Stream outputXmlStream)
        {
            if (objectT == default(T))
            {
                throw new ArgumentNullException(nameof(objectT));
            }
            if (outputXmlStream == null)
            {
                throw new ArgumentNullException(nameof(outputXmlStream));
            }

            using (var xmlTxtWriter = XmlTextWriter.Create(outputXmlStream))
            {
                new DataContractSerializer(typeof(T)).WriteObject(xmlTxtWriter, objectT);
            }
        }

        public T DeserializeFromXml(Stream xmlStream)
        {
            var settings = new XmlReaderSettings();
            settings.XmlResolver = null;
            settings.DtdProcessing = DtdProcessing.Ignore;
            settings.CloseInput = false;

            using (var xmlReader = XmlTextReader.Create(xmlStream, settings))
            {
                return (T)new DataContractSerializer(typeof(T)).ReadObject(xmlReader);
            }
        }

    }
}
