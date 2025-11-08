using Constructs;

namespace ConstructsExample.Domain;

/// <summary>
/// Provides helpers for traversing and flattening a tree of <see cref="Construct"/> instances.
/// </summary>
public static class Traversal
{
    /// <summary>
    /// Represents a single entry in the flattened traversal.
    /// </summary>
    /// <param name="Id">The unique identifier of the underlying construct node.</param>
    /// <param name="Path">The hierarchical path of the node within the construct tree.</param>
    /// <param name="ChildrenCount">The number of direct child constructs beneath this node.</param>
    /// <param name="Node">The <see cref="Construct"/> wrapper that contains the underlying node and metadata.</param>
    public record Item(string Id, string Path, int ChildrenCount, Construct Node);

    /// <summary>
    /// Traverses the construct tree starting from the provided <paramref name="root"/> and produces a flat
    /// sequence of <see cref="Item"/> entries describing each visited node.
    /// </summary>
    /// <param name="root">The root <see cref="Construct"/> to begin traversal from. The traversal will include the root itself.</param>
    /// <returns>
    /// An <see cref="IEnumerable{T}"/> of <see cref="Item"/> instances representing the nodes visited in pre-order
    /// (a parent node appears before its children).
    /// </returns>
    /// <remarks>
    /// The method does not modify the construct tree. If <paramref name="root"/> is null, a <c>NullReferenceException</c>
    /// may occur due to dereferencing; callers should ensure a non-null root is provided.
    /// </remarks>
    public static IEnumerable<Item> Flatten(Construct root)
    {
        List<Item> list = [];

        void Walk(Construct c)
        {
            // Get the underlying node stored on the Construct wrapper.
            var node = c.Node;

            // Select direct children that are Construct wrappers.
            var children = node.Children
                .OfType<Construct>()
                .ToList();

            // Record the current node with its id, path and direct child count.
            list.Add(new Item(Id: node.Id, Path: node.Path, ChildrenCount: children.Count, Node: c));

            // Recurse into each child construct.
            foreach (var ch in children)
            {
                Walk(ch);
            }
        }

        Walk(root);
        return list;
    }
}
