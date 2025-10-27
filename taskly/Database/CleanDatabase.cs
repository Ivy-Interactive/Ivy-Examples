using Dapper;

namespace Taskly.Database;

/// <summary>
/// Utility methods to clean/reset database tables
/// </summary>
public class CleanDatabase
{
    /// <summary>
    /// Delete only tutorial data from all tables (used by IntroductionApp restart)
    /// </summary>
    public static async Task CleanTutorialData()
    {
        try
        {
            Console.WriteLine("⚠️  WARNING: Cleaning tutorial data from database tables...");

            using var connection = InitDatabase.GetConnection();

            // Delete all tutorial backlog items
            var itemCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM backlog_items WHERE is_tutorial = 1");
            connection.Execute("DELETE FROM backlog_items WHERE is_tutorial = 1");
            Console.WriteLine($"✓ Deleted {itemCount} tutorial backlog items");

            // Delete all tutorial sprints
            var sprintCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM sprints WHERE is_tutorial = 1");
            connection.Execute("DELETE FROM sprints WHERE is_tutorial = 1");
            Console.WriteLine($"✓ Deleted {sprintCount} tutorial sprints");

            Console.WriteLine("✓ Tutorial data cleaned successfully!");

            await Task.CompletedTask; // Keep async signature for compatibility
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error cleaning tutorial data: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Delete ALL data from all tables (used by PlanningApp clean database button)
    /// </summary>
    public static async Task CleanAllTables()
    {
        try
        {
            Console.WriteLine("⚠️  WARNING: Cleaning ALL data from database tables...");

            using var connection = InitDatabase.GetConnection();

            // Delete all backlog items
            var itemCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM backlog_items");
            connection.Execute("DELETE FROM backlog_items");
            Console.WriteLine($"✓ Deleted {itemCount} backlog items");

            // Delete all sprints
            var sprintCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM sprints");
            connection.Execute("DELETE FROM sprints");
            Console.WriteLine($"✓ Deleted {sprintCount} sprints");

            // Delete all developers
            var developerCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM developers");
            connection.Execute("DELETE FROM developers");
            Console.WriteLine($"✓ Deleted {developerCount} developers");

            // Reset introduction completion flag so user can go through tutorial again
            AppSettings.SetBoolSetting("introduction_completed", false);
            Console.WriteLine("✓ Introduction completion flag reset");

            Console.WriteLine("✓ All data cleaned successfully!");

            await Task.CompletedTask; // Keep async signature for compatibility
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error cleaning all data: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Delete only backlog items (keeps sprints)
    /// </summary>
    public static async Task CleanBacklogItems()
    {
        try
        {
            Console.WriteLine("Cleaning backlog items...");

            var items = InitDatabase.GetAllBacklogItems();
            foreach (var item in items)
            {
                InitDatabase.DeleteBacklogItem(item.Id);
            }

            Console.WriteLine($"✓ Deleted {items.Count} backlog items");

            await Task.CompletedTask; // Keep async signature for compatibility
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error cleaning backlog items: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Delete only sprints (keeps backlog items)
    /// </summary>
    public static async Task CleanSprints()
    {
        try
        {
            Console.WriteLine("Cleaning sprints...");

            using var connection = InitDatabase.GetConnection();
            var sprintCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM sprints");
            connection.Execute("DELETE FROM sprints");

            Console.WriteLine($"✓ Deleted {sprintCount} sprints");

            await Task.CompletedTask; // Keep async signature for compatibility
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error cleaning sprints: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Reset database to empty state and restart sequences
    /// </summary>
    public static async Task ResetDatabase()
    {
        try
        {
            Console.WriteLine("⚠️  WARNING: Resetting entire database...");

            // Clean all tables first
            await CleanAllTables();

            // Reset auto-increment sequences in SQLite
            using var connection = InitDatabase.GetConnection();
            connection.Execute("DELETE FROM sqlite_sequence WHERE name IN ('backlog_items', 'sprints', 'developers')");

            Console.WriteLine("✓ Database reset complete!");
            Console.WriteLine("✓ Auto-increment IDs have been reset to 1");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error resetting database: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Interactive prompt to confirm database cleaning
    /// </summary>
    public static async Task<bool> PromptAndClean()
    {
        Console.WriteLine("⚠️  WARNING: This will delete ALL data from the database!");
        Console.Write("Are you sure you want to continue? (type 'yes' to confirm): ");

        var confirmation = Console.ReadLine()?.Trim().ToLower();

        if (confirmation == "yes")
        {
            await CleanAllTables();
            return true;
        }
        else
        {
            Console.WriteLine("Operation cancelled.");
            return false;
        }
    }
}
