﻿#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ParseFlags.Converters;

namespace ParseFlags
{
	class Context
	{
		readonly Stack<(object obj, PropertyInfo parentProp, string[] path)> _targets = new Stack<(object, PropertyInfo, string[])>();
		readonly List<string> _targetNames = new List<string>();

		public object RootTarget { get; }
		public ParseOptions Options { get; }
		public Arg[] AllArgs { get; }

		public int Depth => _targets.Count;
		public object Target => _targets.Count == 0 ? RootTarget : _targets.Peek().obj;
		public object TargetName => _targets.Count == 0 ? "" : _targets.Peek().parentProp.Name;
		public string[] CurrentPath { get; private set; } // path of property names

		public Context(object rootTarget, ParseOptions? options, Arg[] args)
		{
			RootTarget = rootTarget;
			Options = options ?? new ParseOptions();
			AllArgs = args;
			CurrentPath = Array.Empty<string>();
		}

		public void Push(object newTarget, PropertyInfo prop)
		{
			_targets.Push((newTarget, prop, CurrentPath));

			_targetNames.Add(prop.Name);
			CurrentPath = _targetNames.ToArray();
		}

		public void Pop()
		{
			var t = _targets.Pop(); // ascend again
			_targetNames.RemoveAt(_targetNames.Count - 1);
			CurrentPath = t.path; // restore old path
		}
	}

	public static class Parser
	{
		static readonly IArgConverter[] BuiltInConverters = new IArgConverter[] {
			new StringConverter(),
			new PrimitiveConverter(),
			new EnumConverter(),
		};

		public static T Parse<T>(string[] args, ParseOptions? options = null) where T : class
			=> Parse<T>(args, options, null);

		public static T Parse<T>(string[] args, ParseOptions? options, T? existingObject) where T : class
		{
			if (args == null)
				throw new ArgumentNullException("args", "parameter args is required and must not be null (but can be empty)");

			T target = existingObject ?? Activator.CreateInstance<T>();


			var (pairs, errors) = ArgsToKeyValuePairs(args);

			var ctx = new Context(target, options, pairs);

			if (errors.Length > 0)
				ctx.Options.OnMalformedArgument?.Invoke(pairs, errors);


			AssignProperties(ctx);

			return target;
		}


		static void AssignProperties(Context ctx)
		{
			// Find relevant args for this level
			var groups = ctx.AllArgs
				.Where(a => a.Path.Length > ctx.Depth) // only keep args at same depth or below
				.Where(a => // only keep args in same namespace as we're currently in
				{
					for (int i = 0; i <= ctx.Depth && i < ctx.CurrentPath.Length; i++)
						if (!string.Equals(a.Path[i], ctx.CurrentPath[i], StringComparison.OrdinalIgnoreCase))
							return false;
					return true;
				})
				.GroupBy(a => a.Path[ctx.Depth]) // for objects, only set them once at this level
				.ToArray();

			foreach (var argGroup in groups)
			{
				// Find property that matches the name
				var propName = argGroup.Key;
				var props = ctx.Target
					.GetType()
					.GetProperties(BindingFlags.Public | BindingFlags.Instance)
					.Where(p => p.CanWrite)
					.Where(p => IsNameMatch(ctx, p, propName))
					.Where(p => ctx.Options.RequireOptionAttribute ? p.GetCustomAttribute<OptionAttribute>() != null : true)
					.ToArray();

				if (props.Length > 1)
				{
					var propIdentifiers = props.Select(p => $"({p.PropertyType.Name} {p.Name})");
					var propsStr = string.Join(", ", propIdentifiers);
					throw new InvalidOperationException($"Ambiguous match! The argument \"{argGroup.Key}\" matches multiple properties: ");
				}

				if (props.Length == 0)
				{
					ctx.Options.OnUnmatchedArgument?.Invoke(argGroup.First());
					continue;
				}


				var prop = props[0];
				var propType = prop.PropertyType;

				// Actual value
				var arg = argGroup.First(); // if there are more: warn that this prop is not an object
				if (TryConvert(ctx, arg, prop, propType, out object? result))
				{
					prop.SetValue(ctx.Target, result);
					arg.SetConsumed(ctx, prop);
					continue;
				}

				// Sub object
				if (propType.IsClass)
				{
					// Get or create object
					object subObj = prop.GetValue(ctx.Target);
					if (subObj == null)
					{
						subObj = ctx.Options.OnCreateObject(propType);
						prop.SetValue(ctx.Target, subObj);
					}

					ctx.Push(subObj, prop);
					AssignProperties(ctx);
					ctx.Pop();
					continue;
				}

				throw new InvalidOperationException($"Cannot handle property type!\n" +
					$"PropertyType: {prop.PropertyType.FullName}\n" +
					$"Argument: {argGroup.Key}\n" +
					$"PropertyName: {prop.Name}\n" +
					$"DeclaringType: {prop.DeclaringType.FullName}");
			}
		}

		static bool TryConvert(Context ctx, Arg arg, PropertyInfo prop, Type targetType, out object? result)
		{
			var value = arg.Value;

			var converterContext = new ConverterContext
			{
				ParseOptions = ctx.Options,
				Arg = arg,
				TargetProperty = prop,
				TargetType = targetType
			};

			// User provided converters
			foreach (var t in ctx.Options._customConverters)
				if (t.converter.TryConvert(converterContext, arg, out result))
					return true;

			// Built in converters
			foreach (var c in BuiltInConverters)
				if (c.TryConvert(converterContext, arg, out result))
					return true;

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
					var elementArg = Arg.CreateForArrayElement(arg, strValues[i]);

					if (TryConvert(ctx, elementArg, prop, elementType, out object? element))
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

		static bool IsNameMatch(Context ctx, PropertyInfo prop, string targetName)
		{
			if (ctx.Options.MatchPropertyByName == NameMatchingMode.Exact)
				if (prop.Name == targetName)
					return true;

			if (ctx.Options.MatchPropertyByName == NameMatchingMode.CaseInsensitive)
				if (string.Equals(prop.Name, targetName, StringComparison.OrdinalIgnoreCase))
					return true;


			var op = prop.GetCustomAttribute<OptionAttribute>();
			if (op != null)
			{
				if (ctx.Options.MatchPropertyByAttribute == NameMatchingMode.Exact)
					if (op.Name == targetName)
						return true;

				if (ctx.Options.MatchPropertyByAttribute == NameMatchingMode.CaseInsensitive)
					if (string.Equals(op.Name, targetName, StringComparison.OrdinalIgnoreCase))
						return true;
			}

			return false;
		}


		static (Arg[], string[] errors) ArgsToKeyValuePairs(string[] args)
		{
			var parsedArgs = new List<Arg>();
			var errors = new List<string>();

			foreach (var rawArg in args)
			{
				// Remove leading dashes
				var arg = rawArg.TrimStart('-');

				// Split name and value
				var split = arg.Split(new[] { '=' }, 2, StringSplitOptions.None);
				if (split.Length != 2)
				{
					errors.Add("Cannot split argument by '=' into name and value: " + arg);
					continue;
				}

				var key = split[0].Trim();
				var value = split[1].Trim();

				// Split name by '.'
				var path = key.Split('.');

				parsedArgs.Add(new Arg(rawArg, arg, key, path, value));
			}

			return (parsedArgs.ToArray(), errors.ToArray());
		}
	}

	public class Arg
	{
		public readonly string Raw;         // original argument:	 "--a.b.c=123"
		public readonly string RawTrimmed;  //						 "a.b.c=123"
		public readonly string Key;         // original key:		 "a.b.c"
		public readonly string[] Path;      // key split by '.':	 [ "a", "b", "c" ]
		public readonly string Value;       // value:				 "123"

		string? consumedBy;
		internal bool IsConsumed { get => consumedBy != null; }
		internal void SetConsumed(Context ctx, PropertyInfo prop)
		{
			var propertyNamePath = ctx.CurrentPath.Concat(new[] { prop.Name });
			var path =
				$"Path: {string.Join(".", propertyNamePath)}\n" +
				$"PropertyType: {prop.PropertyType.Name}\n" +
				$"DeclaringType: {prop.DeclaringType.Name}";

			if (consumedBy != null)
				throw new InvalidOperationException(
					$"Trying to consume argument again!\n" +
					$"Argument: \"{Key}\"\n\n" +
					$"New contending consumer:\n{path}\n\n" +
					$"Already consumed by:\n{consumedBy}");

			consumedBy = path;
		}


		internal Arg(string raw, string rawTrim, string key, string[] path, string value)
		{
			Raw = raw;
			RawTrimmed = rawTrim;
			Key = key;
			Path = path;
			Value = value;
		}

		internal static Arg CreateForArrayElement(Arg arrayArg, string elementValue)
			=> new Arg(arrayArg.Raw, arrayArg.RawTrimmed, arrayArg.Key, arrayArg.Path, elementValue);
	}

	abstract class AssignmentTarget { }
	class PropertyTarget : AssignmentTarget
	{
		public PropertyInfo Property { get; }

		public PropertyTarget(PropertyInfo property)
		{
			Property = property;
		}
	}
	class ArrayTarget : AssignmentTarget
	{
		public PropertyInfo ArrayProperty { get; }
		public Type ElementType { get; }
		public int Index { get; }
		public ArrayTarget(PropertyInfo arrayProperty, Type elementType, int index)
		{
			ArrayProperty = arrayProperty;
			ElementType = elementType;
			Index = index;
		}
	}
}
