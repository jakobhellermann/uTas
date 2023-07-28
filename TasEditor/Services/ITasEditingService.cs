namespace TasEditor.Services;

public interface ITasEditingService {
    void ToggleCommentLineByLine();

    void ToggleComment();

    void CombineConsecutiveInputs();

    public void ExtendSelectionLineBoundaries();
}

internal class DummyTasEditingService : ITasEditingService {
    public void ToggleCommentLineByLine() {
    }

    public void ToggleComment() {
    }

    public void CombineConsecutiveInputs() {
    }

    public void ExtendSelectionLineBoundaries() {
    }
}