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
		
		// Preview mode
		var isPreviewMode = this.UseState(false);

		var modulesPath = fs.FindModulesFolder();
		var tree = BuildTree(modulesPath, isPreviewMode.Value);

		MenuItem ToMenuItem(FileNode node)
		{
			if (node.IsFolder)
			{
				// В Preview Mode получаем иконку из _Index.md файла
				var icon = Icons.Folder;
				if (isPreviewMode.Value)
				{
					var indexPath = System.IO.Path.Combine(node.FullPath, "_Index.md");
					if (System.IO.File.Exists(indexPath))
					{
						try
						{
							var content = System.IO.File.ReadAllText(indexPath);
							// Ищем иконку в YAML frontmatter или в содержимом
							var iconMatch = System.Text.RegularExpressions.Regex.Match(content, @"icon:\s*(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
							if (iconMatch.Success && Enum.TryParse<Icons>(iconMatch.Groups[1].Value, out var parsedIcon))
							{
								icon = parsedIcon;
							}
						}
						catch { }
					}
				}
				
				return new MenuItem(
					node.Label,
					node.Children.Select(ToMenuItem).ToArray(),
					icon,
					Expanded: true,
					Tag: node.FullPath
				);
			}
			else
			{
				// В Preview Mode убираем иконку у файлов
				return new MenuItem(
					node.Label,
					Icon: isPreviewMode.Value ? null : Icons.FileText,
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
			? (object)(Layout.Vertical().Width(Size.Fraction(0.5f)) | new SidebarMenu(OnSelect, ToMenuItem(tree)))
			: Text.Muted("Modules folder not found");

		string? GetParentDir(string? path)
		{
			if (string.IsNullOrEmpty(path)) return modulesPath;
			if (Directory.Exists(path)) return path;
			try { return System.IO.Path.GetDirectoryName(path); } catch { return modulesPath; }
		}

		var client = this.UseService<IClientProvider>();
		var appRepository = this.UseService<IAppRepository>();
		var navigator = this.UseNavigation();

		bool hasSelection = !string.IsNullOrEmpty(selectedPath.Value);
		bool isFolderSelection = hasSelection && Directory.Exists(selectedPath.Value!);
		bool isFileSelection = hasSelection && !isFolderSelection;

		// Inline creation mode
		var isInlineNewPage = this.UseState(false);
		var isInlineNewFolder = this.UseState(false);
		var inlineNewPageName = this.UseState<string?>(() => null);
		var inlineNewFolderName = this.UseState<string?>(() => null);
		var isSaving = this.UseState(false);

		object actionsColumn = !isInlineNewPage.Value && !isInlineNewFolder.Value
			? (object)(Vertical().Gap(2)
				| new Button("Edit").Large().Width(Size.Full()).Icon(Icons.Pen).Disabled(!hasSelection)
				| new Button("New Page").Large().Width(Size.Full()).Icon(Icons.FilePlus).Disabled(!isFolderSelection).HandleClick(() =>
				{
					isInlineNewPage.Value = true;
				})
				| new Button("New Folder").Large().Width(Size.Full()).Icon(Icons.FolderPlus).Disabled(!isFolderSelection).HandleClick(() =>
				{
					isInlineNewFolder.Value = true;
				})
				| new Button(isPreviewMode.Value ? "Preview Mode: On" : "Preview Mode: Off").Large().Width(Size.Full()).Icon(isPreviewMode.Value ? Icons.Eye : Icons.EyeClosed).Disabled(false).HandleClick(() =>
				{
					isPreviewMode.Value = !isPreviewMode.Value;
					refreshKey.Value++; // Принудительное обновление дерева
				})
				| new Button("Delete").Large().Width(Size.Full()).Icon(Icons.Trash).Destructive().Disabled(!hasSelection).HandleClick(() =>
			{
				if (string.IsNullOrEmpty(selectedPath.Value)) { client.Toast("Select item first", "Warning"); return; }
				
				// Сохраняем информацию о том, что это папка, до удаления
				var wasFolder = Directory.Exists(selectedPath.Value);
				
				var res = repo.DeletePath(selectedPath.Value);
				if (!res.ok) client.Toast(res.message, "Error"); else { 
					client.Toast(res.message, "Success"); 
					selectedPath.Value = null; 
					
					// Полная перегенерация для папок (как при старте проекта)
					if (wasFolder)
					{
						generator.RegenerateAllBlocking();
					}
					
					refreshKey.Value++; 
					if (appRepository is AppRepository impl2) impl2.Reload(); 
					navigator.Navigate(this.GetType());
				}
			}).WithConfirm("Delete selected item?")
			)
			: isInlineNewPage.Value
				? (object)(Vertical().Gap(2)
					| new Button(isSaving.Value ? "Saving..." : "Save").Icon(Icons.Save).Primary().Width(Size.Full()).Disabled(isSaving.Value).HandleClick(() =>
					{
						if (string.IsNullOrWhiteSpace(inlineNewPageName.Value)) { client.Toast("Enter name", "Warning"); return; }
						var parent = GetParentDir(selectedPath.Value);
						if (parent == null) return;
						isSaving.Value = true;
						try
						{
							var res = repo.CreatePage(parent, inlineNewPageName.Value);
							if (!res.ok) { client.Toast(res.message, "Error"); return; }
							if (res.createdPath != null)
							{
								selectedPath.Set(res.createdPath);
								// регенерация + ожидание завершения
								generator.RegenerateSingleBlocking(res.createdPath);
								if (appRepository is AppRepository impl) impl.Reload();
								client.Sender.Send("HotReload", null);
								navigator.Navigate(this.GetType());
							}
							refreshKey.Value++;
							client.Toast("Page created", "Success");
							inlineNewPageName.Set((string?)null); isInlineNewPage.Set(false);
						}
						finally
						{
							isSaving.Value = false;
						}
					})
					| new Button("Cancel").Variant(ButtonVariant.Outline).Width(Size.Full()).Disabled(isSaving.Value).HandleClick(() => { inlineNewPageName.Set((string?)null); isInlineNewPage.Set(false); })
				)
				: (object)(Vertical().Gap(2)
					| new Button(isSaving.Value ? "Saving..." : "Save").Icon(Icons.Save).Primary().Width(Size.Full()).Disabled(isSaving.Value).HandleClick(() =>
					{
						if (string.IsNullOrWhiteSpace(inlineNewFolderName.Value)) { client.Toast("Enter name", "Warning"); return; }
						var parent = GetParentDir(selectedPath.Value);
						if (parent == null) return;
						isSaving.Value = true;
						try
						{
							var res = repo.CreateFolder(parent, inlineNewFolderName.Value);
							if (!res.ok) { client.Toast(res.message, "Error"); return; }
							if (res.createdPath != null)
							{
								selectedPath.Set(res.createdPath);
								// регенерация _Index.md файла
								generator.RegenerateSingleBlocking(System.IO.Path.Combine(res.createdPath, "_Index.md"));
								if (appRepository is AppRepository impl) impl.Reload();
								client.Sender.Send("HotReload", null);
								navigator.Navigate(this.GetType());
							}
							refreshKey.Value++;
							client.Toast("Folder created", "Success");
							inlineNewFolderName.Set((string?)null); isInlineNewFolder.Set(false);
						}
						finally
						{
							isSaving.Value = false;
						}
					})
					| new Button("Cancel").Variant(ButtonVariant.Outline).Width(Size.Full()).Disabled(isSaving.Value).HandleClick(() => { inlineNewFolderName.Set((string?)null); isInlineNewFolder.Set(false); })
				);

		object selectedInfo = !isInlineNewPage.Value && !isInlineNewFolder.Value
			? (selectedPath.Value != null
				? (object)(Layout.Horizontal().Align(Align.Left).Gap(2)
					| Text.Muted("Selected:")
					| (Directory.Exists(selectedPath.Value) ? Icons.Folder.ToIcon().Small() : Icons.FileText.ToIcon().Small())
					| Text.Muted(FormatSelectedName(selectedPath.Value)))
				: (object)Muted("Select an item on the left to manage"))
			: isInlineNewPage.Value
				? (object)(Layout.Horizontal().Align(Align.Left).Gap(2)
					| Text.Muted("Name:")
					| inlineNewPageName.ToTextInput("Enter name...").Width(Size.Grow()))
				: (object)(Layout.Horizontal().Align(Align.Left).Gap(2)
					| Text.Muted("Name:")
					| inlineNewFolderName.ToTextInput("Enter name...").Width(Size.Grow()));

		var mainContent = Vertical().Padding(2).Gap(3)
			| (isInlineNewPage.Value
				? (object)(Layout.Vertical().Gap(0)
					| H4("Create new page")
					| (Layout.Horizontal().Align(Align.Left).Gap(1)
						| Text.Muted("Directory:")
						| Icons.Folder.ToIcon().Small()
						| Text.Muted($"{FormatSelectedName(GetParentDir(selectedPath.Value) ?? selectedPath.Value ?? "")}")
					)
				)
				: isInlineNewFolder.Value
					? (object)H4("Create new folder")
					: (object)H4("Actions"))
			| selectedInfo
			| new Separator()
			| actionsColumn;

        return new SidebarLayout(
            sidebarContent, // move tree to the right (main area)
            mainContent,    // move actions to the left (sidebar)
            sidebarHeader,
            null
        ).MainAppSidebar(false).Padding(2);
	}

	private static string FormatSelectedPath(string fullPath)
	{
		try
		{
			var baseDir = AppContext.BaseDirectory;
			var dir = baseDir;
			string? modulesRoot = null;
			for (int i = 0; i < 5; i++)
			{
				var candidate = System.IO.Path.Combine(dir, "Modules");
				if (System.IO.Directory.Exists(candidate)) { modulesRoot = candidate; break; }
				var parent = System.IO.Directory.GetParent(dir)?.FullName;
				if (parent == null) break;
				dir = parent;
			}

			if (modulesRoot != null && fullPath.StartsWith(modulesRoot + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
			{
				var rel = System.IO.Path.GetRelativePath(modulesRoot, fullPath);
				return $"Modules{System.IO.Path.DirectorySeparatorChar}{rel}";
			}
		}
		catch { }
		return fullPath;
	}

	private static string FormatSelectedName(string fullPath)
	{
		var isFolder = Directory.Exists(fullPath);
		var name = isFolder ? System.IO.Path.GetFileName(fullPath) : System.IO.Path.GetFileName(fullPath);
		// Показываем реальные имена (с нумерацией)
		return name ?? "";
	}

	private static FileNode? BuildTree(string? root, bool isPreviewMode = false)
	{
		if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return null;
		return BuildNode(root, isPreviewMode);
	}

	private static FileNode BuildNode(string path, bool isPreviewMode = false)
	{
		var name = System.IO.Path.GetFileName(path);
		
		// В Preview Mode убираем префиксы и скрываем _Index.md файлы
		if (isPreviewMode)
		{
			// Скрываем _Index.md файлы
			if (name == "_Index.md")
				return null;
			
			// Убираем префиксы для отображения
			var display = System.Text.RegularExpressions.Regex.Replace(name, "^\\d+_", "");
			
			if (Directory.Exists(path))
			{
				var children = Directory.GetFileSystemEntries(path)
					.OrderBy(p => p)
					.Select(p => BuildNode(p, isPreviewMode))
					.Where(node => node != null)
					.ToArray();
				return new FileNode(display, path, true, children);
			}
			else
			{
				return new FileNode(display, path, false, Array.Empty<FileNode>());
			}
		}
		else
		{
			// Обычный режим - показываем реальные имена
			var display = name;
			if (Directory.Exists(path))
			{
				var children = Directory.GetFileSystemEntries(path)
					.OrderBy(p => p)
					.Select(p => BuildNode(p, isPreviewMode))
					.ToArray();
				return new FileNode(display, path, true, children);
			}
			else
			{
				return new FileNode(display, path, false, Array.Empty<FileNode>());
			}
		}
	}
}
