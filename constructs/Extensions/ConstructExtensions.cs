using Constructs;

namespace ConstructsExample.Extensions
{
    /// <summary>
    /// Provides extension methods for working with <see cref="Construct"/> instances.
    /// </summary>
    public static class ConstructExtensions
    {
        /// <summary>
        /// Searches the construct tree rooted at <paramref name="root"/> for a construct whose node path
        /// exactly matches <paramref name="path"/>.
        /// </summary>
        /// <param name="root">The root construct to start the search from.</param>
        /// <param name="path">The node path to find. The search looks for an exact match against <c>Node.Path</c>.</param>
        /// <returns>
        /// The <see cref="Construct"/> whose <c>Node.Path</c> equals <paramref name="path"/>, or
        /// <c>null</c> if no matching construct is found within the subtree rooted at <paramref name="root"/>.
        /// </returns>
        /// <remarks>
        /// The search performs a depth-first traversal of the children (recursively) and returns the first match found.
        /// If multiple constructs could match the same path, the first match encountered in the traversal order is returned.
        /// </remarks>
        public static Construct? FindByPath(this Construct root, string path)
        {
            if (root.Node.Path.Equals(path.Trim('/'), StringComparison.Ordinal))
            {
                return root;
            }
            foreach (var ch in root.Node.Children.OfType<Construct>())
            {
                var hit = FindByPath(ch, path);
                if (hit != null)
                {
                    return hit;
                }
            }
            return null;
        }

        /// <summary>
        /// Renders an ASCII tree representation of the construct hierarchy starting at <paramref name="basePath"/>.
        /// </summary>
        /// <param name="root">The root construct that contains the subtree to render.</param>
        /// <param name="basePath">
        /// The path of the construct to use as the starting node for rendering. If a construct with this path
        /// is not found, rendering starts from <paramref name="root"/>.
        /// </param>
        /// <returns>
        /// A list of strings where each string is a line in the ASCII tree. The first line is the starting node's path,
        /// and subsequent lines show children with visual connectors (for example, "├─" and "└─") and the child node IDs.
        /// </returns>
        /// <remarks>
        /// The method resolves <paramref name="basePath"/> using <see cref="FindByPath"/>. Children are enumerated
        /// in the order provided by <c>Node.Children</c>, filtered to <see cref="Construct"/> instances. A local recursive
        /// function walks the subtree and appends lines using characters that visually represent branch and last-child
        /// relationships.
        /// </remarks>
        public static List<string> RenderAsciiLines(this Construct root, string basePath)
        {
            var start = FindByPath(root, basePath) ?? root;
            List<string> lines = [start.Node.Path];

            void Walk(Construct node, string prefix)
            {
                var kids = node.Node.Children.OfType<Construct>().ToList();
                for (int i = 0; i < kids.Count; i++)
                {
                    var child = kids[i];
                    bool isLast = i == kids.Count - 1;
                    lines.Add(prefix + (isLast ? "└─ " : "├─ ") + child.Node.Id);
                    Walk(child, prefix + (isLast ? "   " : "│  "));
                }
            }

            Walk(start, string.Empty);
            return lines;
        }
    }
}
