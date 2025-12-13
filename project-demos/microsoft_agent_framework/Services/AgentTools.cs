using System.ComponentModel;

namespace MicrosoftAgentFramework.Services;

/// <summary>
/// Tools/functions that the AI agent can use
/// </summary>
public static class AgentTools
{
    /// <summary>
    /// Gets the current date and time
    /// </summary>
    [Description("Get the current date and time. Useful for answering questions about what day it is, what time it is, or scheduling-related queries.")]
    public static string GetCurrentTime(
        [Description("The timezone to get the time for (optional, defaults to local time). Examples: 'UTC', 'America/New_York', 'Europe/London'.")] 
        string? timezone = null)
    {
        var now = DateTime.Now;
        
        if (!string.IsNullOrWhiteSpace(timezone))
        {
            try
            {
                var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, timeZoneInfo);
            }
            catch
            {
                // If timezone is invalid, use local time
            }
        }
        
        return $"Current date and time: {now:yyyy-MM-dd HH:mm:ss} ({now:dddd})";
    }

    /// <summary>
    /// Performs mathematical calculations
    /// </summary>
    [Description("Perform mathematical calculations. Can handle basic arithmetic (+, -, *, /), powers (^), and common functions like sqrt, sin, cos, etc. Use this when the user asks to calculate something, solve a math problem, or perform computations.")]
    public static string Calculate(
        [Description("The mathematical expression to evaluate. Examples: '2 + 2', '10 * 5', 'sqrt(16)', '2^3', '(10 + 5) / 3'.")] 
        string expression)
    {
        try
        {
            // Simple calculation using DataTable.Compute for safety
            var dataTable = new System.Data.DataTable();
            
            // Replace common math functions
            expression = expression.Replace("sqrt", "Math.Sqrt", StringComparison.OrdinalIgnoreCase);
            expression = expression.Replace("sin", "Math.Sin", StringComparison.OrdinalIgnoreCase);
            expression = expression.Replace("cos", "Math.Cos", StringComparison.OrdinalIgnoreCase);
            expression = expression.Replace("tan", "Math.Tan", StringComparison.OrdinalIgnoreCase);
            expression = expression.Replace("log", "Math.Log", StringComparison.OrdinalIgnoreCase);
            expression = expression.Replace("^", "Pow", StringComparison.OrdinalIgnoreCase);
            
            // For simple expressions, use DataTable.Compute
            // For more complex expressions with functions, use a simple evaluator
            if (expression.Contains("Math.", StringComparison.OrdinalIgnoreCase) || 
                expression.Contains("Pow", StringComparison.OrdinalIgnoreCase))
            {
                // Use C# expression evaluator for complex math
                var result = EvaluateMathExpression(expression);
                return $"Result: {result}";
            }
            else
            {
                var result = dataTable.Compute(expression, null);
                return $"Result: {result}";
            }
        }
        catch (Exception ex)
        {
            return $"Error calculating '{expression}': {ex.Message}. Please provide a valid mathematical expression.";
        }
    }

    private static double EvaluateMathExpression(string expression)
    {
        // Simple evaluator for basic math functions
        // For production, consider using a proper expression evaluator library
        var code = @"
            using System;
            public class MathEvaluator 
            { 
                public static double Evaluate() 
                { 
                    return " + expression + @"; 
                } 
            }";
        
        // For now, use a simpler approach
        // Replace Math functions with actual values
        expression = System.Text.RegularExpressions.Regex.Replace(
            expression, 
            @"Math\.Sqrt\(([^)]+)\)", 
            m => Math.Sqrt(double.Parse(m.Groups[1].Value)).ToString());
        
        expression = System.Text.RegularExpressions.Regex.Replace(
            expression, 
            @"Math\.Sin\(([^)]+)\)", 
            m => Math.Sin(double.Parse(m.Groups[1].Value)).ToString());
        
        expression = System.Text.RegularExpressions.Regex.Replace(
            expression, 
            @"Math\.Cos\(([^)]+)\)", 
            m => Math.Cos(double.Parse(m.Groups[1].Value)).ToString());
        
        var dataTable = new System.Data.DataTable();
        return Convert.ToDouble(dataTable.Compute(expression, null));
    }
}