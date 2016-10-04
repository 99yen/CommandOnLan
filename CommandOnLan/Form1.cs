using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using Ini;

namespace CommandOnLan
{
    public partial class Form1 : Form
    {
        const string INI_PATH = @".\col.ini";
        readonly char[] delimiterChars = {':', '-'};
        
        private UdpClient[] objSck;
        private IniFile ini;
        private int cmdNum;
        private int[] port;
        private string[] cmd;
        private byte[] macAddr = new byte[6];

        public Form1()
        {
            InitializeComponent();
            InitializeSetting();
        }

        // 初期化
        private void InitializeSetting()
        {
            ReadSetting();
            InitializeUdpClient();
        }

        // 項目設定を読み込む
        private void ReadSetting()
        {
            ini = new IniFile(INI_PATH);
            cmdNum = int.Parse(ini.IniReadValue("main", "num"));

            port = new int[cmdNum];
            cmd = new string[cmdNum];
            for (int i = 0; i < cmdNum; i++)
            {
                port[i] = int.Parse(ini.IniReadValue("command" + (i+1).ToString(), "port"));
                cmd[i]  = ini.IniReadValue("command" + (i+1).ToString(), "command");
            }
            
            // 受信MACアドレス設定
            string[] macTmp = (ini.IniReadValue("main", "mac")).Split(delimiterChars);
            // 整数に直す
            for(int i=0; i<6; i++)
            {
                macAddr[i] = Convert.ToByte(macTmp[i], 16);
            }
        }

        // UdpClientを設定
        private void InitializeUdpClient()
        {
            objSck = new UdpClient[cmdNum];
            for(int i=0; i<cmdNum; i++)
            {
                objSck[i] = new UdpClient(port[i]);
                objSck[i].BeginReceive(ReceiveCallback, objSck[i]);
            }
        }

        // コマンド実行
        private void execCmd(int resPort)
        {
            Process.Start(cmd[Array.IndexOf(port, resPort)]);
        }

        // マジックパケットをチェック
        bool checkMagicPacket(byte[] packet)
        {
            // 102バイト未満のマジックパケットは存在しない
            if (packet.Length < 102)
            {
                return false;
            }
            // FF:FF:FF:FF:FF:FFから始まっているか？
            for (int i = 0; i < 6; i++)
            {
                if (packet[i] != 0xFF)
                {
                    return false;
                }
            }
            // MACアドレスが16回連続で入っているか？
            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    if (packet[6 * i + j + 6] != macAddr[j])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // 受信コールバック
        private void ReceiveCallback(IAsyncResult AR)
        {
            // ソケット受信
            IPEndPoint ipAny = new IPEndPoint(IPAddress.Any, 0);
            byte[] dat = ((UdpClient)(AR.AsyncState)).EndReceive(AR, ref ipAny);
            int resPort = ((IPEndPoint)(((UdpClient)(AR.AsyncState)).Client.LocalEndPoint)).Port;
            // コマンド起動
            if (checkMagicPacket(dat))
            {
                execCmd(resPort);
            }
            // コールバック再設定
            ((UdpClient)(AR.AsyncState)).BeginReceive(ReceiveCallback, AR.AsyncState);
        }

        private void 終了ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void バージョン情報ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("CommandOnLan ver.0.1 (C) 2012 99yen.", "バージョン情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
