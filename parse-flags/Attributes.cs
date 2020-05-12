using System;
using System.Collections.Generic;
using System.Text;

namespace ParseFlags
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class OptionAttribute : Attribute
	{
		public string Name { get; }

		public OptionAttribute(string name)
		{
			Name = name;
		}
	}

	
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class EnumOptionAttribute : Attribute
	{
		public string Name { get; }

		public EnumOptionAttribute(string name)
		{
			Name = name;
		}
	}
}
