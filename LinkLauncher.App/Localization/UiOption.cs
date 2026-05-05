namespace LinkLauncher.App.Localization;

public sealed class UiOption
{
    public UiOption(string key, string label)
    {
        Key = key;
        Label = label;
    }

    public string Key { get; }

    public string Label { get; }

    public override string ToString() => Label;
}
