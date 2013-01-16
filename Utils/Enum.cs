using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public static class Enum<TEnum>
    {
        public static TEnum Parse(string value)
        {
            return (TEnum)Enum.Parse(typeof(TEnum), value);
        }

        public static IEnumerable<TEnum> GetValues()
        {
            foreach (object value in Enum.GetValues(typeof(TEnum)))
                yield return ((TEnum)value);
        }

        public static TEnum MaxValue()
        {
            return Enum<TEnum>.GetValues().Max();
        }

        public static TEnum GetAll(params TEnum[] not)
        {
            ulong flags = 0;
            foreach (var f in GetValues().Where(v => !not.Contains(v)))
                flags |= Convert.ToUInt64(f);

            return (TEnum)Enum.ToObject(typeof(TEnum), flags);
        }
    }

    public class StringToEnumConverter : Newtonsoft.Json.Converters.StringEnumConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value.GetType().IsEnum)
            {
                writer.WriteValue(Enum.GetName(value.GetType(), value));
                return;
            }

            base.WriteJson(writer, value, serializer);
        }
    }
    
    public static class EnumExtensions
    {
        //public static TEnum GetAll<TEnum>(this TEnum notThese)
        //{
        //    Type enumType = typeof(TEnum);
        //    if (!enumType.IsEnum)
        //        throw new ArgumentException("Cannot call GetNot on non-Enum types");

        //    ulong not = Convert.ToUInt64(notThese);
        //    ulong flags = 0;
        //    foreach (var f in Enum<TEnum>.GetValues().Where(n => (Convert.ToUInt64(n) & not) == 0))
        //        flags |= Convert.ToUInt64(f);

        //    return (TEnum)Enum.ToObject(typeof(TEnum), flags);
        //}

        /// <summary>
        /// Gets the Flags that are set on a Generic Enum that is being used as Bit Flags
        /// </summary>
        /// <typeparam name="TEnum">Type of the Enum</typeparam>
        /// <param name="input">Enum of Type TEnum value</param>
        /// <param name="includeZero">Include 0 as a Flag that can be set</param>
        /// <param name="checkIsFlags">Check that TEnum has the FlagsAttribute to indicate to the compiler that it is being used as Bit Flags</param>
        /// <param name="checkCombinators">When false, will not check the input value against TEnum values that are combinations of Bit Flags, e.g. will ignore
        /// a TEnum value of 3 and only check input for 1, 2, and 4.  Default is true.</param>
        /// <returns></returns>
        public static IEnumerable<TEnum> GetFlags<TEnum>(this TEnum input, bool includeZero = false, bool checkIsFlags = true, bool checkCombinators = true)
        {
            Type enumType = typeof(TEnum);
            if (!enumType.IsEnum)
                yield break;


            ulong setBits = Convert.ToUInt64(input);
            // if no flags are set, return empty
            if (!includeZero && (0 == setBits))
                yield break;

            // if it's not a flag enum, return empty
            if (checkIsFlags && !input.GetType().IsDefined(typeof(FlagsAttribute), false))
                yield break;

            if (checkCombinators)
            {
                if (!includeZero)
                {
                    // check each enum value mask if it is in input bits
                    foreach (TEnum value in Enum<TEnum>.GetValues().Where(e => Convert.ToUInt64(e) > 0))
                    {
                        ulong valMask = Convert.ToUInt64(value);

                        if ((setBits & valMask) == valMask)
                            yield return value;
                    }
                }
                else
                {
                    // check each enum value mask if it is in input bits
                    foreach (TEnum value in Enum<TEnum>.GetValues())
                    {
                        ulong valMask = Convert.ToUInt64(value);

                        if ((setBits & valMask) == valMask)
                            yield return value;
                    }
                }
            }
            else
            {
                if (!includeZero)
                {
                    // check each enum value mask if it is in input bits
                    foreach (TEnum value in Enum<TEnum>.GetValues().Where(e => Convert.ToUInt64(e) > 0))
                    {
                        ulong valMask = Convert.ToUInt64(value);

                        if ((setBits & valMask) == valMask)
                            yield return value;
                    }
                }
                else
                {
                    // check each enum value mask if it is in input bits
                    foreach (TEnum value in Enum<TEnum>.GetValues())
                    {
                        ulong valMask = Convert.ToUInt64(value);

                        if ((setBits & valMask) == valMask)
                            yield return value;
                    }
                }
            }

        }

        public static IEnumerable<Enum> GetFlags(this Enum input, bool includeZero = false, bool checkIsFlags = true, bool checkCombinators = true)
        {
            ulong setBits = Convert.ToUInt64(input);
            // if no flags are set, return empty
            if (!includeZero && (0 == setBits))
                yield break;

            // if it's not a flag enum, return empty
            if (checkIsFlags && !input.GetType().IsDefined(typeof(FlagsAttribute), false))
                yield break;

            if (checkCombinators)
            {
                if (!includeZero)
                {
                    // check each enum value mask if it is in input bits
                    foreach (Enum value in Enum.GetValues(input.GetType()))
                    {
                        ulong valMask = Convert.ToUInt64(value);
                        if (valMask == 0)
                            continue;

                        if ((setBits & valMask) == valMask)
                            yield return value;
                    }
                }
                else
                {
                    // check each enum value mask if it is in input bits
                    foreach (Enum value in Enum.GetValues(input.GetType()))
                    {
                        ulong valMask = Convert.ToUInt64(value);

                        if ((setBits & valMask) == valMask)
                            yield return value;
                    }
                }
            }
            else
            {
                if (!includeZero)
                {
                    // check each enum value mask if it is in input bits
                    foreach (Enum value in Enum.GetValues(input.GetType()))
                    {
                        ulong valMask = Convert.ToUInt64(value);
                        if (valMask == 0)
                            continue;

                        if ((setBits & valMask) == valMask)
                            yield return value;
                    }
                }
                else
                {
                    // check each enum value mask if it is in input bits
                    foreach (Enum value in Enum.GetValues(input.GetType()))
                    {
                        ulong valMask = Convert.ToUInt64(value);

                        if ((setBits & valMask) == valMask)
                            yield return value;
                    }
                }
            }
        }

        //public static int MaxValue(this Enum input)
        //{

        //}


    }
}
