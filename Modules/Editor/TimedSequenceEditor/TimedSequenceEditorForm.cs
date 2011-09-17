﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Vixen.Execution;
using Vixen.Sys;
using Vixen.Module.Editor;
using Vixen.Module.Sequence;
using Vixen.Module.Effect;
using Vixen.Module.EffectEditor;
using Vixen.Module.Property;
using CommonElements.Timeline;
using VixenModules.Sequence.Timed;

namespace VixenModules.Editor.TimedSequenceEditor
{
	public partial class TimedSequenceEditorForm : Form, IEditorUserInterface
	{
		#region Member Variables

		private TimedSequence _sequence;
		private Dictionary<CommandNode, List<TimelineElement>> _commandNodeToElements;
		private Dictionary<ChannelNode, List<TimelineRow>> _channelNodeToRows;

		#endregion



		public TimedSequenceEditorForm()
		{
			InitializeComponent();

			_commandNodeToElements = new Dictionary<CommandNode, List<TimelineElement>>();
			_channelNodeToRows = new Dictionary<ChannelNode, List<TimelineRow>>();

			timelineControl.ElementChangedRows += ElementChangedRowsHandler;

			LoadAvailableEffects();
		}

		private void LoadAvailableEffects()
		{
			foreach (IEffectModuleInstance effect in ApplicationServices.GetAll<IEffectModuleInstance>().Cast<IEffect>().ToArray()) {
				ToolStripMenuItem item = new ToolStripMenuItem(effect.EffectName);
				item.Tag = effect.InstanceId;
				item.Click += (sender, e) => {

				};
				addEffectToolStripMenuItem.DropDownItems.Add(item);
			}
		}



		#region IEditorUserInterface implementation

		// TODO: what is this supposed to be for?
		public EditorValues EditorValues
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsModified { get; private set; }

		public void NewSequence()
		{
			// load all the system channels/groups to the control as rows. This will
			// also clear the grid entirely, and start fresh.
			LoadSystemNodesToRows();

			_sequence = new TimedSequence();

			// TODO: do other stuff here like setting default zooms, clearing snaps, etc.
		}

		public void RefreshSequence()
		{
			throw new NotImplementedException();
		}

		public void Save(string filePath = null)
		{
			SaveSequence(filePath);
		}

		public ISelection Selection
		{
			get { throw new NotImplementedException(); }
		}

		public Vixen.Sys.ISequence Sequence
		{
			get { return _sequence; }
			set { LoadSequence(value);  }
		}

		public IEditorModuleInstance OwnerModule { get; set; }

		public void Start()
		{
			Show();
		}

		#endregion






		#region Saving / Loading Methods

		/// <summary>
		/// Loads all nodes (groups/channels) currently in the system as rows in the timeline control.
		/// </summary>
		private void LoadSystemNodesToRows(bool clearCurrentRows = true)
		{
			if (clearCurrentRows)
				timelineControl.ClearAllRows();

			foreach (ChannelNode node in Vixen.Sys.Execution.Nodes.RootNodes) {
				AddNodeAsRow(node, null);
			}
		}

		/// <summary>
		/// Adds a single given channel node as a row in the timeline control. Recursively adds all
		/// child nodes of the given node as children, if needed.
		/// </summary>
		/// <param name="node">The node to generate a row for.</param>
		/// <param name="parentRow">The parent node the row should belong to, if any.</param>
		private void AddNodeAsRow(ChannelNode node, TimelineRow parentRow)
		{
			// made the new row from the given node and add it to the control.
			TimelineRow newRow = timelineControl.AddRow(node.Name, parentRow);
			newRow.ElementRemoved += ElementRemovedFromRowHandler;

			// Tag it with the node it refers to, and take note of which row the given channel node will refer to.
			newRow.Tag = node;
			if (_channelNodeToRows.ContainsKey(node))
				_channelNodeToRows[node].Add(newRow);
			else
				_channelNodeToRows[node] = new List<TimelineRow> { newRow };

			// iterate through all if its children, adding them as needed
			foreach (ChannelNode child in node.Children) {
				AddNodeAsRow(child, newRow);
			}
		}

		private void LoadSequence(Vixen.Sys.ISequence sequence)
		{
			// check if it's the right type of sequence that we know how to deal with. If not, complain bitterly.
			if (sequence is TimedSequence)
				_sequence = (TimedSequence)sequence;
			else {
				throw new NotImplementedException("Cannot use sequence type with a Timed Sequence Editor");
			}

			// clear out all the old data
			LoadSystemNodesToRows();

			// load the new data: get all the commands in the sequence, and make a new element for each of them.
			foreach (CommandNode node in _sequence.Data.GetCommands()) {
				AddNewElementForCommandNode(node);
			}
		}

		/// <summary>
		/// Populates the TimelineControl grid with a new TimedSequenceElement for the given CommandNode.
		/// </summary>
		/// <param name="node">The CommandNode to make an element in the grid for.</param>
		private void AddNewElementForCommandNode(CommandNode node)
		{
			// for the command, make a new element for every separate row its in; even though there's only
			// a single command, if there are multiple copies of the same channel node that it is in (ie.
			// belonging to different groups, etc.) then add a new element for each row. We can just track the
			// elements, and change the other "identical" elements if one of them changes.
			if (_channelNodeToRows.ContainsKey(node.TargetNodes.First())) {
				List<TimelineRow> targetRows = _channelNodeToRows[node.TargetNodes.First()];
				// make a new element for each row that represents the channel this command is in.
				foreach (TimelineRow row in targetRows) {
					TimedSequenceElement element = new TimedSequenceElement();
					element.StartTime = node.StartTime;
					element.Duration = node.TimeSpan;
					element.CommandNode = node;
					element.ElementContentChanged += ElementContentChangedHandler;
					element.ElementMoved += ElementMovedHandler;
					if (_commandNodeToElements.ContainsKey(node))
						_commandNodeToElements[node].Add(element);
					else
						_commandNodeToElements[node] = new List<TimelineElement> { element };
				}
			} else {
				// we don't have a row for the channel this command is referencing; most likely, the row has
				// been deleted, or we're opening someone else's sequence, etc. Big fat TODO: here for that, then.
				// dunno what we want to do: prompt to add new channels for them? map them to others? etc.
			}
		}

		private void SaveSequence(string filePath = null)
		{
			if (_sequence == null) {
				throw new NullReferenceException("Trying to save a sequence that is null!");
			}

			if (filePath == null) {
				if (_sequence.FilePath.Trim() == "") {
					// TODO: browse window to find a suitable name for a sequence?
					string newPath = "./asdfasdf.tim";
					_sequence.Save(newPath);
				} else {
					_sequence.Save();
				}
			} else {
				_sequence.Save(filePath);
			}

			IsModified = false;
		}


		#endregion


		#region Event Handlers

		protected void ElementContentChangedHandler(object sender, EventArgs e)
		{
			TimedSequenceElement element = sender as TimedSequenceElement;
			// TODO: I'm not sure if we will need to do anything here; if we are updating effect details,
			// will the EffectEditors configure the CommandNode object directly?
			IsModified = true;
		}

		protected void ElementMovedHandler(object sender, EventArgs e)
		{
			TimedSequenceElement element = sender as TimedSequenceElement;
			element.CommandNode.StartTime = element.StartTime;
			element.CommandNode.TimeSpan = element.Duration;
			IsModified = true;
		}

		protected void ElementRemovedFromRowHandler(object sender, ElementEventArgs e)
		{
			e.Element.ElementContentChanged -= ElementContentChangedHandler;
			e.Element.ElementMoved -= ElementMovedHandler;
			IsModified = true;
		}

		protected void ElementChangedRowsHandler(object sender, ElementRowChangeEventArgs e)
		{
			ChannelNode oldNode = e.OldRow.Tag as ChannelNode;
			ChannelNode newNode = e.NewRow.Tag as ChannelNode;
			TimedSequenceElement element = e.Element as TimedSequenceElement;
			if (element.CommandNode.TargetNodes.Contains(oldNode))
				element.CommandNode.TargetNodes.Remove(oldNode);

			element.CommandNode.TargetNodes.Add(newNode);
		}

		#endregion



		#region Tool Strip Menu Items

		private void newToolStripMenuItem_Click(object sender, EventArgs e)
		{
			NewSequence();
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{

		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveSequence();
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		#endregion

		private void toolStripButton1_Click(object sender, EventArgs e)
		{
			Command command = new Command(new Guid("{603E3297-994C-4705-9F17-02A62ECC14B5}"), null);
			CommandNode cn = _sequence.InsertData(new[] { timelineControl.First().Tag as ChannelNode },
				TimeSpan.FromSeconds(0.0), TimeSpan.FromSeconds(2.0), command);

			TimedSequenceElement newItem = new TimedSequenceElement();
			newItem.StartTime = cn.StartTime;
			newItem.Duration = cn.TimeSpan;
			newItem.BackColor = Color.DodgerBlue;
			newItem.CommandNode = cn;

			timelineControl.First().AddElement(newItem);
		}
	}
}