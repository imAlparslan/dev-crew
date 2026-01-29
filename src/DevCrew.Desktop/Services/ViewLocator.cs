using System;
using System.Collections.Concurrent;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevCrew.Desktop.Services;

/// <summary>
/// ViewLocator service for dynamic mapping from ViewModel to View
/// </summary>
public class ViewLocator : IDataTemplate
{
    private static readonly ConcurrentDictionary<Type, Type?> _viewTypeCache = new();

    public Control? Build(object? data)
    {
        if (data is null)
            return null;

        var viewModelType = data.GetType();
        
        var viewType = _viewTypeCache.GetOrAdd(viewModelType, vmType =>
        {
            var fullName = vmType.FullName;

            if (string.IsNullOrWhiteSpace(fullName))
                return null;

            // ViewModels ad uzayını Views ile değiştir, son ekleri ViewModel → View çevir.
            var candidateName = fullName
                .Replace(".ViewModels.", ".Views.")
                .Replace("ViewModel", "View");

            // Assembly-qualified dene, sonra aynı assembly içi dene.
            var assemblyName = vmType.Assembly.FullName;
            return Type.GetType(candidateName) ?? Type.GetType($"{candidateName}, {assemblyName}");
        });

        if (viewType != null)
        {
            var control = (Control)Activator.CreateInstance(viewType)!;
            control.DataContext = data;
            return control;
        }

        var viewModelName = viewModelType.Name;
        return new TextBlock { Text = $"View bulunamadı: {viewModelName}" };
    }

    public bool Match(object? data)
    {
        // Match all ViewModels derived from ObservableObject
        return data is ObservableObject && data.GetType().Name.EndsWith("ViewModel");
    }
}
