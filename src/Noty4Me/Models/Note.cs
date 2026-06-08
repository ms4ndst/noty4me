using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Noty4Me.Models;

public sealed class Note : INotifyPropertyChanged
{
    private string _title = "";
    private string _body = "";
    private DateTimeOffset _updated = DateTimeOffset.UtcNow;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title
    {
        get => _title;
        set { if (_title == value) return; _title = value; OnChanged(); }
    }

    public string Body
    {
        get => _body;
        set { if (_body == value) return; _body = value; OnChanged(); }
    }

    public DateTimeOffset Updated
    {
        get => _updated;
        set { if (_updated == value) return; _updated = value; OnChanged(); }
    }

    [JsonIgnore]
    public string DisplayTitle => string.IsNullOrWhiteSpace(_title) ? "(untitled)" : _title;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        if (name != nameof(DisplayTitle) && name == nameof(Title))
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayTitle)));
    }
}
