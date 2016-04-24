using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommandLine;
using SInnovations.VSTeamServices.TasksBuilder.Attributes;
using SInnovations.VSTeamServices.TasksBuilder.ConsoleUtils;
using SInnovations.VSTeamServices.TasksBuilder.ResourceTypes;
using SInnovations.VSTeamServices.TasksBuilder.Tasks;

namespace ExtractServiceFabricApplicationVersionTask
{
    [EntryPoint("Extracting ServiceFabric Version")]
   
    public class ProgramOptions
    {
        [Required]
        [Display(Description = "The Application Manifest", Name = "ApplicationManifest", ShortName = "ApplicationManifest", ResourceType = typeof(GlobPath))]
        public string Manifest { get; set; }
        
        
        [Option("VariableName", HelpText = "The Variable Name")]
        public string VariableName { get; set; }

        [Option("UpdateBuild", HelpText = "Update Build Version")]
        public bool UpdateBuild { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            args = new[] { "--build" };
#endif

            var options = ConsoleHelper.ParseAndHandleArguments<ProgramOptions>("Extracting ServiceFabric Version", args);

            var XDoc = XDocument.Load(options.Manifest);
            var version = XDoc.Root.Attribute("ApplicationTypeVersion")?.Value;
            Console.WriteLine("Extracted Version: " + version);

            if (!string.IsNullOrEmpty(options.VariableName))
                TaskHelper.SetVariable(options.VariableName, version);

            if (options.UpdateBuild)
            {
                Console.WriteLine($"##vso[build.updatebuildnumber]{version}");
            }
        }
    }
}
