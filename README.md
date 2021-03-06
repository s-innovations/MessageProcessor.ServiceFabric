# MessageProcessor.ServiceFabric

This repository contains the S-Innovations MessageProcessor Service Fabric Application, which allows orchestration of Azure ServiceBus queues and dynamically scalable and reliable processor nodes using Azure VM ScaleSets. 

* ![](https://sinnovations.visualstudio.com/_apis/public/build/definitions/809e415c-4783-4434-8d16-bd10e8ac55ed/12/badge) ![](https://sinnovations.visualstudio.com/_apis/public/build/definitions/809e415c-4783-4434-8d16-bd10e8ac55ed/11/badge) Git/VSTS Sync
* ![](https://sinnovations.visualstudio.com/_apis/public/build/definitions/809e415c-4783-4434-8d16-bd10e8ac55ed/10/badge) Tasks and Apps


## Concept
The concept is simple. It�s a developer service made to build message based cloud architecture applications on top. It allows creation of complex message queue structures with simple JSON configuration similar to how the Azure Resource Manager works. Let�s give an example.

TODO: Introduce small examples here to get a better understanding of what this can be used for.

### MessageCluster
A message cluster is the root resource which can have sub resources defined later in this document. The Cluster resource have the concept of variables (this is due to the service do not actually run on top of the Azure Resource Manager, so there is not a deployment concept as one know it.
The variables can then be used in sub resources, allowing to save some typing.
```
{
    "apiVersion": "2016-01-01",
    "type": "S-Innovations.MessageProcessor/MessageCluster",
    "name": "mycoolcluster",
    "location": "West Europe",
    "properties": {
    },
    "variables": {
        "servicebusNamespaceId": "/subscriptions/8393a037-5d39-462d-a583-09915b4493df/resourceGroups/TestServiceFabric12/providers/Microsoft.ServiceBus/namespaces/sb-3wodhzoece",
        "authRuleResourceId": "/subscriptions/8393a037-5d39-462d-a583-09915b4493df/resourceGroups/TestServiceFabric12/providers/Microsoft.ServiceBus/namespaces/sb-3wodhzoece/authorizationRules/RootManageSharedAccessKey",
        "queueDescription": {
            "enableBatchedOperations": true,
            "enableDeadLetteringOnMessageExpiration": true,
            "enableExpress": true,
            "enablePartitioning": true
        }
    },
    "resources": []
}
```
### Queues

The template looks like this
```
{
    "type": "S-Innovations.MessageProcessor/queue",
    "name": "queue-a4",
    "properties": {
        "servicebus": {
            "servicebusNamespaceId": "[variables('servicebusNamespaceId')]",
            "authRuleResourceId": "[variables('authRuleResourceId')]"
        },
        "queueDescription": "[variables('queueDescription')]",
        "listenerDescription": {
            "partitionCount": 1,
            "idleTimeout": "PT5M45S",
            "applicationTypeName": "MyDemoAppType1",
            "applicationTypeVersion": "1.0.0",
            "serviceTypeName": "TestProcessorType",
            "processorNode": "processor-a4",
            "usePrimaryNode": true
        }
    }
}
```
and this will ensure that a servicebus queue is created when setting up the message cluster. Since the message cluster is all about pushing and polling messages of a queue, this resource also contains the information about the listener..
#### Listener
A listener should be represented by a different ServiceFabric application that is provisioned to the same Fabric Instance, from where the stateless services is defined that should be deployed. The stateless service is responsible of polling off messages and when there is no more messages on the queue, the services will be deprovisioned.
All the required information about which service to use for polling off messages is defined in the listener description element. 

### Processor Nodes
The reader may also notice the processor node element on the queue element, which represent the physical hardware to where the service should run. If the `usePrimaryNode` on the queue listener description is set to true, the service is also allowed to run on the primary nodes.

Each processor nodes represent a VM Scale Set and when creating the message cluster each processor node will be created as a node type on the Service Fabric Cluster, which allow placement of the listener services.

```
{
    "type": "S-Innovations.MessageProcessor/processorNode",
    "name": "processor-a4",
    "properties": {
        "name": "Standard_A4",
        "tier": "Standard",
        "vmImagePublisher": "MicrosoftWindowsServer",
        "vmImageOffer": "WindowsServer",
        "vmImageSku": "2012-R2-Datacenter",
        "vmImageVersion": "latest",
        "location": "West Europe",
        "minCapacity": 0,
        "maxCapacity": 10,
        "messagesPerInstance": 10,
        "scaleDownCooldown": "PT10M",
        "scaleUpCooldown": "PT1M"
    }
}
```

The message cluster service fabric application also has all the logic to do automatically scale/provisioning of the under laying VM Scale Sets. In fact, when a new message cluster is added, it will create all the resources on azure with capacity set to 0.

### Dispatcher
The dispatcher resource can be used to setup one or multiply topic endpoints that delegate messages by forwarding them to a target queue. This allows more put though and unified endpoint to push messages to and based on the messages properties end up in the correct queue.
```
{
    "type": "S-Innovations.MessageProcessor/dispatcher",
    "name": "algs",
    "properties": {
        "servicebus": {
            "servicebusNamespaceId": "[variables('servicebusNamespaceId')]",
            "authRuleResourceId": "[variables('authRuleResourceId')]"
        },
        "topicScaleCount": 2,
        "correlationFilters": {
            "messagetype1": "queue-a0",
            "messagetype2": "queue-a1",
            "messagetype3": "queue-a2",
            "messagetype4": "queue-a3",
            "messagetype5": "queue-a4",
            "messagetype6": "queue-a5",
            "messagetype7": "queue-a6"
        }
    }
}
```

Currently only forwarding based on correlation filters, which ensures high performance filtering directly within the servicebus layer on azure. Using the S-Innovations.Azure.MessageProcessor project the correlation filter on servicebus messages are automatically set based on configuration.




## Getting Started

Personally I have all my stuff in VSTS and will also provide a few guides on this later, and for now we will stick with the PowerShell way here. It is possible to deploy the application to a local dev cluster, but keep in mind that to actually add a message cluster to the application it also requires a cluster running on azure as it needs to add custom node types using the azure resource manager and also create additional VM Scale Sets. So first you must setup a service fabric cluster on azure. I suggest to use the azure portal market place, searching for service fabric. 

### Parameters
First you need to create a application parameters xml file (Can be omitted if using poweshell). Here are two examples for [local](https://github.com/s-innovations/MessageProcessor.ServiceFabric/blob/master/src/MessageProcessor.ServiceFabricHost/ApplicationParameters/Local.xml) and [cloud](https://github.com/s-innovations/MessageProcessor.ServiceFabric/blob/master/src/MessageProcessor.ServiceFabricHost/ApplicationParameters/Cloud.xml).

#### PlacementConstraints
The placement constraints parameter can be used to constraints the application to only run on a subset of nodes. In my case i use `NodeTypeName==nt1vm` to make it only use the primary nodes.

#### SubscriptionId, ResourceGroupName, ClusterName and TenantId
The Azure subscription Id, resoucegroup name and clustername to where the cluster is running. This is used to resolve the resource id of the cluster for updating its nodetypes.
#### StorageName
The storageName parameter is used to persist cluster models to. I use the same storage accountn for which the vm scale sets stores the disks.
#### BasicAuth
The management API is protected by simple basic auth right now, and the username and password can be provided here as `username:password`. The future plan is to update the application to federate with the Azure AD.
#### AzureADServicePrincipal
The Azure AD service principal that the application will act on behalf off when deploying and talking with Azure Resource Manager. Therefore the Azure AD Application also needs contributer rights to the resourcegroup provided in these parameters.


### Powershell
Download the latest release package from  [](https://github.com/s-innovations/MessageProcessor.ServiceFabric/releases) and unpack it to `C:\sfapps\MessageClusterApp-0.9.1`.

First connect to the service fabric cluster, here used a local development cluster
```
Connect-ServiceFabricCluster localhost:19000
```
and live cluster
```
Connect-ServiceFabricCluster -ConnectionEndpoint pksservicefabric12.westeurope.cloudapp.azure.com:19000 -X509Credential -ServerCertThumbprint 584C645A30253DDA98EF8B7ED09B87F61468F3EE -FindType FindByThumbprint -FindValue 584C645A30253DDA98EF8B7ED09B87F61468F3EE -StoreLocation LocalMachine -StoreName My
```


Using the following commands will register and create an application instance.
```
$RegKey = "HKLM:\SOFTWARE\Microsoft\Service Fabric SDK"
$ModuleFolderPath = (Get-ItemProperty -Path $RegKey -Name FabricSDKPSModulePath).FabricSDKPSModulePath
Import-Module "$ModuleFolderPath\ServiceFabricSDK.psm1"

$ApplicationName = "fabric:/MessageClusterApp"
$ApplicationParameter =  @{
    "SubscriptionId" = "8393a037-5d39-462d-a583-09915b4493df"; 
    "ResourceGroupName" = "TestServiceFabric12"; 
    "ClusterName" = "pksservicefabric12";
    "TenantId" = "802626c6-0f5c-4293-a8f5-198ecd481fe3";
    "StorageName"="3wodhzoece5io1";
    "BasicAuth" ="pks:123456";
    "AzureADServicePrincipal"="MIICNQYJKoZIhvcNAQcDoIICJjCCAiICAQAxggFMMIIBSAIBADAwMBwxGjAYBgNVBAMTEVNlcnZpY2VGYWJyaWNDZXJ0AhAbUlzRHgKrqEZPDwbKUzt4MA0GCSqGSIb3DQEBAQUABIIBACYG53iZrYKmBShWhh2+QrGbIzMuxAwVp9pqJMsRNZnxD3Ds9Hq/Xp350jsBz0KNM+8KwI2wcUtDcRUqs8ZVA3j8oFnQkhEJpnv5Vc9ueuJ+/zPrbdI+Cyrwp4iWpGiOlFU222f4qYRQppy83YeLBejG+ftwUNXPXst3Q7ubcYCwPHsusOpKfhwqF5jle+jdxZ1o0RFNaXut4SZnjP24xbaGc1763ScrbHPO8zVe3pReA358+fLgf+ej+dOeeJAomYiEzwMOhZH5NMB8geZTOr3tKybzKl0lHO0vXKQmXCkeO5hWKPGPPBh8TWWiPcRK4vG80GICRhoMkeR4wrahwhMwgcwGCSqGSIb3DQEHATAUBggqhkiG9w0DBwQITFHhHgT/B3qAgah0xIsWULhVnO0JGwzu8/Oe2VYMwUtKnHQbeJTJyK8xQNVkKaO7lvskVz37OLINSUTPl0wbrGY2Bx4g+/UuI1agxgFCU3ydeLQPUGZpsAS4HFyS3bNpyfS6vEGPdWbjrVOcXMpbRxgb+d+1/GTK/gse1+cC4wzN1fZDUqdhDKg6DiTHqU5QdupgXp1h37qmFj5VIO0v+qc+I9QkQ1yyT0ZIx9DsmuqN4Z0="
}
#for production cluster use also "PlacementConstraints" = "NodeTypeName==nt1vm"

Publish-NewServiceFabricApplication -ApplicationPackagePath C:\sfapps\MessageClusterApp-0.9.1\ -ApplicationName $ApplicationName -ApplicationParameter $ApplicationParameter -Action RegisterAndCreate  -OverwriteBehavior Always -ErrorAction Stop
```
Using the service fabric explorer, for my case [](https://pksservicefabric12.westeurope.cloudapp.azure.com:19080/Explorer/Index.html#/), it can be verified that the applcation was deployed successfull. 

When the application is deployed one can then add the message cluster configuration. Here is a small sample based on the resource types described above.
```
{
  "apiVersion": "2016-01-01",
  "type": "S-Innovations.MessageProcessor/MessageCluster",
  "name": "mycoolcluster",
  "location": "West Europe",
  "properties": {},
  "variables": {
    "servicebusNamespaceId": "/subscriptions/8393a037-5d39-462d-a583-09915b4493df/resourceGroups/TestServiceFabric12/providers/Microsoft.ServiceBus/namespaces/sb-3wodhzoece",
    "authRuleResourceId": "/subscriptions/8393a037-5d39-462d-a583-09915b4493df/resourceGroups/TestServiceFabric12/providers/Microsoft.ServiceBus/namespaces/sb-3wodhzoece/authorizationRules/RootManageSharedAccessKey",
    "queueDescription": {
      "enableBatchedOperations": true,
      "enableDeadLetteringOnMessageExpiration": true,
      "enableExpress": true,
      "enablePartitioning": true
    }
  },
  "resources": [
    {
      "type": "S-Innovations.MessageProcessor/queue",
      "name": "queue-a4",
      "properties": {
        "servicebus": {
          "servicebusNamespaceId": "[variables('servicebusNamespaceId')]",
          "authRuleResourceId": "[variables('authRuleResourceId')]"
        },
        "queueDescription": "[variables('queueDescription')]",
        "listenerDescription": {
          "partitionCount": 1,
          "idleTimeout": "PT5M45S",
          "applicationTypeName": "MyDemoAppType1",
          "applicationTypeVersion": "1.0.0",
          "serviceTypeName": "TestProcessorType",
          "processorNode": "processor-a4"
        }
      }
    },
    {
      "type": "S-Innovations.MessageProcessor/queue",
      "name": "queue-a0",
      "properties": {
        "servicebus": {
          "servicebusNamespaceId": "[variables('servicebusNamespaceId')]",
          "authRuleResourceId": "[variables('authRuleResourceId')]"
        },
        "queueDescription": "[variables('queueDescription')]",
        "listenerDescription": {
          "partitionCount": 1,
          "idleTimeout": "PT5M45S",
          "applicationTypeName": "MyDemoAppType1",
          "applicationTypeVersion": "1.0.0",
          "serviceTypeName": "TestProcessorType",
          "processorNode": "processor-a0",
          "usePrimaryNode": true
        }
      }
    },
    {
      "type": "S-Innovations.MessageProcessor/processorNode",
      "name": "processor-a0",
      "properties": {
        "name": "Standard_A0",
        "tier": "Standard",
        "vmImagePublisher": "MicrosoftWindowsServer",
        "vmImageOffer": "WindowsServer",
        "vmImageSku": "2012-R2-Datacenter",
        "vmImageVersion": "latest",
        "location": "West Europe",
        "minCapacity": 0,
        "maxCapacity": 10,
        "messagesPerInstance": 10,
        "scaleDownCooldown": "PT10M",
        "scaleUpCooldown": "PT1M"
      }
    },
    {
      "type": "S-Innovations.MessageProcessor/processorNode",
      "name": "processor-a4",
      "properties": {
        "name": "Standard_A4",
        "tier": "Standard",
        "vmImagePublisher": "MicrosoftWindowsServer",
        "vmImageOffer": "WindowsServer",
        "vmImageSku": "2012-R2-Datacenter",
        "vmImageVersion": "latest",
        "location": "West Europe",
        "minCapacity": 0,
        "maxCapacity": 10,
        "messagesPerInstance": 10,
        "scaleDownCooldown": "PT10M",
        "scaleUpCooldown": "PT1M"
      }
    },
    {
      "type": "S-Innovations.MessageProcessor/dispatcher",
      "name": "algs",
      "properties": {
        "servicebus": {
          "servicebusNamespaceId": "[variables('servicebusNamespaceId')]",
          "authRuleResourceId": "[variables('authRuleResourceId')]"
        },
        "topicScaleCount": 2,
        "correlationFilters": {
          "messagetype1": "queue-a0",
          "messagetype5": "queue-a4"
        }
      }
    }
  ]
}
```
which can be added to the application using the following powershell script
```
$queueClusters = "http://pksservicefabric12.westeurope.cloudapp.azure.com/webapp/a25b86e3-d6d2-43fc-b6ac-20cedd94a999/test/providers/SInnovations.MessageProcessor/MessageCluster/mycoolcluster"
$body = [IO.File]::ReadAllText("C:\sfapps\msgcluster.json")
$pair = "pks:kodetilsf"
$encodedCreds = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes($pair))
$basicAuthValue = "Basic $encodedCreds"
Invoke-WebRequest -Debug -Verbose -Uri $queueClusters -Method PUT -Header @{ "Authorization" = $basicAuthValue; "Content-Type" = "application/json" } -Body $body
```

The guid and part of the url `a25b86e3-d6d2-43fc-b6ac-20cedd94a999/test` is just random picked right now and is used together with the msg cluster name `mycoolcluster` as the key. This was done such later seperation can be done where more tenants could use the same service fabric cluster. For now you may pick anything for these two params.

After putting the resource into the system, it will reconfigure the service fabic cluster to have node types matching the processor nodes, followed by provisioning capacity=0 VM scalesets that can be automatically scaled up when messages are added to the queues. It takes a bit of time for azure resource manager to reconfigure the service fabric cluster. Best way to see this is going to the azure portal and looking at the status of the essential dropdown of the service fabric cluster.

![Essential Tab](imgs/cluster_essentials.png "Essentiel Tab")

and when its in status = ready the nodetypes will be updated, which can be seen under nodetypes.
![NodeTypes](imgs/nodetypes.png "NodeTypes")

As the last step it will create all the vm scale sets.

![NodeTypes](imgs/resources.png "NodeTypes")

## How to help
First, deploying and trying it out and providing some feedback - positive or negative will help me decide if I should put more time into the project. 

The key focus behind the project is to simplify and make it easy to use, so any feedback on this topic is valuable at this point. What is blocking you from trying it out or using it?
1. Help with deployment of Service Fabric Cluster?
2. Better Getting Started Documentation?

## Roadmap
1. Documentation of creating a listerner app 2016Q2
2. Clean up application code + add event/logging for essential parts.2016Q3
3. Experiment with creating a listener app on linux machines running tasks in containers. 2016Q4

## Disclaimer
I have a full time job also, so issues and questiosn will mainly be answered out of normal office hours. But I hope you will get in touch.



## TODO
[ ] https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-cluster-capacity writes aboute how to scale up and down, in and out. Can these be handled in a nice way