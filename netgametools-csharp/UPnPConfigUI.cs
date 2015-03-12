﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using chainedlupine.UPnP;
using System.Diagnostics;

namespace netgametools_csharp
{
    enum ePortCollectionProtocol { TCP, UDP } ;

    public partial class UPnPConfigUI : Form
    {
        private BindingSource pcBindingSource = new BindingSource();

        ControlPoint cp = new ControlPoint();

        NetworkControl networkControl;

        public UPnPConfigUI()
        {
            InitializeComponent();

            networkControl = new NetworkControl();
            networkControl.LoadNetworkInterfaces();


            pcBindingSource.DataSource = typeof(PortCollectionDetail);
            pcBindingSource.AllowNew = true;

            DataGridViewComboBoxColumn col = dataGridViewCollection.Columns[0] as DataGridViewComboBoxColumn;
            col.ValueType = typeof(ePortCollectionProtocol);
            col.DataSource = Enum.GetValues(typeof(ePortCollectionProtocol));

            dataGridViewCollection.AutoGenerateColumns = false;
            dataGridViewCollection.DataSource = pcBindingSource;
        }

        private void On_Load(object sender, EventArgs e)
        {
        }

        private void comboPortCollections_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox control = (ComboBox)sender;

            if (control.SelectedIndex >= 0)
            {
                btnPortCollectionRemove.Enabled = true;
                grpPortDetails.Enabled = true;
                textCollectionTitle.Text = comboPortCollections.Text;

                // Load datagrid
                // Protocol (TCP/UDP), Port
                pcBindingSource.Clear();
                pcBindingSource.Add(new PortCollectionDetail(ePortCollectionProtocol.TCP, 1000));
                pcBindingSource.Add(new PortCollectionDetail(ePortCollectionProtocol.UDP, 5000));

                dataGridViewCollection.Visible = true;
            }
            else
            {
                grpPortDetails.Enabled = false;
                btnPortCollectionRemove.Enabled = false;
                dataGridViewCollection.Visible = false;
            }
                
        }

        private void btnPortCollectionAdd_Click(object sender, EventArgs e)
        {
            comboPortCollections.Items.Add("New Collection");
            comboPortCollections.SelectedIndex = comboPortCollections.Items.Count - 1;
        }

        private void btnPortCollectionRemove_Click(object sender, EventArgs e)
        {
            comboPortCollections.Items.RemoveAt(comboPortCollections.SelectedIndex);

            if (comboPortCollections.Items.Count > 0)
                comboPortCollections.SelectedIndex = 0;
            else
            {
                grpPortDetails.Enabled = false;
                dataGridViewCollection.Visible = false;
                comboPortCollections.Text = "";
            }
        }

        private void textCollectionTitle_TextChanged(object sender, EventArgs e)
        {
            if (textCollectionTitle.Text.Length > 0)

                comboPortCollections.Items[comboPortCollections.SelectedIndex] = textCollectionTitle.Text;
            else
                textCollectionTitle.Text = comboPortCollections.Text;
        }

        private void BuildSelectedDeviceList()
        {
            listViewDevices.Items.Clear();

            foreach (Device device in cp.knownDeviceList)
            {
                if (networkControl.optionShowOnlyNetworkDevices && !DeviceGateway.isGateway(device))
                    continue;

                ListViewItem item = new ListViewItem(device.descFriendlyName);
                item.SubItems.Add(device.descModelName);
                item.SubItems.Add(device.descManufacturer);

                if (DeviceGateway.isGateway(device))
                {
                    item.SubItems.Add(DeviceGateway.GetExternalIP(device));
                    //Debug.WriteLine(string.Format("ports mapped={0}", DeviceGateway.GetPortMappingNumberOfEntries(device)));
                    //List<DeviceGatewayPortRecord> mappings = DeviceGateway.GetPortMappingEntries(device);
                } 
                else
                    item.SubItems.Add("None");

                item.SubItems.Add(device.uuid);

                listViewDevices.Items.Add(item);
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            WriteStatus("Searching!", Color.Blue);
            using (SearchBusyForm busyForm = new SearchBusyForm())
            {
                Form darker;

                darker = new Form();
                darker.ControlBox = darker.MinimizeBox = false;
                darker.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                darker.Text = "";
                darker.BackColor = Color.Black;
                darker.Opacity = 0.7f;
                darker.Show();
                darker.Size = ClientSize;
                darker.Location = PointToScreen(Point.Empty);

                busyForm.StartPosition = FormStartPosition.Manual;
                busyForm.Location = new Point(this.Location.X + (this.Width - busyForm.Width) / 2, this.Location.Y + (this.Height - busyForm.Height) / 2);
                busyForm.Show();
                busyForm.Update();


                if (cp.FindAllDevices())
                {
                    WriteStatus(string.Format("Found {0} devices on network!", cp.knownDeviceList.Count));
                    BuildSelectedDeviceList();

                }
                else
                {
                    WriteStatus("Unable to find any uPnP-enabled devices!", Color.Red);
                }

                busyForm.Close();
                darker.Close();
            }
        }

        private void WriteStatus (string text, Color? c = null)
        {
            textStatus.Text = text;
            textStatus.ForeColor = c ?? Color.Black;
            textStatus.Invalidate();
            textStatus.Update();
            textStatus.Refresh();
            Application.DoEvents();
        }

        private void checkBoxIGDOnly_CheckedChanged(object sender, EventArgs e)
        {
            BuildSelectedDeviceList();
        }

        private void listViewDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            /*
            if (listViewDevices.SelectedItems.Count > 0)
            {
                grpDevice.Enabled = true;

                string uuid = listViewDevices.SelectedItems[0].SubItems[4].Text;

                Device device = cp.FindDeviceByUUID(uuid);

                if (device != null)
                {
                    listViewDeviceMappings.Items.Clear();

                    if (DeviceGateway.isGateway(device))
                    {
                        List<DeviceGatewayPortRecord> mappings = DeviceGateway.GetPortMappingEntries(device);

                        foreach (DeviceGatewayPortRecord portRec in mappings)
                        {
                            ListViewItem item = new ListViewItem(portRec.Desc);
                            item.SubItems.Add(portRec.InternalClient);
                            item.SubItems.Add(portRec.Protocol);
                            item.SubItems.Add(portRec.InternalPort.ToString());
                            item.SubItems.Add(portRec.ExternalPort.ToString());
                            listViewDeviceMappings.Items.Add(item);
                        }
                    }
                }
            }
            else
                grpDevice.Enabled = false;
           */
        }


    }

    class PortCollectionDetail
    {
        public ePortCollectionProtocol Protocol { get; set; }
        public ushort Port { get; set; }

        public PortCollectionDetail()
        {
            this.Protocol = ePortCollectionProtocol.TCP;
            this.Port = 0;
        }

        public PortCollectionDetail(ePortCollectionProtocol protocol, ushort port)
        {
            this.Protocol = protocol;
            this.Port = port;
        }
    }


}
