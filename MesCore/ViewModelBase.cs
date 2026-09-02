using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MesCore;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>
    /// 값이 실제로 바뀌었을 때만 화면에 알린다.
    /// </summary>
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}

/* ============================================================
   RelayCommand - 버튼 클릭을 ViewModel 의 메서드에 연결한다.
   ------------------------------------------------------------
   WinForms:  btn.Click += (s,e) => Save();       (코드 뒤에서 연결)
   WPF:       <Button Command="{Binding SaveCommand}"/>   (XAML 에서 연결)

   CanExecute 가 false 를 돌려주면 버튼이 자동으로 비활성화된다.
   이게 WPF 바인딩의 장점 - 버튼 Enabled 를 직접 켜고 끌 필요가 없다.
   ============================================================ */
public class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => execute();
}