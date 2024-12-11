using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
namespace multi_launcher;


public static class ProcessExtensions
{
    public static IList<Process> GetChildProcesses(this Process process)
    {
        var childProcesses = new List<Process>();

        // Use ManagementObjectSearcher to find immediate child processes
        var result = new ManagementObjectSearcher(
            $"Select * From Win32_Process Where ParentProcessID={process.Id}")
            .Get()
            .Cast<ManagementObject>();

        // For each immediate child process, retrieve the process and add to the list
        foreach (var mo in result)
        {
            var childProcess = Process.GetProcessById(Convert.ToInt32(mo["ProcessID"]));
            childProcesses.Add(childProcess);

            // Recursively get children of this child process
            childProcesses.AddRange(childProcess.GetChildProcesses());
        }

        return childProcesses;
    }
}
 
 