namespace DevCrew.Core.Models.Results;

public record JsonDiffOptions
{
    public bool IgnoreObjectPropertyOrder { get; init; } = true;

    public bool TreatArrayOrderAsSignificant { get; init; } = true;

    public bool IgnoreWhitespaceDifferences { get; init; } = true;

    public bool TreatNullAndEmptyStringAsEqual { get; init; }
}
