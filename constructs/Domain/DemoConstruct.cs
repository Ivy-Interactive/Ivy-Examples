using Constructs;

namespace ConstructsExample.Domain;

/// <summary>
/// Represents a demonstration construct that builds a small construct tree for examples and tests.
/// The tree structure created by this construct is:
/// <code>
/// Root
///  └─ Demo
///     ├─ ChildA
///     ├─ ChildB
///     └─ Nested
///        ├─ Leaf1
///        └─ Leaf2
/// </code>
/// </summary>
/// <remarks>
/// This type is intended for examples and unit tests that need a predictable construct hierarchy.
/// It does not perform any validation or side effects beyond creating child constructs.
/// </remarks>
/// <example>
/// var root = DemoConstruct.BuildRoot();
/// // root now contains a Demo construct with two direct children (ChildA, ChildB)
/// // and a Nested construct containing Leaf1 and Leaf2.
/// </example>
public class DemoConstruct : Construct
{
    /// <summary>
    /// Gets the first direct child construct named "ChildA".
    /// </summary>
    public Construct ChildA { get; }

    /// <summary>
    /// Gets the second direct child construct named "ChildB".
    /// </summary>
    public Construct ChildB { get; }

    /// <summary>
    /// Gets the intermediate nested construct named "Nested" that contains leaf children.
    /// </summary>
    public Construct Nested { get; }

    /// <summary>
    /// Gets the nested leaf construct named "Leaf1" under <see cref="Nested"/>.
    /// </summary>
    public Construct Leaf1 { get; }

    /// <summary>
    /// Gets the nested leaf construct named "Leaf2" under <see cref="Nested"/>.
    /// </summary>
    public Construct Leaf2 { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DemoConstruct"/> class.
    /// </summary>
    /// <param name="scope">
    /// The parent construct that will act as the scope for this instance.
    /// This value becomes the logical parent in the construct tree.
    /// </param>
    /// <param name="id">
    /// The local identifier for this construct within the provided <paramref name="scope"/>.
    /// This id is used to distinguish this construct among its siblings.
    /// </param>
    public DemoConstruct(Construct scope, string id) : base(scope, id)
    {
        ChildA = new Construct(this, "ChildA");
        ChildB = new Construct(this, "ChildB");

        Nested = new Construct(this, "Nested");
        Leaf1 = new Construct(Nested, "Leaf1");
        Leaf2 = new Construct(Nested, "Leaf2");
    }

    /// <summary>
    /// Builds and returns a <see cref="RootConstruct"/> containing a <see cref="DemoConstruct"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="RootConstruct"/> instance named "Root" with a single child
    /// <see cref="DemoConstruct"/> named "Demo".
    /// </returns>
    /// <remarks>
    /// Use this helper to quickly create the canonical example construct tree for tests
    /// or documentation samples.
    /// </remarks>
    public static RootConstruct BuildRoot()
    {
        var root = new RootConstruct("Root");
        _ = new DemoConstruct(root, "Demo");
        return root;
    }
}
