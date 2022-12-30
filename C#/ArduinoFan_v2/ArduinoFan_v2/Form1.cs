using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using OpenHardwareMonitor.Hardware;
using System.IO.Ports;
using System.IO;

namespace ArduinoFan_v2
{
    public partial class Form1 : Form
    {

        Thread temp_getting_thread;
        Computer comp;


        // SETTINGS

        int maxOut;     // Procent = 100 or PWM = 255
        int start_temp; // Start temperature 
        int end_temp;   // End temperature

        int BufferTime;   // Buffer size in Seconds
        int CheckTime;    // Period for get values
        int BufferLength; // Total Buffer size in bytes
        //

        int baseSpeed; // default output value in percent, select in track bar on first page

        int CPUTEMP; // Variable for temperature CPU
        int GPUTEMP; // Variable for temperature GPU
        bool on; // Variable of program status
        int input_temp;
        int[] buffer;
        int[] temp;
        byte[] inputbuf;
        
        int[] output;
        public Form1()
        {
            InitializeComponent();

            maxOut = 100;
            start_temp = 50;
            end_temp = 70;

            BufferTime = 30;
            CheckTime = 500;
            BufferLength = (1000 / CheckTime) * BufferTime;

            output = new int[2];

            temp_getting_thread = new Thread(temp_getting);
            comp = new Computer();

            comp.CPUEnabled = true;
            comp.GPUEnabled = true;
            comp.Open();
            temp_getting_thread.Start();

            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(SerialPort.GetPortNames());
            if (comboBox1.Items.Count != 0)
            {
                comboBox1.Text = comboBox1.Items[0].ToString();
            }

            foreach (var elem in comboBox1.Items)
            {
                if (elem.ToString() != "COM1")
                {
                    comboBox1.Text = elem.ToString();
                }
            }
            buffer = new int[BufferLength];
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            ConnectButtonClick(sender, e);
        }

        private void Form1_Closing(object sender, CancelEventArgs e)
        {
            button2_Click(sender, e);
            e.Cancel = false;
            temp_getting_thread.Abort();
        }

        private void ConnectButtonClick(object sender, EventArgs e)
        {
            try
            {
                on = true;
                serialPort1.Open();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            serialPort1.DiscardNull = true;
            serialPort1.Write(textBox1.Text);
            Thread.Sleep(500);
            inputbuf = new byte[serialPort1.BytesToRead];
            serialPort1.Read(inputbuf, 0, serialPort1.BytesToRead);
            Console.WriteLine(Encoding.UTF8.GetString(inputbuf) + " (" + inputbuf.Length + ")");
        }

        private void send(string str)
        {
            if (serialPort1.IsOpen) serialPort1.Write(str);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            serialPort1.Write("R");
            Thread.Sleep(500);
            inputbuf = new byte[serialPort1.BytesToRead];
            serialPort1.Read(inputbuf, 0, serialPort1.BytesToRead);
            MessageBox.Show("OK\nArduino returned: " + Encoding.UTF8.GetString(inputbuf), "Response", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            /*
            if (trackBar1.InvokeRequired)
            {
                Action x = delegate
                {
                    trackBar1.value = 10;
                }
                trackBar1.Invoke(x);
            }
            */


            baseSpeed = 0;// LOAD FROM INI
            switch (trackBar1.Value)
            {
                case 0:
                default:
                    baseSpeed = 0;
                    break;
                case 1:
                    baseSpeed = 1;
                    break;
                case 2:
                    baseSpeed = 100;
                    break;
            };
        }

        private int[] get_temp()
        {

            foreach (var device in comp.Hardware)
            {
                device.Update();
                if (device.HardwareType == HardwareType.CPU)
                {
                    foreach (ISensor sensor in device.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature && sensor.Name == "CPU Package")
                        {
                            CPUTEMP = (int)sensor.Value;
                        }
                    }
                }
                else if (device.HardwareType == HardwareType.GpuAti)
                {
                    foreach (ISensor sensor in device.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature)
                        {
                            GPUTEMP = (int)sensor.Value;
                        }
                    }
                }
            }

            output[0] = CPUTEMP;
            output[1] = GPUTEMP;

            return output;
        }
        private void temp_getting()
        {
            while (true)
            {
                Thread.Sleep(CheckTime);
                if (!on)
                {
                    continue;
                }
                temp = get_temp();

                /*
                0-50 %
                start temp = 40
                end temp = 60
                func: linear
                */

                if (radioButton1.Checked)
                {
                    input_temp = temp[1];
                }
                else if (radioButton2.Checked)
                {
                    input_temp = temp[0];
                }

                for (int i = buffer.Length - 1; i > 0; --i)
                {
                    buffer[i] = buffer[i - 1];
                }

                buffer[0] = GetPercent(input_temp);
                
                int FanSpeed = (int)Math.Floor(buffer.Average());
                if (FanSpeed == 0)
                {
                    FanSpeed = baseSpeed;
                }

                if (checkBox1.Checked)
                {
                    FanSpeed = (int)numericUpDown1.Value;
                }
                send("SP" + FanSpeed);
            }
        }

        private int GetPercent(int temperature)
        {
            int percent = 0;

            if (temperature > end_temp)
            {
                percent = 100;
            }
            else if (temperature < start_temp)
            {
                percent = 0;
            }
            else if (temperature >= start_temp && temperature <= end_temp)
            {
                // Sigmoid
                //percent = (int)Math.Floor(maxOut / (1 + Math.Pow(1.4;, -input_temp + (end_temp - start_temp) / 2 + start_temp)));

                // Linear
                percent = (int)Math.Floor((double)(temperature - start_temp) / (end_temp - start_temp) * maxOut);
            }
            return percent;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            on = false;
            send("SP" + baseSpeed);
            serialPort1.Close();
        }
    }
}
