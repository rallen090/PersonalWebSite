using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Web;

namespace Web.Utilities
{
	public static class CoreUtilities
	{
		public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		{
			var seenKeys = new HashSet<TKey>();
			foreach (var element in source)
			{
				if (seenKeys.Add(keySelector(element)))
				{
					yield return element;
				}
			}
		}

		public static bool Contains(this string source, string toCheck, StringComparison stringComparison)
		{
			return source?.IndexOf(toCheck, stringComparison) >= 0;
		}

		public static double ToPercentage(this double @this, int decimals = 1)
		{
			return (double)Math.Round((decimal)(@this * 100.0), decimals: decimals);
		}

		public static string ToDelimitedString<T>(this IEnumerable<T> items, string separator = ",")
		{
			return string.Join(separator, items);
		}

		public static bool HasProperty(this object objectToCheck, string propertyName)
		{
			var type = objectToCheck.GetType();
			return type.GetProperty(propertyName) != null;
		}

		public static void Each<T>(this IEnumerable<T> ie, Action<T, int> action)
		{
			var i = 0;
			foreach (var e in ie) action(e, i++);
		}

		public static IEnumerable<TReturn> SelectWithSiblings<TSource, TReturn>(this IEnumerable<TSource> elements, Func<TSource, TSource, TSource, TReturn> selector)
		{
			var evaluatedElements = elements.ToList();
			return evaluatedElements.Select((e, i) => selector(evaluatedElements.ElementAtOrDefault(i - 1), e, evaluatedElements.ElementAtOrDefault(i + 1)));
		}

		public static string Truncate(this string value, int maxLength, string tail = "...")
		{
			if (string.IsNullOrEmpty(value)) return value;
			var maxWithTail = (maxLength - tail.Length).Floor(0);
			return value.Length <= maxWithTail ? value : $"{value.Substring(0, maxWithTail)}...";
		}

		public static T Ceiling<T>(this T @this, T max) where T : IComparable
		{
			return @this.Capped(min: default(T), max: max, useMin: false, useMax: true);
		}

		public static T Floor<T>(this T @this, T min) where T : IComparable
		{
			return @this.Capped(min: min, max: default(T), useMin: true, useMax: false);
		}

		public static T Capped<T>(this T @this, T min, T max) where T : IComparable
		{
			return @this.Capped(min: min, max: max, useMin: true, useMax: true);
		}

		private static T Capped<T>(this T @this, T min, T max, bool useMin = false, bool useMax = false) where T : IComparable
		{
			if (@this == null)
			{
				return default(T);
			}
			var floored = useMin
				? (@this.CompareTo(min) < 0 ? min : @this)
				: @this;
			return useMax
				? (floored.CompareTo(max) > 0 ? max : floored)
				: floored;
		}

		public static bool Between<T>(this T @this, T a, T b, bool inclusiveA = true, bool inclusiveB = true) where T : IComparable
		{
			if (@this == null || a == null || b == null)
			{
				return false;
			}

			bool inclusiveLower;
			bool inclusiveHigher;
			T lower;
			T higher;
			if (a.CompareTo(b) <= 0)
			{
				lower = a;
				inclusiveLower = inclusiveA;
				higher = b;
				inclusiveHigher = inclusiveB;
			}
			else
			{
				lower = b;
				inclusiveLower = inclusiveB;
				higher = a;
				inclusiveHigher = inclusiveA;
			}

			var higherThanLower = inclusiveLower
				? @this.CompareTo(lower) >= 0
				: @this.CompareTo(lower) > 0;
			var lowerThanHigher = inclusiveHigher
				? @this.CompareTo(higher) <= 0
				: @this.CompareTo(higher) < 0;
			return higherThanLower && lowerThanHigher;
		}

		public static void NoOp()
		{
		}

		public static T NoOp<T>(this T @this)
		{
			return @this;
		}

		public static void Ignore<T>(this T @this)
		{
		}

		public static T ApplyIf<T>(this T @this, Func<T, T> apply, bool @if)
		{
			return @if ? apply(@this) : @this;
		}

		public static object GetPrivateFieldValue<T>(this T @this, string fieldName)
		{
			var bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
			var field = @this.GetType().GetField(fieldName, bindFlags);
			return field.GetValue(@this);
		}

		public static object AddProperty(this object obj, string name, object value)
		{
			var dictionary = obj.ToDictionary();
			dictionary.Add(name, value);
			return dictionary;
		}

		public static object AddPropertyIf(this object obj, string name, object value, bool @if)
		{
			if (@if)
			{
				return obj.AddProperty(name, value);
			}
			return obj;
		}

		// helper
		public static IDictionary<string, object> ToDictionary(this object obj)
		{
			IDictionary<string, object> result = new Dictionary<string, object>();
			PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(obj);
			foreach (PropertyDescriptor property in properties)
			{
				result.Add(property.Name, property.GetValue(obj));
			}
			return result;
		}

		public static IEnumerable<Type> FindInheritorsOf<TBaseType>()
		{
			var baseType = typeof(TBaseType);
			var assembly = Assembly.GetExecutingAssembly();

			return assembly.GetTypes().Where(t => t.IsSubclassOf(baseType));
		}
	}
}