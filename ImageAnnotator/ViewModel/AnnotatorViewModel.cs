﻿using ImageAnnotator.Model;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace ImageAnnotator.ViewModel;

public struct DoublePoint {
    public double X { get; set; }
    public double Y { get; set; }


    public static implicit operator DoublePoint(System.Windows.Point v) {
        return new DoublePoint() {
            X = v.X,
            Y = v.Y
        };
    }


    public static implicit operator System.Windows.Point(DoublePoint v) {
        return new System.Windows.Point() {
            X = v.X,
            Y = v.Y
        };
    }
}

public enum InputState {
    /// <summary>
    /// waiting for user input
    /// </summary>
    Idle,

    /// <summary>
    /// Indicates that application is waiting for user input
    /// </summary>
    WaitingForInput,
}

public enum InsertionType {
    Node,
    Line,
    Rectangle
}


/// <summary>
/// The view model that is currently displayed
/// </summary>
public class AnnotatorViewModel : INotifyPropertyChanged {

    /// <summary>
    /// The image model that this view model deals with
    /// </summary>
    public required AnnotatorModel ImageModel { get; init; }

    /// <summary>
    /// The path of the image that the model holds
    /// </summary>
    public string? ImagePath => ImageModel.ImagePath;

    /// <summary>
    /// An ocasional status message to aid the user
    /// </summary>
    public string? StatusMessage { get; private set; }

    /// <summary>
    /// Defines the input state of the application
    /// </summary>
    public InputState CurrentInputState { get; private set; } = InputState.Idle;

    /// <summary>
    /// Defines the current awaited insertion type
    /// </summary>
    public InsertionType? CurrentInsertionType { get; private set; }

    /// <summary>
    /// The path of the image that is displayed to the user
    /// </summary>
    public string ImageDisplayPath => ImageModel.ImagePath ?? "No Image";

    /// <summary>
    /// The cursor position
    /// </summary>
    public Point CursorPosition { get; set; }

    /// <summary>
    /// The normalized position
    /// </summary>
    public DoublePoint NormalizedCursorPosition { get; set; }

    /// <summary>
    /// The size of the image
    /// </summary>
    public Size ImageSize { get; set; }

    /// <summary>
    /// The canvas onto which all the annotations are drawn
    /// </summary>
    public Canvas? AnnotationCanvas { get; set; }

    /// <summary>
    /// The canvas where the grid is drawn
    /// </summary>
    public Canvas? GridCanvas { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Indicates if the view is correctly setup to insert a node
    /// </summary>
    public bool CanInsertNode => CurrentInputState is InputState.Idle;

    /// <summary>
    /// Indicates if the view is waiting for any input input
    /// </summary>
    public bool IsWaitingForInput => CurrentInputState is InputState.WaitingForInput;

    /// <summary>
    /// Indicates if the view is waiting for a node input
    /// </summary>
    public bool IsWaitingForNodeInput => CurrentInsertionType is InsertionType.Node;

    /// <summary>
    /// Loads an image to the model
    /// </summary>
    /// <param name="filename">The path to the file of the image</param>
    public Exception? LoadImage(string filename) {
        try {
            ImageModel.Image = new Bitmap(filename);
        } catch (Exception ex) {
            ImageModel.Image = null;
            return ex;
        }

        ImageModel.ImagePath = filename;
        ImageSize = ImageModel.Image.Size;
        OnPropertyChanged(nameof(ImageSize));
        OnPropertyChanged(nameof(ImagePath));
        OnPropertyChanged(nameof(ImageDisplayPath));

        // try {
        //     DrawGrid(GridCanvas);
        // } catch (Exception ex) {
        //     return ex;
        // }

        return null;
    }

    public static void DrawGrid(Canvas? canvas) {
        if (canvas is null) {
            return;
        }

        if (canvas.Width is 0) {
            throw new InvalidOperationException("Zero canvas width!");
        }

        if (canvas.Height is 0) {
            throw new InvalidOperationException("Zero canvas height!");
        }

        double w = canvas.Width;
        double h = canvas.Height;
        for (int i = 0; i < 11; i++) {
            double step = (int)(w * i * 0.1);
            Line l = new() {
                X1 = step,
                X2 = step,

                Y1 = 0,
                Y2 = h,

                Stroke = System.Windows.Media.Brushes.LightSteelBlue,
                StrokeThickness = 2
                //Brush = System.Windows.Media.Brushes.Black

            };

            _ = canvas.Children.Add(l);
        }

        for (int i = 0; i < 11; i++) {
            double step = (int)(h * i * 0.1);
            Line l = new() {
                Y1 = step,
                Y2 = step,

                X1 = 0,
                X2 = w,

                Stroke = System.Windows.Media.Brushes.LightSteelBlue,
                StrokeThickness = 2
                //Brush = System.Windows.Media.Brushes.Black

            };

            _ = canvas.Children.Add(l);
        }
    }

    public void DrawAnnotations(Canvas? canvas) {
        if (canvas is null) {
            return;
        }

        if (canvas.Width is 0) {
            throw new InvalidOperationException("Zero canvas width!");
        }

        if (canvas.Height is 0) {
            throw new InvalidOperationException("Zero canvas height!");
        }

        foreach (IAnnotation a in ImageModel.Annotations) {
            _ = canvas.Children.Add(a.ToShape());
        }
    }

    public void UpdateCursorPosition(Point p, Size controlSize) {
        CursorPosition = p;
        //NormalizedCursorPosition = new DoublePoint() { X = (double)ImageSize.Width / (double)p.X, Y = (double)ImageSize.Height / (double)p.Y };
        //
        //The Zero of the image is the top left. For for tikz is the bottom left.
        NormalizedCursorPosition = new DoublePoint() {
            X = (double)p.X / controlSize.Width,
            Y = ((double)p.Y / controlSize.Height * -1) + 1
        };

        //To do the transformation we should assoumte a vector on the Normalti
        //

        OnPropertyChanged(nameof(CursorPosition));
        OnPropertyChanged(nameof(NormalizedCursorPosition));
    }

    protected virtual void OnPropertyChanged(string propertyName) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    /// <summary>
    /// Performs an insertion action
    /// </summary>
    public void InsertionAction(InsertionType itype, System.Windows.Point? point) {
        if (ImageModel.Image is null) {
            StatusMessage = "No Image is Loaded";
            OnPropertyChanged(nameof(StatusMessage));
            return;
        }

        switch ((itype, CurrentInputState)) {
            case (InsertionType.Node, InputState.Idle):
                BeginNodeInsertion();
                break;
            case (InsertionType.Node, InputState.WaitingForInput):
                if (point is null) {
                    throw new InvalidOperationException("Cannot insert a node at a null point");
                }
                InsertNode(point.Value);
                break;
            case (InsertionType.Line, InputState.Idle):
                break;
            case (InsertionType.Line, InputState.WaitingForInput):
                break;
            case (InsertionType.Rectangle, InputState.Idle):
                break;
            case (InsertionType.Rectangle, InputState.WaitingForInput):
                break;
            default:
                throw new InvalidOperationException("Condition not handled!");
        }

        DrawAnnotations(AnnotationCanvas);
    }

    public void BeginNodeInsertion() {
        StatusMessage = "Click on Image to insert node";
        CurrentInputState = InputState.WaitingForInput;
        CurrentInsertionType = InsertionType.Node;
        OnPropertyChanged(nameof(StatusMessage));
        OnPropertyChanged(nameof(CurrentInputState));
        return;
    }

    public void InsertNode(DoublePoint point) {
        StatusMessage = null;

        ImageModel.InsertNode(point);
        CurrentInputState = InputState.Idle;
        CurrentInsertionType = null;

        DrawAnnotations(AnnotationCanvas);

        OnPropertyChanged(nameof(StatusMessage));
        OnPropertyChanged(nameof(CurrentInputState));
        OnPropertyChanged(nameof(CurrentInsertionType));
        return;
    }
    //public bool Load(string filename) {
    //    try {
    //        return true;
    //    } catch {
    //        return false;
    //    }
    //    // Reticle? r = Reticle.Load(filename);
    //    // if (r is not null) {
    //    //     this.Reticle = r;
    //    //     return true;
    //    // } else { return false; }
    //}

    // public void SaveImage(Canvas canvas, string filename) {
    //     canvas.Children.Clear();
    //
    //     List<Line> lines = LineSet;
    //     List<TextBlock> textBlocks = TextBlocksSet;
    //     foreach (Line line in lines) {
    //         canvas.Children.Add(line);
    //     }
    //     foreach (TextBlock textBlock in textBlocks) {
    //         canvas.Children.Add(textBlock);
    //     }
    //
    //     Reticle.SaveCanvasToFile(canvas, filename);
    //
    //     canvas.Children.Clear();
    // }
}