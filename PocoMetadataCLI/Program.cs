using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using System.Linq;

namespace Breeze.PocoMetadata
{
    class Program
    {
        static readonly CommandLineOptions Options = new CommandLineOptions();
        //class person { public string name { get; set; } }
        static void Main(string[] args)
        {

            //var persons = new[] {
            //    new person { name = "personA" },
            //    new person { name = "personAA"},
            //    new person { name = "personB" }
            //};

            //var filteredPersons = 
            //    persons
            //        .Where(p => { var x = p.name.ToLower(); return x.EndsWith("a"); })
            //        .Where(p => p.name.ToLower().Count(c => c == 'a') > 0);

            var procname = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            Console.WriteLine(procname + ' ' + string.Join(" ", args));
            
            var parser = new CommandLine.Parser(ps => { ps.MutuallyExclusive = true; ps.HelpWriter = Console.Out; });

            if (!parser.ParseArguments(args, Options))
            {
                return;
            }
            var assemblyName = GetAssemblyName(Options.InputFile);

            if (!File.Exists(assemblyName))
            {
                Console.WriteLine("The specified file {0} cannot be found", assemblyName);
                return;
            }
            string outfile = GetFilePath();

            // TODO: how to get this from the command line?
            EntityDescriptor descriptor;
            if (assemblyName.Contains("Northwind"))
                descriptor = new NorthwindEntityDescriptor();
            else
                descriptor = new EntityDescriptor();

            var metadata = Generator.Generate(assemblyName, descriptor);
            var json = ToJson(metadata);

            if (outfile != null)
            {
                Console.WriteLine("Writing to " + outfile);
                File.WriteAllText(outfile, json);
            }
            else
            {
                Console.WriteLine(json);
            }
            Console.WriteLine("Done");

            Console.ReadKey();
        }

        private static string GetFilePath()
        {
            var fileName = Options.OutputFile;
            if (string.IsNullOrEmpty(fileName)) return null;

            return string.IsNullOrEmpty(Options.OutputFolder) ?
                Path.GetFullPath(fileName) : Path.Combine(Options.OutputFolder, fileName);
        }

        private static string ToJson(Metadata metadata)
        {
            var serializerSettings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            var json = JsonConvert.SerializeObject(metadata, Formatting.Indented);
            return json;

        }

        private static string GetAssemblyName(string fileName)
        {
            var path = Path.GetFullPath(fileName);
            var extension = Path.GetExtension(path);
            if (extension != ".dll")
            {
                return Path.ChangeExtension(path, extension + ".dll");
            }
            return path;
        }

    }
}
