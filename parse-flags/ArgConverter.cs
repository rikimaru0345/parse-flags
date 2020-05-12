using System;
using System.Collections.Generic;
using System.Text;

namespace ParseFlags
{
	public interface IArgConverter<TTargetType>
	{
		bool CanConvert(Arg arg);

		TTargetType ParseAgument(string value);
	}

	static class ConverterHelper
	{
		public static bool CanConvert(this object converter, Arg arg)
		{
			var converterType = converter.GetType();
			var type = converterType.FindClosedArg(typeof(IArgConverter<>));
			
			var m = converterType.GetMethod(nameof(IArgConverter<int>.CanConvert));
			return (bool)m.Invoke(converter, new object[] { arg });
		}

		public static object Parse(this object converter, string value)
		{
			var converterType = converter.GetType();
			var type = converterType.FindClosedArg(typeof(IArgConverter<>));
			
			var m = converterType.GetMethod(nameof(IArgConverter<int>.ParseAgument));
			return m.Invoke(converter, new object[] { value });
		}
	}
}
