namespace SliplaneDeploy.Services;

using System.Security.Cryptography;
using System.Text.RegularExpressions;

/// <summary>
/// Template engine for <c>ivy-deploy.yaml</c> strings.
///
/// Two kinds of substitution are applied in this order:
/// <list type="number">
///   <item><c>{parentServiceName}</c> — early-bound literal replaced with the parent service
///     name everywhere.</item>
///   <item>Late-bound references — after early substitution, any <c>{child:host}</c> or
///     <c>{child:env:KEY}</c> (single or double braces around the expression) resolves to the
///     corresponding child service's <c>internalDomain</c> or passed env value.</item>
/// </list>
/// Running early substitution first means that <c>{{parentServiceName}-db:host}</c> naturally
/// becomes <c>{myapp-db:host}</c>, which is then a valid late reference.
/// </summary>
public static class IvyDeployTemplateEngine
{
    public const string ParentServicePlaceholder = "{parentServiceName}";

    // Matches '{child:host}' / '{child:env:KEY}' with optional duplicated braces on each side.
    // The inner content forbids '{' and '}' (early substitution has already run), so inner-most
    // refs are found regardless of whether the author used '{...}' or '{{...}}'.
    private static readonly Regex LateRefPattern = new(
        @"\{{1,2}(?<expr>[^{}]*:[^{}]+?)\}{1,2}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string SubstituteEarly(string input, string parentServiceName) =>
        string.IsNullOrEmpty(input) ? input
            : input.Replace(ParentServicePlaceholder, parentServiceName, StringComparison.Ordinal);

    /// <summary>
    /// Describes a resolved child service: the host (<c>network.internalDomain</c>) plus any
    /// env values that were passed to it at creation time.
    /// </summary>
    public record ChildResolution(string Host, IReadOnlyDictionary<string, string> Env);

    /// <summary>
    /// Applies early-bound substitution, then resolves every late-bound reference using the
    /// supplied child resolutions. Unknown references throw
    /// <see cref="InvalidOperationException"/> so a misconfigured manifest fails loudly rather
    /// than shipping broken env values.
    /// </summary>
    public static string Resolve(
        string input,
        string parentServiceName,
        IReadOnlyDictionary<string, ChildResolution> children)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var early = SubstituteEarly(input, parentServiceName);
        if (!early.Contains(':', StringComparison.Ordinal)) return early;

        return LateRefPattern.Replace(early, match =>
        {
            var expr = match.Groups["expr"].Value.Trim();
            return ResolveRef(expr, children);
        });
    }

    private static string ResolveRef(string expr, IReadOnlyDictionary<string, ChildResolution> children)
    {
        var parts = expr.Split(':');
        if (parts.Length < 2)
            throw new InvalidOperationException(
                $"Invalid template reference '{{{expr}}}'. Expected '{{child:host}}' or '{{child:env:KEY}}'.");

        var childName = parts[0].Trim();
        var field = parts[1].Trim();

        if (!children.TryGetValue(childName, out var resolution))
            throw new InvalidOperationException(
                $"Template reference '{{{expr}}}' points to unknown child service '{childName}'.");

        return field switch
        {
            "host" when parts.Length == 2 => resolution.Host,
            "env" when parts.Length == 3 => ResolveEnv(childName, resolution, parts[2].Trim()),
            _ => throw new InvalidOperationException(
                $"Invalid template field in '{{{expr}}}'. Use 'host' or 'env:KEY'."),
        };
    }

    private static string ResolveEnv(string childName, ChildResolution resolution, string key)
    {
        if (!resolution.Env.TryGetValue(key, out var value))
            throw new InvalidOperationException(
                $"Template reference '{{{childName}:env:{key}}}' targets env '{key}' which was not set on '{childName}'.");
        return value;
    }

    /// <summary>
    /// Resolves <c>generate: random:N</c>. Currently the only supported form. Returns a URL-safe
    /// Base64 string of length <c>N</c>.
    /// </summary>
    public static string Generate(string spec)
    {
        const string prefix = "random:";
        if (!spec.StartsWith(prefix, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"Unsupported generate spec '{spec}'. Only '{prefix}N' is supported.");

        if (!int.TryParse(spec[prefix.Length..], out var length) || length <= 0 || length > 256)
            throw new InvalidOperationException(
                $"Invalid length in generate spec '{spec}'. Must be an integer in (0, 256].");

        var byteLen = (int)Math.Ceiling(length * 3.0 / 4.0);
        var bytes = RandomNumberGenerator.GetBytes(byteLen);
        var base64 = Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        return base64.Length <= length ? base64 : base64[..length];
    }
}
