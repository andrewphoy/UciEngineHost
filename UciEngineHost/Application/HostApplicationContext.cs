using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UciEngineHost {
    public class HostApplicationContext : ApplicationContext {

        private System.ComponentModel.IContainer _components;
        private NotifyIcon _notifyIcon;
        private readonly Configuration _configuration;

        public HostApplicationContext(Configuration config, WebServer server) {
            _configuration = config;
            InitializeContext();
        }

        private void InitializeContext() {
            _components = new System.ComponentModel.Container();
            CreateNotifyIcon();
        }

        private void CreateNotifyIcon() {
            _notifyIcon = new NotifyIcon(_components) {
                ContextMenuStrip = new ContextMenuStrip(),
                Icon = Properties.Resources.KnightCheckmark,
                Text = Properties.Resources.NotifyIcon_Tooltip,
                Visible = true
            };

            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
            _notifyIcon.MouseUp += NotifyIcon_MouseUp;

            var engineMenu = new ToolStripMenuItem(Properties.Resources.ContextMenu_Engines);
            
            // how do we want to order the engines?
            foreach (var engine in _configuration.Engines) {
                var tsi = new ToolStripMenuItem(engine.Name, null, EngineMenuItem_Click) {
                    Checked = engine.Selected
                };
                engineMenu.DropDownItems.Add(tsi);
            }

            _notifyIcon.ContextMenuStrip.Items.Add(engineMenu);
            _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem(Properties.Resources.ContextMenu_Preferences, null, NotifyIconContext_PreferencesClicked));
        }

        private bool _suppressContext = false;
        private void NotifyIcon_MouseUp(object? sender, MouseEventArgs e) {
            if (_suppressContext) { return; }
            if (e.Button == MouseButtons.Left) {
                MethodInfo? mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                mi?.Invoke(_notifyIcon, null);
            }
        }

        private void NotifyIcon_DoubleClick(object? sender, EventArgs e) {
            //_suppressContext = true;
            //_notifyIcon.ContextMenuStrip.Visible = false;
            //bool refreshRequired = DialogFormManager.ExecutePreferencesDialog(_configuration);
            //_suppressContext = false;
            //RefreshNotifyContext();

            //if (refreshRequired) {
            //    //_wallpaperManager.Refresh(true);
            //}
        }

        private void NotifyIconContext_PreferencesClicked(object? sender, EventArgs e) {
            //bool refreshRequired = DialogFormManager.ExecutePreferencesDialog(_configuration);
            //RefreshNotifyContext();

            //if (refreshRequired) {
            //    //_wallpaperManager.Refresh(true);
            //}
        }

        private void EngineMenuItem_Click(object? sender, EventArgs e) {

        }
    }
}
