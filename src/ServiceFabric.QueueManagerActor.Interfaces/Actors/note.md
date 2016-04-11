 //          //try
      //          //{
      //          //    var fabricClient = new FabricClient();
      //          //    //      var services = await fabricClient.QueryManager.GetServiceListAsync(new Uri(this.ApplicationName), new Uri("fabric:/MessageProcessor.ServiceFabricHost/QueueListenerActorService"));
      //          //    await fabricClient.ServiceManager.CreateServiceAsync(new StatelessServiceDescription
      //          //    {
      //          //        ServiceTypeName = ServiceFabricConstants.ActorServiceTypes.QueueListenerActorService,
      //          //        ServiceName = serviceUri,
      //          //        PartitionSchemeDescription = new UniformInt64RangePartitionSchemeDescription { PartitionCount = 2, LowKey = Int64.MinValue, HighKey = Int64.MaxValue },
      //          //        InstanceCount = 1,
      //          //        //  PlacementConstraints = "NodeType == something",
      //          //        ApplicationName = new Uri(this.ApplicationName),
      //          //    });

      //          //    //   var xml = await fabricClient.ClusterManager.GetClusterManifestAsync();
      //          //    //  var xmlDoc = XDocument.Parse(xml);
      //          //    //  var Infrastructure = xmlDoc.Root.Element("Infrastructure");
      //          //    //  File.WriteAllText("testmani.xml", xml);

      //          //    // await fabricClient.ClusterManager.ProvisionFabricAsync(null, "testMani.xml");

      //          //}
      //          //catch (Exception ex)
      //          //{
      //          //    return this.ServiceUri + "\n" + ex.ToString();
      //          //}

      //          State.RunningActors.Add(key, "Started");
      //      }