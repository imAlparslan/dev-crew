using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevCrew.Desktop.Services;

/// <summary>
/// ViewLocator service for dynamic mapping from ViewModel to View
/// </summary>
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null)
            return null;

        var viewModelType = data.GetType();
        var fullName = viewModelType.FullName;

        if (string.IsNullOrWhiteSpace(fullName))
            return new TextBlock { Text = "View bulunamadı: (adı çözülemedi)" };

        // ViewModels ad uzayını Views ile değiştir, son ekleri ViewModel → View çevir.
        var candidateName = fullName
            .Replace(".ViewModels.", ".Views.")
            .Replace("ViewModel", "View");

        // Assembly-qualified dene, sonra aynı assembly içi dene.
        var assemblyName = viewModelType.Assembly.FullName;
        var type = Type.GetType(candidateName) ?? Type.GetType($"{candidateName}, {assemblyName}");

        if (type != null)
        {
            var control = (Control)Activator.CreateInstance(type)!;
            control.DataContext = data;
            return control;
        }

        return new TextBlock { Text = $"View bulunamadı: {candidateName}" };
    }

    public bool Match(object? data)
    {
        // Match all ViewModels derived from ObservableObject
        return data is ObservableObject && data.GetType().Name.EndsWith("ViewModel");
    }
}
