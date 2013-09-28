﻿using System.IO.Ports;
using System.Text;
using Vixen.Commands;
using Vixen.Module;
using Vixen.Module.Controller;
using System.Timers;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Threading.Tasks;
using System.Diagnostics;

namespace VixenModules.Output.CommandController
{
	public class Module : ControllerModuleInstanceBase
	{
		private static NLog.Logger Logging = NLog.LogManager.GetCurrentClassLogger();

		private Data _Data;
		private CommandHandler _commandHandler;
		public Module()
		{
			DataPolicyFactory = new DataPolicyFactory();
			_commandHandler = new CommandHandler();
		}

		internal static bool Send(Data RdsData, string rdsText, string rdsArtist = null)
		{
			switch (RdsData.HardwareID) {
				case Hardware.MRDS192:
				case Hardware.MRDS1322:
					NativeMethods.ConnectionSetup(RdsData.ConnectionMode, RdsData.PortNumber, RdsData.BiDirectional, RdsData.Slow);
					if (NativeMethods.Connect()) {
						try {


							byte[] Data = new byte[9];
							int i, Len;
							Data[0] = 0x02;             // buffer address
							Len = 8;
							for (i = 1; i <= Len; i++)
								Data[i] = 0x20; // fill 8 blank spaces first
							//Data[0] = 0x02;
							//Len = 8;
							for (i = 0; i < rdsText.Length; i++) {
								byte vOut = Convert.ToByte(rdsText[i]);
								Data[i + 1] = vOut;
							}

							if (NativeMethods.Send(Len, Data))
								return true;
							else
								return false;

						} finally {
							NativeMethods.Disconnect();
						}
					}
					return false;
				case Hardware.VFMT212R:
				case Hardware.HTTP:
					System.Threading.Tasks.Task.Factory.StartNew(() => {
						try {
							string url = RdsData.HttpUrl.ToLower().Replace("{text}",HttpUtility.UrlEncode(rdsText)).Replace("{time}", HttpUtility.UrlEncode(DateTime.Now.ToLocalTime().ToShortTimeString()));
							WebRequest request = WebRequest.Create(url);
							if (RdsData.RequireHTTPAuthentication) {
								request.Credentials= new NetworkCredential(RdsData.HttpUsername, RdsData.HttpPassword);
							}
							var response = request.GetResponse();
						} catch (Exception e) {
							Logging.ErrorException(e.Message, e);

						}
					});
					return true;
				default:
					return false;
			}


		}
		internal static bool Launch(Data RdsData, string Executable, string Arguments)
		{
			if (File.Exists(Executable)) {
				Logging.Info(string.Format("Launching Executable: {0} with arguments [{1}]", Executable, Arguments));
				Task.Factory.StartNew(() => {
					try {
						Stopwatch w = Stopwatch.StartNew();
						Process proc = new Process();

						proc.StartInfo.FileName   = Executable;
						if (!string.IsNullOrWhiteSpace(Arguments))
							proc.StartInfo.Arguments = Arguments;

						proc.StartInfo.CreateNoWindow= RdsData.HideLaunchedWindows;

						proc.Start();
						proc.WaitForExit();
						w.Stop();
						Logging.Info(string.Format("Process {0} Completed After {1} with Exit code {2}", Executable, w.Elapsed, proc.ExitCode));

					} catch (Exception e) {

						Logging.ErrorException(e.Message, e);
					}

				});
			} else
				Logging.Error(string.Format("File Not found to Launch: [{0}]", Executable));
			return false;
		}
		public override void UpdateState(int chainIndex, ICommand[] outputStates)
		{
			 foreach (var item in outputStates.Where(i => i != null)) {
				var cmd = item as StringCommand;
				if (cmd != null) {
					var cmdType = cmd.CommandValue.Split('|')[0];
					switch (cmdType.ToUpper()) {
						case "RDS":
							Module.Send(_Data, cmd.CommandValue.Split('|')[1]);
							 
							break;
						case "LAUNCHER":
							var args = cmd.CommandValue.Split('|')[1].Split(',');

							Module.Launch(_Data, args[0], args[1]);
							 
							break;

					}
					Logging.Info("RDS Value Sent: " + cmd.CommandValue);

				}
			}
		}

		public override bool HasSetup
		{
			get { return true; }
		}

		public override bool Setup()
		{
			using (var setupForm = new SetupForm(_Data)) {
				if (setupForm.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
					_Data= setupForm.RdsData;

					return true;
				}
				return false;
			}
		}

		public override IModuleDataModel ModuleData
		{
			get { return _Data; }
			set
			{
				_Data = (Data)value;
			}
		}

		public override void Start()
		{
			base.Start();

		}

		public override void Stop()
		{

			base.Stop();
		}

	}
}