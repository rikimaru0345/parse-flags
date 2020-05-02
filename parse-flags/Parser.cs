using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ParseFlags
{
	class Context
	{
		public object RootTarget { get; }
		public Stack<(object obj, PropertyInfo parentProp)> TargetObjects { get; }
		public ParseOptions Options { get; }
		public Arg[] Args { get; }

		public int Depth => TargetObjects.Count;
		public object Target => TargetObjects.Count == 0 ? RootTarget : TargetObjects.Peek().obj;
		public object TargetName => TargetObjects.Count == 0 ? "" : TargetObjects.Peek().parentProp.Name;

		public Context(object rootTarget, ParseOptions options, Arg[] args)
		{
			RootTarget = rootTarget;
			TargetObjects = new Stack<(object obj, PropertyInfo parentProp)>();
			Options = options ?? new ParseOptions();
			Args = args;
		}

	}

	public static class Parser
	{
		public static T Parse<T>(string[] args, ParseOptions options = null) where T : class
			=> Parse<T>(args, options, null);


		public static T Parse<T>(string[] args, ParseOptions options, T existingObject) where T : class
		{
			if (args == null)
				throw new ArgumentNullException("args", "parameter args is required and must not be null (but can be empty)");

			T target = existingObject ?? Activator.CreateInstance<T>();

			var pairs = ArgsToKeyValuePairs(args);

			var ctx = new Context(target, options, pairs);

			AssignProperties(ctx);

			return target;
		}


		static void AssignProperties(Context ctx)
		{
			foreach (var arg in ctx.Args)
			{
				// Can't be a match since the name is still too "deep"
				if (ctx.Depth >= arg.Path.Length)
					continue;

				// Matching namespace?
				// todo: ctx.Args.Zip(arg.Key)


				// Find property that matches the name
				var propName = arg.Path[ctx.Depth];
				var props = ctx.Target
					.GetType()
					.GetProperties(BindingFlags.Public | BindingFlags.Instance)
					.Where(p => p.CanWrite)
					.Where(p => string.Equals(p.Name, propName, StringComparison.OrdinalIgnoreCase))
					.ToArray();

				if (props.Length > 1)
				{
					var propIdentifiers = props.Select(p => $"({p.PropertyType.Name} {p.Name})");
					var propsStr = string.Join(", ", propIdentifiers);
					throw new InvalidOperationException($"Ambiguous match! The argument \"{arg.Key}\" matches multiple properties: ");
				}

				if (props.Length == 0)
					// todo: "got arg that didn't match any properties!"
					// todo: "did you mean ___" (case-insensitive check)
					continue;

				var prop = props[0];
				SetProp(ctx, prop, arg);
			}
		}

		static void SetProp(Context ctx, PropertyInfo prop, Arg arg)
		{
			var propType = prop.PropertyType;

			// Actual values
			if (TryConvert(arg.Value, propType, out object result))
			{
				prop.SetValue(ctx.Target, result);
				arg.SetConsumed(ctx, prop);
				return;
			}

			// Sub object
			if (propType.IsClass)
			{
				// Ensure the arg isn't trying to set the object itself
				if(arg.Path.Length == ctx.Depth)
					throw new InvalidOperationException($"Argument path \"{arg.Key}\" is invalid because it points to an object! Did you misspell the argument name, or did you mean to access one of the properties inside property \"{prop.Name}\" (PropType=\"{propType.FullName}\")?");

				// Get or create object
				object subObj = prop.GetValue(ctx.Target);
				if (subObj == null)
				{
					subObj = Activator.CreateInstance(propType); // todo: maybe add support for a user-provided factory method?
					prop.SetValue(ctx.Target, subObj);
				}

				// 

				AssignProperties(depth + 1, subObj, args);

				return;
			}

			throw new InvalidOperationException($"Cannot handle property type!\n" +
				$"PropertyType: {prop.PropertyType.FullName}\n" +
				$"Argument: {arg.Key}\n" +
				$"PropertyName: {prop.Name}\n" +
				$"DeclaringType: {prop.DeclaringType.FullName}");
		}

		static bool TryConvert(string value, Type targetType, out object result)
		{
			// todo: allow user-provided converters

			// String
			if (targetType == typeof(string))
			{
				result = value;
				return true;
			}

			// Bool, Int, Float, Enum, DateTime,  ...
			if (targetType.IsPrimitive || targetType == typeof(DateTime) || targetType == typeof(decimal))
			{
				result = Convert.ChangeType(value, targetType);
				return true;
			}

			// Array
			if (targetType.IsArray)
			{
				var elementType = targetType.GetElementType();
				// todo: get seperator from attribute or options
				// todo: maybe add support for quotes (so seperator can be used inside)
				var strValues = value.Split(',');

				var ar = Array.CreateInstance(elementType, strValues.Length);
				for (int i = 0; i < strValues.Length; i++)
				{
					if(TryConvert(strValues[i], elementType, out object element))
						ar.SetValue(element, i);
					else
						throw new InvalidCastException($"Error while converting array element to type \"{elementType.FullName}\". Index: [{i}]. SourceValue: \"{strValues[i]}\"");
				}

				result = ar;
				return true;
			}

			result = null;
			return false;
		}


		static Arg[] ArgsToKeyValuePairs(string[] args)
		{
			return args.Select(rawArg =>
			{
				// Remove leading dashes
				var arg = rawArg.TrimStart('-');

				// Split name and value
				var split = arg.Split(new[] { '=' }, 2, StringSplitOptions.None);
				var key = split[0].Trim();
				var value = split[1].Trim();

				// Split name by '.'
				var path = key.Split('.');

				return new Arg(rawArg, arg, key, path, value);
			}).ToArray();
		}
	}

	class Arg
	{
		public readonly string Raw;         // original argument:	 "--a.b.c=123"
		public readonly string RawTrimmed;  //						 "a.b.c=123"
		public readonly string Key;         // original key:		 "a.b.c"
		public readonly string[] Path;      // key split by '.':	 [ "a", "b", "c" ]
		public readonly string Value;       // value:				 "123"

		string consumedBy;
		public bool IsConsumed { get => consumedBy != null; }
		public void SetConsumed(Context ctx, PropertyInfo prop)
		{
			var path = $"(Type={prop.PropertyType.FullName} Name={prop.Name} Path={string.Join(".", propertyNamePath)} DeclaringType={prop.DeclaringType.FullName})";

			if (consumedBy != null)
				throw new InvalidOperationException(
					$"Trying to consume argument again!\n" +
					$"Argument: \"{Key}\"\n" +
					$"Already consumed by: \"{consumedBy}\"\n" +
					$"New contending consumer: \"{path}\"");

			consumedBy = path;
		}


		public Arg(string raw, string rawTrim, string key, string[] path, string value)
		{
			Raw = raw;
			RawTrimmed = rawTrim;
			Key = key;
			Path = path;
			Value = value;
		}
	}
}
