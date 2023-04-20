using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ContactPoint.BaseDesign;
using ContactPoint.Common;
using ContactPoint.Common.Audio;
using ContactPoint.Common.PluginManager;
using ContactPoint.Common.SIP;
using ContactPoint.Common.SIP.Account;
using Microsoft.Win32;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

namespace ContactPoint.Forms
{
    public enum SettingsFormPart
    {
        General = 0,
        Audio,
        Codecs,
        Plugins
    }

    public partial class SettingsForm : Form
    {
        private ICore _core;
        
        private Action<IEnumerable<IAudioDevice>> _audioDevicesChangedHandler;
        private Action<IAudioDevice> _audioDeviceChangedHandler;

        private bool _restart = false;
        public bool RestartRequired
        {
            get { return _restart; }
            private set { _restart = value; }
        }

        private bool _reregister = false;
        public bool ReregisterRequired
        {
            get { return _reregister; }
            private set { _reregister = value; }
        }

        public event EventHandler ReRegistered;

        public SettingsForm(ICore core)
        {
            _audioDevicesChangedHandler = new Action<IEnumerable<IAudioDevice>>(AudioDevicesChanged);
            _audioDeviceChangedHandler = new Action<IAudioDevice>(AudioDeviceChanged);

            InitializeComponent();

            _core = core;

            _core.Audio.AudioDevicesAdded += _audioDevicesChangedHandler;
            _core.Audio.AudioDevicesRemoved += _audioDevicesChangedHandler;
            _core.Audio.PlaybackDeviceChanged += _audioDeviceChangedHandler;
            _core.Audio.RecordingDeviceChanged += _audioDeviceChangedHandler;
        }

        public void Show(SettingsFormPart part)
        {
            switch (part)
            {
                case SettingsFormPart.Audio: tabControlSettings.SelectedTab = tabPageSettingsAudio; break;
                case SettingsFormPart.Codecs: tabControlSettings.SelectedTab = tabCodecsPage; break;
                default: tabControlSettings.SelectedTab = tabPageSettingsSIP; break;
            }

            Show();
        }
        
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            _core.Audio.AudioDevicesAdded -= _audioDevicesChangedHandler;
            _core.Audio.AudioDevicesRemoved -= _audioDevicesChangedHandler;
            _core.Audio.PlaybackDeviceChanged -= _audioDeviceChangedHandler;
            _core.Audio.RecordingDeviceChanged -= _audioDeviceChangedHandler;
        }
        
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void clearAll()
        {
            textBoxDisplayName.Text = "";
            textBoxUsername.Text = "";
            textBoxPassword.Text = "";
            textBoxRegistrarAddress.Text = "";
            textBoxDomain.Text = "*";
        }

        private bool ApplySettings()
        {
            this._core.Sip.Account.Server = textBoxRegistrarAddress.Text;
            this._core.Sip.Account.FullName = textBoxDisplayName.Text;
            this._core.Sip.Account.UserName = textBoxUsername.Text;
            this._core.Sip.Account.Password = textBoxPassword.Text;
            this._core.Sip.Account.Realm = textBoxDomain.Text;

            this._core.Sip.TransportType = (SipTransportType)comboBoxSIPTransport.SelectedIndex;

            // additional settings
            this._core.Sip.Account.Port = Int16.Parse(textBoxListenPort.Text);
            this._core.Sip.DTMFMode = (SipDTMFMode)comboBoxDtmfMode.SelectedIndex;
            
            // save audio devices
            _cancelAudioEvents = true;
            this._core.Audio.PlaybackDevice = this.comboBoxPlaybackDevices.SelectedItem as IAudioDevice;
            this._core.Audio.RecordingDevice = this.comboBoxRecordingDevices.SelectedItem as IAudioDevice;
            _cancelAudioEvents = false;
            
            _core.SettingsManager.Set("AudioPlayOnIncoming", checkBoxPlayIncomingSound.Checked);
            _core.SettingsManager.Set("AudioPlayOnIncomingAndActive", checkBoxPlayIncomingSound.Checked && checkBoxPlayIncomingWhileActive.Checked);
            _core.SettingsManager.Set("AudioPlayOutgoing", checkBoxPlayRingback.Checked);

            // check if at least 1 codec selected
            if (listBoxEnCodecs.Items.Count == 0)
            {
                (new AlertForm(ContactPoint.CaptionStrings.CaptionStrings.NoCodecsSelected)).ShowDialog();

                this.tabControlSettings.SelectTab(this.tabCodecsPage);

                return false;
            }

            // save codec list
            foreach (ISipCodec codec in this._core.Sip.Codecs)
            {
                if (this.listBoxEnCodecs.Items.Contains(codec.Name))
                    codec.Enabled = true;
                if (this.listBoxDisCodecs.Items.Contains(codec.Name))
                    codec.Enabled = false;
            }

            // save license
            if (!String.IsNullOrEmpty(textBoxLicensePath.Text))
                try
                {
                    using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\ContactPoint\IpPhone", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.None))
                    {
                        if (key != null)
                        {
                            key.SetValue("LicenseObject", File.ReadAllBytes(textBoxLicensePath.Text));
                            RestartRequired = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarn(ex);
                }

            return true;
        }
        
        private void buttonOK_Click(object sender, EventArgs e)
        {
            if (ApplySettings())
            {
                this._core.SettingsManager.Save();

                // reinitialize stack
                if (RestartRequired)
                {
                    (new AlertForm(ContactPoint.CaptionStrings.CaptionStrings.RestartNeeded)).ShowDialog();

                    Application.Restart();
                }

                if (ReregisterRequired)
                {
                    if (this._core.Sip.Account.RegisterState != SipAccountState.Offline)
                        this._core.Sip.Account.UnRegister();

                    this._core.Sip.Account.Register();

                    if (ReRegistered != null)
                        ReRegistered(this, null);
                }
             
                Close();
            }
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            textBoxDisplayName.Text = this._core.Sip.Account.FullName;
            textBoxUsername.Text = this._core.Sip.Account.UserName;
            textBoxPassword.Text = this._core.Sip.Account.Password;
            textBoxRegistrarAddress.Text = this._core.Sip.Account.Server;
            textBoxDomain.Text = this._core.Sip.Account.Realm;
            comboBoxSIPTransport.SelectedIndex = (int)this._core.Sip.TransportType;
            textBoxListenPort.Text = this._core.Sip.Account.Port.ToString();

            comboBoxDtmfMode.SelectedIndex = (int)this._core.Sip.DTMFMode;

            checkBoxPlayIncomingSound.Checked = _core.SettingsManager.GetValueOrSetDefault("AudioPlayOnIncoming", true);
            checkBoxPlayIncomingWhileActive.Enabled = checkBoxPlayIncomingSound.Checked;
            checkBoxPlayIncomingWhileActive.Checked = _core.SettingsManager.GetValueOrSetDefault("AudioPlayOnIncomingAndActive", false);
            checkBoxPlayRingback.Checked = _core.SettingsManager.GetValueOrSetDefault("AudioPlayOutgoing", true);

            // init audio
            ReloadAudioDevices();

            // load codecs from system 
            foreach (ISipCodec codec in this._core.Sip.Codecs)
                if (codec.Enabled) listBoxEnCodecs.Items.Add(codec.Name);
                else listBoxDisCodecs.Items.Add(codec.Name);

            // set stack flags
            ReregisterRequired = false;
            RestartRequired = false;
        }
        
        public void activateTab(int index)
        {
            this.tabControlSettings.SelectTab(index);
        }

        private void buttonEnable_Click(object sender, EventArgs e)
        {
            if (listBoxDisCodecs.SelectedItems.Count > 0)
            {
                // get selected item from disabled codecs
                listBoxEnCodecs.Items.Add(listBoxDisCodecs.SelectedItem);
                // remove from disabled list
                listBoxDisCodecs.Items.Remove(listBoxDisCodecs.SelectedItem);
            }
        }

        private void buttonDisable_Click(object sender, EventArgs e)
        {
            if (listBoxEnCodecs.SelectedItems.Count > 0)
            {
                // get selected item from enabled codecs
                listBoxDisCodecs.Items.Add(listBoxEnCodecs.SelectedItem);
                // remove from enabled list
                listBoxEnCodecs.Items.Remove(listBoxEnCodecs.SelectedItem);
            }
        }

        private void reregistrationRequired_TextChanged(object sender, EventArgs e)
        {
            ReregisterRequired = true;
        }

        private void restartRequired_TextChanged(object sender, EventArgs e)
        {
            RestartRequired = true;
            ReregisterRequired = true;
        }

        private void checkBoxDefault_CheckedChanged(object sender, EventArgs e)
        {
            ReregisterRequired = true;
        }

        private void numEvaluate_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (((e.KeyChar < '0') || (e.KeyChar > '9')) && (e.KeyChar != '\b'))
            {
                e.Handled = true;
            }
        }

        private void buttonLogger_Click(object sender, EventArgs e)
        {
            (new LoggerForm()).Show();
        }

        private void checkBoxPlayIncomingSound_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBoxPlayIncomingSound.Checked)
                checkBoxPlayIncomingWhileActive.Checked = checkBoxPlayIncomingWhileActive.Enabled = false;
            else
                checkBoxPlayIncomingWhileActive.Enabled = true;
        }

        private void buttonLicenseUpload_Click(object sender, EventArgs e)
        {
            var uploadDialog = new OpenFileDialog();
            if (uploadDialog.ShowDialog() == DialogResult.OK)
            {
                if (!uploadDialog.CheckFileExists) return;

                textBoxLicensePath.Text = Path.GetFullPath(uploadDialog.FileName);
            }
        }

        #region Audio

        private bool _cancelAudioEvents = false;
        private void ReloadAudioDevices()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new EmptyDelegate(ReloadAudioDevices));

                return;
            }
            
            // Load playback devices
            this.comboBoxPlaybackDevices.Items.Clear();

            this.comboBoxPlaybackDevices.DisplayMember = "Name";
            this.comboBoxPlaybackDevices.ValueMember = "Name";

            var devices = this._core.Audio.AudioDevices.ToArray();

            var tamanhoDevice = devices.Length;

            var corretosDevice = tamanhoDevice / 2;
            
            var aq1 = tamanhoDevice.ToString();
            var aq2 = corretosDevice.ToString();

            var cout1 = 1;
            var cout2 = 1;

            foreach (IAudioDevice device in devices)
            {
                if (device.Type == AudioDeviceType.Playback)
                    if (cout1 > corretosDevice)
                    {
                        this.comboBoxPlaybackDevices.Items.Add(device);
                    }
                    var t = device.Name;
                    cout1++;
            }

            if (_core.Audio.PlaybackDevice != null)
                comboBoxPlaybackDevices.SelectedItem = comboBoxPlaybackDevices.Items[0];
            else
                MessageBox.Show("Nenhum dispositivo de audio encontrado!");
            
            // Load recording devices
            this.comboBoxRecordingDevices.Items.Clear();

            this.comboBoxRecordingDevices.DisplayMember = "Name";
            this.comboBoxRecordingDevices.ValueMember = "Name";
            
            foreach (IAudioDevice device in devices)
            {
                if (device.Type == AudioDeviceType.Recording)
                    if (cout2 > corretosDevice)
                    {
                        this.comboBoxRecordingDevices.Items.Add(device);
                    }
                    var t = device.Name;
                    cout2++;
            }

            if (_core.Audio.RecordingDevice != null) 
                comboBoxRecordingDevices.SelectedItem = comboBoxRecordingDevices.Items[0];
            else
                MessageBox.Show("Nenhum microfone encontrado!");

            //if (_core.Audio.RecordingDevice != null) this.comboBoxRecordingDevices.SelectedItem = devices.FirstOrDefault(x => x.Name == this._core.Audio.RecordingDevice.Name && x.Type == AudioDeviceType.Recording);
            //else comboBoxRecordingDevices.SelectedItem = comboBoxRecordingDevices.Items[0];

        }

        private void AudioDeviceChanged(IAudioDevice device)
        {
            if (!_cancelAudioEvents)
                ReloadAudioDevices();
        }

        private void AudioDevicesChanged(IEnumerable<IAudioDevice> devices)
        {
            if (!_cancelAudioEvents)
                ReloadAudioDevices();
        }

        #endregion Audio

        #region Plugins

        private void listBoxPlugins_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.labelPluginName.Text = "-";
            this.labelPluginVersion.Text = "-";
            this.labelPluginID.Text = "-";

            this.buttonPluginStart.Enabled = false;
            this.buttonPluginStop.Enabled = false;
            this.buttonPluginSettings.Enabled = false;
        }

        private void SetStartStopButtonStates(IPluginInformation plugin) {
            if (plugin.IsStarted) {
                this.buttonPluginStart.Enabled = false;
                this.buttonPluginStop.Enabled = true;
            } else {
                this.buttonPluginStart.Enabled = true;
                this.buttonPluginStop.Enabled = false;
            }
        }

        private void buttonPluginStart_Click(object sender, EventArgs e)
        {
            this.buttonPluginStart.Enabled = false;
        }

        private void buttonPluginStop_Click(object sender, EventArgs e)
        {
            this.buttonPluginStop.Enabled = false;
        }

        void plugin_Stopped(object sender, string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => plugin_Stopped(sender, message)));

                return;
            }
        }

        void plugin_Started(object sender)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => plugin_Started(sender)));

                return;
            }
        }

        #endregion

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void comboBoxSIPTransport_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
