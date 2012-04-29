﻿using Vixen.Sys;

namespace Vixen.Commands {
	public class UnsignedLongValue : Dispatchable<UnsignedLongValue>, ICommand<ulong> {
		public UnsignedLongValue(ulong value) {
			CommandValue = value;
		}

		public ulong CommandValue { get; set; }

		object ICommand.CommandValue {
			get { return CommandValue; }
			set { CommandValue = (ulong)value; }
		}

		//public void Dispatch(CommandDispatch commandDispatch) {
		//    // Must be done in the classes being dispatched.
		//    if(commandDispatch != null)
		//        commandDispatch.DispatchCommand(this);
		//}
	}
}
