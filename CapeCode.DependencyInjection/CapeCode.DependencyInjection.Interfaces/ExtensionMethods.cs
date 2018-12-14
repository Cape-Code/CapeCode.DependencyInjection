using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CapeCode.ExtendedList;

namespace CapeCode.ExtensionMethods
{
    public static class ExtensionMethods
    {
        [DebuggerStepThrough]
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
                action(item);
        }

        [DebuggerStepThrough]
        public static SortedList<TKey, TSource> ToSortedList<TKey, TSource>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            var list = new SortedList<TKey, TSource>();
            source.ToList().ForEach(i => list.Add(keySelector(i), i));
            return list;
        }

        [DebuggerStepThrough]
        public static SortedList<T, T> ToSortedList<T>(this IEnumerable<T> source)
        {
            var list = new SortedList<T, T>();
            source.ToList().ForEach(i => list.Add(i, i));
            return list;
        }

        [DebuggerStepThrough]
        public static T ToEnum<T>(this string s)
        {
            return (T)Enum.Parse(typeof(T), s);
        }

        [DebuggerStepThrough]
        public static string ToString(this IEnumerable source, string separator)
        {
            return string.Join(separator, (from s in source.Cast<object>()
                                           where s != null
                                           select s.ToString()).ToArray());
        }

        [DebuggerStepThrough]
        public static string ToSeparatedString<TSource>(this IEnumerable<TSource> source, Func<TSource, string> elementToString, string separator = ",")
        {
            return source.Aggregate(string.Empty, (result, value) => result += elementToString(value) + separator, result => result.RemoveLast(separator.Length));
        }

        [DebuggerStepThrough]
        public static string ToSeparatedString(this IEnumerable<string> source, string separator = ",")
        {
            return source.Aggregate(string.Empty, (result, value) => result += value + separator, result => result.RemoveLast(separator.Length));
        }

        [DebuggerStepThrough]
        public static void Times<T>(this int count, T element, Action<T> execute)
        {
            for (int i = 0; i < count; i++)
                execute(element);
        }

        [DebuggerStepThrough]
        public static void Times(this int count, Action execute)
        {
            for (int i = 0; i < count; i++)
                execute();
        }

        [DebuggerStepThrough]
        public static IEnumerable<T> ListItemsOf<T>(this int count, T resultValue)
        {
            for (int i = 0; i < count; i++)
                yield return resultValue;
        }

        /// <summary>
        /// returns the current string duplicated for the given number of requested duplicates
        /// </summary>
        /// <param name="s">e.g. test</param>
        /// <param name="numberOfDuplicates">e.g. 2</param>
        /// <param name="separator">e.g. ;</param>
        /// <returns>e.g. test;test</returns>
        [DebuggerStepThrough]
        public static string Duplicate(this string s, int numberOfDuplicates, string separator)
        {
            var result = string.Empty;
            for (int i = 0; i < numberOfDuplicates; i++)
                result += s + separator;
            result.RemoveLast(separator.Length);
            return result;
        }

        [DebuggerStepThrough]
        public static string Format(this string s, params object[] args)
        {
            return string.Format(s, args);
        }

        [DebuggerStepThrough]
        public static string SeparateCamelCase(this string s)
        {
            return Regex.Replace(s, "([A-Z])", " $1", RegexOptions.Compiled).Trim();
        }

        [DebuggerStepThrough]
        public static TSource PickRandom<TSource>(this IEnumerable<TSource> source)
        {
            var list = source.ToList();

            if (list.Count == 0)
                return default(TSource);

            if (list.Count == 1)
                return list[0];

            return list[new Random().Next(list.Count)];
        }

        [DebuggerStepThrough]
        public static IEnumerable<List<T>> Partition<T>(this IList<T> source, int size)
        {
            for (int i = 0; i < Math.Ceiling(source.Count / (double)size); i++)
                yield return new List<T>(source.Skip(size * i).Take(size));
        }

        [DebuggerStepThrough]
        public static List<TResult> SelectToList<TSource, TResult>(this IEnumerable<TSource> source, params Func<TSource, TResult>[] selectors)
        {
            return source.Aggregate(new List<TResult>(), (c, n) =>
            {
                selectors.ForEach(s =>
                {
                    var v = s(n);
                    if (typeof(TResult).IsValueType || v != null)
                        c.Add(v);
                });
                return c;
            });
        }

        [DebuggerStepThrough]
        public static IList<TResult> Split<TSource, TResult>(this List<TSource> inputList, Func<IList<TSource>, TResult> resultSelector, int splitSize)
        {
            var outputList = new List<TResult>();

            for (int i = 0; i < inputList.Count; i += splitSize)
            {
                var split = inputList.GetRange(i, Math.Min(splitSize, inputList.Count - i));
                var splitAsResult = resultSelector(split);
                outputList.Add(splitAsResult);
            }

            return outputList;
        }

        [DebuggerStepThrough]
        public static ExtendedList<TSource> ToExtendedList<TSource>(this IEnumerable<TSource> source)
        {
            return new ExtendedList<TSource>(source);
        }

        [DebuggerStepThrough]
        public static SortedDictionary<TKey, TValue> ToSortedDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector)
        {
            var d = new SortedDictionary<TKey, TValue>();

            foreach (var item in source)
                d.Add(keySelector(item), valueSelector(item));

            return d;
        }

        [DebuggerStepThrough]
        public static SortedDictionary<TKey, List<TSource>> ToSortedDictionaryList<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            var d = new SortedDictionary<TKey, List<TSource>>();

            foreach (var item in source)
            {
                if (!d.ContainsKey(keySelector(item)))
                    d.Add(keySelector(item), new List<TSource>());
                d[keySelector(item)].Add(item);
            }

            return d;
        }

        [DebuggerStepThrough]
        public static Dictionary<TKey, List<TSource>> ToDictionaryList<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            var d = new Dictionary<TKey, List<TSource>>();

            foreach (var item in source)
            {
                if (!d.ContainsKey(keySelector(item)))
                    d.Add(keySelector(item), new List<TSource>());
                d[keySelector(item)].Add(item);
            }

            return d;
        }

        [DebuggerStepThrough]
        public static Dictionary<TKey, List<TValue>> ToDictionaryList<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector)
        {
            var d = new Dictionary<TKey, List<TValue>>();

            foreach (var item in source)
            {
                if (!d.ContainsKey(keySelector(item)))
                    d.Add(keySelector(item), new List<TValue>());
                d[keySelector(item)].Add(valueSelector(item));
            }

            return d;
        }

        [DebuggerStepThrough]
        public static Dictionary<TKey, HashSet<TValue>> ToDictionaryHashSet<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector)
        {
            var d = new Dictionary<TKey, HashSet<TValue>>();

            foreach (var item in source)
            {
                if (!d.ContainsKey(keySelector(item)))
                    d.Add(keySelector(item), new HashSet<TValue>());
                d[keySelector(item)].Add(valueSelector(item));
            }

            return d;
        }

        [DebuggerStepThrough]
        public static Dictionary<TKey, Dictionary<TKey2, TValue>> ToDictionaryDictionary<TSource, TKey, TKey2, TValue>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TKey2> keySelector2, Func<TSource, TValue> valueSelector)
        {
            var d = new Dictionary<TKey, Dictionary<TKey2, TValue>>();

            foreach (var item in source)
            {
                TKey key = keySelector(item);
                TKey2 key2 = keySelector2(item);

                if (!d.ContainsKey(key))
                    d.Add(key, new Dictionary<TKey2, TValue>());

                d[key].Add(key2, valueSelector(item));
            }

            return d;
        }

        [DebuggerStepThrough]
        public static Dictionary<TKey, Dictionary<TKey2, List<TValue>>> ToDictionaryDictionaryList<TSource, TKey, TKey2, TValue>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TKey2> keySelector2, Func<TSource, TValue> valueSelector)
        {
            var d = new Dictionary<TKey, Dictionary<TKey2, List<TValue>>>();

            foreach (var item in source)
            {
                TKey key = keySelector(item);
                TKey2 key2 = keySelector2(item);

                if (!d.ContainsKey(key))
                    d.Add(key, new Dictionary<TKey2, List<TValue>>());
                if (!d[key].ContainsKey(key2))
                    d[key].Add(key2, new List<TValue>());

                d[key][key2].Add(valueSelector(item));
            }

            return d;

        }

        [DebuggerStepThrough]
        public static Dictionary<TKey, TValue> ToMultipleKeyDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, List<TKey>> keySelector, Func<TSource, TValue> valueSelector)
        {
            var d = new Dictionary<TKey, TValue>();

            foreach (var item in source)
            {
                List<TKey> keys = keySelector(item);

                TValue value = valueSelector(item);

                foreach (var key in keys)
                    d[key] = value;
            }

            return d;
        }

        [DebuggerStepThrough]
        public static Dictionary<TKey, List<TValue>> ToMultipleKeyDictionaryList<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, List<TKey>> keySelector, Func<TSource, TValue> valueSelector)
        {
            var d = new Dictionary<TKey, List<TValue>>();

            foreach (var item in source)
            {
                List<TKey> keys = keySelector(item);

                TValue value = valueSelector(item);

                foreach (var key in keys)
                {
                    if (!d.ContainsKey(key))
                        d.Add(key, new List<TValue>());

                    d[key].Add(value);
                }
            }

            return d;
        }

        [DebuggerStepThrough]
        public static Dictionary<TKey, Dictionary<TKey2, TValue>> ToMultipleKeyDictionaryDictionary<TSource, TKey, TKey2, TValue>(this IEnumerable<TSource> source, Func<TSource, List<TKey>> keySelector, Func<TSource, TKey2> keySelector2, Func<TSource, TValue> valueSelector)
        {
            var d = new Dictionary<TKey, Dictionary<TKey2, TValue>>();

            foreach (var item in source)
            {
                List<TKey> keys = keySelector(item);
                TKey2 key2 = keySelector2(item);

                TValue value = valueSelector(item);

                foreach (var key in keys)
                {
                    if (!d.ContainsKey(key))
                        d.Add(key, new Dictionary<TKey2, TValue>());

                    d[key].Add(key2, value);
                }
            }

            return d;
        }

        [DebuggerStepThrough]
        public static HashSet<TKey> ToHashSet<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            var hs = new HashSet<TKey>();

            foreach (var item in source)
                hs.Add(keySelector(item));

            return hs;
        }

        [DebuggerStepThrough]
        public static HashSet<TKey> ToHashSet<TSource, TKey>(this IEnumerable<TSource> source, params Func<TSource, TKey>[] keySelectors)
        {
            var hs = new HashSet<TKey>();

            foreach (var key in source.SelectMany(item => keySelectors.Select(ks => ks(item)).Where(key => !hs.Contains(key))))
                hs.Add(key);

            return hs;
        }

        [DebuggerStepThrough]
        public static Dictionary<TKey, TValue> Clone<TKey, TValue>(this IDictionary<TKey, TValue> d, Func<TValue, TValue> cloneValue = null)
        {
            var cloned = d.ToDictionary(k => k.Key, v => cloneValue != null ? cloneValue(v.Value) : v.Value);
            return cloned;
        }

        [DebuggerStepThrough]
        public static string RemoveLast(this string s, int length)
        {
            return s.Length >= length ? s.Remove(s.Length - length, length) : string.Empty;
        }

        [DebuggerStepThrough]
        public static string ReplaceFirst(this string s, string oldValue, string newValue)
        {
            var indexOfOldValue = s.IndexOf(oldValue);
            return s.Substring(0, indexOfOldValue) +
                   newValue +
                   s.Substring(indexOfOldValue + oldValue.Length, s.Length - (indexOfOldValue + oldValue.Length));
        }

        [DebuggerStepThrough]
        public static string ReplaceFirst(this string s, string oldValue, string newValue, int indexOfOldValue)
        {
            return s.Substring(0, indexOfOldValue) +
                   newValue +
                   s.Substring(indexOfOldValue + oldValue.Length, s.Length - (indexOfOldValue + oldValue.Length));
        }

        [DebuggerStepThrough]
        public static string[] Split(this string s, string splitter)
        {
            return s.Split(splitter.ToCharArray());
        }

        /// <summary>
        /// Parse a string to any other type including nullable types.
        /// </summary>
        /// <typeparam name="T">target type</typeparam>
        /// <param name="value">the current string instance</param>
        /// <returns>casted value</returns>
        [DebuggerStepThrough]
        public static T Parse<T>(this string value)
        {
            // Get default value for type so if string
            // is empty then we can return default value.
            T result = default(T);
            if (!string.IsNullOrEmpty(value))
            {
                // we are not going to handle exception here
                // if you need SafeParse then you should create
                // another method specially for that.
                TypeConverter tc = TypeDescriptor.GetConverter(typeof(T));
                result = (T)tc.ConvertFrom(value);
            }
            return result;
        }

        [DebuggerStepThrough]
        public static int ToInt(this object o)
        {
            if (o is int)
            {
                return (int)o;
            }
            else
            {
                try
                {
                    return int.Parse(o.ToString());
                }
                catch (System.FormatException ex)
                {
                    throw new Exception(string.Format("object '{0}' could not be converted to an int", o), ex);
                }
            }

        }

        [DebuggerStepThrough]
        public static long ToMinutes(this DateTime dt)
        {
            return (long)new TimeSpan(dt.Ticks).TotalMinutes;
        }

        [DebuggerStepThrough]
        public static string ToShortString(this TimeSpan ts)
        {
            int multiplier = 1;
            if (ts.Ticks < 0)
                multiplier = -1;

            DateTime dt = new DateTime(ts.Ticks * multiplier);
            return (multiplier == -1 ? "- " : string.Empty) + ((dt.Day - 1) + ((dt.Month - 1) * 30)) + " d " + (dt.ToString("HH:mm:ss") == "00:00:00" ? string.Empty : dt.ToString("HH:mm:ss"));
        }

        [DebuggerStepThrough]
        public static byte[] ToByte(this string s, System.Text.Encoding encoding)
        {
            Encoding encoder = null;

            if (encoding == System.Text.Encoding.UTF8)
                encoder = new UTF8Encoding();
            else if (encoding == System.Text.Encoding.ASCII)
                encoder = new ASCIIEncoding();
            else if (encoding == System.Text.Encoding.Default)
                encoder = System.Text.Encoding.Default;
            else
                throw new NotSupportedException();


            if (!string.IsNullOrEmpty(s))
                return encoder.GetBytes(s);
            else
                return new byte[] { };
        }

        [DebuggerStepThrough]
        public static byte[] ToByte(this Stream s)
        {
            byte[] bytes = null;
            using (BinaryReader br = new BinaryReader(s))
            {
                bytes = br.ReadBytes((int)s.Length);
            }
            return bytes;
        }

        [DebuggerStepThrough]
        public static string ToString(this byte[] b, System.Text.Encoding encoding)
        {
            return new StreamReader(
                                    new MemoryStream(b),
                                    encoding
                                ).ReadToEnd();
        }

        [DebuggerStepThrough]
        public static string LamdbaToHumanReadableString(this Expression exp)
        {
            var expString = exp.ToString();

            var parameter = Regex.Match(expString, "[^=]+").Value.Trim();
            var lambdaStripped = Regex.Replace(expString, @"value[(][^)]+[)].", string.Empty);
            return Regex.Replace(lambdaStripped, @"[^(]+[(]", string.Empty).RemoveLast(1).Replace(parameter + ".", string.Empty);
        }

        [DebuggerStepThrough]
        public static string ToISO8601DateTime(this DateTime dt)
        {
            return dt.ToString("yyyy-MM-ddTHH:mm:ss.fff");
        }

        [DebuggerStepThrough]
        public static T Use<T>(this T item, Action<T> work)
        {
            work(item);
            return item;
        }

        [DebuggerStepThrough]
        public static IEnumerable<int> GetAllForeignKeyIds(this DataTable dt, string fkName)
        {
            foreach (DataRow row in dt.Rows)
                if (row[fkName] != DBNull.Value)
                    yield return row[fkName].ToInt();
        }

        [DebuggerStepThrough]
        public static IEnumerable<int> GetAllIds(this DataTable dt)
        {
            foreach (DataRow row in dt.Rows)
                yield return row["Id"].ToInt();
        }

        [DebuggerStepThrough]
        public static void AddDelta<T>(this HashSet<T> hs, IEnumerable<T> values)
        {
            values.ForEach(v =>
            {
                if (!hs.Contains(v))
                    hs.Add(v);
            });
        }

        [DebuggerStepThrough]
        public static string ToMD5Hash(this string s)
        {
            if ((s == null) || (s.Length == 0))
            {
                return string.Empty;
            }

            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] textToHash = Encoding.Default.GetBytes(s);
            byte[] result = md5.ComputeHash(textToHash);

            return System.BitConverter.ToString(result);
        }

        [DebuggerStepThrough]
        public static string ToCSharpTypeName(this Type t, bool includeGenericParameterConstraints = false)
        {
            if (!t.IsGenericType)
            {
                switch (t.Name)
                {
                    case "Boolean":
                        return "bool";
                    case "Byte":
                        return "byte";
                    case "Char":
                        return "char";
                    case "Decimal":
                        return "decimal";
                    case "Double":
                        return "double";
                    case "Int16":
                        return "short";
                    case "Int32":
                        return "int";
                    case "Int64":
                        return "long";
                    case "Single":
                        return "float";
                    case "String":
                        return "string";
                    case "Object":
                        return "object";
                    default:
                        return t.Name;
                }
            }
            else
            {
                var genericType = t.GetGenericArguments().Aggregate(t.ToCSharpTypeNameForGeneric() + "<", (c, n) => c += n.ToCSharpTypeName() + ", ", res => res.RemoveLast(2) + ">");

                if (includeGenericParameterConstraints)
                    genericType += t.GetGenericArguments().Where(arg => arg.IsGenericParameter).Select(arg => new { GenericArgument = arg.Name, Constraint = arg.GetGenericParameterConstraints().Aggregate(string.Empty, (c, n) => c += n.Name + ", ", res => res.RemoveLast(2)) }).Aggregate(string.Empty, (c, n) => c += string.Format(" where {0} : {1}", n.GenericArgument, n.Constraint));

                return genericType;
            }
        }

        [DebuggerStepThrough]
        public static string ToCSharpTypeNameForGeneric(this Type t) { return t.Name.Substring(0, t.Name.IndexOf('`')); }

        [DebuggerStepThrough]
        public static IEnumerable<string> GetNamespaces(this Type t)
        {
            yield return t.Namespace;

            if (t.IsGenericType)
                foreach (var arg in t.GetGenericArguments())
                    foreach (var s in GetNamespaces(arg))
                        yield return s;
        }

        [DebuggerStepThrough]
        public static T As<T>(this object o) { return (T)o; }

        [DebuggerStepThrough]
        public static string AssemblyDescription(this Type t)
        {
            var assemblyOfType = System.Reflection.Assembly.GetAssembly(t);
            var assemblyDescriptionAttributes = assemblyOfType.GetCustomAttributes(typeof(System.Reflection.AssemblyDescriptionAttribute), false);
            if (assemblyDescriptionAttributes.Count() > 0)
            {
                var assemblyDescriptionObject = assemblyDescriptionAttributes.First();
                if (assemblyDescriptionObject is System.Reflection.AssemblyDescriptionAttribute)
                {
                    var assemblyDescription = assemblyDescriptionObject as System.Reflection.AssemblyDescriptionAttribute;
                    return assemblyDescription.Description;
                }
            }

            return string.Empty;
        }

        [DebuggerStepThrough]
        public static string AssemblyVersion(this Type t)
        {
            var assemblyOfType = System.Reflection.Assembly.GetAssembly(t);
            var assemblyDescriptionAttributes = assemblyOfType.GetCustomAttributes(typeof(System.Reflection.AssemblyVersionAttribute), false);
            if (assemblyDescriptionAttributes.Count() > 0)
            {
                var assemblyVersionObject = assemblyDescriptionAttributes.First();
                if (assemblyVersionObject is System.Reflection.AssemblyVersionAttribute)
                {
                    var assemblyVersion = assemblyVersionObject as System.Reflection.AssemblyVersionAttribute;
                    return assemblyVersion.Version;
                }
            }

            return string.Empty;
        }

        [DebuggerStepThrough]
        public static Stream LoadResourceAsStream(this Assembly a, string resourcePathInAssembly)
        {
            var documentStream = a.GetManifestResourceStream(resourcePathInAssembly);
            if (documentStream != null)
            {
                return documentStream;
            }
            else
            {
                throw new Exception(string.Format("Resource '{0}' not found in assembly '{1}'", resourcePathInAssembly, a.FullName));
            }
        }

        [DebuggerStepThrough]
        public static byte[] ToByteArray(this Stream s)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                s.CopyTo(ms);
                return ms.ToArray();
            }
        }

        [DebuggerStepThrough]
        public static string LoadResourceAsString(this Assembly a, string resourcePathInAssembly)
        {
            var documentStream = a.LoadResourceAsStream(resourcePathInAssembly);
            var documentString = new StreamReader(documentStream).ReadToEnd();
            return documentString;
        }

        [DebuggerStepThrough]
        public static string Shorten(this string s, int maxLength, string continuation = "...")
        {
            if (s.Length <= maxLength)
            {
                return s;
            }
            else
            {
                var shortenedS = s.Substring(0, maxLength) + continuation;
                return shortenedS;
            }
        }

        [DebuggerStepThrough]
        public static string ToReadableString(this object o)
        {
            var type = o.GetType();
            var properties = type.GetProperties();
            var propertyValues = properties.ToDictionary(k => k.Name, v => v.GetValue(o, null));

            return string.Format("{0}[{1}]", type.Name, propertyValues.ToSeparatedString(k => string.Format("{0}:{1}", k.Key, k.Value is IList || k.Value is ICollection ? (k.Value as IEnumerable).Cast<object>().ToSeparatedString(enumerableElement => enumerableElement != null ? enumerableElement.ToString() : "null") : k.Value)));
        }
    }
}

