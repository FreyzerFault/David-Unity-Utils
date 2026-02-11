using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DavidUtils.Utils
{
	public static class MiscExtensions
	{
		#region NULL or EMPTY

		// COLLECTION
		public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection) => collection == null || !collection.Any();
		public static bool NotNullOrEmpty<T>(this IEnumerable<T> collection) => !collection.IsNullOrEmpty();

		// STRING
		public static bool IsNullOrEmpty(this string str) => string.IsNullOrEmpty(str);
		public static bool NotNullOrEmpty(this string str) => !string.IsNullOrEmpty(str);

		#endregion


		#region STRING MODIFICATION

		/// <summary>
		///     "Camel case string" => "CamelCaseString"
		/// </summary>
		public static string ToCamelCase(this string message)
		{
			message = message.Replace("-", " ").Replace("_", " ");
			message = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(message);
			message = message.Replace(" ", "");
			return message;
		}

		/// <summary>
		///     "CamelCaseString" => "Camel Case String"
		/// </summary>
		public static string SplitCamelCase(this string camelCaseString)
		{
			if (string.IsNullOrEmpty(camelCaseString)) return camelCaseString;

			string camelCase = Regex.Replace(
				Regex.Replace(camelCaseString, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"),
				@"(\p{Ll})(\P{Ll})",
				"$1 $2"
			);
			string firstLetter = camelCase.Substring(0, 1).ToUpper();

			if (camelCaseString.Length > 1)
			{
				string rest = camelCase.Substring(1);

				return firstLetter + rest;
			}

			return firstLetter;
		}

		/// <summary>
		///     Convert a string value to an Enum value.
		/// </summary>
		public static T AsEnum<T>(this string source, bool ignoreCase = true) where T : Enum =>
			(T)Enum.Parse(typeof(T), source, ignoreCase);
		
		/// <summary>
		/// Convierte un string a otro truncado a un maximo de caracteres
		/// Ejemplo con 5 caracteres:
		///  "123456789" -> "12345..."
		///  "1234" -> " 1234 "
		/// </summary>
		public static string TruncateFixedSize(this string str, int maxSize)
		{
			if (str.Length == maxSize) return str;
            
			if (str.Length > maxSize)
				return str[..(maxSize - 3)] + "...";

			int padding = Mathf.FloorToInt((maxSize - str.Length) / 2f);
			return str.PadLeft(str.Length + padding).PadRight(maxSize);
		}

		#endregion


		#region SPECIAL STRING CONVERSIONS

		/// <summary>
		///     Number presented in Roman numerals
		/// </summary>
		public static string ToRoman(this int i) => i switch
		{
			> 999 => "M" + ToRoman(i - 1000),
			> 899 => "CM" + ToRoman(i - 900),
			> 499 => "D" + ToRoman(i - 500),
			> 399 => "CD" + ToRoman(i - 400),
			> 99 => "C" + ToRoman(i - 100),
			> 89 => "XC" + ToRoman(i - 90),
			> 49 => "L" + ToRoman(i - 50),
			> 39 => "XL" + ToRoman(i - 40),
			> 9 => "X" + ToRoman(i - 10),
			> 8 => "IX" + ToRoman(i - 9),
			> 4 => "V" + ToRoman(i - 5),
			> 3 => "IV" + ToRoman(i - 4),
			> 0 => "I" + ToRoman(i - 1),
			_ => ""
		};

		#endregion


		#region STRING FORMAT

		/// <summary>
		///     Surround string with "color" tag
		/// </summary>
		public static string Colored(this string message, DebugColor debugColor) => $"<color={debugColor}>{message}</color>";

		/// <summary>
		///     Surround string with "color" tag
		/// </summary>
		public static string Colored(this string message, Color color) => $"<color={color.ToHex()}>{message}</color>";

		/// <summary>
		///     Surround string with "color" tag
		/// </summary>
		public static string Colored(this string message, string colorCode) => $"<color={colorCode}>{message}</color>";

		/// <summary>
		///     Surround string with "size" tag
		/// </summary>
		public static string Sized(this string message, int size) => $"<size={size}>{message}</size>";

		/// <summary>
		///     Surround string with "u" tag
		/// </summary>
		public static string Underlined(this string message) => $"<u>{message}</u>";

		/// <summary>
		///     Surround string with "b" tag
		/// </summary>
		public static string Bold(this string message) => $"<b>{message}</b>";

		/// <summary>
		///     Surround string with "i" tag
		/// </summary>
		public static string Italics(this string message) => $"<i>{message}</i>";

		#endregion


		#region COLLECTION EXTENSIONS

		public static int IndexOf<T>(this IEnumerable<T> collection, T item)
		{
			if (collection == null)
			{
				Debug.LogError("IndexOfItem Caused: source collection is null");
				return -1;
			}

			var index = 0;
			foreach (T i in collection)
			{
				if (Equals(i, item)) return index;
				++index;
			}

			return -1;
		}

		/// <summary>
		///     Elements in 2 collections are EQUALS (Order does not matter)
		/// </summary>
		public static bool ContentsMatch<T>(this IEnumerable<T> first, IEnumerable<T> second)
		{
			IEnumerable<T> enumerable = first as T[] ?? first.ToArray();
			IEnumerable<T> enumerable1 = second as T[] ?? second.ToArray();
			if (enumerable.IsNullOrEmpty() && enumerable1.IsNullOrEmpty()) return true;
			if (enumerable.IsNullOrEmpty() || enumerable1.IsNullOrEmpty()) return false;

			int firstCount = enumerable.Count();
			int secondCount = enumerable1.Count();
			return firstCount == secondCount && enumerable.All(x1 => enumerable1.Contains(x1));
		}

		/// <summary>
		///     Keys in Dictionary match collection (Order does not matter)
		/// </summary>
		public static bool ContentsMatchKeys<T1, T2>(this IDictionary<T1, T2> source, IEnumerable<T1> check)
		{
			IEnumerable<T1> enumerable = check as T1[] ?? check.ToArray();
			if (source.IsNullOrEmpty() && enumerable.IsNullOrEmpty()) return true;
			if (source.IsNullOrEmpty() || enumerable.IsNullOrEmpty()) return false;

			return source.Keys.ContentsMatch(enumerable);
		}

		/// <summary>
		///     Values in Dictionary match collection (Order does not matter)
		/// </summary>
		public static bool ContentsMatchValues<T1, T2>(this IDictionary<T1, T2> source, IEnumerable<T2> check)
		{
			IEnumerable<T2> enumerable = check as T2[] ?? check.ToArray();
			if (source.IsNullOrEmpty() && enumerable.IsNullOrEmpty()) return true;
			if (source.IsNullOrEmpty() || enumerable.IsNullOrEmpty()) return false;

			return source.Values.ContentsMatch(enumerable);
		}

		/// <summary>
		///     Assign DEFAULT value to key in Dictionary if key not contained.
		/// </summary>
		/// <returns>New value, or existing value if the key exists.</returns>
		public static TValue GetOrAddDefault<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key)
			where TValue : new()
		{
			if (!source.ContainsKey(key)) source[key] = new TValue();
			return source[key];
		}

		/// <summary>
		///     Assign value to key in Dictionary if key not contained.
		/// </summary>
		/// <returns>New value, or existing value if the key exists.</returns>
		public static TValue GetOrAdd<TKey, TValue>(
			this IDictionary<TKey, TValue> source,
			TKey key, TValue value
		)
		{
			source.TryAdd(key, value);
			return source[key];
		}


		/// <summary>
		///     Performs an action on each element of a collection.
		/// </summary>
		public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
		{
			if (source == null) return;
			foreach (T element in source) action(element);
		}

		/// <summary>
		///		Performs an action on each element of a collection
		///		modifying the source
		/// </summary>
		public static void ForEachMutable<T>(this T[] source, Func<T,T> action)
		{
			if (source == null) return;
			for (var i = 0; i < source.Length; i++) 
				source[i] = action(source[i]);
		}
			
		/// <summary>
		///     Performs an action on each element of a collection using the index.
		/// </summary>
		public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
		{
			if (source == null) return;
			var i = 0;
			source.ForEach(element => action(element, i++));
		}

		/// <summary>
		///     Find the element of a collection that has the highest selected value.
		/// </summary>
		public static T MaxBy<T, TVal>(this IEnumerable<T> source, Func<T, TVal> selector)
			where TVal : IComparable<TVal>
		{
			IEnumerable<T> enumerable = source as T[] ?? source.ToArray();
			if (enumerable.IsNullOrEmpty())
			{
				Debug.LogError("MaxBy Caused: source collection is null or empty");
				return default;
			}

			return enumerable.Aggregate((e, n) => selector(e).CompareTo(selector(n)) > 0 ? e : n);
		}

		/// <summary>
		///     Find the element of a collection that has the lowest selected value.
		/// </summary>
		public static T MinBy<T, TVal>(this IEnumerable<T> source, Func<T, TVal> selector)
			where TVal : IComparable<TVal>
		{
			IEnumerable<T> enumerable = source as T[] ?? source.ToArray();
			if (enumerable.IsNullOrEmpty())
			{
				Debug.LogError("MinBy Caused: source collection is null or empty");
				return default;
			}

			return enumerable.Aggregate((e, n) => selector(e).CompareTo(selector(n)) < 0 ? e : n);
		}


		/// <summary>
		///     Convert element into an Array with this single element
		/// </summary>
		public static IEnumerable<T> ToSingleArray<T>(this T source) => Array.Empty<T>().Append(source);

		/// <summary>
		///     Convert element into an Array filled with this element
		/// </summary>
		public static IEnumerable<T> ToFilledArray<T>(this T source, int size)
		{
			if (size <= 0) return Array.Empty<T>();
			var array = new T[size];
			Array.Fill(array, source);
			return array;
		}

		/// <summary>
		///     Fills an array with values generated using a factory function with index.
		/// </summary>
		public static IEnumerable<T> FillBy<T>(this IEnumerable<T> source, Func<int, T> valueFactory)
		{
			IEnumerable<T> array = source as T[] ?? source.ToArray();
			return array.Select((_, i) => valueFactory(i));
		}

		/// <summary>
		///     First index of an item that matches a predicate.
		/// </summary>
		/// <returns>
		///		-1 if not found
		/// </returns>
		public static int FirstIndex<T>(this IEnumerable<T> source, Predicate<T> predicate)
		{
			var index = 0;
			foreach (T e in source)
			{
				if (predicate(e)) return index;
				++index;
			}

			return -1;
		}

		/// <summary>
		///     Last index of an item that matches a predicate.
		/// </summary>
		public static int LastIndex<T>(this IEnumerable<T> source, Predicate<T> predicate)
		{
			IEnumerable<T> enumerable = source as T[] ?? source.ToArray();
			int index = enumerable.Count() - 1;
			foreach (T e in enumerable)
			{
				if (index < 0) return -1;
				if (predicate(e)) return index;
				index--;
			}

			return -1;
		}
		
		/// <summary>
		///		All Indices of items that match a predicate.
		/// </summary>
		public static int[] AllIndices<T>(this IEnumerable<T> source, Predicate<T> predicate)
		{
			IEnumerable<T> enumerable = source as T[] ?? source.ToArray();
			List<int> indices = new();
			int index = 0;
			foreach (T e in enumerable)
			{
				if (predicate(e)) indices.Add(index);
				++index;
			}

			return indices.ToArray();
		}
		
		/// <summary>
		///		Consulta elementos con los indices dados
		///		NULL si algun indice esta fuera de rango
		/// </summary>
		public static IEnumerable<T> FromIndices<T>(this IEnumerable<T> source, params int[] indices)
		{
			IEnumerable<T> enumerable = source as T[] ?? source.ToArray();
			return indices.Any(i => i >= enumerable.Count()) ? null : indices.Select(i => enumerable.ElementAt(i));
		}  

		/// <summary>
		///     Swaps 2 elements at the specified index positions in place.
		/// </summary>
		public static IList<T> SwapElements<T>(this IList<T> source, int index1, int index2)
		{
			(source[index1], source[index2]) = (source[index2], source[index1]);
			return source;
		}

		/// <summary>
		///     Shuffles a collection in place using the Knuth algorithm.
		/// </summary>
		public static IList<T> Shuffle<T>(this IList<T> source)
		{
			for (var i = 0; i < source.Count - 1; ++i)
			{
				int indexToSwap = Random.Range(i, source.Count);
				source.SwapElements(i, indexToSwap);
			}

			return source;
		}
		
		/// <summary>
		/// 2D Array to 1D Array
		/// </summary>
		public static float[] Flatten(this float[,] source)
		{
			int n = source.GetLength(0);
			int m = source.GetLength(1);
			float[] flat = new float[n * m];
			for (var i = 0; i < n; i++)
			for (var j = 0; j < m; j++)
				flat[i * m + j] = source[i, j];
			return flat;
		}
		public static float[] Flatten(this float[][] source)
		{
			int n = source.GetLength(0);
			int m = source.GetLength(1);
			float[] flat = new float[n * m];
			for (var i = 0; i < n; i++)
			for (var j = 0; j < m; j++)
				flat[i * m + j] = source[i][j];
			return flat;
		}
		
		
		/// <summary>
		/// Recorta el array en un rango [i,j] (i y j INCLUIDOS)
		/// </summary>
		public static IEnumerable<T> CropRange<T>(this IEnumerable<T> source, int i, int j)
		{
			if (j < i) (i, j) = (j, i);
			IEnumerable<T> enumerable = source as T[] ?? source.ToArray();
			if (i < 0 || j < 0 || i >= enumerable.Count() || j >= enumerable.Count())
				throw new Exception("Crop Caused: Index out of bounds");

			return enumerable.Skip(i).Take(j - i + 1);
		}
		
		/// <summary>
		/// Recorta el array en un rango [0,i] + [j,^1] (i y j INCLUIDOS)
		/// </summary>
		public static IEnumerable<T> CropOutOfRange<T>(this IEnumerable<T> source, int i, int j)
		{
			if (j < i) (i, j) = (j, i);
			IEnumerable<T> enumerable = source as T[] ?? source.ToArray();
			if (i < 0 || j < 0 || i >= enumerable.Count() || j >= enumerable.Count())
				throw new Exception("Crop Caused: Index out of bounds");

			return enumerable.Take(i + 1).Concat(enumerable.Skip(j));
		}

		#endregion


		#region ITERATIONS

		// ================ ITERACION POR PARES - [i,j] => [0,1], [1,2], [2,3], ... ================
		/// <summary>
		///     Itera la colección por pares [i,j] => [0,1], [1,2], [2,3], ...
		///     Es un ciclo, el ultimo par sera [^1, 0]
		/// </summary>
		public static IEnumerable<R> IterateByPairs<T, R>(
			this IEnumerable<T> source, Func<T, T, R> action, bool loop = true, bool loopAtStart = true
		)
		{
			IEnumerable<T> enumerable = source as T[] ?? source.ToArray();
			List<R> results = new();
			int n = enumerable.Count();

			// Start at end-first pair - [^1, 0]
			if (loop && loopAtStart)
				results.Add(action(enumerable.Last(), enumerable.First()));

			// ITERATE
			for (var i = 0; i < n - 1; i++)
				results.Add(action(enumerable.ElementAt(i), enumerable.ElementAt(i + 1)));

			// Cycle through - [^1, 0]
			if (loop && !loopAtStart)
				results.Add(action(enumerable.Last(), enumerable.First()));

			return results;
		}

		/// <summary>
		///     Itera la colección como un ciclo, por pares [i,j] => [0,1], [1,2], [2,3], ...
		/// </summary>
		/// <param name="source">Colleción</param>
		/// <param name="action">Acción por cada par [i,j]</param>
		/// <param name="loopAtStart">Procesa el Par [End,Start] 1º</param>
		public static IEnumerable<R> IterateByPairs_InLoop<T, R>(
			this IEnumerable<T> source, Func<T, T, R> action, bool loopAtStart = true
		) =>
			source.IterateByPairs(action, true, loopAtStart);

		/// <summary>
		///     Itera la colección por pares [i,j] => [0,1], [1,2], [2,3], ...
		/// </summary>
		/// <param name="source">Colección</param>
		/// <param name="action">Acción por cada par [i,j]</param>
		public static IEnumerable<R> IterateByPairs_NoLoop<T, R>(this IEnumerable<T> source, Func<T, T, R> action) =>
			source.IterateByPairs(action, false, false);

		/// <summary>
		///		Create a Pair List from a Collection [a,b], [c,d], [e,f], ...
		/// </summary>
		public static IEnumerable<Tuple<T,T>> GroupInPairs<T>(this IEnumerable<T> source)
		{
			IEnumerable<T> enumerable = source as T[] ?? source.ToArray();

			switch (enumerable.Count())
			{
				// TRIVIAL Cases
				case 1:
					return new[] { new Tuple<T, T>(enumerable.ElementAt(0), default) };
				case 2:
					return new[] { new Tuple<T, T>(enumerable.ElementAt(0), enumerable.ElementAt(1)) };
				
				default:
					List<Tuple<T, T>> pairs = new();
					for (var i = 0; i < enumerable.Count(); i += 2)
					{
						// Odd number of elements
						if (i + 1 == enumerable.Count()) return pairs;
				
						T a = enumerable.ElementAt(i);
						T b = enumerable.ElementAt(i + 1);
				
						pairs.Add(new Tuple<T, T>(a,b));
					}

					return pairs;
			}
		}
		
		#endregion


		#region RANDOMIZATION

		public static T PickRandom<T>(this IEnumerable<T> source)
		{
			IEnumerable<T> enumerable = source as T[] ?? source.ToArray();

			if (enumerable.IsNullOrEmpty())
				throw new Exception("PickRandom Caused: source collection is null or empty");

			return enumerable.ElementAt(Random.Range(0, enumerable.Count()));
		}

		public static T PickByProbability<T>(this IEnumerable<T> source, float[] probabilities)
		{
			IEnumerable<T> enumerable = source as T[] ?? source.ToArray();
			if (enumerable.IsNullOrEmpty())
				throw new Exception("PickRandom Caused: source collection is null or empty");

			// Si las probabilidades no coinciden con la cantidad de elementos
			// se ajusta el tamaño de las probabilidades
			// Si faltan probabilidades, se rellenan con un 0 %
			if (enumerable.Count() > probabilities.Length)
				probabilities = probabilities.Concat(0f.ToFilledArray(enumerable.Count() - probabilities.Length))
					.ToArray();
			else if (enumerable.Count() < probabilities.Length)
				probabilities = probabilities.Take(enumerable.Count()).ToArray();

			// Si la SUMA de Probabilidades es > 1
			// se normaliza restando el exceso a cada probabilidad
			float suma = probabilities.Sum();
			if (suma > 1)
				probabilities = probabilities.Select(p => p - (suma - 1) / probabilities.Length).ToArray();

			float random = Random.value;
			float probAdded = 0;
			for (var i = 0; i < probabilities.Length; i++)
			{
				probAdded += probabilities[i];
				if (random < probAdded) return enumerable.ElementAt(i);
			}

			return enumerable.Last();
		}

		/// <summary>
		/// Normaliza las Probabilidades de un array para que la suma sea == 1
		/// Si se conoce el Index del valor incorrecto, se ignora y se normalizan los demas solamente
		/// </summary>
		public static IEnumerable<float> NormalizeProbabilities(this IEnumerable<float> probs, int badIndex = -1)
		{
			IEnumerable<float> enumerable = probs as float[] ?? probs.ToArray();
			float suma = enumerable.Sum();
			
			// Suma == 1 => Ya esta normalizado
			if (Mathf.Approximately(suma, 1)) return enumerable;
			
			float fixOffset = (suma - 1)
			                  // Si conocemos el Index del valor incorrecto => Count - 1
			                  // Si no lo conocemos => Count
			                  / (enumerable.Count() - (badIndex == -1 ? 0 : 1));
			
			return enumerable.Select((p, i) =>
			{
				// Si conocemos el Index del valor incorrecto => Lo ignoramos
				if (i == badIndex) return p;
				
				if (p - fixOffset < 0)
					fixOffset -= p - fixOffset;
				return Mathf.Clamp01(p - fixOffset);
			});
		}

		#endregion


		#region TYPES

		// Get ALL Enum Values to an Array
		public static T[] GetEnumValues<T>(this Type enumType) => 
			!enumType.IsEnum ? Array.Empty<T>() : Enum.GetValues(enumType).Cast<T>().ToArray();

		#endregion
	}
}
