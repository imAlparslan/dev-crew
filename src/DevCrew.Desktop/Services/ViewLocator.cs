using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevCrew.Desktop.Services;

/// <summary>
/// ViewModel'den View'a dinamik eşleme için ViewLocator servisi
/// </summary>
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null)
            return null;

        var fullName = data.GetType().FullName!;
        var name = fullName.Replace("ViewModel", "View");
        
        // Assembly-qualified name ile dene
        var type = Type.GetType(name);
        
        // Bulunamazsa, aynı assembly'den dene
        if (type == null)
        {
            var assemblyName = data.GetType().Assembly.FullName;
            type = Type.GetType($"{name}, {assemblyName}");
        }

        if (type != null)
        {
            var control = (Control)Activator.CreateInstance(type)!;
            control.DataContext = data;
            return control;
        }

        return new TextBlock { Text = $"View bulunamadı: {name}" };
    }

    public bool Match(object? data)
    {
        // ObservableObject'ten türeyen tüm ViewModel'leri eşleştir
        return data is ObservableObject && data.GetType().Name.EndsWith("ViewModel");
    }
}
