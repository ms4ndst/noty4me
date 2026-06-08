using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Noty4Me.Models;
using Noty4Me.Services;

namespace Noty4Me.UI;

public partial class NotesWindow : Window
{
    private static NotesWindow? _instance;

    private readonly ObservableCollection<Note> _notes;
    private Note? _current;
    private bool _suppressEditorEvents;

    private NotesWindow()
    {
        InitializeComponent();
        WindowChrome.Attach(this);

        _notes = new ObservableCollection<Note>(NotesStore.Load());
        NoteList.ItemsSource = _notes;

        if (_notes.Count == 0)
            _notes.Add(new Note { Title = "Welcome", Body = "Right-click the tray icon for options. Notes autosave on edit." });

        NoteList.SelectedIndex = 0;
        UpdateStatus();
    }

    public static void ShowOrFocus()
    {
        if (_instance is null)
        {
            _instance = new NotesWindow();
            _instance.Closed += (_, _) => _instance = null;
            _instance.Show();
        }
        else
        {
            if (_instance.WindowState == WindowState.Minimized)
                _instance.WindowState = WindowState.Normal;
            _instance.Activate();
        }
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _current = NoteList.SelectedItem as Note;
        _suppressEditorEvents = true;
        TitleBox.Text = _current?.Title ?? "";
        BodyBox.Text  = _current?.Body  ?? "";
        _suppressEditorEvents = false;
        TitleBox.IsEnabled = _current is not null;
        BodyBox.IsEnabled  = _current is not null;
    }

    private void OnTitleChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressEditorEvents || _current is null) return;
        _current.Title = TitleBox.Text;
        _current.Updated = DateTimeOffset.UtcNow;
        Persist();
    }

    private void OnBodyChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressEditorEvents || _current is null) return;
        _current.Body = BodyBox.Text;
        _current.Updated = DateTimeOffset.UtcNow;
        Persist();
    }

    private void OnNew(object sender, RoutedEventArgs e)
    {
        var n = new Note { Title = "New note" };
        _notes.Insert(0, n);
        NoteList.SelectedItem = n;
        TitleBox.Focus();
        TitleBox.SelectAll();
        Persist();
    }

    private void OnDelete(object sender, RoutedEventArgs e)
    {
        if (_current is null) return;
        var result = MessageBox.Show(this, $"Delete \"{_current.DisplayTitle}\"?",
            "Confirm delete", MessageBoxButton.OKCancel, MessageBoxImage.Question);
        if (result != MessageBoxResult.OK) return;
        var idx = _notes.IndexOf(_current);
        _notes.Remove(_current);
        if (_notes.Count == 0) _current = null;
        else NoteList.SelectedIndex = Math.Min(idx, _notes.Count - 1);
        Persist();
    }

    private void OnOpenSettings(object sender, RoutedEventArgs e) => SettingsWindow.ShowOrFocus();

    private void Persist()
    {
        NotesStore.Save(_notes);
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        StatusText.Text = $"{_notes.Count} note(s) • saved to {Paths.NotesFile}";
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        NotesStore.Save(_notes);
        base.OnClosing(e);
    }
}
