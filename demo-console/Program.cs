using System;
using ParseFlags;

namespace demo_console
{
	/*
	 * Goals:
	 * - create object, or overwrite existing
	 * - case-insensitive
	 * - enums (name, value, or attribute)
	 * - arrays (only: primitives, strings, and enums)
	 * - support nested classes
	 */
	/*
	 * Later:
	 * - attributes to:
	 *		- include/exclude props
	 *		- required/optional
	 *		- rename props
	 *		- description/help text
	 *		- custom enum names
	 * - auto generate help text: show default values, maybe limits, ...
	 */

	class Options
	{
		public string ConfigPath { get; set; }
		public double[] HistogramBuckets { get; set; } = new double[] { 1, 5, 10, 50, 100, 500, 1000 };
		public LogOptions Logger { get; set; }
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

					"--logger.level=err",

					"--login.enabled=true",
					"--login.port=6666",
					"--login.servers=localhost,google.com,github.com",

					"--server.port=5555",
				};

			var options = Parser.Parse<Options>(args);
		}
	}
}
