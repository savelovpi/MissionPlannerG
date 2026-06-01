using MissionPlanner.Plugin;
using System;
using System.Drawing;
using System.Windows.Forms;

public class GripperAuxFunctionPlugin : Plugin
{
    private const int MavCmdDoGripper = 211;
    private const int GripperActionRelease = 0;
    private const int GripperActionGrab = 1;
    private const string PluginPanelName = "GripperAuxFunctionPluginPanel";

    private Control gripperPanel;
    private NumericUpDown gripperNumber;
    private Label statusLabel;

    public override string Name
    {
        get { return "Gripper Aux Function"; }
    }

    public override string Version
    {
        get { return "1.0.0"; }
    }

    public override string Author
    {
        get { return "MissionPlannerG"; }
    }

    public override bool Init()
    {
        return true;
    }

    public override bool Loaded()
    {
        RunOnFlightDataUiThread(new MethodInvoker(InstallUi));
        return true;
    }

    public override bool Exit()
    {
        try
        {
            if (gripperPanel != null)
            {
                if (gripperPanel.InvokeRequired)
                {
                    gripperPanel.BeginInvoke(new MethodInvoker(RemoveUi));
                }
                else
                {
                    RemoveUi();
                }
            }
        }
        catch
        {
        }

        return true;
    }

    private void RunOnFlightDataUiThread(MethodInvoker action)
    {
        try
        {
            Control flightData = Host.MainForm.FlightData as Control;
            if (flightData == null)
            {
                return;
            }

            if (flightData.InvokeRequired)
            {
                flightData.BeginInvoke(action);
            }
            else
            {
                action();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to load Gripper Aux Function plugin:\r\n" + ex.Message, Name,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void InstallUi()
    {
        Control flightData = Host.MainForm.FlightData as Control;
        if (flightData == null)
        {
            return;
        }

        FlowLayoutPanel auxFlow = FindControlByName<FlowLayoutPanel>(flightData, "flowLayoutPanel1");
        TabPage auxTab = FindControlByName<TabPage>(flightData, "tabAuxFunction");
        Control parent = auxFlow as Control;

        if (parent == null)
        {
            parent = auxTab as Control;
        }

        if (parent == null)
        {
            return;
        }

        if (FindControlByName<Control>(parent, PluginPanelName) != null)
        {
            return;
        }

        gripperPanel = BuildGripperPanel();

        if (!(parent is FlowLayoutPanel))
        {
            gripperPanel.Location = new Point(8, 8);
            gripperPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        }

        parent.Controls.Add(gripperPanel);
        gripperPanel.BringToFront();
    }

    private Control BuildGripperPanel()
    {
        GroupBox groupBox = new GroupBox();
        groupBox.Name = PluginPanelName;
        groupBox.Text = "Gripper";
        groupBox.Width = 360;
        groupBox.Height = 92;
        groupBox.Margin = new Padding(6);
        groupBox.Padding = new Padding(8);

        Label numberLabel = new Label();
        numberLabel.Text = "Gripper #";
        numberLabel.AutoSize = true;
        numberLabel.Location = new Point(10, 23);

        gripperNumber = new NumericUpDown();
        gripperNumber.Minimum = 0;
        gripperNumber.Maximum = 16;
        gripperNumber.Value = 0;
        gripperNumber.Width = 55;
        gripperNumber.Location = new Point(80, 20);

        Button openButton = new Button();
        openButton.Text = "Open / Release";
        openButton.Width = 105;
        openButton.Height = 27;
        openButton.Location = new Point(145, 18);
        openButton.Click += delegate { SendGripperCommand(GripperActionRelease, "release"); };

        Button closeButton = new Button();
        closeButton.Text = "Close / Grab";
        closeButton.Width = 95;
        closeButton.Height = 27;
        closeButton.Location = new Point(255, 18);
        closeButton.Click += delegate { SendGripperCommand(GripperActionGrab, "grab"); };

        statusLabel = new Label();
        statusLabel.Text = "Sends MAV_CMD_DO_GRIPPER";
        statusLabel.AutoEllipsis = true;
        statusLabel.Width = 340;
        statusLabel.Height = 32;
        statusLabel.Location = new Point(10, 52);

        groupBox.Controls.Add(numberLabel);
        groupBox.Controls.Add(gripperNumber);
        groupBox.Controls.Add(openButton);
        groupBox.Controls.Add(closeButton);
        groupBox.Controls.Add(statusLabel);

        return groupBox;
    }

    private void SendGripperCommand(int action, string actionName)
    {
        try
        {
            if (statusLabel != null)
            {
                statusLabel.Text = "Sending gripper " + actionName + "...";
                statusLabel.Refresh();
            }

            bool commandAccepted = Host.comPort.doCommand(
                (byte)Host.comPort.sysidcurrent,
                (byte)Host.comPort.compidcurrent,
                (MAVLink.MAV_CMD)MavCmdDoGripper,
                (float)gripperNumber.Value,
                (float)action,
                0,
                0,
                0,
                0,
                0);

            if (commandAccepted)
            {
                if (statusLabel != null)
                {
                    statusLabel.Text = "Gripper command accepted: " + actionName;
                }
            }
            else
            {
                if (statusLabel != null)
                {
                    statusLabel.Text = "Gripper command failed: " + actionName;
                }

                MessageBox.Show("Gripper command was rejected. Check that the vehicle is connected and the gripper is enabled in ArduPilot.",
                    Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            if (statusLabel != null)
            {
                statusLabel.Text = "Gripper command error";
            }

            MessageBox.Show("Failed to send gripper command:\r\n" + ex.Message, Name,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RemoveUi()
    {
        if (gripperPanel == null)
        {
            return;
        }

        Control parent = gripperPanel.Parent;
        if (parent != null)
        {
            parent.Controls.Remove(gripperPanel);
        }

        gripperPanel.Dispose();
        gripperPanel = null;
        gripperNumber = null;
        statusLabel = null;
    }

    private static T FindControlByName<T>(Control parent, string name) where T : Control
    {
        if (parent == null)
        {
            return null;
        }

        if (parent.Name == name && parent is T)
        {
            return (T)parent;
        }

        foreach (Control child in parent.Controls)
        {
            T result = FindControlByName<T>(child, name);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
