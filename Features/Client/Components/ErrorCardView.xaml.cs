using System.Windows.Input;

namespace Paynest;

public partial class ErrorCardView : ContentView
{
	public static readonly BindableProperty ErrorMessageProperty = BindableProperty.Create(
		nameof(ErrorMessage), typeof(string), typeof(ErrorCardView), "Ocurrió un error inesperado.");

	public static readonly BindableProperty RetryCommandProperty = BindableProperty.Create(
		nameof(RetryCommand), typeof(ICommand), typeof(ErrorCardView));

	public ErrorCardView()
	{
		InitializeComponent();
	}

	public string ErrorMessage
	{
		get => (string)GetValue(ErrorMessageProperty);
		set => SetValue(ErrorMessageProperty, value);
	}

	public ICommand RetryCommand
	{
		get => (ICommand)GetValue(RetryCommandProperty);
		set => SetValue(RetryCommandProperty, value);
	}
}
