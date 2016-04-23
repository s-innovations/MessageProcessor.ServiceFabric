# MessageProcessor.ServiceFabric

This repository contains the S-Innovations MessageProcessor Service Fabric Application, which allows orcastration of Azure ServiceBus queues and dynamically scalable and reliable processor nodes using Azure VM ScaleSets. 

## Concept
The concept is simple. Its a developer service made to build message based cloud architecture applications ontop. It allows creation of complex message queue structures with simple JSON configuration similar to how the Azure Resource Manager works. Lets give an example.

### MessageCluster
A message cluster is the root resource which can have sub resources defined later in this document. The Cluster resource have the concept of variables (this is due to the service do not actually run ontop of the Azure Resource Manager, so there is not a deployment concept as one know it.
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
All the required information about which service to use for polling off messages is defined in the listenerDescription element. 

### Processor Nodes
The reader may also notice the processorNode element on the queue element, which represent the physical hardware to where the service should run. If the `usePrimaryNode` on the queue listener description is set to true, the service is also allowed to run on the primary nodes.

Each processor nodes represent a VM Scale Set and when creating the message cluster each processor node will be created as a nodetype on the Service Fabric Cluster, which allow placement of the listener services.

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
},
```

The message cluster service fabric application also have all the logic to do automatically scale/provisioning of the underlaying VM Scale Sets. Infact when a new message cluster is added, it will create all the resources on azure with capacity set to 0.

### Dispatcher
The dispatcher resource can be used to setup one or multiply topic endpoints that delegate messages by forwarding them to a target queue. This allows more putthough and unified endpoint to push messages to and based on the messages properties end up in the correct queue.
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

