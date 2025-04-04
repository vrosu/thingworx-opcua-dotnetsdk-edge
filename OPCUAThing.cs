using log4net;
using com.thingworx.communications.client;
using com.thingworx.communications.client.things;
using com.thingworx.metadata;
using com.thingworx.metadata.annotations;
using com.thingworx.metadata.collections;
using com.thingworx.types;
using com.thingworx.types.collections;
using com.thingworx.types.constants;
using com.thingworx.types.primitives;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using com.thingworx.communications.client.things.properties;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using static System.Collections.Specialized.BitVector32;
using Newtonsoft.Json.Linq;
using System.Text.Json.Nodes;
using System.Collections;
using static com.thingworx.common.RESTAPIConstants;


namespace OPCUA
{
    // Steam Thing virtual thing class that simulates a Steam Sensor
    public class OPCUAThing : VirtualThing
    {
        [System.Runtime.InteropServices.DllImport("twApi.dll", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl, CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
        public static extern void twApi_DisableWebSocketCompression();

        private static readonly ILog  LOG = LogManager.GetLogger(typeof(OPCUAThing));
        

        // Lock and bool field used to keep the shutdown logic from occuring multiple times before the actual shutdown
        private object _shutdownLock = new object();
        private bool _shuttingDown = false;

        //OPC UA
        ApplicationConfiguration config;
        ApplicationInstance application;
        EndpointDescription endpoint;
        Opc.Ua.Client.Session opcSession;
        List<Session> lst_OPCUASessions;


        public OPCUAThing(string name, string description, string identifier, ConnectedThingClient client)
            : base(name, description, identifier, client)
        {
            // Data Shape definition that is used by the steam sensor fault event
            // The event only has one field, the message
            FieldDefinitionCollection faultFields = new FieldDefinitionCollection();
            faultFields.addFieldDefinition(new FieldDefinition(CommonPropertyNames.PROP_MESSAGE, BaseTypes.STRING));
            base.defineDataShapeDefinition("SteamSensor.Fault", faultFields);

            //Data shape definition that is used by the GetSteamSensorReadings service
            //FieldDefinitionCollection readingfields = new FieldDefinitionCollection();
            //readingfields.addFieldDefinition(new FieldDefinition(SENSOR_NAME_FIELD, BaseTypes.STRING));
            //readingfields.addFieldDefinition(new FieldDefinition(ACTIV_TIME_FIELD, BaseTypes.DATETIME));
            //readingfields.addFieldDefinition(new FieldDefinition(TEMPERATURE_FIELD, BaseTypes.NUMBER));
            //readingfields.addFieldDefinition(new FieldDefinition(PRESSURE_FIELD, BaseTypes.NUMBER));
            //readingfields.addFieldDefinition(new FieldDefinition(FAULT_STATUS_FIELD, BaseTypes.BOOLEAN));
            //readingfields.addFieldDefinition(new FieldDefinition(INLET_VALVE_FIELD, BaseTypes.BOOLEAN));
            //readingfields.addFieldDefinition(new FieldDefinition(TEMPERATURE_LIMIT_FIELD, BaseTypes.NUMBER));
            //readingfields.addFieldDefinition(new FieldDefinition(TOTAL_FLOW_FIELD, BaseTypes.INTEGER));
            //defineDataShapeDefinition("SteamSensorReadings", readingfields);

            // Populate the thing shape with the properties, services, and events that are annotated in this code
            base.initializeFromAnnotations();

            config = new ApplicationConfiguration()
            {
                ApplicationName = "My OPC UA Client",
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier(),
                    AutoAcceptUntrustedCertificates = true,
                    AddAppCertToTrustedStore = true
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 }
            };

            config.Validate(ApplicationType.Client);

            // Create an application configuration
            application = new ApplicationInstance
            {
                ApplicationName = "ThingWorxOpcUaClient",
                ApplicationType = ApplicationType.Client,
                ConfigSectionName = "Opc.Ua.Client"
            };

            
            lst_OPCUASessions = new List<Session>();


        }

        // From the VirtualThing class
        // This method will get called when a connect or reconnect happens
        // Need to send the values when this happens
        // This is more important for a solution that does not send its properties on a regular basis
        public override void synchronizeState()
        {
            // Be sure to call the base class
            base.synchronizeState();
            // Send the property values to Thingworx when a synchronization is required
            base.syncProperties();
            
            // Dump out the current list of subscribed properties. This will be updated as soon as a change is made in 
            // the composer
            LOG.Debug("Current Bound Properties of "+getName());
            if (this._subscribedProperties.Count == 0)
            {
	            LOG.Debug("No Properties are bound.");
            }
            else
            {
	            foreach (KeyValuePair<string, PropertySubscription> entry in this._subscribedProperties)
	            {
		            LOG.Debug(entry.Key);
	            };
            }
        }

        // The processScanRequest is called by the SteamSensorClient every scan cycle
        public override void processScanRequest()
        {
            // Be sure to call the base classes scan request
            base.processScanRequest();
            // Execute the code for this simulation every scan
            this.scanDevice();
        }

        // Performs the logic for the steam sensor, occurs every scan cycle
        public void scanDevice()
        {
            Random random = new Random();
            // Set the property values
           // base.setProperty("Temperature", temperature);
            //base.setProperty("Pressure", pressure);
            //base.setProperty("TotalFlow", this._totalFlow);
            //base.setProperty("InletValve", inletValveStatus);

            
                     
                    // Set the event information of the defined data shape for the event
                   // ValueCollection eventInfo = new ValueCollection();
                    //eventInfo.Add(CommonPropertyNames.PROP_MESSAGE, new StringPrimitive("Temperature at " + temperature + " was above limit of " + temperatureLimit));
                    // Queue the event
                    //base.queueEvent("SteamSensorFault", DateTime.UtcNow, eventInfo);
               
           


            try {
                // Update the subscribed properties and events to send any updates to Thingworx
                // Without calling these methods, the property and event updates will not be sent
                // The numbers are timeouts in milliseconds.
                base.updateSubscribedProperties(15000);

               // LOG.DebugFormat( "Current Temperature limit: {0}", temperatureLimit);
               

                base.updateSubscribedEvents(60000);
            } catch (Exception ex) {    
                LOG.Error(ex);
            }
        }


        [method: ThingworxServiceDefinition(name = "InitializeEndpointAndSession", description = "Initializes the specific OPC UA endpoint", category = "OPC UA")]
        [return: ThingworxServiceResult(name = CommonPropertyNames.PROP_RESULT, description = "Result", baseType = "NOTHING")]
        public void InitializeEndpointAndSession(

              [ThingworxServiceParameter(name = "endpointURL", description = "OPC UA Endpoint URL", baseType = "STRING", aspects = new string[] { "defaultValue:opc.tcp://localhost:49320" })] string endpointURL
           )
        {
            endpoint = CoreClientUtils.SelectEndpoint(endpointURL, useSecurity: false);
            opcSession = Session.Create(config, new ConfiguredEndpoint(null, endpoint, EndpointConfiguration.Create(config)), false, "", 60000, null, null).Result;
        }

        [method: ThingworxServiceDefinition(name = "CloseSession", description = "Closes the specific OPC UA session", category = "OPC UA")]
        [return: ThingworxServiceResult(name = CommonPropertyNames.PROP_RESULT, description = "Result", baseType = "NOTHING")]
        public void CloseSession(

             [ThingworxServiceParameter(name = "endpointURL", description = "OPC UA Endpoint URL", baseType = "STRING", aspects = new string[] { "defaultValue:opc.tcp://localhost:49320" })] string endpointURL
          )
        {
           
            opcSession.Close();
        }

        [method: ThingworxServiceDefinition(name = "GetOPCUAStringNodeValue", description = "Get OPC UA string value", category = "OPC UA")]
        [return: ThingworxServiceResult(name = CommonPropertyNames.PROP_RESULT, description = "Result", baseType = "STRING")]
        public String GetOPCUAStringNodeValue(
           
                [ThingworxServiceParameter(name = "nodeId", description = "OPC UA nodeId", baseType = "STRING", aspects = new string[] {"defaultValue:ns=2;s=Data Type Examples.16 Bit Device.R Registers.DWord1" })] string nodeId
            )

        {
           
            NodeId node_nodeId = new NodeId(nodeId);

            // Read the value of the node
            DataValue value = opcSession.ReadValue(node_nodeId);

            // Print the value
            return value.Value.ToString();

      
          
        }

        [method: ThingworxServiceDefinition(name = "SetOPCUAStringNodeValue", description = "Set OPC UA string value", category = "OPC UA")]
        [return: ThingworxServiceResult(name = CommonPropertyNames.PROP_RESULT, description = "Result", baseType = "STRING")]
        public String SetOPCUAStringNodeValue(

               [ThingworxServiceParameter(name = "nodeId", description = "OPC UA nodeId", baseType = "STRING", aspects = new string[] { "defaultValue:ns=2;s=Simulation Examples.Functions.StringTag" })] string nodeId,
               [ThingworxServiceParameter(name = "nodeValue", description = "OPC UA node value", baseType = "STRING" )] string nodeValue
           )

        {

            NodeId node_nodeId = new NodeId(nodeId);

            // Read the value of the node
            WriteValue writeValue = new WriteValue();
            writeValue.NodeId = nodeId;
            writeValue.AttributeId = Attributes.Value;
            writeValue.Value = new DataValue(new Variant(nodeValue));
            
            ResponseHeader header_Result =  opcSession.Write(null, [writeValue],out StatusCodeCollection statusCodeResult,out DiagnosticInfoCollection diagnostifInfoResult);
            // Check the result
            if (header_Result.ServiceResult == StatusCodes.Good && statusCodeResult[0] == StatusCodes.Good)
            {
                return "Value written successfully.";
            }
            else
            {
                return $"Failed to write value. StatusCode: {statusCodeResult[0]}";
            }
        }

        [method: ThingworxServiceDefinition(name = "SetOPCUA2DArrayNodeValue", description = "Set OPC UA 2D Array value", category = "OPC UA")]
        [return: ThingworxServiceResult(name = CommonPropertyNames.PROP_RESULT, description = "Result", baseType = "STRING")]
        public String SetOPCUA2DArrayNodeValue(

             [ThingworxServiceParameter(name = "nodeId", description = "OPC UA nodeId", baseType = "STRING", aspects = new string[] { "defaultValue:ns=2;s=Simulation Examples.Functions.Double2DArray" })] string nodeId,
             [ThingworxServiceParameter(name = "dataType", description = "OPC UA dataType", baseType = "STRING", aspects = new string[] { "defaultValue:Boolean|SByte|Byte|Int16|UInt16|Int32|UInt32|Int64|UInt64|Float|Double|String" })] string dataType,
             [ThingworxServiceParameter(name = "nodeValue", description = "OPC UA node value; should be in the form of a {value:JSON array value} JSON", baseType = "JSON")] JObject nodeValue
         )
        {
            int arrayRows, arrayColumns;
            JArray values = (JArray)nodeValue["value"];
            arrayRows = values.Count;
            arrayColumns = ((JArray)values[0]).Count;
            Variant variant;
            switch (dataType)
            {
                case "Boolean":
                    Boolean[,] bool_arrayValue = new Boolean[arrayRows, arrayColumns];
                    bool_arrayValue = values.ToObject<Boolean[,]>();
                    variant = new Variant(bool_arrayValue);
                    break;
                case "SByte":
                    SByte[,] sbyte_arrayValue = new SByte[arrayRows, arrayColumns];
                    sbyte_arrayValue = values.ToObject<SByte[,]>();
                    variant = new Variant(sbyte_arrayValue);
                    break;
                case "Byte":
                    Byte[,] byte_arrayValue = new Byte[arrayRows, arrayColumns];
                    byte_arrayValue = values.ToObject<Byte[,]>();
                    variant = new Variant(byte_arrayValue);
                    break;
                case "Int16":
                    Int16[,] int16_arrayValue = new Int16[arrayRows, arrayColumns];
                    int16_arrayValue = values.ToObject<Int16[,]>();
                    variant = new Variant(int16_arrayValue);
                    break;
                case "UInt16":
                    UInt16[,] uint16_arrayValue = new UInt16[arrayRows, arrayColumns];
                    uint16_arrayValue = values.ToObject<UInt16[,]>();
                    variant = new Variant(uint16_arrayValue);
                    break;
                case "Int32":
                    Int32[,] int32_arrayValue = new Int32[arrayRows, arrayColumns];
                    int32_arrayValue = values.ToObject<Int32[,]>();
                    variant = new Variant(int32_arrayValue);
                    break;
                case "UInt32":
                    UInt32[,] uint32_arrayValue = new UInt32[arrayRows, arrayColumns];
                    uint32_arrayValue = values.ToObject<UInt32[,]>();
                    variant = new Variant(uint32_arrayValue);
                    break;
                case "Int64":
                    Int64[,] int64_arrayValue = new Int64[arrayRows, arrayColumns];
                    int64_arrayValue = values.ToObject<Int64[,]>();
                    variant = new Variant(int64_arrayValue);
                    break;
                case "UInt64":
                    UInt64[,] uint64_arrayValue = new UInt64[arrayRows, arrayColumns];
                    uint64_arrayValue = values.ToObject<UInt64[,]>();
                    variant = new Variant(uint64_arrayValue);
                    break;
                case "Float":
                    Single[,] float_arrayValue = new Single[arrayRows, arrayColumns];
                    float_arrayValue = values.ToObject<Single[,]>();
                    variant = new Variant(float_arrayValue);
                    break;
                case "Double":
                    Double[,] double_arrayValue = new Double[arrayRows, arrayColumns];
                    double_arrayValue = values.ToObject<Double[,]>();
                    variant = new Variant(double_arrayValue);
                    break;
                case "String":
                    String[,] string_arrayValue = new String[arrayRows, arrayColumns];
                    string_arrayValue = values.ToObject<String[,]>();
                    variant = new Variant(string_arrayValue);
                    break;
                //Other OPC UA Basetypes should be implemented here
                default:
                    break;
            }
            if (variant != Variant.Null)
            {
                WriteValue writeValue = new();
                writeValue.NodeId = nodeId;
                writeValue.AttributeId = Attributes.Value;
                writeValue.Value = new DataValue(variant);
                ResponseHeader header_Result = opcSession.Write(null, [writeValue], out StatusCodeCollection statusCodeResult, out DiagnosticInfoCollection diagnostifInfoResult);
                // Check the result
                if (header_Result.ServiceResult == StatusCodes.Good && statusCodeResult[0] == StatusCodes.Good)
                {
                    return "Value written successfully.";
                }
                else
                {
                    return $"Failed to write value. StatusCode: {statusCodeResult[0]}";
                }
            }
            else return "Failed to write value. No valid basetype provided.";
        }
        /*
        private bool findOrCreateSession(string str_Endpoint)
        {
            List<Session> lst_ExistingSessions = lst_OPCUASessions.FindAll(session => session.Endpoint.EndpointUrl == str_Endpoint);
            if (lst_ExistingSessions.Count >0) return true;
            else return false;  

        }
        */

        [method: ThingworxServiceDefinition(name = "GetOPCUA2DArrayNodeValue", description = "Get OPC UA 2D array value", category = "OPC UA")]
        [return: ThingworxServiceResult(name = CommonPropertyNames.PROP_RESULT, description = "Result", baseType = "JSON")]
        public JSONPrimitive GetOPCUA2DArrayNodeValue(

               [ThingworxServiceParameter(name = "nodeId", description = "OPC UA nodeId", baseType = "STRING", aspects = new string[] { "defaultValue:ns=2;s=Simulation Examples.Functions.Double2DArray" })] string nodeId
           )
        {
            //note that OPC UA session management is done in the InitializeEndpointAndSession. Sessions are being opened until they are closed via CloseSession
            // Define the node to read
            NodeId node_nodeId = new NodeId(nodeId);
            // Read the value of the node
            DataValue value = opcSession.ReadValue(node_nodeId);
            Matrix matr_KepwareArray = ((Opc.Ua.Matrix)value.Value);
            //get number of rows
            int int_KepwareArrayLength = matr_KepwareArray.Dimensions[0];
            int int_ColumnCount = matr_KepwareArray.Dimensions[1];
            JArray json_Array = new JArray();
            for (int i = 0; i < int_KepwareArrayLength; i++)
            {
                JArray json_RowArray = new JArray();
                for (int j = 0; j < int_ColumnCount; j++)
                {
                    json_RowArray.Add(matr_KepwareArray.Elements.GetValue((i*j+j)));
                }
                json_Array.Add(json_RowArray);
            }
            JObject json_OutputJSON = new JObject();
            json_OutputJSON["value"] = json_Array;
            return new JSONPrimitive(json_OutputJSON);
        }

        [method: ThingworxServiceDefinition(name = "Shutdown", description = "Shutdown the client")]
        [return: ThingworxServiceResult(name=CommonPropertyNames.PROP_RESULT, description="", baseType="NOTHING")]
        public void Shutdown()
        {
            // Highly unlikely that this service could be called more than once, but guard against it anyway
            lock (this._shutdownLock)
            {
                if (!this._shuttingDown)
                {
                    // Start a thread to begin the shutdown or the shutdown could happen before the service returns
                    ThreadPool.QueueUserWorkItem(new WaitCallback(this.shutdownThread));
                }
                this._shuttingDown = true;
            }
        }

        private void shutdownThread(object state)
        {
            try
            {
                // Delay for a period to verify that the Shutdown service will return
                Thread.Sleep(1000);
                // Shutdown the client
                this.getClient().shutdown();
            }
            catch
            {
                // Not much can be done if there is an exception here
                // In the case of production code should at least log the error
            }
        }
    }
}
