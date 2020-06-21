using System;
using System.Collections.Generic;
using System.Text;

namespace ParseFlags
{
	/// <summary>
	/// Add this to the properties of your config class to customize things like name or description.
	/// <para>You can also make this required (so properties without an <see cref="OptionAttribute"/> will be ignored by the parser)</para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class OptionAttribute : Attribute
	{
		public string Name { get; }
		/// <summary>
		/// 
		/// </summary>
		public string Description { get; }

		public OptionAttribute(string name = null)
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
