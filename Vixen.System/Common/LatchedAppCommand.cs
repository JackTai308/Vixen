﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace Vixen.Common {
    public class LatchedAppCommand : AppCommand {
        private bool _isChecked;
        private ToolStripMenuItem _menuItem;
        private EventHandler _itemCheckedHandler;

        public event EventHandler<LatchedEventArgs> Checked;

        public LatchedAppCommand() {
            _Init();
        }

        public LatchedAppCommand(string name)
            : base(name) {
            _Init();
        }

        public LatchedAppCommand(string name, string text)
            : base(name, text) {
            _Init();
        }

        public LatchedAppCommand(string name, string text, Image image)
            : base(name, text, image) {
            _Init();
        }

        private void _Init() {
            _itemCheckedHandler = (sender, e) => IsChecked = _menuItem.CheckState == CheckState.Checked;
            MenuItem = Item as ToolStripMenuItem;
        }

        public bool IsChecked {
            get { return _isChecked; }
            set {
                if(value != _isChecked) {
                    _isChecked = value;
                    (this.Item as ToolStripMenuItem).Checked = value;
                    OnChecked(value);
                }
            }
        }

        virtual protected void OnChecked(bool state) {
            if(Checked != null) {
                Checked(this, new LatchedEventArgs(state));
            }
        }

        private ToolStripMenuItem MenuItem {
            get { return _menuItem; }
            set {
                if(value != _menuItem) {
                    if(_menuItem != null) {
                        _menuItem.CheckedChanged -= _itemCheckedHandler;
                    }

                    _menuItem = value as ToolStripMenuItem;
                    _menuItem.CheckOnClick = true;
                    _menuItem.CheckedChanged += _itemCheckedHandler;
                }
            }
        }
    }
}
