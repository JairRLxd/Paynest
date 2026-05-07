using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Paynest.Features.Client.ViewModels;

public abstract class BaseViewModel : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;

	protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string name = "")
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
		{
			return;
		}

		field = value;
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	protected void RaisePropertyChanged([CallerMemberName] string name = "")
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}
