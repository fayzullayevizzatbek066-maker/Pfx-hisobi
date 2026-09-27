namespace PFXManager.App.Services;

public interface IExplorerService
{
    void ShowFileInExplorer(string fullPath);
    void CopyToClipboard(string text);
}
