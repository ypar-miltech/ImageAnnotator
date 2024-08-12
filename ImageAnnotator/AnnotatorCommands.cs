using System.Windows.Input;

namespace ImageAnnotator;

public static class AnnotatorCommands {
    public static readonly RoutedUICommand InsertNode = new("Inserts a Node to the Model", "InsertNode", typeof(AnnotatorCommands));
    public static readonly RoutedUICommand InsertLine = new("Inserts a Line to the Model", "InsertLine", typeof(AnnotatorCommands));
    public static readonly RoutedUICommand InsertRectangle = new("Inserts a Rectangle to the Model", "InsertRectangle", typeof(AnnotatorCommands));
}