using System.Globalization;

namespace DirectRoute.TypeConverters;

/// <summary>
/// This is located in a subdirectory in order to allow users to opt-in to this type name being available.
/// </summary>
public static class TypeConverter
{
    public static T? Convert<T>(object value)
    {
        return (T?)Convert(value, typeof(T));
    }

    public static object? Convert(object? value, Type type)
    {
        if (value == null)
            return null;

        if (type.IsArray)
        {
            if (value.GetType().IsArray)
            {
                Array array = (Array)value;
                Array typedArray = Array.CreateInstance(type.GetElementType()!, array.Length);
                for (var i = 0; i < array.Length; i++)
                {
                    var item = array.GetValue(i);
                    var typedItem = System.Convert.ChangeType(item, type.GetElementType()!);
                    typedArray.SetValue(typedItem, i);
                }
                return typedArray;
            }
            else
            {
                Array typedArray = Array.CreateInstance(type.GetElementType()!, 1);
                var typedValue = ConvertValue(value, type.GetElementType()!);
                typedArray.SetValue(typedValue, 0);
                return typedValue;
            }
        }
        else
        {
            if (value.GetType().IsArray && ((Array)value).Length == 1)
                value = ((Array)value).GetValue(0);

            var typedValue = ConvertValue(value, type);
            return typedValue;
        }
    }

    private static object? ConvertValue(object? value, Type type)
    {
        if (value == null)
            return null;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            type = type.GetGenericArguments()[0];
        }

        if (value is string @string && type.IsEnum)
        {
            return Enum.Parse(type, @string);
        }
        else if (value is string dateString && type == typeof(DateTime))
        {
            return DateTime.ParseExact(dateString, "o", CultureInfo.InvariantCulture);
        }
        else if (value is DateTime dateTime)
        {
            return dateTime.ToString("o");
        }
        else
        {
            return System.Convert.ChangeType(value, type);
        }
    }
}