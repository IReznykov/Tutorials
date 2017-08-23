using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace ResizeImageJobEx
{
	class Program
	{
		static void Main(string[] args)
		{
			var config = new JobHostConfiguration();
			config.Tracing.ConsoleLevel = TraceLevel.Verbose;

			config.UseFiles();
			config.UseTimers();

			var host = new JobHost(config);
			// The following code ensures that the WebJob will be running continuously
			host.RunAndBlock();
		}
	}
}
