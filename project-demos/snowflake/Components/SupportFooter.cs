namespace SnowflakeExample;

public class SupportFooter : ViewBase
{
    public override object? Build()
    {
        var navigator = this.UseNavigation();
        
        return Layout.Vertical().Gap(2)
            | new Button("Snowflake Settings")
                .HandleClick(() => navigator.Navigate(typeof(SnowflakeIntroductionApp)))
                .Icon(Icons.Settings2)
                .Secondary()
                .Width(Size.Full());
    }
}

