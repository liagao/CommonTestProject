namespace EnumBenchmarkDemo
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    public static class EnumCaster<T>
        where T : struct, Enum
    {
        /// <summary>
        /// Gets a converter from enum type T to int
        /// </summary>
        public static readonly Func<T, int> ToInt = EnumCaster<T>.CreateCaster<T, int>();

        /// <summary>
        /// Gets a converter from enum type T to uint
        /// </summary>
        public static readonly Func<T, uint> ToUint = EnumCaster<T>.CreateCaster<T, uint>();

        /// <summary>
        /// Gets a converter from enum type T to long
        /// </summary>
        public static readonly Func<T, long> ToLong = EnumCaster<T>.CreateCaster<T, long>();

        /// <summary>
        /// Gets a converter from int to enum type T
        /// </summary>
        public static readonly Func<int, T> FromInt = EnumCaster<T>.CreateCaster<int, T>();

        /// <summary>
        /// Gets a converter from uint to enum type T
        /// </summary>
        public static readonly Func<uint, T> FromUint = EnumCaster<T>.CreateCaster<uint, T>();

        /// <summary>
        /// Gets a converter from long to enum type T
        /// </summary>
        public static readonly Func<long, T> FromLong = EnumCaster<T>.CreateCaster<long, T>();

        /// <summary>
        /// Obtain a function that converts from the type TFrom to the type TTo with casts and without boxing if possible
        /// </summary>
        /// <typeparam name="TFrom">the type to cast from</typeparam>
        /// <typeparam name="TTo">the type to cast to</typeparam>
        /// <returns>a delegate that casts TFrom to TTo</returns>
        private static Func<TFrom, TTo> CreateCaster<TFrom, TTo>()
        {
            ParameterExpression parameter = Expression.Parameter(typeof(TFrom));
            UnaryExpression convert = Expression.Convert(parameter, typeof(TTo));
            return Expression.Lambda<Func<TFrom, TTo>>(convert, parameter).Compile();
        }
    }

    public interface IEnumHelper
    {
        /// <summary>
        /// Get an enum string value from its index.
        /// </summary>
        /// <param name="index">index value to search</param>
        /// <returns>String value mapping to the index.</returns>
        string ToString(long index);
    }

    /// <summary>
    /// Non-generic version of EnumHelper.TryParse, used when Enum type is not known at compile type
    /// </summary>
    public static class EnumHelper
    {
        /// <summary>
        /// Parse method
        /// </summary>
        private static readonly MethodInfo genericParseMethod = typeof(EnumHelper).GetMethod(nameof(ParseTyped), BindingFlags.Static | BindingFlags.NonPublic);

        /// <summary>
        /// Try to parse a value to enum type
        /// </summary>
        /// <param name="enumType">Enum type</param>
        /// <param name="strValue">Input string value</param>
        /// <param name="ignoreCase">Should case be ignored?</param>
        /// <param name="enumValue">Output boxed enum value</param>
        /// <returns>Whether parsing was successful</returns>
        public static bool TryParse(Type enumType, string strValue, bool ignoreCase, out object enumValue)
        {
            if (string.IsNullOrEmpty(strValue) || enumType == null || !enumType.IsValueType || !enumType.IsEnum)
            {
                enumValue = null;
                return false;
            }

            MethodInfo parseMethod = EnumHelper.genericParseMethod.MakeGenericMethod(enumType);
            enumValue = parseMethod.Invoke(null, new object[] { strValue, ignoreCase });
            return enumValue != null;
        }

        /// <summary>
        /// Generic helper method that invokes <see cref="EnumHelper{T}"/>
        /// </summary>
        /// <typeparam name="T">Enum type</typeparam>
        /// <param name="strValue">input string value</param>
        /// <param name="ignoreCase">Should case be ignored?</param>
        /// <returns>Boxed enum value or null</returns>
        private static object ParseTyped<T>(string strValue, bool ignoreCase)
            where T : struct, Enum
            => EnumHelper<T>.Instance.TryParse(strValue, ignoreCase, out T result) ? (object)result : null;
    }

    /// <summary>
    /// Enum helper class. This class is intended to be instantiated once on start-up with the requisite Enum to be parsed.
    /// This implementation prevents boxing of the enum values by calling enum.ToString etc.
    /// This works with contiguous and non-contiguous enums.
    /// 
    /// Note: 
    /// This class supports negative values in the same way as flags values.
    /// This class is not a replacement for Enum.TryParse since we do not support parsing of underlying values back to its Enum value.
    /// 
    /// Consider an Enum like below where the value 2 is unfilled.
    /// 0 -> A
    /// 1 -> B
    /// 3 -> C
    /// 
    /// We will create an Array that looks like below.
    /// This allows similar usage as Enum.TryParse which allows lookups by name and by the underlying value.
    /// A, B, null, C
    /// In addition we will store the Enum values mapping to each of those values with the 1:1 mapping.
    /// A, B, Default, C
    /// 
    /// When a user looks up the value, by sending a string, we will loop through the names and when there is a match, 
    /// we will pull out the value from the enumValues that matches index.
    /// 
    /// Even though the example above contains a missing value, this will still work with contiguous enums.
    /// </summary>
    /// <typeparam name="T">Enum type being helped</typeparam>
    public class EnumHelper<T> : IEnumHelper
        where T : struct, Enum
    {
        /// <summary>
        /// Helper instance.
        /// </summary>
        public static readonly EnumHelper<T> Instance = new EnumHelper<T>();

        /// <summary>
        /// Enum names. This contains all the string names of the enum.
        /// </summary>
        private readonly string[] enumNames;

        /// <summary>
        /// Enum values for string names.
        /// </summary>
        private readonly T[] enumValues;

        /// <summary>
        /// Dictionary to support flag Enum
        /// </summary>
        private readonly ConcurrentDictionary<long, string> flagEnumNames;

        /// <summary>
        /// Dictionary to support flag Enum name lookup
        /// </summary>
        private readonly ConcurrentDictionary<string, T> flagEnumValues;

        /// <summary>
        /// Prevents a default instance of the EnumHelper class from being created.
        /// </summary>
        private EnumHelper()
        {
            Type enumType = typeof(T);
            if (!enumType.IsEnum)
            {
                // defense-in-depth: this check should be unnecessary and enforced by the compiler...
                throw new ArgumentException($"Expecting typeof Enum but recieved type {enumType.FullName}");
            }

            if (enumType.IsDefined(typeof(FlagsAttribute), false))
            {
                this.flagEnumNames = new ConcurrentDictionary<long, string>();
                this.flagEnumValues = new ConcurrentDictionary<string, T>();

                return;
            }

            T[] values = (T[])Enum.GetValues(typeof(T));

            // This is the max value within the array. The GetValues output is always sorted so, pick the last value.
            long maxUnderlyingValue = EnumCaster<T>.ToLong(values[values.Length - 1]);

            if (maxUnderlyingValue < 0 // Enum has negative element: elements sorted as ulong - negatives larger than positives
                || values.Length * 100 < maxUnderlyingValue) // 99% of enumNames & enumValues elements will be null
            {
                this.flagEnumNames = new ConcurrentDictionary<long, string>();
                this.flagEnumValues = new ConcurrentDictionary<string, T>();

                return;
            }

            // Since values will be from 0 to n. Add 1 to create the total number of values.
            long totalStringCount = maxUnderlyingValue + 1;

            // Create the arrays to store the strings.
            this.enumNames = new string[totalStringCount];
            this.enumValues = new T[totalStringCount];

            // Populate the string names and the enum array corresponding to the same index.
            foreach (T value in values)
            {
                long enumVal = EnumCaster<T>.ToLong(value);
                this.enumNames[enumVal] = value.ToString();
                this.enumValues[enumVal] = value;
            }
        }

        /// <summary>
        /// Gets name count, for non flag Enum types
        /// </summary>
        public int Count => this.enumNames?.Length ?? 0;

        /// <summary>
        /// Get an enum string value from its index.
        /// </summary>
        /// <remarks>prefer type safe wrapper: ConvertEnum.ToString(enumVariable)</remarks>
        /// <param name="index">index value to search</param>
        /// <returns>String value mapping to the index.</returns>
        public static string ToString(long index)
        {
            return EnumHelper<T>.Instance.GetStringValue(index, throwOnError: false);
        }

        /// <summary>
        /// Try get an enum value from its string name.
        /// </summary>
        /// <param name="value">String value to search</param>
        /// <param name="result">The enum value.</param>
        /// <returns>True if a match is found. False if not.</returns>
        public static bool TryParse(string value, out T result)
        {
            return EnumHelper<T>.Instance.TryGetValue(value, out result);
        }

        /// <summary>
        /// Get an enum value from its string name, or the default value of the enum
        /// </summary>
        /// <param name="value">String value to search</param>
        /// <returns>Value of the enum, or the default if it is not a valid name.</returns>
        public static T ParseOrDefault(string value)
        {
            return EnumHelper<T>.ParseOrDefault(value, ignoreCase: false);
        }

        /// <summary>
        /// Get an enum value from its string name, or the default value of the enum
        /// </summary>
        /// <param name="value">String value to search</param>
        /// <param name="ignoreCase">True if case insensitive.</param>
        /// <returns>Value of the enum, or the default if it is not a valid name.</returns>
        public static T ParseOrDefault(string value, bool ignoreCase)
        {
            if (EnumHelper<T>.Instance.TryParse(value, ignoreCase, out T result))
            {
                return result;
            }

            return default(T);
        }

        /// <summary>
        /// Check whether the enum value has the provided flag
        /// </summary>
        /// <remarks>
        /// If "flag" is the 0/default/None value, the result will always be true
        /// </remarks>
        /// <param name="value">value to check</param>
        /// <param name="flag">flag to check for</param>
        /// <returns>true if the value has all of the flags within flag set.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasFlag(T value, T flag)
        {
            long valueLong = EnumCaster<T>.ToLong(value);
            long flagLong = EnumCaster<T>.ToLong(flag);
            return (valueLong & flagLong) == flagLong;
        }

        /// <summary>
        /// Check whether the enum value has any of the provided flags set
        /// </summary>
        /// <remarks>
        /// If "flag" is the 0/default/None value, the result will always be false
        /// </remarks>
        /// <param name="value">value to check</param>
        /// <param name="flag">flag to check for</param>
        /// <returns>true if the value has any of the flags within flag set.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAnyFlag(T value, T flag)
        {
            long valueLong = EnumCaster<T>.ToLong(value);
            long flagLong = EnumCaster<T>.ToLong(flag);
            return (valueLong & flagLong) != 0L;
        }

        /// <summary>
        /// Checks whether two enum values are equal to each other
        /// </summary>
        /// <remarks>
        /// Helps avoid the boxing for an Equals comparison between first and second when we don't have a concrete type
        /// </remarks>
        /// <param name="first">The first enum value</param>
        /// <param name="second">The second enum value</param>
        /// <returns>True if first and second are equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(T first, T second)
        {
            long firstLong = EnumCaster<T>.ToLong(first);
            long secondLong = EnumCaster<T>.ToLong(second);
            return firstLong == secondLong;
        }

        /// <summary>
        /// Try get an enum value from its string name.
        /// </summary>
        /// <param name="stringName">String value to search</param>
        /// <param name="enumValue">The enum value</param>
        /// <returns>True if a match is found. False if not.</returns>
        public bool TryGetValue(string stringName, out T enumValue)
        {
            return this.TryParse(stringName, false, out enumValue);
        }

        /// <summary>
        /// Try get an enum value from its string name.
        /// </summary>
        /// <param name="stringName">String value to search</param>
        /// <param name="ignoreCase">True if case insensitive.</param>
        /// <param name="enumValue">The enum value</param>
        /// <returns>True if a match is found. False if not.</returns>
        public bool TryParse(string stringName, bool ignoreCase, out T enumValue)
        {
            enumValue = default(T);

            if (string.IsNullOrWhiteSpace(stringName))
            {
                return false;
            }

            // .NET's TryParse uses a trimmed string match
            stringName = stringName.Trim();

            if (this.flagEnumValues != null)
            {
                if (this.flagEnumValues.TryGetValue(stringName, out enumValue))
                {
                    return true;
                }
                else
                {
                    if (Enum.TryParse<T>(stringName, ignoreCase, out enumValue))
                    {
                        this.flagEnumValues[stringName] = enumValue;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            if (ignoreCase)
            {
                // Loop through the enum names.
                string[] names = this.enumNames;

                if (names != null)
                {
                    int count = names.Length;

                    for (int i = 0; i < count; i++)
                    {
                        if (stringName.Equals(names[i], StringComparison.OrdinalIgnoreCase))
                        {
                            enumValue = this.enumValues[i];
                            return true;
                        }
                    }
                }
            }
            else
            {
                // Loop through the enum names.
                string[] names = this.enumNames;
                int count = names.Length;
                for (int i = 0; i < count; i++)
                {
                    if (stringName.Equals(names[i]))
                    {
                        enumValue = this.enumValues[i];
                        return true;
                    }
                }
            }

            // check if the string is an integer/long instead of the string value
            // note that only non-flags types make it to here...
            char firstChar = stringName[0];
            if (char.IsDigit(firstChar) || firstChar == '-')
            {
                if (long.TryParse(stringName, out long result) && this.TryGetValue(result, out enumValue))
                {
                    return true;
                }
            }

            // Slow default implementation in mscorlib.
            return Enum.TryParse<T>(stringName, ignoreCase, out enumValue);
        }

        /// <summary>
        /// Try Get an enum string value from its index.
        /// </summary>
        /// <param name="index">int value to search</param>
        /// <param name="enumValue">The enum value</param>
        /// <returns>True if a match is found. False if not.</returns>
        public bool TryGetValue(long index, out T enumValue)
        {
            // this index has to be less than the # of names in the names array.
            if ((ulong)index < (ulong)this.enumValues.Length)
            {
                enumValue = this.enumValues[index];
                return true;
            }

            enumValue = default(T);
            return false;
        }

        /// <summary>
        /// Try Get an enum string value from its index.
        /// </summary>
        /// <param name="index">index value to search</param>
        /// <param name="stringName">enum string value</param>
        /// <returns>True if a match is found. False if not.</returns>
        public bool TryGetStringValue(long index, out string stringName)
        {
            // this index has to be less than the # of names in the names array.
            if ((ulong)index < (ulong)this.enumNames.Length)
            {
                stringName = this.enumNames[index];
                if (stringName != null)
                {
                    return true;
                }
            }

            stringName = index.ToString();
            return false;
        }

        /// <summary>
        /// Get an enum string value from its index.
        /// This method will throw if the index is out of range or if the index being referenced has no underlying value.
        /// </summary>
        /// <param name="index">int value to search</param>
        /// <param name="throwOnError">Throw exception on error</param>
        /// <returns>String value mapping to the index.</returns>
        public string GetStringValue(long index, bool throwOnError = true)
        {
            string stringVal;

            if (this.flagEnumNames != null)
            {
                if (!this.flagEnumNames.TryGetValue(index, out stringVal))
                {
                    stringVal = Enum.ToObject(typeof(T), index).ToString();

                    this.flagEnumNames[index] = stringVal;
                }
            }
            else
            {
                if (!this.TryGetStringValue(index, out stringVal))
                {
                    if (throwOnError)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }
                }
            }

            return stringVal;
        }

        /// <summary>
        /// Get an enum string value from its index.
        /// </summary>
        /// <param name="index">index value to search</param>
        /// <returns>String value mapping to the index.</returns>
        string IEnumHelper.ToString(long index)
        {
            return this.GetStringValue(index, throwOnError: false);
        }
    }
}