#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParseFlags
{
	public enum NameMatchingMode
	{
		Disabled,
		Exact,
		CaseInsensitive
	}

	public class ParseOptions
	{
		internal readonly List<(Type targetType, IArgConverter converter)> _customConverters = new List<(Type, IArgConverter)>();

		/// <summary>
		/// Determines how the name of a property should be used when trying to match an argument to a property.
		/// <para>Default: CaseInsensitive</para>
		/// </summary>
		public NameMatchingMode MatchPropertyByName { get; set; } = NameMatchingMode.CaseInsensitive;

		/// <summary>
		/// How to use the <see cref="OptionAttribute"/> (if the property even has one) when trying to match an argument to a property.
		/// <para>Default: Exact</para>
		/// </summary>
		public NameMatchingMode MatchPropertyByAttribute { get; set; } = NameMatchingMode.Exact;

		/// <summary>
		/// Set this to make the parser ignore any properties that don't have <see cref="OptionAttribute"/>
		/// <para>Default: false</para>
		/// </summary>
		public bool RequireOptionAttribute { get; set; } = false;

		/// <summary>
		/// How the names of enum values are used when trying to parse them from a string.
		/// <para>Default: CaseInsensitive</para>
		/// </summary>
		public NameMatchingMode ParseEnumsByName { get; set; } = NameMatchingMode.CaseInsensitive;

		/// <summary>
		/// How <see cref="EnumOptionAttribute"/> is handled when matching trying to convert an argument to an enum value.
		/// <para>When set to 'Disabled', the <see cref="EnumOptionAttribute"/> will not be used to convert from strings to enum values </para>
		/// <para>Default: CaseInsensitive</para>
		/// </summary>
		public NameMatchingMode ParseEnumsByAttribute { get; set; } = NameMatchingMode.Exact;

		/// <summary>
		/// Called when the parser can't find any matching property for an argument.
		/// <para>Useful when you want to notify the user that an unexpected (or mistyped) argument given.</para>
		/// <para>Default: null</para>
		/// </summary>
		public Action<Arg>? OnUnmatchedArgument { get; set; } = null;

		/// <summary>
		/// Called when at least one malformed argument was found. An argument is malformed when it doesn't contain an '=' for example.
		/// <para>The action has two parameters: an array of successfully parsed arguments, and an array of reasons/descriptions for the malformed arguments.</para>
		/// <para>Default: throws an exception!</para>
		/// </summary>
		public Action<Arg[], string[]>? OnMalformedArgument { get; set; } =
			(args, errors) => throw new ArgumentException("At least one argument was malformed.\n" + string.Join("\n", errors));

		/// <summary>
		/// Called when the parser needs to create an object, for example when an argument specifies a value in a nested object that doesn't yet exist.
		/// <para>Default: Activator.CreateInstance(propType)</para>
		/// </summary>
		public Func<Type, object> OnCreateObject { get; set; } =
			propType => Activator.CreateInstance(propType);

		/// <summary>
		/// Add an instance of a custom converter.
		/// <para>You can customize how arguments are parsed/converted by adding an IArgConverter.</para>
		/// <para>Converters are used in the order they were added (only relevant, of course, if multiple converters can all handle the same type)</para>
		/// <para>Custom converters are go first (before any built-in converters) of course! That way they can override how basic types are handled.</para>
		/// </summary>
		public void AddCustomConverter<TTargetType>(IArgConverter<TTargetType> converter)
		{
			var type = typeof(TTargetType);
			var (_, existing) = _customConverters.FirstOrDefault(t => t.targetType == type);
			if (existing != null)
				throw new InvalidOperationException($"There is already a custom converter registered for this type!\n" +
					$"Type: {type.FullName}\n" +
					$"ExistingConverter: {existing}\n" +
					$"NewConverter: {converter}");

			_customConverters.Add((type, converter));
		}
	}
}
