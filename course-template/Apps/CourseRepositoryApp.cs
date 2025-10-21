using Ivy;
using Ivy.Apps;
using Ivy.Shared;
using Ivy.Core;
using static Ivy.Views.Layout;
using static Ivy.Views.Text;
using CourseTemplate.Apps.Services;
using Ivy.Client;
using Ivy.Views.Alerts;

namespace CourseTemplate.Apps;

[App(order: 2, title: "Course Repository", icon: Icons.FolderTree)]
public class CourseRepositoryApp() : ViewBase
{
	private record FileNode(string Label, string FullPath, bool IsFolder, FileNode[] Children);

	public override object? Build()
	{
		var fs = new FileSystemService();
		var repo = new CourseRepositoryService(fs);
		var generator = new GenerationService(fs);

		var selectedPath = this.UseState<string?>();
		var refreshKey = this.UseState(0);

		var modulesPath = fs.FindModulesFolder();
		var tree = BuildTree(modulesPath);

		MenuItem ToMenuItem(FileNode node)
		{
			if (node.IsFolder)
			{
				return new MenuItem(
					node.Label,
					node.Children.Select(ToMenuItem).ToArray(),
					Icons.Folder,
					Expanded: true,
					Tag: node.FullPath
				);
			}
			else
			{
				return new MenuItem(
					node.Label,
					Icon: Icons.FileText,
					Tag: node.FullPath
				);
			}
		}

		ValueTask OnSelect(Event<SidebarMenu, object> ev)
		{
			if (ev.Value is string path)
			{
				selectedPath.Set(path);
			}
			return ValueTask.CompletedTask;
		}

		var sidebarHeader = Vertical().Padding(2)
			| H3("Course Repository");

		object sidebarContent = tree != null
			? new SidebarMenu(OnSelect, ToMenuItem(tree))
			: Text.Muted("Modules folder not found");

		string? GetParentDir(string? path)
		{
			if (string.IsNullOrEmpty(path)) return modulesPath;
			if (Directory.Exists(path)) return path;
			try { return System.IO.Path.GetDirectoryName(path); } catch { return modulesPath; }
		}

		var client = this.UseService<IClientProvider>();

		var actionsColumn = Vertical().Gap(2)
			| new Button("New Folder").Large().Icon(Icons.FolderPlus).WithPrompt<string>(value =>
			{
				var parent = GetParentDir(selectedPath.Value);
				if (parent == null) return;
				var name = value;
				if (string.IsNullOrWhiteSpace(name)) return;
				var res = repo.CreateFolder(parent, name);
				if (!res.ok) client.Toast(res.message, "Error"); else { client.Toast(res.message, "Success"); refreshKey.Value++; }
			}, "NewFolder", "Folder name")
			| new Button("New Page").Large().Icon(Icons.FilePlus).WithPrompt<string>(value =>
			{
				var parent = GetParentDir(selectedPath.Value);
				if (parent == null) return;
				var name = value;
				if (string.IsNullOrWhiteSpace(name)) return;
				var res = repo.CreatePage(parent, name);
				if (!res.ok) client.Toast(res.message, "Error");
				else
				{
					client.Toast(res.message, "Success");
					if (res.createdPath != null)
					{
						generator.RegenerateSingleBlocking(res.createdPath);
					}
					refreshKey.Value++;
				}
			}, "NewPage", "Page name")
			| new Button("Delete").Large().Icon(Icons.Trash).Destructive().HandleClick(() =>
			{
				if (string.IsNullOrEmpty(selectedPath.Value)) { client.Toast("Select item first", "Warning"); return; }
				var res = repo.DeletePath(selectedPath.Value);
				if (!res.ok) client.Toast(res.message, "Error"); else { client.Toast(res.message, "Success"); selectedPath.Value = null; refreshKey.Value++; }
			}).WithConfirm("Delete selected item?");

		var mainContent = Vertical().Padding(2).Gap(3)
			| H4("Actions")
			| (selectedPath.Value != null
				? Block($"Selected: {selectedPath.Value}")
				: Muted("Select an item on the left to manage"))
			| new Separator()
			| actionsColumn;

		return new SidebarLayout(
			mainContent,
			sidebarContent,
			sidebarHeader,
			null
		).MainAppSidebar(false).Padding(2);
	}

	private static FileNode? BuildTree(string? root)
	{
		if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return null;
		return BuildNode(root);
	}

	private static FileNode BuildNode(string path)
	{
		var name = System.IO.Path.GetFileName(path);
		var display = System.Text.RegularExpressions.Regex.Replace(name, "^\\d+_", "");
		if (Directory.Exists(path))
		{
			var children = Directory.GetFileSystemEntries(path)
				.OrderBy(p => p)
				.Select(BuildNode)
				.ToArray();
			return new FileNode(display, path, true, children);
		}
		else
		{
			return new FileNode(display, path, false, Array.Empty<FileNode>());
		}
	}
}
