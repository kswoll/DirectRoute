using DirectRoute.TypeConverters;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace DirectRoute;

internal static class PrivateExtensions
{
    public static string[] SplitAndIncludeDelimiters(this string s, char[] separators)
    {
        var regexSeparators = string.Concat(separators.Select(x => $"\\{x}"));
        var pattern = $"([{regexSeparators}])";
        var results = Regex.Split(s, pattern).ToArray();
        return results;
    }

    public static IEnumerable<(int, T)> Enumerate<T>(this IEnumerable<T> source, int start = 0)
    {
        int i = start;
        foreach (var item in source)
        {
            yield return (i++, item);
        }
    }

    public static string Query(this string s, object? queryString)
    {
        if (queryString == null)
            return s;

        var builder = new StringBuilder(s);

        var delimiter = "?";
        var dictionary = queryString.ToDictionary();
        foreach (var (key, value) in dictionary)
        {
            void Append(string key, object? value)
            {
                if (value != null)
                {
                    if (value is IConvertible)
                    {
                        var stringValue = (string?)TypeConverter.Convert(value, typeof(string));
                        builder.Append($"{delimiter}{key}={UrlEncoder.Default.Encode(stringValue ?? "")}");
                    }
                    else
                    {
                        AppendObject(value);
                    }
                    delimiter = "&";
                }
            }

            void AppendObject(object @object)
            {
                // For now only support shallow objects.  TODO: potentially support recursion
                foreach (var property in @object.GetType().GetProperties())
                {
                    var value = property.GetValue(@object, null);
                    var subKey = $"{key}.{property.Name}";
                    Append(subKey, value);
                }
            }

            if (value?.GetType()?.IsArray ?? false)
            {
                foreach (var element in (Array)value)
                {
                    if (element != null)
                        Append(key, element);
                }
            }
            else if (value != null)
            {
                Append(key, value);
            }
        }

        return builder.ToString();
    }

    public static Dictionary<string, object?> ToDictionary(this object o)
    {
        if (o is Dictionary<string, object?> dictionary)
            return dictionary;

        var result = new Dictionary<string, object?>();
        foreach (var property in o.GetType().GetProperties())
        {
            var value = property.GetValue(o);
            result[property.Name] = value;
        }
        return result;
    }

    public static string GetFullFriendlyName(this Type type)
    {
        return $"{type.Namespace}.{type.GetFriendlyName()}";
    }

    public static string GetFriendlyName(this Type type)
    {
        if (type.IsGenericType)
            return $"{type.Name.Split('`')[0]}<{string.Join(", ", type.GetGenericArguments().Select(x => x.GetFriendlyName()))}>";
        else
            return type.Name;
    }

    public static string Capitalize(this string s)
    {
        return string.Concat(s[0].ToString().ToUpper(), s[1..]);
    }

    public static string Decapitalize(this string s)
    {
        return string.Concat(s[0].ToString().ToLower(), s[1..]);
    }
}