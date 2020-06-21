using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ParseFlags
{
	public class ConverterContext
	{
		/// <summary>
		/// Options that were given to <see cref="Parser.Parse{T}"/> (or an instances of the default settings if the user didn't pass one)
		/// </summary>
		public ParseOptions ParseOptions { get; internal set; }

		/// <summary>
		/// The expected result type (doesn't have to be an exact match, so a more derived type will work as well)
		/// <para>If the actual target is an array, this is the "element type" (so for an 'int[]' the target type would be 'int')</para>
		/// </summary>
		public Type TargetType { get; internal set; }

		/// <summary>
		/// The property that the parser will assign the value to, should you return true in <see cref="IArgConverter{TTargetType}.TryConvert(ConverterContext, Arg, out TTargetType)"/>
		/// <para>You should use <see cref="TargetType"/> instead of <see cref="TargetProperty"/>.PropertyType, because for arrays 'TargetProperty.PropertyType' will be an array type, not the element type!</para>
		/// </summary>
		public PropertyInfo TargetProperty { get; internal set; }

		/// <summary>
		/// The argument that the parser wants you to convert
		/// </summary>
		public Arg Arg { get; internal set; }
	}

	/// <summary>
	/// This is just a marker interface, don't use it. To make a custom converter see <see cref="IArgConverter{TTargetType}"/>
	/// </summary>
	public interface IArgConverter { }

	/// <summary>
	/// This interface describes a "converter", which parses a given string (from arg.Value) to some type.
	/// Also take a look at the documentation for <seealso cref="IArgConverter{TTargetType}.TryConvert(ConverterContext, Arg, out TTargetType)"/>
	/// </summary>
	/// <typeparam name="TTargetType">the type that the converter will convert a given argument to</typeparam>
	public interface IArgConverter<TTargetType> : IArgConverter
	{
		/// <summary>
		/// <para>Return true if you have successfully converted the give argument and assigned a valid value to the 'value' parameter.</para>
		/// <para>Return false if you were unable to handle the argument, and have set value to null</para>
		/// Also take a look at the documentation for <seealso cref="IArgConverter{TTargetType}"/>
		/// </summary>
		bool TryConvert(ConverterContext ctx, Arg arg, out TTargetType value);
	}

	static class ConverterHelper
	{
		public static bool TryConvert(this IArgConverter converter, ConverterContext ctx, Arg arg, out object result)
		{
			var converterType = converter.GetType();
			var type = converterType.FindClosedArg(typeof(IArgConverter<>));

			if (!type.IsAssignableFrom(ctx.TargetType))
			{
				result = null; // type doesn't match
				return false;
			}

			var args = new object[] { ctx, arg, null };

			var m = converterType.GetMethod(nameof(IArgConverter<int>.TryConvert));
			var success = (bool)m.Invoke(converter, args);

			result = success ? args[2] : null;
			return success;
		}
	}
}
