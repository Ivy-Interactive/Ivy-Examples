namespace YamlDotNetExample.Models;

public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
}

public class Person
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public float HeightInInches { get; set; }
    public Dictionary<string, Address> Addresses { get; set; } = new();
}
