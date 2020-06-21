#nullable enable

using ParseFlags;
using System;
using System.Collections.Generic;
using System.Text;


namespace ParseFlags.Converters
{
	// Enums
	public class EnumConverter : IArgConverter<Enum>
	{
		public bool TryConvert(ConverterContext ctx, Arg arg, out Enum? value)
		{
			value = null;
			var targetType = ctx.TargetType;

			if (!targetType.IsEnum)
				return false;

			// Try to find enum value by attribute, name, or value
			var enumValue = ResolveEnum(ctx.ParseOptions, targetType, arg.Value) as Enum;
			if (enumValue == null)
				throw new InvalidCastException($"Given value \"{value}\" cannot be converted to enum type \"{targetType.FullName}\"");

			return true;
		}



		static object? ResolveEnum(ParseOptions options, Type enumType, string value)
		{
			Array values = Enum.GetValues(enumType);
			foreach (var enumValue in values)
			{
				var decimalStr = Enum.Format(enumType, enumValue, "d");
				if (decimalStr == value)
					return enumValue;

				var enumName = Enum.Format(enumType, enumValue, "f");
				if (options.ParseEnumsByName == NameMatchingMode.Exact)
					if (enumName == value)
						return enumValue;

				if (options.ParseEnumsByName == NameMatchingMode.CaseInsensitive)
					if (string.Equals(enumName, value, StringComparison.OrdinalIgnoreCase))
						return enumValue;

				var enumValueField = enumType.GetField(enumName);
				var nameAttribute = Attribute.GetCustomAttribute(enumValueField, typeof(EnumOptionAttribute)) as EnumOptionAttribute;
				if (nameAttribute != null)
				{
					if (options.ParseEnumsByAttribute == NameMatchingMode.Exact)
						if (nameAttribute.Name == value)
							return enumValue;

					if (options.ParseEnumsByAttribute == NameMatchingMode.CaseInsensitive)
						if (string.Equals(nameAttribute.Name, value, StringComparison.OrdinalIgnoreCase))
							return enumValue;
				}
			}

			return null;
		}

	}
}
