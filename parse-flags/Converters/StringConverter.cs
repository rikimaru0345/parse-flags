using ParseFlags;
using System;
using System.Collections.Generic;
using System.Text;

namespace ParseFlags.Converters
{
	public class StringConverter : IArgConverter<string>
	{
		public bool TryConvert(ConverterContext ctx, Arg arg, out string value)
		{
			value = arg.Value;
			return true;
		}
	}
}
