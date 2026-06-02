namespace CovolSplitter.Winforms.Models;

public sealed class FilterOption
{
    public string Value { get; set; } = "";
    public string Text { get; set; } = "";

    public override string ToString()
    {
        return Text;
    }
}