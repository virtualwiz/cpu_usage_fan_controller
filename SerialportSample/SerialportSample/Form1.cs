﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Timers;
using System.Diagnostics;

namespace SerialportSample
{
    public partial class SerialportSampleForm : Form
    {
        private SerialPort comm = new SerialPort();
        private StringBuilder builder = new StringBuilder();//避免在事件处理方法中反复的创建，定义到外面。
        private long received_count = 0;//接收计数
        private long send_count = 0;//发送计数

        System.Timers.Timer aTimer = new System.Timers.Timer();

        public SerialportSampleForm()
        {
            InitializeComponent();
        }

        //窗体初始化
        public void Form1_Load(object sender, EventArgs e)
        {
            //初始化下拉串口名称列表框
            string[] ports = SerialPort.GetPortNames();
            Array.Sort(ports);
            comboPortName.Items.AddRange(ports);
            comboPortName.SelectedIndex = comboPortName.Items.Count > 0 ? 0 : -1;
            comboBaudrate.SelectedIndex = comboBaudrate.Items.IndexOf("9600");


            aTimer.Elapsed += new ElapsedEventHandler(theout);  //到达时间的时候执行事件；
            aTimer.Interval = 333;
            aTimer.AutoReset = true;//设置是执行一次（false）还是一直执行(true)；


            //初始化SerialPort对象
            comm.NewLine = "\r\n";
            comm.RtsEnable = true;//根据实际情况吧。

            //添加事件注册
            comm.DataReceived += comm_DataReceived;
        }

        void comm_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int n = comm.BytesToRead;//先记录下来，避免某种原因，人为的原因，操作几次之间时间长，缓存不一致
            byte[] buf = new byte[n];//声明一个临时数组存储当前来的串口数据
            received_count += n;//增加接收计数
            comm.Read(buf, 0, n);//读取缓冲数据
            builder.Clear();//清除字符串构造器的内容
            //因为要访问ui资源，所以需要使用invoke方式同步ui。
            this.Invoke((EventHandler)(delegate
            {
                //判断是否是显示为16禁止
                if (checkBoxHexView.Checked)
                {
                    //依次的拼接出16进制字符串
                    foreach (byte b in buf)
                    {
                        builder.Append(b.ToString("X2") + " ");
                    }
                }
                else
                {
                    //直接按ASCII规则转换成字符串
                    builder.Append(Encoding.ASCII.GetString(buf));
                }
                //追加的形式添加到文本框末端，并滚动到最后。
                this.txGet.AppendText(builder.ToString());
                //修改接收计数
                labelGetCount.Text = "Get:" + received_count.ToString();
            }));
        }

        private void buttonOpenClose_Click(object sender, EventArgs e)
        {
            //根据当前串口对象，来判断操作
            if (comm.IsOpen)
            {
                aTimer.Enabled = false;
                //打开时点击，则关闭串口
                comm.Close();
            }
            else
            {
                //关闭时点击，则设置好端口，波特率后打开
                comm.PortName = comboPortName.Text;
                comm.BaudRate = int.Parse(comboBaudrate.Text);
                try
                {
                    comm.Open();
                }
                catch(Exception ex)
                {
                    //捕获到异常信息，创建一个新的comm对象，之前的不能用了。
                    comm = new SerialPort();
                    //现实异常信息给客户。
                    MessageBox.Show(ex.Message);
                }
                aTimer.Enabled = true;
            }
            //设置按钮的状态
            buttonOpenClose.Text = comm.IsOpen ? "Close" : "Open";
            buttonSend.Enabled = comm.IsOpen;
        }

        //动态的修改获取文本框是否支持自动换行。
        private void checkBoxNewlineGet_CheckedChanged(object sender, EventArgs e)
        {
            txGet.WordWrap = checkBoxNewlineGet.Checked;
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            //定义一个变量，记录发送了几个字节
            int n = 0;
            //16进制发送
            if (checkBoxHexSend.Checked)
            {
                //我们不管规则了。如果写错了一些，我们允许的，只用正则得到有效的十六进制数
                MatchCollection mc = Regex.Matches(txSend.Text, @"(?i)[\da-f]{2}");
                List<byte> buf = new List<byte>();//填充到这个临时列表中
                //依次添加到列表中
                foreach (Match m in mc)
                {
                    buf.Add(byte.Parse(m.Value));
                }
                //转换列表为数组后发送
                comm.Write(buf.ToArray(), 0, buf.Count);
                //记录发送的字节数
                n = buf.Count;
            }
            else//ascii编码直接发送
            {
                //包含换行符
                if (checkBoxNewlineSend.Checked)
                {
                    comm.WriteLine(txSend.Text);
                    n = txSend.Text.Length + 2;
                }
                else//不包含换行符
                {
                    comm.Write(txSend.Text);
                    n = txSend.Text.Length;
                }
            }
            send_count += n;//累加发送字节数
            labelSendCount.Text = "Send:" + send_count.ToString();//更新界面
        }

        private void buttonReset_Click(object sender, EventArgs e)
        {
            //复位接受和发送的字节数计数器并更新界面。
            send_count = received_count = 0;
            labelGetCount.Text = "Get:0";
            labelSendCount.Text = "Send:0";
        }

        PerformanceCounter _oPerformanceCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        int cpuUsage;
        public void SendCpuUsage()
        {
            cpuUsage = (int)_oPerformanceCounter.NextValue();
            char[] buff = new char[1] {(char)0x00};
            buff[0] = (char)cpuUsage;
            comm.Write(buff, 0, 1);
        }

        private void theout(object source, System.Timers.ElapsedEventArgs e)
        {
            SendCpuUsage();
        }


        private void progressBarCPU_Click(object sender, EventArgs e)
        {
            progressBarCPU.Value = cpuUsage;
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
    }
}
