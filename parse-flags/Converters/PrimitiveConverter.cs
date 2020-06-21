#nullable enable
using ParseFlags;
using System;
using System.Collections.Generic;
using System.Text;

namespace ParseFlags.Converters
{
	// Bool, Int, Float, DateTime,  ...
	public class PrimitiveConverter : IArgConverter<object>
	{
		public bool TryConvert(ConverterContext ctx, Arg arg, out object? value)
		{
			var targetType = ctx.TargetType;
			if (targetType.IsPrimitive || targetType == typeof(DateTime) || targetType == typeof(decimal))
			{
				value = Convert.ChangeType(arg.Value, targetType);
				return true;
			}

			value = null;
			return false;
		}
	}
}
