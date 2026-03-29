namespace IvyAskStatistics.Models;

// Row for the widgets table in QuestionsApp (Actions is a placeholder for the Generate button)
public record WidgetRow(string Widget, string Category, int Easy, int Medium, int Hard, string Actions = "");
