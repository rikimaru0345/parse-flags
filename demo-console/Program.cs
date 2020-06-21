using System;
using System.Linq;
using Microsoft.Extensions.Primitives;
using ParseFlags;

namespace demo_console
{
	/*
	 * Features:
	 * - create new object, or overwrite existing
	 * - support nested objects
	 * - case sensitive and insensitive matching
	 * - support for enums by name, value, or custom name (using attribute)
	 * - support for arrays (only primitives and strings)
	 * - attributes for custom field names
	 * - custom value converters
	 * 
	 * Todo:
	 * - attributes for:
	 *	  - descriptions
	 *	  - required/optional
	 * - auto generate help text: show default values, maybe limits, ...
	 * - maybe allow making converters for array types
	 */

	class Options
	{
		public string ConfigPath { get; set; }
		public double[] HistogramBuckets { get; set; } = new double[] { 1, 5, 10, 50, 100, 500, 1000 };
		public LogOptions Logger { get; set; }
		[Option("Server")]
		public ServerOptions Server { get; set; }
		public LoginOptions Login { get; set; }
	}

	class LogOptions
	{
		public LogLevel Level { get; set; } = LogLevel.Info;
	}

	class ServerOptions
	{
		public int Port { get; set; } = 80;
	}

	class LoginOptions
	{
		public bool Enabled { get; set; }
		public string[] Servers { get; set; }
		public int Port { get; set; } = 90;
	}

	enum LogLevel
	{
		Debug,

		[EnumOption("Information")]
		Info,

		[EnumOption("Warning")]
		Warn,

		[EnumOption("Err")]
		Error
	}


	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length == 0)
				args = new[] {
					"--configPath=conf.yaml",
					"--HistogramBuckets=1,2,3,4,5,6",

					"--logger.level=Err",

					"--login.enabled=true",
					"--login.port=6666",
					"--login.servers=localhost,google.com,github.com",

					"--server.port=5555",

					// Test: unmatched, wrong, ...
					"--unmatched",
					"asdasd",
					"login.port.abc=4",
					"-login.port.abc=4",
					"--login.port.abc=4",

				};

			var options = Parser.Parse<Options>(args, new ParseOptions{ OnMalformedArgument = null });
		}
	}
}
