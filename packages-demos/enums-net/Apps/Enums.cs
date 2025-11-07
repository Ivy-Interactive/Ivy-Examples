using System;
using System.ComponentModel;
using EnumsNET;
// Target: Enums.NET v5.0.0
// Drop these two files into /Apps/enums-net/ inside your Ivy project.
// Add to your .csproj:
// <ItemGroup>
//   <PackageReference Include="Enums.NET" Version="5.0.0" />
// </ItemGroup>

/* -----------------------------
   File: Enums.cs
   Purpose: Shared enum definitions for the demo.
   Notes: Keep enums in a separate file to mirror real-world project layout.
   ----------------------------- */



namespace EnumsNetApp.Apps
{

    enum NumericOperator
    {
        [Symbol("="), Description("Is")]
        Equals,
        [Symbol("!="), Description("Is not")]
        NotEquals,
        [Symbol("<")]
        LessThan,
        [Symbol(">="), PrimaryEnumMember] // PrimaryEnumMember indicates enum member as primary duplicate for extension methods
        GreaterThanOrEquals,
        NotLessThan = GreaterThanOrEquals,
        [Symbol(">")]
        GreaterThan,
        [Symbol("<="), PrimaryEnumMember]
        LessThanOrEquals,
        NotGreaterThan = LessThanOrEquals
    }

    [AttributeUsage(AttributeTargets.Field)]
    class SymbolAttribute : Attribute
    {
        public string Symbol { get; }

        public SymbolAttribute(string symbol)
        {
            Symbol = symbol;
        }
    }

    [Flags]
    enum DaysOfWeek
    {
        None = 0,
        Sunday = 1,
        Monday = 2,
        Tuesday = 4,
        Wednesday = 8,
        Thursday = 16,
        Friday = 32,
        Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
        Saturday = 64,
        Weekend = Sunday | Saturday,
        All = Sunday | Monday | Tuesday | Wednesday | Thursday | Friday | Saturday
    }

    [Flags, DayTypeValidator]
    enum DayType
    {
        Weekday = 1,
        Weekend = 2,
        Holiday = 4
    }

    sealed class DayTypeValidatorAttribute : EnumValidatorAttribute<DayType>
    {
        public override bool IsValid(DayType value) =>
            value.GetFlagCount(DayType.Weekday | DayType.Weekend) == 1 && FlagEnums.IsValidFlagCombination(value);
    }

    public enum PriorityLevel
    {
        [Display(Name = "Low Priority", Order = 3)]
        Low = 1,

        [Display(Name = "Medium Priority", Order = 2)]
        Medium = 2,

        [Display(Name = "High Priority", Order = 1)]
        High = 3
    }

}