# thingworx-opcua-dotnetsdk-edge
# Content:
ThingWorx .NET SDK implementation for OPC UA solicited reads and writes for 2D Arrays
 # Description:
This ThingWorx Edge .NET SDK implementation provides an alternative way of reading and writing 2D arrays of the same basetypes (eg: [[1,2,3,4],[3,2,4,5]]) defined in an OPC UA server to and from ThingWorx.
The reads and writes are explicit (blocking) and they use the synchronous API (readValue and write methods) of OPC UA .NET library. The functionalities have been tested with the Kepware's OPC UA server.

# How to use:
 1.  **Install** Microsoft Visual Studio Community 2022 (64-bit) (the example was built with Version 17.13.5) and the ThingWorx Edge .NET SDK 5.9.0
 2.  **Open** the OPCUASolution.sln file in Visual Studio Community.
 3.  **Remove** the existing thingworx-dotnet-common.dll (since that points to a relative path) and re-add it by right-clicking on Dependencies, Add COM Reference, Go to the Browse Tab and select the thingworx-dotnet-common.dll from its installation path, typically "Program Files (x86)\tw-dotnet-sdk\thingworx-dotnet-common.dll"
 4. **Click on Debug / OPCUA Debug Properties and modify the command line parameter values to match your ThingWorx server**:
   The parameters below are: h = server hostname, p = port, k = application key
   example: -h xxyyzz.portal.ptc.io -p 443 -k 1111111-1111-1111-1111-123891209831
 5. **Create in ThingWorx Composer a Remote Thing with name "OPCUAClient1**"
 6. **Start** the Debug configuration SDK implementation
 7.  Go to ThingWorx Composer and **map the remote services below**:
    
![image](https://github.com/user-attachments/assets/89f72c57-3129-4066-864a-2aa97e48faa4)

6. **Use the services in this order** :
   
    6.1. **InitializeEndpointAndSession**
    - **Description** : this service opens an OPCUA session to the specified endpoint. For as long as this session is opened, you will be able to read/write data using the services below. Please close the session as soon as you finished working with those services. It uses the default session timeout of 60 seconds, meaning that if you don't use any of the read/write services in 60 seconds, the session will be disconnected automatically.
    - **Input parameters** :
        -  **endpointURL** : This is the OPC UA server endpoint: Default value: opc.tcp://localhost:49320
    - **Output**: none
  
    6.2. **GetOPCUA2DArrayNodeValue**
    - **Description** : this retrieves the value of a tag in a JSON format.
    - **Input parameters** :
        -  **nodeId** : This is the OPC UA node ID to read from: Default value: ns=2;s=Simulation Examples.Functions.Double2DArray
    - **Output**: JSON with the following structure

       > {"value":[[0,0,0,0,0,0],[0,0,0,0,0,0],[0,0,0,0,0,0],[0,0,0,0,0,0]]}

    6.3. **SetOPCUA2DArrayNodeValue** 
    - **Description** : this sets the value of a tag in a JSON format. Note that it will write all array values (but if the server supports writing at a specific index, this functionality could be added)
    - **Input parameters** :
    
        - **nodeId** : This is the OPC UA node ID to write to: Default value: ns=2;s=Simulation Examples.Functions.Double2DArray
        - **dataType**: the basetype for the array elements. This should be the same basetype for all array elements. Acceptable values: any of Boolean|SByte|Byte|Int16|UInt16|Int32|UInt32|Int64|UInt64|Float|Double|String (as defined in https://reference.opcfoundation.org/Core/Part6/v104/docs/5.1.2 )
        - **nodeValue**: a JSON with the same structure as the result from service GetOPCUA2DArrayNodeValue
          > {"value":[[1,0,0,0,0,0],[0,0,0,0,0,0],[0,0,0,0,0,0],[0,0,0,0,0,0]]}
    - **Output**: String with success or error messages

    6.4. **CloseSession**
    - **Description** : closes the session created by InitializeEndpointAndSession
   
# Required libraries/OS versions:
 - PTC ThingWorx Edge .NET SDK 5.9.0
 - log4net 3.0.4
 - [OPCFoundation .NET SDK 1.5.375.457](https://www.nuget.org/packages/OPCFoundation.NetStandard.Opc.Ua.Core/1.5.375.457)
 - Microsoft .NET Core 8.0 
 - Windows 64 bit

# Good to know:
 - The implementation was tested with the OPC UA endpoint running with no Security Policies defined. If there's a requirement to connect to an OPC UA server with security policies enabled, than the Security Configuration section of the OPCUAThing class (line 77) will need to be modified.
 - This example is based on the Steam Sensor example from the ThingWorx Edge .NET SDK 5.9.0. The implementation was cleaned by removing almost all the example services and property definitions, but some may still be present there.
 - Additional OPC UA basetypes can be implemented as needed
 - The implementation contains two additional services, SetOPCUAStringNodeValue and GetOPCUAStringNodeValue. Those have been used during testing, but can be extended as needed to allow the read/write for other non-array basetypes.
 

