using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using uTas.TasFormat;

namespace TasEditor.Views;

public partial class FrameByFrameEditor : UserControl {
    public FrameByFrameEditor() {
        InitializeComponent();
    }

    private List<string> _inputKinds = new();
    private List<string[]> _frameInputs = new();
    private TasFile _tasInputs = new(new List<TasLineInfo>());

    public Action<string> OnChange = (_) => { };

    public void GenerateGrid(TasFile tasInputs) {
        _inputKinds = new List<string>();

        _frameInputs.Clear();
        foreach (var lineInfo in tasInputs.Lines) {
            if (lineInfo.Line is not TasLine.FrameInput input) continue;

            foreach (var key in input.Inputs)
                if (!_inputKinds.Contains(key.Key))
                    _inputKinds.Add(key.Key);

            var inputs = input.Inputs.Select(i => i.Key).ToArray();
            for (var i = 0; i < input.FrameCount; i++) _frameInputs.Add(inputs);
        }

        tasInputs.Expand();
        _tasInputs = tasInputs;

        var width = GridLength.Auto;
        var height = GridLength.Auto;

        // column/row definitions
        InputGrid.ColumnDefinitions.Clear();
        InputGrid.ColumnDefinitions.Add(new ColumnDefinition(width));
        for (var i = 0; i < _inputKinds.Count; i++)
            InputGrid.ColumnDefinitions.Add(new ColumnDefinition(width));

        InputGrid.RowDefinitions.Clear();
        InputGrid.RowDefinitions.Add(new RowDefinition(height));
        for (var i = 0; i < _frameInputs.Count; i++) InputGrid.RowDefinitions.Add(new RowDefinition(height));

        InputGrid.Children.Clear();

        // header
        for (var i = 0; i < _inputKinds.Count; i++) {
            var header = new TextBlock { Text = _inputKinds[i] };
            Grid.SetRow(header, 0);
            Grid.SetColumn(header, i + 1);
            InputGrid.Children.Add(header);
        }


        // frames
        for (var frame = 0; frame < _frameInputs.Count; frame++) {
            var frameno = new TextBlock {
                Text = (frame + 1).ToString(),
                TextAlignment = TextAlignment.End,
                Padding = new Thickness(0, 0, 4, 0),
                FontFamily = "Cascadia Code,Consolas,Menlo,Monospace"
            };
            Grid.SetRow(frameno, frame + 1);
            Grid.SetColumn(frameno, 0);
            InputGrid.Children.Add(frameno);

            for (var input = 0; input < _inputKinds.Count; input++) {
                var button = new ToggleButton();

                var frameCopy = frame;
                var inputCopy = input;
                button.Click += (_, _) => { OnInputToggled(frameCopy, inputCopy); };

                Grid.SetRow(button, frame + 1);
                Grid.SetColumn(button, input + 1);

                button.IsChecked = _frameInputs[frame].Contains(_inputKinds[input]);
                InputGrid.Children.Add(button);
            }
        }
    }

    private void OnInputToggled(int frame, int input) {
        var frameInputs = _frameInputs[frame];
        var toggledInput = _inputKinds[input];

        var tasInputIndex = 0;
        for (var i = 0; i < frame; i++)
            if (_tasInputs.Lines[i].Line is TasLine.FrameInput)
                tasInputIndex += 1;


        var index = Array.IndexOf(frameInputs, toggledInput);
        if (index == -1) {
            var newInputs = new string[frameInputs.Length + 1];
            Array.Copy(frameInputs, newInputs, frameInputs.Length);
            newInputs[^1] = toggledInput;
            _frameInputs[frame] = newInputs;

            if (_tasInputs.Lines[tasInputIndex].Line is not TasLine.FrameInput frameInput) throw new Exception();
            frameInput.Inputs.Add(new Input(toggledInput));
        } else {
            var newInputs = new string[frameInputs.Length - 1];
            Array.Copy(frameInputs, 0, newInputs, 0, index);
            Array.Copy(frameInputs, index + 1, newInputs, index, frameInputs.Length - index - 1);
            _frameInputs[frame] = newInputs;


            if (_tasInputs.Lines[tasInputIndex].Line is not TasLine.FrameInput frameInput) throw new Exception();
            frameInput.Inputs.RemoveWhere(i => i.Key == toggledInput);
        }


        _tasInputs.Combine();
        OnChange(_tasInputs.ToTasFormat());
        _tasInputs.Expand();
    }
}