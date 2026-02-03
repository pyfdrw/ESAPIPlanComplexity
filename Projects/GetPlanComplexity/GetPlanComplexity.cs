using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

using Newtonsoft.Json;

// TODO: Replace the following version attributes by creating AssemblyInfo.cs. You can do this in the properties of the Visual Studio project.
[assembly: AssemblyVersion("1.0.0.1")]
[assembly: AssemblyFileVersion("1.0.0.1")]
[assembly: AssemblyInformationalVersion("1.0")]

// TODO: Uncomment the following line if the script requires write access.
// [assembly: ESAPIScript(IsWriteable = true)]

namespace GetPlanComplexity
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                using (Application app = Application.CreateApplication())
                {
                    Execute(app, args);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
        }
        static void Execute(Application app, string[] args)
        {
            if (2 == args.Length)
            {
                string patientId = args[0].Trim();
                string outputJsonPath = args[1].Trim();  // Store results in this file

                Patient patient = app.OpenPatientById(patientId);
                if (patient == null)
                {
                    Console.Error.WriteLine("Patient not found: " + patientId);
                    return;
                }
                else
                {
                    List<PlanComplexityValues> planComplexityValuesList = new List<PlanComplexityValues>();
                    // Skip validation plan
                    foreach (Course course in patient.Courses)
                    {
                        foreach (PlanSetup plan in course.PlanSetups)
                        {
                            if (plan.PlanIntent.ToUpper().Contains("VERIFICATION"))
                            {
                                // Skip
                                Console.WriteLine($"Skipped Verification Plan: {plan.Id} in Course: {course.Id}");
                                continue;
                            }
                            else
                            {
                                PlanComplexityValues pcv = new PlanComplexityValues(plan);
                                planComplexityValuesList.Add(pcv);
                                Console.WriteLine($"Processed Plan: {plan.Id} in Course: {course.Id}");
                            }
                        }
                    }
                    // planComplexityValuesList To Json
                    string jsonResults = JsonConvert.SerializeObject(planComplexityValuesList);

                    // Write the results to the output file
                    if (null != jsonResults)
                    {
                        System.IO.File.WriteAllText(outputJsonPath, jsonResults, Encoding.UTF8);
                        Console.WriteLine($"Results written to {outputJsonPath}");
                    }
                    else
                    {
                        Console.Error.WriteLine("No results to write.");
                    }
                }

            }
            else
            {
                Console.Error.WriteLine("Wrong args");
                return;
            }
        }
    }
}
