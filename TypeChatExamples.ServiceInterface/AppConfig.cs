namespace TypeChatExamples.ServiceInterface;

public class AppConfig
{
    public string Project { get; set; }
    public string Location { get; set; }
    public SiteConfig CoffeeShop { get; set; }
    public SiteConfig Sentiment { get; set; }
    public SiteConfig Calendar { get; set; }
    public SiteConfig Restaurant { get; set; }
    public SiteConfig Math { get; set; }
    public SiteConfig Music { get; set; }
    public string NodePath { get; set; }
    public string? FfmpegPath { get; set; }
    public string? WhisperPath { get; set; }
    public int NodeProcessTimeoutMs { get; set; } = 120 * 1000;

    public SiteConfig GetSiteConfig(string name)
    {
        return name.ToLower() switch
        {
            "coffeeshop" => CoffeeShop,
            "sentiment" => Sentiment,
            "calendar" => Calendar,
            "restaurant" => Restaurant,
            "math" => Math,
            "music" => Music,
            _ => throw new NotSupportedException($"No SiteConfig exists for '{name}'")
        };
    }
}

public class SiteConfig
{
    public string GptPath { get; set; }
    public string Bucket { get; set; }
    public string RecognizerId { get; set; }
    public string PhraseSetId { get; set; }
}