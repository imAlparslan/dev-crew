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

    /// <summary>
    /// Builds a view for the supplied view model instance.
    /// </summary>
    /// <param name="data">View model instance.</param>
    /// <returns>The resolved view or a fallback control.</returns>
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

            // Replace ViewModels namespace with Views and convert ViewModel suffix to View.
            var candidateName = fullName
                .Replace(".ViewModels.", ".Views.")
                .Replace("ViewModel", "View");

            // Try assembly-qualified name first, then try within the same assembly.
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
        return new TextBlock { Text = $"View not found for {viewModelName}" };
    }

    /// <summary>
    /// Determines whether this template can build a view for the data.
    /// </summary>
    /// <param name="data">View model instance.</param>
    /// <returns>True when the data is a supported view model.</returns>
    public bool Match(object? data)
    {
        // Match all ViewModels derived from ObservableObject
        return data is ObservableObject && data.GetType().Name.EndsWith("ViewModel");
    }
}
