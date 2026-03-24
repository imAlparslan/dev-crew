using System.Text;
using System.Text.Json;

namespace DevCrew.Core.Services;

public class JsonDiffService : IJsonDiffService
{
    private const int MaxLcsCells = 2_000_000;

    public JsonDiffResult Compare(string leftJson, string rightJson, JsonDiffOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(leftJson) || string.IsNullOrWhiteSpace(rightJson))
        {
            return new JsonDiffResult
            {
                IsValid = false,
                ErrorMessage = "Her iki JSON girişi de zorunludur",
                ErrorKey = ErrorKeys.JsonDiff.InputsRequired
            };
        }

        options ??= new JsonDiffOptions();

        JsonDocument? leftDocument = null;
        JsonDocument? rightDocument = null;

        try
        {
            leftDocument = JsonDocument.Parse(leftJson);
        }
        catch (JsonException ex)
        {
            return new JsonDiffResult
            {
                IsValid = false,
                ErrorMessage = $"Sol JSON geçersiz: {ex.Message}",
                ErrorKey = ErrorKeys.JsonDiff.LeftInvalid,
                ErrorArgs = [ex.Message]
            };
        }

        try
        {
            rightDocument = JsonDocument.Parse(rightJson);
        }
        catch (JsonException ex)
        {
            leftDocument.Dispose();
            return new JsonDiffResult
            {
                IsValid = false,
                ErrorMessage = $"Sağ JSON geçersiz: {ex.Message}",
                ErrorKey = ErrorKeys.JsonDiff.RightInvalid,
                ErrorArgs = [ex.Message]
            };
        }

        using (leftDocument)
        using (rightDocument)
        {
            var pathDiffs = new List<JsonPathDiffEntry>();
            var summary = new JsonDiffSummary();

            CompareElements("$", leftDocument.RootElement, rightDocument.RootElement, options, pathDiffs, ref summary);

            var leftDisplayJson = SerializeForDisplay(leftDocument.RootElement, options);
            var rightDisplayJson = SerializeForDisplay(rightDocument.RootElement, options);

            var leftLines = SplitLines(options.IgnoreWhitespaceDifferences ? leftDisplayJson : NormalizeLineEndings(leftJson));
            var rightLines = SplitLines(options.IgnoreWhitespaceDifferences ? rightDisplayJson : NormalizeLineEndings(rightJson));
            var lineDiffs = BuildLineDiffs(leftLines, rightLines);

            return new JsonDiffResult
            {
                IsValid = true,
                Summary = summary,
                PathDiffs = pathDiffs,
                LineDiffs = lineDiffs,
                LeftDisplayJson = leftDisplayJson,
                RightDisplayJson = rightDisplayJson
            };
        }
    }

    private static void CompareElements(
        string path,
        JsonElement left,
        JsonElement right,
        JsonDiffOptions options,
        List<JsonPathDiffEntry> diffs,
        ref JsonDiffSummary summary)
    {
        if (TryTreatAsEqualNullAndEmptyString(left, right, options))
        {
            summary = summary with { UnchangedCount = summary.UnchangedCount + 1 };
            return;
        }

        if (left.ValueKind != right.ValueKind)
        {
            diffs.Add(new JsonPathDiffEntry
            {
                Path = path,
                Kind = JsonDiffKind.Changed,
                LeftValue = SerializeCompact(left),
                RightValue = SerializeCompact(right)
            });
            summary = summary with { ChangedCount = summary.ChangedCount + 1 };
            return;
        }

        switch (left.ValueKind)
        {
            case JsonValueKind.Object:
                CompareObjects(path, left, right, options, diffs, ref summary);
                break;

            case JsonValueKind.Array:
                CompareArrays(path, left, right, options, diffs, ref summary);
                break;

            default:
                if (ValuesAreEqual(left, right))
                {
                    summary = summary with { UnchangedCount = summary.UnchangedCount + 1 };
                }
                else
                {
                    diffs.Add(new JsonPathDiffEntry
                    {
                        Path = path,
                        Kind = JsonDiffKind.Changed,
                        LeftValue = SerializeCompact(left),
                        RightValue = SerializeCompact(right)
                    });
                    summary = summary with { ChangedCount = summary.ChangedCount + 1 };
                }
                break;
        }
    }

    private static void CompareObjects(
        string path,
        JsonElement left,
        JsonElement right,
        JsonDiffOptions options,
        List<JsonPathDiffEntry> diffs,
        ref JsonDiffSummary summary)
    {
        if (options.IgnoreObjectPropertyOrder)
        {
            var leftProps = left.EnumerateObject().ToDictionary(x => x.Name, x => x.Value, StringComparer.Ordinal);
            var rightProps = right.EnumerateObject().ToDictionary(x => x.Name, x => x.Value, StringComparer.Ordinal);

            var allNames = leftProps.Keys
                .Union(rightProps.Keys, StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal);

            foreach (var propertyName in allNames)
            {
                var propertyPath = BuildPropertyPath(path, propertyName);

                var hasLeft = leftProps.TryGetValue(propertyName, out var leftValue);
                var hasRight = rightProps.TryGetValue(propertyName, out var rightValue);

                if (!hasLeft)
                {
                    diffs.Add(new JsonPathDiffEntry
                    {
                        Path = propertyPath,
                        Kind = JsonDiffKind.Added,
                        RightValue = SerializeCompact(rightValue)
                    });
                    summary = summary with { AddedCount = summary.AddedCount + 1 };
                    continue;
                }

                if (!hasRight)
                {
                    diffs.Add(new JsonPathDiffEntry
                    {
                        Path = propertyPath,
                        Kind = JsonDiffKind.Removed,
                        LeftValue = SerializeCompact(leftValue)
                    });
                    summary = summary with { RemovedCount = summary.RemovedCount + 1 };
                    continue;
                }

                CompareElements(propertyPath, leftValue, rightValue, options, diffs, ref summary);
            }

            return;
        }

        var leftProperties = left.EnumerateObject().ToList();
        var rightProperties = right.EnumerateObject().ToList();
        var max = Math.Max(leftProperties.Count, rightProperties.Count);

        for (var i = 0; i < max; i++)
        {
            var hasLeft = i < leftProperties.Count;
            var hasRight = i < rightProperties.Count;

            if (!hasLeft)
            {
                var addedProperty = rightProperties[i];
                diffs.Add(new JsonPathDiffEntry
                {
                    Path = BuildPropertyPath(path, addedProperty.Name),
                    Kind = JsonDiffKind.Added,
                    RightValue = SerializeCompact(addedProperty.Value)
                });
                summary = summary with { AddedCount = summary.AddedCount + 1 };
                continue;
            }

            if (!hasRight)
            {
                var removedProperty = leftProperties[i];
                diffs.Add(new JsonPathDiffEntry
                {
                    Path = BuildPropertyPath(path, removedProperty.Name),
                    Kind = JsonDiffKind.Removed,
                    LeftValue = SerializeCompact(removedProperty.Value)
                });
                summary = summary with { RemovedCount = summary.RemovedCount + 1 };
                continue;
            }

            var leftProperty = leftProperties[i];
            var rightProperty = rightProperties[i];

            if (!string.Equals(leftProperty.Name, rightProperty.Name, StringComparison.Ordinal))
            {
                diffs.Add(new JsonPathDiffEntry
                {
                    Path = BuildPropertyPath(path, leftProperty.Name),
                    Kind = JsonDiffKind.Removed,
                    LeftValue = SerializeCompact(leftProperty.Value)
                });
                summary = summary with { RemovedCount = summary.RemovedCount + 1 };

                diffs.Add(new JsonPathDiffEntry
                {
                    Path = BuildPropertyPath(path, rightProperty.Name),
                    Kind = JsonDiffKind.Added,
                    RightValue = SerializeCompact(rightProperty.Value)
                });
                summary = summary with { AddedCount = summary.AddedCount + 1 };
                continue;
            }

            CompareElements(BuildPropertyPath(path, leftProperty.Name), leftProperty.Value, rightProperty.Value, options, diffs, ref summary);
        }
    }

    private static void CompareArrays(
        string path,
        JsonElement left,
        JsonElement right,
        JsonDiffOptions options,
        List<JsonPathDiffEntry> diffs,
        ref JsonDiffSummary summary)
    {
        if (options.TreatArrayOrderAsSignificant)
        {
            var leftItems = left.EnumerateArray().ToList();
            var rightItems = right.EnumerateArray().ToList();
            var max = Math.Max(leftItems.Count, rightItems.Count);

            for (var i = 0; i < max; i++)
            {
                var itemPath = $"{path}[{i}]";
                var hasLeft = i < leftItems.Count;
                var hasRight = i < rightItems.Count;

                if (!hasLeft)
                {
                    diffs.Add(new JsonPathDiffEntry
                    {
                        Path = itemPath,
                        Kind = JsonDiffKind.Added,
                        RightValue = SerializeCompact(rightItems[i])
                    });
                    summary = summary with { AddedCount = summary.AddedCount + 1 };
                    continue;
                }

                if (!hasRight)
                {
                    diffs.Add(new JsonPathDiffEntry
                    {
                        Path = itemPath,
                        Kind = JsonDiffKind.Removed,
                        LeftValue = SerializeCompact(leftItems[i])
                    });
                    summary = summary with { RemovedCount = summary.RemovedCount + 1 };
                    continue;
                }

                CompareElements(itemPath, leftItems[i], rightItems[i], options, diffs, ref summary);
            }

            return;
        }

        var leftCounts = BuildArrayCounter(left, options);
        var rightCounts = BuildArrayCounter(right, options);
        var allKeys = leftCounts.Keys.Union(rightCounts.Keys, StringComparer.Ordinal);

        foreach (var key in allKeys)
        {
            leftCounts.TryGetValue(key, out var leftCount);
            rightCounts.TryGetValue(key, out var rightCount);

            var shared = Math.Min(leftCount, rightCount);
            if (shared > 0)
            {
                summary = summary with { UnchangedCount = summary.UnchangedCount + shared };
            }

            if (leftCount > rightCount)
            {
                var removed = leftCount - rightCount;
                for (var i = 0; i < removed; i++)
                {
                    diffs.Add(new JsonPathDiffEntry
                    {
                        Path = $"{path}[*]",
                        Kind = JsonDiffKind.Removed,
                        LeftValue = key
                    });
                }

                summary = summary with { RemovedCount = summary.RemovedCount + removed };
            }

            if (rightCount > leftCount)
            {
                var added = rightCount - leftCount;
                for (var i = 0; i < added; i++)
                {
                    diffs.Add(new JsonPathDiffEntry
                    {
                        Path = $"{path}[*]",
                        Kind = JsonDiffKind.Added,
                        RightValue = key
                    });
                }

                summary = summary with { AddedCount = summary.AddedCount + added };
            }
        }
    }

    private static Dictionary<string, int> BuildArrayCounter(JsonElement arrayElement, JsonDiffOptions options)
    {
        var map = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var item in arrayElement.EnumerateArray())
        {
            var canonical = SerializeForDisplay(item, options, writeIndented: false);
            map.TryGetValue(canonical, out var count);
            map[canonical] = count + 1;
        }

        return map;
    }

    private static bool TryTreatAsEqualNullAndEmptyString(JsonElement left, JsonElement right, JsonDiffOptions options)
    {
        if (!options.TreatNullAndEmptyStringAsEqual)
        {
            return false;
        }

        return (left.ValueKind == JsonValueKind.Null && right.ValueKind == JsonValueKind.String && right.GetString() == string.Empty)
            || (right.ValueKind == JsonValueKind.Null && left.ValueKind == JsonValueKind.String && left.GetString() == string.Empty);
    }

    private static bool ValuesAreEqual(JsonElement left, JsonElement right)
    {
        return left.ValueKind switch
        {
            JsonValueKind.String => left.GetString() == right.GetString(),
            JsonValueKind.Number => left.GetRawText() == right.GetRawText(),
            JsonValueKind.True or JsonValueKind.False => left.GetBoolean() == right.GetBoolean(),
            JsonValueKind.Null => true,
            _ => left.GetRawText() == right.GetRawText()
        };
    }

    private static string BuildPropertyPath(string parentPath, string propertyName)
    {
        return IsSimpleIdentifier(propertyName)
            ? $"{parentPath}.{propertyName}"
            : $"{parentPath}[\"{propertyName.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"]";
    }

    private static bool IsSimpleIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!(char.IsLetter(value[0]) || value[0] == '_'))
        {
            return false;
        }

        for (var i = 1; i < value.Length; i++)
        {
            if (!(char.IsLetterOrDigit(value[i]) || value[i] == '_'))
            {
                return false;
            }
        }

        return true;
    }

    private static string SerializeCompact(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.Undefined
            ? string.Empty
            : element.GetRawText();
    }

    private static string SerializeForDisplay(JsonElement root, JsonDiffOptions options, bool writeIndented = true)
    {
        object? normalized = NormalizeElement(root, options);

        var serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = writeIndented
        };

        return JsonSerializer.Serialize(normalized, serializerOptions);
    }

    private static object? NormalizeElement(JsonElement element, JsonDiffOptions options)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
            {
                var properties = element.EnumerateObject();
                if (options.IgnoreObjectPropertyOrder)
                {
                    var sorted = new SortedDictionary<string, object?>(StringComparer.Ordinal);
                    foreach (var property in properties)
                    {
                        sorted[property.Name] = NormalizeElement(property.Value, options);
                    }

                    return sorted;
                }

                var map = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (var property in properties)
                {
                    map[property.Name] = NormalizeElement(property.Value, options);
                }

                return map;
            }

            case JsonValueKind.Array:
            {
                var list = element.EnumerateArray().Select(x => NormalizeElement(x, options)).ToList();
                if (!options.TreatArrayOrderAsSignificant)
                {
                    list = [.. list.OrderBy(x => JsonSerializer.Serialize(x), StringComparer.Ordinal)];
                }

                return list;
            }

            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Number:
                return element.GetRawText();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
                return null;

            default:
                return null;
        }
    }

    private static IReadOnlyList<string> SplitLines(string value)
    {
        return NormalizeLineEndings(value).Split('\n');
    }

    private static string NormalizeLineEndings(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }

    private static IReadOnlyList<JsonLineDiffEntry> BuildLineDiffs(IReadOnlyList<string> leftLines, IReadOnlyList<string> rightLines)
    {
        if ((long)leftLines.Count * rightLines.Count > MaxLcsCells)
        {
            return BuildSimpleLineDiffs(leftLines, rightLines);
        }

        var lcs = new int[leftLines.Count + 1, rightLines.Count + 1];
        for (var i = leftLines.Count - 1; i >= 0; i--)
        {
            for (var j = rightLines.Count - 1; j >= 0; j--)
            {
                if (leftLines[i] == rightLines[j])
                {
                    lcs[i, j] = 1 + lcs[i + 1, j + 1];
                }
                else
                {
                    lcs[i, j] = Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
                }
            }
        }

        var entries = new List<JsonLineDiffEntry>();
        var leftIndex = 0;
        var rightIndex = 0;

        while (leftIndex < leftLines.Count && rightIndex < rightLines.Count)
        {
            if (leftLines[leftIndex] == rightLines[rightIndex])
            {
                entries.Add(new JsonLineDiffEntry
                {
                    Kind = JsonDiffKind.Unchanged,
                    LeftLineNumber = leftIndex + 1,
                    RightLineNumber = rightIndex + 1,
                    LeftLine = leftLines[leftIndex],
                    RightLine = rightLines[rightIndex]
                });

                leftIndex++;
                rightIndex++;
                continue;
            }

            if (lcs[leftIndex + 1, rightIndex] >= lcs[leftIndex, rightIndex + 1])
            {
                entries.Add(new JsonLineDiffEntry
                {
                    Kind = JsonDiffKind.Removed,
                    LeftLineNumber = leftIndex + 1,
                    LeftLine = leftLines[leftIndex]
                });
                leftIndex++;
            }
            else
            {
                entries.Add(new JsonLineDiffEntry
                {
                    Kind = JsonDiffKind.Added,
                    RightLineNumber = rightIndex + 1,
                    RightLine = rightLines[rightIndex]
                });
                rightIndex++;
            }
        }

        while (leftIndex < leftLines.Count)
        {
            entries.Add(new JsonLineDiffEntry
            {
                Kind = JsonDiffKind.Removed,
                LeftLineNumber = leftIndex + 1,
                LeftLine = leftLines[leftIndex]
            });
            leftIndex++;
        }

        while (rightIndex < rightLines.Count)
        {
            entries.Add(new JsonLineDiffEntry
            {
                Kind = JsonDiffKind.Added,
                RightLineNumber = rightIndex + 1,
                RightLine = rightLines[rightIndex]
            });
            rightIndex++;
        }

        return MergeAdjacentChanges(entries);
    }

    private static IReadOnlyList<JsonLineDiffEntry> BuildSimpleLineDiffs(IReadOnlyList<string> leftLines, IReadOnlyList<string> rightLines)
    {
        var entries = new List<JsonLineDiffEntry>();
        var max = Math.Max(leftLines.Count, rightLines.Count);

        for (var i = 0; i < max; i++)
        {
            var hasLeft = i < leftLines.Count;
            var hasRight = i < rightLines.Count;

            if (hasLeft && hasRight)
            {
                var kind = leftLines[i] == rightLines[i] ? JsonDiffKind.Unchanged : JsonDiffKind.Changed;
                entries.Add(new JsonLineDiffEntry
                {
                    Kind = kind,
                    LeftLineNumber = i + 1,
                    RightLineNumber = i + 1,
                    LeftLine = leftLines[i],
                    RightLine = rightLines[i]
                });
            }
            else if (hasLeft)
            {
                entries.Add(new JsonLineDiffEntry
                {
                    Kind = JsonDiffKind.Removed,
                    LeftLineNumber = i + 1,
                    LeftLine = leftLines[i]
                });
            }
            else
            {
                entries.Add(new JsonLineDiffEntry
                {
                    Kind = JsonDiffKind.Added,
                    RightLineNumber = i + 1,
                    RightLine = rightLines[i]
                });
            }
        }

        return entries;
    }

    private static IReadOnlyList<JsonLineDiffEntry> MergeAdjacentChanges(IReadOnlyList<JsonLineDiffEntry> entries)
    {
        if (entries.Count == 0)
        {
            return entries;
        }

        var merged = new List<JsonLineDiffEntry>(entries.Count);
        var index = 0;

        while (index < entries.Count)
        {
            if (index + 1 < entries.Count
                && entries[index].Kind == JsonDiffKind.Removed
                && entries[index + 1].Kind == JsonDiffKind.Added)
            {
                merged.Add(new JsonLineDiffEntry
                {
                    Kind = JsonDiffKind.Changed,
                    LeftLineNumber = entries[index].LeftLineNumber,
                    RightLineNumber = entries[index + 1].RightLineNumber,
                    LeftLine = entries[index].LeftLine,
                    RightLine = entries[index + 1].RightLine
                });
                index += 2;
                continue;
            }

            merged.Add(entries[index]);
            index++;
        }

        return merged;
    }
}
