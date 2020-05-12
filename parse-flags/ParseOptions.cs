#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParseFlags
{
	public class ParseOptions
	{
		internal readonly List<(Type targetType, object converter)> _customConverters = new List<(Type, object)>();


		// - MatchByAttribute: Disabled, Exact, CaseInsensitive
		// - MatchByPropertyName: Disabled, Exact, CaseInsensitive
		public bool AllowCaseInsensitiveMatching { get; set; } = false;

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
