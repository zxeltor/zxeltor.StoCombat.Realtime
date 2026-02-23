// Copyright (c) 2024, Todd Taylor (https://github.com/zxeltor)
// All rights reserved.
// 
// This source code is licensed under the Apache-2.0-style license found in the
// LICENSE file in the root directory of this source tree.

using System.Text.Json;
using System.Text.Json.Serialization;

using zxeltor.Types.Lib.Extensions;

namespace zxeltor.StoCombat.Realtime.Helpers;

/// <summary>
///     A collection of helpers to serialize or deserialize object to or from JSON.
///     <para>The helpers in this class require <see cref="Newtonsoft" /></para>
/// </summary>
public static class ConfigSerializationHelper
{
    #region Public Members

    /// <summary>
    ///     Creates a new instance of JsonSerializerOptions configured for camel case property naming and case-insensitive
    ///     property name matching.
    /// </summary>
    /// <remarks>The returned options also ignore properties with null values during serialization by setting
    /// DefaultIgnoreCondition to WhenWritingNull.</remarks>
    /// <param name="writeIndented">true to format the JSON output for readability; otherwise, false to minimize whitespace.</param>
    /// <returns>A JsonSerializerOptions instance with camel case property naming, case-insensitive property name matching, and
    /// the specified indentation setting.</returns>
    private static JsonSerializerOptions GetOptions(bool writeIndented)
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            Converters =
            {
                new SolidColorBrushConverter()
            },
            WriteIndented = writeIndented
        };
    }

    /// <summary>
    ///     Deserialize a JSON string as a chosen type.
    /// </summary>
    /// <typeparam name="T">The chosen type</typeparam>
    /// <param name="data">The JSON string to deserialize.</param>
    /// <param name="removeSpecialCharacters">True to remove special characters. False otherwise.</param>
    /// <returns>An object of the chosen type.</returns>
    public static T? Deserialize<T>(string data, bool removeSpecialCharacters = true)
    {
        var source = removeSpecialCharacters ? data.RemoveSpecialChars() : data;
        return JsonSerializer.Deserialize<T>(source, GetOptions(false));
    }

    /// <summary>
    ///     Serialize a chosen type to a JSON string.
    /// </summary>
    /// <param name="data">The data object to serialize.</param>
    /// <param name="useJsonIndentation">
    ///     If True, the JSON will be formatted with indents. False and the JSON will be a simple
    ///     string.
    /// </param>
    /// <returns>A JSON string representation of the data object.</returns>
    public static string Serialize(object? data, bool useJsonIndentation = false)
    {
        return JsonSerializer.Serialize(data, GetOptions(useJsonIndentation));
    }

    /// <summary>
    ///     Try to deserialize a JSON string as a chosen type.
    /// </summary>
    /// <typeparam name="T">The chosen type</typeparam>
    /// <param name="data">The JSON string to deserialize.</param>
    /// <param name="output">An object of the chosen type.</param>
    /// <param name="removeSpecialCharacters">True to remove special characters. False otherwise.</param>
    /// <returns>True if successful. False otherwise.</returns>
    public static bool TryDeserializeString<T>(string? data, out T? output, bool removeSpecialCharacters = true)
        where T : class
    {
        output = null;

        if (data == null)
        {
            return false;
        }

        try
        {
            var source = removeSpecialCharacters ? data.RemoveSpecialChars() : data;
            output = JsonSerializer.Deserialize<T>(source, GetOptions(false));
            return true;
        }
        catch
        {
            output = default;
        }

        return false;
    }

    /// <summary>
    ///     Try to serialize a chosen type to a JSON string.
    /// </summary>
    /// <param name="data">The data object to serialize.</param>
    /// <param name="output">A JSON string representation of the data object</param>
    /// <param name="useJsonIndentation">
    ///     If True, the JSON will be formatted with indents. False and the JSON will be a simple
    ///     string.
    /// </param>
    /// <returns>True if successful. False otherwise.</returns>
    public static bool TrySerialize(object data, out string output, bool useJsonIndentation = false)
    {
        try
        {
            output = JsonSerializer.Serialize(data, GetOptions(useJsonIndentation));
            return true;
        }
        catch
        {
            output = default!;
        }

        return false;
    }

    #endregion
}