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
		internal readonly List<(Type targetType, object converter)> _customConverters = new List<(Type, object)>();

		// -
		public NameMatchingMode MatchByPropertyName { get; set; } = NameMatchingMode.CaseInsensitive;
		
		// -
		public NameMatchingMode MatchByAttributeName { get; set; } = NameMatchingMode.Exact;

		// -
		public NameMatchingMode EnumMatchByName { get; set; } = NameMatchingMode.CaseInsensitive;

		// -
		public NameMatchingMode EnumMatchByAttributeName { get; set; } = NameMatchingMode.CaseInsensitive;


		// - 
		public Action<Arg>? OnUnmatchedArgument { get; set; } = null;

		// -
		public Func<Type, object> OnCreateObject { get; set; } = propType => Activator.CreateInstance(propType);



		// -
		public void AddCustomConverter<TTargetType>(IArgConverter<TTargetType> converter)
		{
			var type = typeof(TTargetType);
			var (_, existing)= _customConverters.FirstOrDefault(t => t.targetType == type);
			if (existing != null)
				throw new InvalidOperationException($"There is already a custom converter registered for this type!\n" +
					$"Type: {type.FullName}\n" +
					$"ExistingConverter: {existing}\n" +
					$"NewConverter: {converter}");
			
			_customConverters.Add((type, converter));
		}
	}
}
