using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WindowsInput;


// Authored by meatRay after a friend dared me to write it within a couple hours.
namespace VN_TAS
{
	public partial class MainWindow : Window
	{
		// USER32 functions implemented to automatically pass focus to a Window.  
		// This doesn't work for Written in the Sky, strangely enough.
		[DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
		public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

		[DllImport("USER32.DLL")]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		private IntPtr _targetHandle;
		private InputSimulator _sim;
		private Task _workTask;
		private CancellationTokenSource _workCancel;
		private TimeSpan _clickSpeed;
		private bool _startOnKey = true;

		public MainWindow()
		{
			InitializeComponent();

			_workCancel = new CancellationTokenSource();
			_clickSpeed = TimeSpan.FromMilliseconds(1);
			_sim = new InputSimulator();
		}

		private void WindowNameInput_TextChanged(object sender, TextChangedEventArgs e)
		{
			WindowSearchButton.IsEnabled = WindowNameInput.Text.Length > 0;
		}

		private void WindowSearchButton_Click(object sender, RoutedEventArgs e)
		{
			_targetHandle = FindWindow(null, WindowNameInput.Text);
			AdvancedGrid.IsEnabled = _targetHandle != null && _targetHandle.ToInt32() != 0;
		}

		private void ShowButton_Click(object sender, RoutedEventArgs e)
		{
			SetForegroundWindow(_targetHandle);
		}

		private void StartButton_Click(object sender, RoutedEventArgs e)
		{
			// In good practice, this would "lock onto" an application.
			// Not working with Written in the Sky though, so not bothering to implement.
			/*
			if (_workTask != null && _workTask.Status == TaskStatus.Running)
				return; */

			SetForegroundWindow(_targetHandle);
			_workTask = GodsWork(_workCancel.Token);
		}

		private async Task GodsWork(CancellationToken cancel)
		{
			ClickDelayInput.IsEnabled = false;
			double c_delay = -1;
			if (!double.TryParse(ClickDelayInput.Text, out c_delay))
			{
				MessageBox.Show("Invalid Time input for click speed.", "Invalid Input", MessageBoxButton.OK);
				return;
			}
			_clickSpeed = TimeSpan.FromMilliseconds(c_delay);
			StatusLabel.Content = "Status.. Waiting";
			if (_startOnKey)
				while (!_sim.InputDeviceState.IsHardwareKeyDown(WindowsInput.Native.VirtualKeyCode.HOME))
					await Task.Delay(TimeSpan.FromMilliseconds(100));
			else
				await Task.Delay(TimeSpan.FromSeconds(5));
			StatusLabel.Content = "Status.. Running";
			while (!cancel.IsCancellationRequested && !_sim.InputDeviceState.IsHardwareKeyDown(WindowsInput.Native.VirtualKeyCode.BACK))
			{

				_sim.Mouse.LeftButtonClick();
				await Task.Delay(_clickSpeed);
			}
			StatusLabel.Content = "Status.. Finished";
			ClickDelayInput.IsEnabled = true;
		}

		private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
			e.Handled = true;
		}

		private void ProcStartToggle_Click(object sender, RoutedEventArgs e)
		{
			_startOnKey = !_startOnKey;
			ToggleText.Content = _startOnKey ? "When `Home` Held" : "After 5 Seconds";
		}
	}
}
